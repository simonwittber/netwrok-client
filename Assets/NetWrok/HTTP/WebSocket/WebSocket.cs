using UnityEngine;
using System;
using System.Text;
using System.Linq;
using System.Threading;
using System.Collections;
using System.Collections.Generic;

using System.IO;


namespace NetWrok.HTTP
{   
    public class WebSocket
    {
        public int niceness = 100;
        const byte FINALBIT = 0x80;
        const byte RESERVEDBIT1 = 0x40;
        const byte RESERVEDBIT2 = 0x20;
        const byte RESERVEDBIT3 = 0x10;
        const byte OP_CODE_MASK = 0xF;
        const byte MASKBIT = 0x80;
        const byte PAYLOAD_LENGTH_MASK = 0x7F;
        const int MASKING_KEY_WIDTH_IN_BYTES = 4;
        const int MAX_PAYLOAD_WITHOUT_EXTENDED_LENGTH_FIELD = 125;
        const int PAYLOAD_WITH_TWO_BYTE_EXTENDED_FIELD = 126;
        const int PAYLOAD_WITH_EIGHT_BYTE_EXTENDED_FIELD = 127;
        
        public delegate void OnTextMessageHandler (string message);
        
        public delegate void OnBinaryMessageHandler (byte[] message);
        
        public event System.Action OnConnect;
        public event System.Action OnDisconnect;
        public event OnTextMessageHandler OnTextMessageRecv;
        public event OnBinaryMessageHandler OnBinaryMessageRecv;
        
        public Exception exception;

        public float connectionTimeout = 0;
        
        [Flags]
        public enum OpCode
        { 
            OpCodeContinuation = 0x0,
            OpCodeText =  0x1,
            OpCodeBinary = 0x2,
            OpCodeClose = 0x8,
            OpCodePing = 0x9,
            OpCodePong = 0xA
        }
        
        enum ParseFrameResult
        {
            FrameIncomplete,
            FrameOK,
            FrameError
        }
        
        public enum CloseEventCode
        {
            CloseEventCodeNotSpecified = -1,
            CloseEventCodeNormalClosure = 1000,
            CloseEventCodeGoingAway = 1001,
            CloseEventCodeProtocolError = 1002,
            CloseEventCodeUnsupportedData = 1003,
            CloseEventCodeFrameTooLarge = 1004,
            CloseEventCodeNoStatusRcvd = 1005,
            CloseEventCodeAbnormalClosure = 1006,
            CloseEventCodeInvalidUTF8 = 1007,
            CloseEventCodeMinimumUserDefined = 3000,
            CloseEventCodeMaximumUserDefined = 4999
        }
        
        class FrameData
        {
            
            public OpCode opCode;
            public bool final;
            public bool reserved1;
            public bool reserved2;
            public bool reserved3;
            public bool masked;
            public int payload;
            public int payloadLength;
            public int end;
        }
        
        class SubArray : IEnumerable<byte>
        { 
            
            List<byte> array;
            int offset;
            int length;
            
            public SubArray (List<byte> array, int offset, int length)
            {
                this.array = array;
                this.offset = offset;
                this.length = length;
            }
            
            IEnumerator IEnumerable.GetEnumerator ()
            {
                return (IEnumerator<byte>)GetEnumerator ();
            }
            
            public IEnumerator<byte> GetEnumerator ()
            {
                return new SubArrayEnum (array, offset, length);
            }
        }
        
        class SubArrayEnum : IEnumerator<byte>
        {
            List<byte> array;
            int offset;
            int length;
            int position = -1;
            
            public SubArrayEnum (List<byte> array, int offset, int length)
            {
                this.array = array;
                this.offset = offset;
                this.length = length;
            }
            
            public bool MoveNext ()
            { 
                position++;
                return (position < length);
            }
            
            public void Reset ()
            { 
                position = -1;
            }
            
            object IEnumerator.Current { 
                get { 
                    return Current;
                }
            }
            
            public byte Current { 
                get { 
                    try {
                        return array [offset + position];
                    } catch (IndexOutOfRangeException) {
                        throw new InvalidOperationException ();
                    }   
                }
            }
            
