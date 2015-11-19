
using System;
namespace NetWrok.HTTP
{
    public class HTTPException : Exception
    {
        public HTTPException (string message) : base(message)
        {
        }
    }
}

