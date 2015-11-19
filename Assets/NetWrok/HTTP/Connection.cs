using System;
using System.Net.Sockets;
using System.IO;


namespace NetWrok.HTTP
{
    public class Connection : IDisposable
    {
        public string host;
        public int port;

        public TcpClient client = null;

		public Stream stream = null;
        
        public Connection ()
        {
            
        }
        
        public void Connect ()
        {
            client = new TcpClient ();
			client.Connect (host, port);
        }

        public void Dispose ()
        {
            stream.Dispose ();
        }
        
    }
}