            public void Dispose ()
            {
            }
        }
        
        public struct OutgoingMessage
        {
            public WebSocket.OpCode    opCode;
            public byte[]              data;
            
            public OutgoingMessage (WebSocket.OpCode opCode, byte[] data)
            {
                this.opCode = opCode;
                this.data = data;
            }
        }
        
        public bool isDone = false;
        public bool connected = false;
        Thread outgoingWorkerThread;
        Thread incomingWorkerThread;
        HTTP.Connection connection = null;
        List<string> incomingText = new List<string> ();
        List<byte[]> incomingBinary = new List<byte[]> ();
        List<OutgoingMessage> outgoing = new List<OutgoingMessage> ();
        bool hasContinuousFrame = false;
        OpCode continuousFrameOpCode;
        List<byte> continuousFrameData = new List<byte> ();
        bool receivedClosingHandshake = false;
        CloseEventCode closeEventCode;
        string closeEventReason = "";
        bool closing = false;
        bool connectionBroken;
        UTF8Encoding enc = new UTF8Encoding ();
        
        void OnTextMessage (string msg)
        {
            lock (incomingText) {
                incomingText.Add (msg); 
            }
        }
        
        void OnBinaryMessage (byte[] msg)
        {
            lock (incomingBinary) {
                incomingBinary.Add (msg);
            }
        }
        
        public IEnumerator Dispatcher ()
        {
            while (true) {
                yield return null;
                if (OnTextMessageRecv != null) {
                    lock (incomingText) {
                        if (incomingText.Count > 0) {
                            foreach (var i in incomingText) {
                                OnTextMessageRecv (i);
                            }
                            incomingText.Clear ();
                        }
                    }
                }
                if (OnBinaryMessageRecv != null) {
                    lock (incomingBinary) {
                        if (incomingBinary.Count > 0) {
                            foreach (var i in incomingBinary) {
                                OnBinaryMessageRecv (i);
                            }
                            incomingBinary.Clear ();
                        }
                    }
                }
                if (connectionBroken) {
                    if (OnDisconnect != null)
                        OnDisconnect ();
                    yield break;
                }
            }
        }

        public void Connect (string uri, Dictionary<string, string> optionalHeaders = null)
        {
            Connect (new System.Uri (uri), optionalHeaders); 
        }
        
        public Coroutine Wait ()
        {
            return Scheduler.Instance.StartCoroutine (_Wait ());
        }
        
        IEnumerator _Wait ()
        {
            while (!isDone)
                yield return null;
        }

        public void Connect (Uri uri, Dictionary<string, string> optionalHeaders = null)
        {
            Scheduler.Instance.StartCoroutine (_Connect (uri, optionalHeaders));
        }

