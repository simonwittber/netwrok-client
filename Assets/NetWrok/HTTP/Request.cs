using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography;

using System.IO;
using System.Net;

namespace NetWrok.HTTP
{
    public class Request
    {
        

        public static Uri proxy = null;

#region public fields
        public bool isDone = false;
        public Exception exception = null;

        public Response response { get; set; }

        public int maximumRedirects = 8;
        public bool acceptGzip = true;
        public bool useCache = false;
        public readonly Headers headers = new Headers ();
        public bool enableCookies = true;
        public float timeout = 0;


        public static Dictionary<string, Connection> connectionPool = new Dictionary<string, Connection>();
        public readonly static CookieContainer cookies = new CookieContainer ();
#endregion
    
#region public properties
        public Uri uri { get; set; }

        public Connection upgradedConnection { get; private set; }

        public float Progress {
            get { return response == null ? 0 : response.progress; }
        }

		public float UploadProgress {
			get {
				return uploadProgress;
			}
		}

        public string Text {
            set { bytes = value == null ? null : Protocol.enc.GetBytes (value); }
        }

        public byte[] Bytes {
            set { bytes = value; }
        }
#endregion

#region public interface
        public Coroutine Send (System.Action<Request> OnDone)
        {
            this.OnDone = OnDone;
            return Send ();
        }

        public Coroutine Send ()
        {
            BeginSending ();
            return Scheduler.Instance.StartCoroutine (_Wait ());   
        }

#endregion
#region constructors
        public Request() {
            this.method = "GET";
        }

        public Request (string method, string uri)
        {
            this.method = method;
            this.uri = new Uri (uri);
        }

        public Request (string method, string uri, bool useCache)
        {
            this.method = method;
            this.uri = new Uri (uri);
            this.useCache = useCache;
        }

        public Request (string uri, WWWForm form)
        {
            this.method = "POST";
            this.uri = new Uri (uri);
            this.bytes = form.data;
            foreach (string k in form.headers.Keys) {
                headers.Set (k, (string)form.headers [k]);
            }
        }

        public Request (string method, string uri, byte[] bytes)
        {
            this.method = method;
            this.uri = new Uri (uri);
            this.bytes = bytes;
        }


        public static Request BuildFromStream(string host, NetworkStream stream) {
            var request = CreateFromTopLine (host, Protocol.ReadLine (stream));
            if (request == null) {
                return null;
            }
            Protocol.CollectHeaders (stream, request.headers);
            float progress = 0;
            using (var output = new System.IO.MemoryStream()) {
                if (request.headers.Get ("transfer-encoding").ToLower () == "chunked") {
                    Protocol.ReadChunks (stream, output, ref progress);
                    Protocol.CollectHeaders (stream, request.headers);
                } else {
                    Protocol.ReadBody (stream, output, request.headers, true, ref progress);
                }
                request.Bytes = output.ToArray ();
            }
            return request;
        }

#endregion
#region implementation

        static Request CreateFromTopLine (string host, string top)
        {
            var parts = top.Split (' ');
            if (parts.Length != 3)
                return null;
            if (parts [2] != "HTTP/1.1")
                return null;
            var request = new HTTP.Request ();
            request.method = parts [0].ToUpper ();
            request.uri = new Uri (host + parts [1]);
            request.response = new Response(request);
            return request;
        }
        

        static bool ValidateServerCertificate (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            //This is where you implement logic to determine if you trust the certificate.
            //By default, we trust all certificates.
            return true;
        }

        
        Connection CreateConnection (string host, int port, bool useSsl)
        {
            Connection connection;

            var key = string.Format("{0}:{1}", host, port);
            if(connectionPool.ContainsKey(key)) {
                connection = connectionPool[key];
                connectionPool.Remove(key);
                return connection;
            }

            connection = new Connection () { host = host, port = port };
			//Debug.Log(host);
			connection.Connect ();

            if (useSsl) {

                connection.stream = new SslStream (connection.client.GetStream (), false, ValidateServerCertificate);
                var ssl = connection.stream as SslStream;
				ssl.AuthenticateAsClient (uri.Host);

            } else {

                connection.stream = connection.client.GetStream ();
            }
            return connection;
        }
                
        IEnumerator Timeout ()
        {
            yield return new WaitForSeconds (timeout);
            if (!isDone) {
                exception = new TimeoutException ("Web request timed out");
                isDone = true;
            }
        }

        void AddHeadersToRequest ()
        {
            if (useCache) {
                string etag = "";
                if (etags.TryGetValue (uri.AbsoluteUri, out etag)) {
                    headers.Set ("If-None-Match", etag);
                }
            }
            var hostHeader = uri.Host;
            if (uri.Port != 80 && uri.Port != 443) {
                hostHeader += ":" + uri.Port.ToString ();
            }
            headers.Set ("Host", hostHeader);
			if(!headers.Contains("User-Agent")) {
				headers.Add("User-Agent", "UniWeb (http://www.differentmethods.com)");
			}
     
            if (acceptGzip) {
                headers.Set ("Accept-Encoding", "gzip");
            }


            if (enableCookies && uri != null) {
                try {
                    var c = cookies.GetCookieHeader (uri);
                    if (c != null && c.Length > 0) {
                        headers.Set ("Cookie", c);
                    }
                } catch (NullReferenceException) {
                    //Some cookies make the .NET cookie class barf. MEGH again.
                } catch (IndexOutOfRangeException) {
                    //Another weird exception that comes through from the cookie class. 
                }
            }

        }

