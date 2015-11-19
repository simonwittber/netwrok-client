using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using NetWrok.HTTP.Zlib;

using UnityEngine;

namespace NetWrok.HTTP
{
    public class Response
    {
        #region public properties
        public int status { get; set; }
        public string protocol;
        public string message { get; set; }

        public float progress;

        public readonly Headers headers = new Headers ();


        public AssetBundleCreateRequest AssetBundleCreateRequest() {
            return AssetBundle.CreateFromMemory (Bytes); 
        }

        public string Text {
            get {
                return Protocol.enc.GetString (Bytes, 0, Bytes.Length);
            }
            set {
                Bytes = Protocol.enc.GetBytes(value);
            }
        }

        public byte[] Bytes {
            get {
                return bytes;
            }
            set {
                bytes = value;
            }
        }
        #endregion
        #region constructors
        public Response (Request request)
        {
            this.request = request;
        }
        #endregion
        #region implementation




        public void ReadFromStream (Stream inputStream, Stream bodyStream)
        {
            progress = 0;

            if (inputStream == null) {
                throw new HTTPException ("Cannot read from server, server probably dropped the connection.");
            }
            var top = Protocol.ReadLine (inputStream).Split (' ');

            status = -1;
            int _status = -1;
            if (!(top.Length > 0 && int.TryParse (top [1], out _status))) {
                throw new HTTPException ("Bad Status Code, server probably dropped the connection.");
            }
            status = _status;
            message = string.Join (" ", top, 2, top.Length - 2);
            protocol = top[0];
            Protocol.CollectHeaders (inputStream, headers);

            if (status == 101) {
                progress = 1;
                return;
            }

            if (status == 204) {
                progress = 1;
                return;
            }

            if (status == 304) {
                progress = 1;
                return;
            }

            var chunked = headers.Get ("Transfer-Encoding").ToLower() == "chunked";

            /*
            //This code would break requests coming from HTTP/1.0
            
            if(!chunked && !headers.Contains("Content-Length")) {
                progress = 1;
                return;
            }
            */

            if(this.request.method.ToLower() == "head") {
                progress = 1;
                return;
            }



            Stream output = (Stream)bodyStream;
            if(output == null) {
                output = new MemoryStream();
            }

            if (chunked) {
                Protocol.ReadChunks (inputStream, output, ref progress);
                Protocol.CollectHeaders (inputStream, headers); //Trailers
            } else {
                Protocol.ReadBody (inputStream, output, headers, false, ref progress);
            }

            if(bodyStream == null) {
                output.Seek (0, SeekOrigin.Begin);
                Stream outputStream = output;
                
                var zipped = headers.Get ("Content-Encoding").ToLower() == "gzip";
                if(zipped) {
                    outputStream = new GZipStream (output, CompressionMode.Decompress);
                } else {
                    outputStream = output;
                }
                
                bytes = new byte[0];
                var buffer = new byte[1024];
                var count = -1;
                while (count != 0) {
                    count = outputStream.Read (buffer, 0, buffer.Length);
                    var offset = bytes.Length;
                    Array.Resize<byte> (ref bytes, offset + count);
                    Array.Copy (buffer, 0, bytes, offset, count);
                }
                outputStream.Dispose();
            }


        }


        Request request;
        byte[] bytes;
        #endregion
    }
    
}