        IEnumerator _Connect (Uri uri, Dictionary<string, string> optionalHeaders = null)
        {
            isDone = false;
            connected = false; 
            exception = null;

            receivedClosingHandshake = false;
            closing = false;

            var req = new Request ("GET", uri.ToString ());
            req.headers.Set ("Upgrade", "websocket");
            req.headers.Set ("Connection", "Upgrade");
            var key = WebSocketKey ();
            req.headers.Set ("Sec-WebSocket-Key", key);
            req.headers.Add ("Sec-WebSocket-Protocol", "chat, superchat");
            req.headers.Set ("Sec-WebSocket-Version", "13");
            req.headers.Set ("Origin", "null");
            

            if (optionalHeaders != null) {
                foreach (string headerName in optionalHeaders.Keys) {
                    req.headers.Set (headerName, optionalHeaders [headerName]);
                }
            }

            
            req.acceptGzip = false;
            req.timeout = connectionTimeout;
            yield return req.Send ();
            if (req.exception != null) {
                exception = req.exception;
            } else {
                if (req.response.headers.Get ("Upgrade").ToLower () == "websocket" && req.response.headers.Get ("Connection").ToLower () == "upgrade") {
                    var receivedKey = req.response.headers.Get ("Sec-Websocket-Accept").ToLower ();
                    var sha = new System.Security.Cryptography.SHA1CryptoServiceProvider ();
                    sha.ComputeHash (enc.GetBytes (key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"));
                    var computedKey = System.Convert.ToBase64String (sha.Hash).ToLower ();
                    if (computedKey == receivedKey) {
                        //good to go    
                        connected = true;
                        connection = req.upgradedConnection;
                        outgoingWorkerThread = new Thread (OutgoingWorker);
                        outgoingWorkerThread.Start ();
                        incomingWorkerThread = new Thread (IncomingWorker);
                        incomingWorkerThread.Start ();
                        Scheduler.Instance.StartCoroutine (Dispatcher ());          
                        Scheduler.Instance.OnQuit (() => {
                            Close (CloseEventCode.CloseEventCodeGoingAway, "Quit");
                            req.upgradedConnection.Dispose ();
                        });
                        if (OnConnect != null)
                            OnConnect ();
                    } else {
                        //invalid
                        connected = false;
                    }   
                }
            }
            isDone = true;
            
        }
        
        public void Send (string data)
        {   
            outgoing.Add (new OutgoingMessage (OpCode.OpCodeText, enc.GetBytes (data)));
        }
        
        public void Send (byte[] data)
        { 
            outgoing.Add (new OutgoingMessage (OpCode.OpCodeBinary, data));
        }
        
        public void Close (CloseEventCode code, string reason)
        { 
            StartClosingHandshake (code, reason);
        }
        
        void OutgoingWorker ()
        {

            while (connected) {
                Thread.Sleep (niceness);
                lock (outgoing) {
                    while (connection.stream.CanWrite && outgoing.Count > 0) {
                        var msg = outgoing [0];
                        var netform = BuildFrame (msg.opCode, msg.data);
                        connection.stream.Write (netform, 0, netform.Length);
                        outgoing.RemoveAt (0);
                    }
                }
            }
        }
        
        void IncomingWorker ()
        {
            connectionBroken = false;
            List<byte> buffer = new List<byte> ();
            try {
                while (connection.client.Client.Connected) {
                    int c = connection.stream.ReadByte ();
                    if (c == -1) {
                        break;
                    }
                    //Do something with byte to build a message
                    //when message built, add to incoming
                    buffer.Add ((byte)c);
                    ProcessBuffer (buffer);
                }
            } catch (IOException) {
                //pass
            }
            connectionBroken = true;

            connected = false;
        }
        
        bool ProcessBuffer (List<byte> buffer)
        {
            return ProcessFrame (buffer);
        }
        
        bool ProcessFrame (List<byte> buffer)
        {
            FrameData frame;
            if (ParseFrame (buffer, out frame) != ParseFrameResult.FrameOK)
                return false;
            
            switch (frame.opCode) {
            case OpCode.OpCodeContinuation:
                // An unexpected continuation frame is received without any leading frame.
                if (!hasContinuousFrame) {
                    Debug.LogWarning ("Received unexpected continuation frame.");
                    return false;
                }
                continuousFrameData.AddRange (new SubArray (buffer, frame.payload, frame.payloadLength));
                RemoveProcessed (buffer, frame.end);
                if (frame.final) {
                    continuousFrameData = new List<byte> ();
                    hasContinuousFrame = false;
                    if (continuousFrameOpCode == OpCode.OpCodeText) {
                        var message = "";
                        if (continuousFrameData.Count > 0) {
                            var array = continuousFrameData.ToArray ();
                            message = enc.GetString (array, 0, array.Length);
                        }
                        OnTextMessage (message);
                    } else if (continuousFrameOpCode == OpCode.OpCodeBinary) {
                        OnBinaryMessage (continuousFrameData.ToArray ());
                    }
                }
                break;
            case OpCode.OpCodeText:
                if (frame.final) {
                    String message = "";
                    if (frame.payloadLength > 0) {
                        var payload = new byte[frame.payloadLength];
                        buffer.CopyTo (frame.payload, payload, 0, frame.payloadLength);
                        message = enc.GetString (payload, 0, payload.Length);
                    } 
                    OnTextMessage (message);
                    RemoveProcessed (buffer, frame.end);
                } else {
                    hasContinuousFrame = true;
                    continuousFrameOpCode = OpCode.OpCodeText;
                    continuousFrameData.AddRange (new SubArray (buffer, frame.payload, frame.payloadLength));
                    RemoveProcessed (buffer, frame.end);
                }
                break;
            case OpCode.OpCodeBinary:
                if (frame.final) {
                    byte[] payload = new byte[frame.payloadLength];
                    buffer.CopyTo (frame.payload, payload, 0, frame.payloadLength);
                    OnBinaryMessage (payload);
                    RemoveProcessed (buffer, frame.end);
                } else {
                    hasContinuousFrame = true;
                    continuousFrameOpCode = OpCode.OpCodeBinary;
                    continuousFrameData.AddRange (new SubArray (buffer, frame.payload, frame.payloadLength));
                    RemoveProcessed (buffer, frame.end);
                }
                break;
            case OpCode.OpCodeClose:
                if (frame.payloadLength >= 2) {
                    byte highByte = buffer [frame.payload + 0];
                    byte lowByte = buffer [frame.payload + 1];
                    closeEventCode = (CloseEventCode)(highByte << 8 | lowByte);
                } else {
                    closeEventCode = CloseEventCode.CloseEventCodeNoStatusRcvd;
                }
                if (frame.payloadLength >= 3) { 
                    byte[] payload = new byte[frame.payloadLength - 2];
                    buffer.CopyTo (2, payload, 0, frame.payloadLength - 2); 
                    closeEventReason = enc.GetString (payload, 0, payload.Length);
                } else {
                    closeEventReason = "";
                }
                RemoveProcessed (buffer, frame.end);
                receivedClosingHandshake = true;
                StartClosingHandshake (closeEventCode, closeEventReason);
                break;
            case OpCode.OpCodePing:
                
                var reply = new byte[frame.payloadLength];
                buffer.CopyTo (frame.payload, reply, 0, frame.payloadLength);
                RemoveProcessed (buffer, frame.end);
                
                // reply with Pong!
                lock (outgoing) {
                    outgoing.Add (new OutgoingMessage (OpCode.OpCodePong, reply)); 
                }
                
                break;
            case OpCode.OpCodePong:
                // do nothing with a pong, just remove processed bytes
                RemoveProcessed (buffer, frame.end);
                break;
            default:
                Debug.LogError ("SHOULD NOT REACH HERE");
                break;
            }
            ;
            
            return buffer.Count != 0;
        }
        
        ParseFrameResult ParseFrame (List<byte> buffer, out FrameData frame)
        {
            
            frame = null;
            
            if (buffer.Count < 2) {
                return ParseFrameResult.FrameIncomplete;
            }
            
            int p = 0;
            
            byte firstByte = buffer [p++];
            byte secondByte = buffer [p++];
            
            bool final = (firstByte & FINALBIT) > 0;
            bool reserved1 = (firstByte & RESERVEDBIT1) > 0;
            bool reserved2 = (firstByte & RESERVEDBIT2) > 0;
            bool reserved3 = (firstByte & RESERVEDBIT3) > 0;
            OpCode opCode = (OpCode)(firstByte & OP_CODE_MASK);
            
            bool masked = (secondByte & MASKBIT) > 0;
            long payloadLength64 = (secondByte & PAYLOAD_LENGTH_MASK);
            if (payloadLength64 > MAX_PAYLOAD_WITHOUT_EXTENDED_LENGTH_FIELD) {
                int extendedPayloadLengthSize;
                if (payloadLength64 == PAYLOAD_WITH_TWO_BYTE_EXTENDED_FIELD)
                    extendedPayloadLengthSize = 2;
                else {
                    extendedPayloadLengthSize = 8;
                }
                if (buffer.Count - p < extendedPayloadLengthSize) {
                    return ParseFrameResult.FrameIncomplete;
                }
                payloadLength64 = 0;
                for (int i = 0; i < extendedPayloadLengthSize; ++i) {
                    payloadLength64 <<= 8;
                    payloadLength64 |= buffer [p++];
                }
            }
            
            const long maxPayloadLength = 0x7FFFFFFFFFFFFFFF;
            
            int maskingKeyLength = masked ? MASKING_KEY_WIDTH_IN_BYTES : 0;
            if (payloadLength64 > maxPayloadLength || payloadLength64 + maskingKeyLength > int.MaxValue) {
                Debug.LogError (string.Format ("WebSocket frame length too large: {0} bytes", payloadLength64));
                return ParseFrameResult.FrameError;
            }
            
            int payloadLength = (int)payloadLength64;
            
            if ((buffer.Count - p) < maskingKeyLength + payloadLength) {
                return ParseFrameResult.FrameIncomplete;
            }
            
            if (masked) {
                int maskingKey = p;
                int payload = p + MASKING_KEY_WIDTH_IN_BYTES;
                for (int i = 0; i < payloadLength; ++i) {
                    buffer [payload + i] ^= buffer [maskingKey + (i % MASKING_KEY_WIDTH_IN_BYTES)]; // Unmask the payload.
                }
            }
            
            frame = new FrameData ();
            frame.opCode = opCode;
            frame.final = final;
            frame.reserved1 = reserved1;
            frame.reserved2 = reserved2;
            frame.reserved3 = reserved3;
            frame.masked = masked;
            frame.payload = p + maskingKeyLength;
            frame.payloadLength = payloadLength;
            frame.end = p + maskingKeyLength + payloadLength;
            
            return ParseFrameResult.FrameOK;
        }
        
        byte[] BuildFrame (OpCode opCode, byte[] data)
        {   
            var frame = new List<byte> ();
            frame.Add ((byte)(FINALBIT | (byte)opCode));
            if (data.Length <= MAX_PAYLOAD_WITHOUT_EXTENDED_LENGTH_FIELD)
                frame.Add ((byte)(MASKBIT | data.Length & 0xFF));
            else if (data.Length <= 0xFFFF) {
                frame.Add (MASKBIT | PAYLOAD_WITH_TWO_BYTE_EXTENDED_FIELD);
                frame.Add ((byte)((data.Length & 0xFF00) >> 8));
                frame.Add ((byte)(data.Length & 0xFF));
            } else {
                frame.Add (MASKBIT | PAYLOAD_WITH_EIGHT_BYTE_EXTENDED_FIELD);
                var extendedPayloadLength = new byte[8];
                int remaining = data.Length;
                // Fill the length into extendedPayloadLength in the network byte order.
                for (int i = 0; i < 8; ++i) {
                    extendedPayloadLength [7 - i] = (byte)(remaining & 0xFF);
                    remaining >>= 8;
                }
                frame.AddRange (extendedPayloadLength);
            }
            
            // Mask the frame.
            int maskingKeyStart = frame.Count;
            frame.AddRange (new byte[MASKING_KEY_WIDTH_IN_BYTES]); // Add placeholder for masking key. Will be overwritten.
            int payloadStart = frame.Count;
            frame.AddRange (data);
            
            RC4.CryptographicallyRandomValues (frame, maskingKeyStart, MASKING_KEY_WIDTH_IN_BYTES);
            
            for (int i = 0; i < data.Length; ++i) {
                frame [payloadStart + i] ^= frame [maskingKeyStart + i % MASKING_KEY_WIDTH_IN_BYTES];
            }
            
            return frame.ToArray ();
        }
        
        void RemoveProcessed (List<byte> buffer, int length)
        {
            buffer.RemoveRange (0, length);
        }
        
        void StartClosingHandshake (CloseEventCode code, string reason)
        {
            
            if (closing) {
                return;
            }
            
            List<byte> buf = new List<byte> ();
            //if (!receivedClosingHandshake && code != CloseEventCode.CloseEventCodeNotSpecified) {
            if (!receivedClosingHandshake) {
                byte hb = (byte)((int)code >> 8);
                byte lb = (byte)code;
                buf.Add (hb);
                buf.Add (lb);
                buf.AddRange (enc.GetBytes (reason));
                outgoing.Add (new OutgoingMessage (OpCode.OpCodeClose, buf.ToArray ()));
            } 
            
            
            closing = true;
        }
        
        string WebSocketKey ()
        {
            return System.Convert.ToBase64String (System.Guid.NewGuid ().ToByteArray ());
        }
    }
    
    
    
    
}