        void BeginSending ()
        {

            isDone = false;

            if (timeout > 0) {
                Scheduler.Instance.StartCoroutine (Timeout ());
            }

            System.Threading.ThreadPool.QueueUserWorkItem (delegate(object state) {

                try {
                    var retryCount = 0;
                    Connection connection = null;

                    while (retryCount < maximumRedirects) {
                        AddHeadersToRequest ();
                        Uri pUri;
                        if(proxy != null)
                            pUri = proxy;
                        else {
							try {
	                            if(System.Net.WebRequest.DefaultWebProxy != null)
	                                pUri = System.Net.WebRequest.DefaultWebProxy.GetProxy(uri);
	                            else
	                                pUri = uri;
							} catch(TypeInitializationException) {
								//Could not get WebRequest type... default to no proxy.
								pUri = uri;
							}

                        }

                        connection = CreateConnection (pUri.Host, pUri.Port, pUri.Scheme.ToLower () == "https");

                        WriteToStream (connection.stream);

                        response = new Response (this);

                        try {

                            response.ReadFromStream (connection.stream, bodyStream);

                        } catch (HTTPException) {

                            retryCount ++;
                            continue;
                        }


                        connectionPool[pUri.Host] = connection;
                        if (enableCookies) {
                            foreach (var i in response.headers.GetAll("Set-Cookie")) {
                                try {
                                    cookies.SetCookies (uri, i);
                                } catch (System.Net.CookieException) {
                                    //Some cookies make the .NET cookie class barf. MEGH.
                                }
                            }
                        }
                        switch (response.status) {
                        case 101:
                            upgradedConnection = connection;
                            break;
                        case 304:
                            break;
                        case 307:
                            uri = new Uri (response.headers.Get ("Location"));
                            retryCount ++;
                            continue;
                        case 302:
                        case 301:
                            method = "GET";
							var location = response.headers.Get ("Location");
							if(location.StartsWith("/")) {
								uri = new Uri(uri, location);
							} else {
								uri = new Uri (location);
							}
                            
                            retryCount ++;
                            continue;
                        default:
                            break;
                        }
                        break;
                    }

                    if (upgradedConnection == null) {
                        if(response.protocol.ToUpper() == "HTTP/1.0" || response.headers.Get("Connection").ToUpper() == "CLOSE") {
                            if(connectionPool.ContainsKey(uri.Host)) {
                                connectionPool.Remove(uri.Host);
                            }
                            connection.Dispose ();
                        } 
                    }

                    if (useCache && response != null) {
                        var etag = response.headers.Get ("etag");
                        if (etag.Length > 0) {
                            etags [uri.AbsoluteUri] = etag;
                        }
                    }
                } catch (Exception e) {

                    exception = e;
                    response = null;
                }

                isDone = true;
            });

        }

        void WriteToStream (Stream outputStream)
        {
			uploadProgress = 0f;
            var stream = new BinaryWriter (outputStream);
            bool hasBody = false;
            var pathComponent = proxy==null?uri.PathAndQuery:uri.AbsoluteUri;
            stream.Write (Protocol.enc.GetBytes (method.ToUpper () + " " + pathComponent + " " + protocol));
            stream.Write (Protocol.EOL);
            if (uri.UserInfo != null && uri.UserInfo != "") {
                if (!headers.Contains ("Authorization")) {
                    headers.Set ("Authorization", "Basic " + System.Convert.ToBase64String (Protocol.enc.GetBytes (uri.UserInfo)));  
                }
            }
            if (!headers.Contains ("Accept")) {
                headers.Add("Accept", "*/*");
            }
            if (bytes != null && bytes.Length > 0) {
                headers.Set ("Content-Length", bytes.Length.ToString ());
                // Override any previous value
                hasBody = true;
            } else {
                headers.Pop ("Content-Length");
            }
            
            headers.Write (stream);
            
            stream.Write (Protocol.EOL);
            
            if (hasBody) {
				var totalBytes = bytes.Length;
				var reader = new MemoryStream(bytes);
				var index = 0;
				var a = new byte[1024];
				while(index < totalBytes) {
					var readCount = reader.Read (a, 0, 1024);
					stream.Write(a, 0, readCount);
					uploadProgress += (totalBytes / (float)readCount);
					index += readCount;
				}
                //stream.Write (bytes);
            }

			uploadProgress = 1f;
            
        }

        IEnumerator _Wait ()
        {
            while (!isDone) {
                yield return null; 
            }
            if (OnDone != null) {
                OnDone (this);
            }
        }
        

        static void AOTStrippingReferences ()
        {
            new System.Security.Cryptography.RijndaelManaged ();
        }

        byte[] bytes;
        public string method;
        string protocol = "HTTP/1.1";
        static Dictionary<string, string> etags = new Dictionary<string, string> ();
        System.Action<HTTP.Request> OnDone;
		float uploadProgress = 0;
        Stream bodyStream = null;
#endregion

    }


}
