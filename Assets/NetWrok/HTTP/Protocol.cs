using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;

namespace NetWrok.HTTP
{
    public class Protocol
    {

        public static byte[] EOL = { (byte)'\r', (byte)'\n' };
        public static UTF8Encoding enc = new UTF8Encoding ();

        public static string ReadLine (Stream stream)
        {
            var line = new List<byte> ();
            while (true) {
                int c = -1;
                try {
                    c = stream.ReadByte ();
                } catch (IOException) {
                    throw new HTTPException ("Terminated Stream");
                }
                if (c == -1) {
                    throw new HTTPException ("Unterminated Stream");
                }
                var b = (byte)c;
                if (b == EOL [1]) {
                    break;
                }
                line.Add (b);
            }
            var array = line.ToArray ();
            var s = enc.GetString (array, 0, array.Length).Trim ();
            return s;
        }
        
        public static string[] ReadKeyValue (Stream stream)
        {
            string line = ReadLine (stream);
            if (line == "") {
                return null;
            } else {
                var split = line.IndexOf (':');
                if (split == -1) {
                    return null;
                }
                var parts = new string[2];
                parts [0] = line.Substring (0, split).Trim ();
                parts [1] = line.Substring (split + 1).Trim ();
                return parts;
            }
        }

        public static void ReadChunks (Stream inputStream, Stream output, ref float progress)
        {
            byte[] buffer = new byte[8192];
            while (true) {
                // Collect Body
                var hexLength = ReadLine (inputStream);
                
                var length = int.Parse (hexLength, NumberStyles.AllowHexSpecifier);
				if(length == 0) break;
                progress = 0;
                var contentLength = length;
                while (length > 0) {
                    var count = inputStream.Read (buffer, 0, Mathf.Min (buffer.Length, length));
                    output.Write (buffer, 0, count);
                    progress = Mathf.Clamp01 (1 - ((float)length / (float)contentLength));
                    length -= count;
                }
                progress = 1;
                //forget the CRLF.
                inputStream.ReadByte ();
                inputStream.ReadByte ();
                
            }
        }
        
        public static void ReadBody (Stream inputStream, Stream output, Headers headers, bool strict, ref float progress)
        {
            // Read Body
            byte[] buffer = new byte[8192];
            int contentLength = 0;
            
            if (int.TryParse (headers.Get ("Content-Length"), out contentLength)) {
                if (contentLength > 0) {
                    var remaining = contentLength;
                    while (remaining > 0) {
                        var count = inputStream.Read (buffer, 0, buffer.Length);
                        if (count == 0) {
                            break;
                        }
                        remaining -= count;
                        output.Write (buffer, 0, count);
                        progress = Mathf.Clamp01 (1.0f - ((float)remaining / (float)contentLength));
                    }
                }
            } else {
                if (!strict) {
                    var count = inputStream.Read (buffer, 0, buffer.Length);
                    while (count > 0) {
                        output.Write (buffer, 0, count);
                        count = inputStream.Read (buffer, 0, buffer.Length);
                    }
                }
                progress = 1;
            }
        }
        
        public static void CollectHeaders (Stream inputStream, Headers headers)
        {
            while (true) {
                // Collect Headers
                string[] parts = ReadKeyValue (inputStream);
                if (parts == null) {
                    break;
                }
                headers.Add (parts [0], parts [1]);
            }
        }

        public static void WriteResponse(Stream stream, int status, string message, Headers headers, byte[] bytes) {
            if(bytes == null) bytes = new byte[0];
            var writer = new BinaryWriter(stream);
            writer.Write(enc.GetBytes(string.Format("HTTP/1.1 {0} {1}\r\n", status, message)));
            headers.Set("Content-Length", bytes.Length.ToString());
            headers.Write(writer);
            writer.Write(enc.GetBytes("\r\n"));
            writer.Write(bytes);
            writer.Flush();
        }

    }
}
