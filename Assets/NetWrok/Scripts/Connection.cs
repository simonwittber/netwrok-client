using UnityEngine;
using System.Collections;
using System.Collections.Generic;

using System.Text;
using System.Linq;

namespace NetWrok
{
    public class Connection : MonoBehaviour
    {

#region GLOBAL_HOOKS
        public static event System.Action<Connection, Message> SendHook;
        public static event System.Action<Connection, Message> RecvHook;
        public static event System.Action<Connection> ConnectHook;
        public static event System.Action<Connection> DisconnectHook;
#endregion
#region LOCAL_HOOKS
        public event System.Action<string, string, string, Message> OnRoomMessage;
        public event System.Action<string, string, Message> OnUserMessage;
        public event System.Action<Message> OnSystemMessage;
        public event System.Action OnConnected, OnDisconnected, OnWelcome;
#endregion

#region API

        public string url = "http://chernobyl.local:8765/";
        public bool connectOnStart = true;
        public bool reconnectOnLostConnection = true;
        public bool connected = false;
        public string status = "";
        public string UID = "";
        public bool logNetworkExceptions = true;
        public float connectionTimeout = 3;
        public API server;

        public void Connect ()
        {
            StartCoroutine (_Connect ());
        }

        public void Disconnect ()
        {
            reconnectOnLostConnection = false;
            ws.Close (HTTP.WebSocket.CloseEventCode.CloseEventCodeNotSpecified, "Bye.");
        }

        public Request Request (string name, params object[] args)
        {
            var msg = new Message (name, args);
            msg.type = "fn";
            if (SendHook != null)
                SendHook (this, msg);
            var req = new Request (this, msg);
            requests.Add (msg.id, req);
            ws.Send (msg.ToString ());
            return req;
        }

        public void Send (string name, params object[] args)
        {
            var msg = new Message (name, args);
            msg.type = "ev";
            if (SendHook != null)
                SendHook (this, msg);
            ws.Send (msg.ToString ());
        }
#endregion
#region UNITY_MESSAGES
        void OnEnable ()
        {
            if (dispatcher == null)
                dispatcher = new MessageDispatcher () { connection = this };
            server = new API () { connection = this };
            dispatcher.Enable ();
            Application.runInBackground = true;
            if (connectOnStart)
                Connect ();
        }
        
        void OnDisable ()
        {
            dispatcher.Disable ();
        }
        
        void OnLevelWasLoaded (int id)
        {
            dispatcher.Reset ();
        }
#endregion
#region IMPLEMENTATION
        IEnumerator _Connect ()
        {
            connected = false;
            ws = new HTTP.WebSocket ();
            StartCoroutine (ws.Dispatcher ());
            ws.connectionTimeout = connectionTimeout;
            ws.OnConnect += HandleOnConnect;
            ws.OnDisconnect += HandleOnDisconnect;
            ws.OnTextMessageRecv += HandleOnTextMessageRecv;
            status = "Connecting";
            ws.Connect (url);
            status = "Waiting for connection";
            yield return ws.Wait ();
            if (ws.exception != null) {
                Debug.Log ("An exception occured when connecting: " + ws.exception);
                if (reconnectOnLostConnection) {
                    status = "Reconnecting";
                    Invoke ("Connect", 2);
                    yield break;
                }
            } 
            status = "Connected";
        }

        void HandleOnTextMessageRecv (string message)
        {
            var msg = Message.FromString (message);
            if (RecvHook != null)
                RecvHook (this, msg);
            ProcessMessage (msg);
        }

        void HandleOnDisconnect ()
        {
            status = "Disconnected";
            connected = false;
            if (DisconnectHook != null)
                DisconnectHook (this);
            if (OnDisconnected != null)
                OnDisconnected ();
            if (reconnectOnLostConnection)
                Connect ();
        }

        void HandleOnConnect ()
        {
            connected = true;
            if (ConnectHook != null)
                ConnectHook (this);
            if (OnConnected != null)
                OnConnected ();
        }

        void HandleReturnMsg (Message msg)
        {
            var fn = msg.GetArg<string> (0);
            var id = msg.GetArg<string> (1);
            var success = msg.GetArg<bool> (2);
            if (requests.ContainsKey (id)) {
                var req = requests [id];
                if (success) {
                    req.Result = msg.GetArg<object> (3);
                } else {
                    req.Error = msg.GetArg<string> (3);
                }
                req.isDone = true;
                requests.Remove (id);
            } else {
                Debug.LogError ("Invalid request ID in return msg: " + fn + " " + id);
            }
        }

        void HandleRoomMsg (Message msg)
        {
            var cmd = msg.GetArg<string> (0);
            var uid = msg.GetArg<string> (1);
            var room = msg.GetArg<string> (2);
            if (OnRoomMessage != null)
                OnRoomMessage (room, uid, cmd, msg);
        }

        void HandleUserMsg (Message msg)
        {
            var cmd = msg.GetArg<string> (0);
            var uid = msg.GetArg<string> (1);
            if (OnUserMessage != null)
                OnUserMessage (cmd, uid, msg);
        }

        void HandleSystemMessage (Message msg)
        {
            if (OnSystemMessage != null)
                OnSystemMessage (msg);
        }

        void HandleExceptionMsg (Message msg)
        {
            if (logNetworkExceptions) {
                var errorMsg = "An exception was received from the server when sending message: " + msg.GetArg<string> (1) + " " + msg.GetArg<string> (2) + "\r\n";
                Debug.LogError (errorMsg);
            }
        }

        void ProcessMessage (Message msg)
        {
            switch (msg.name) {
            case "return":
                HandleReturnMsg (msg);
                break;
            case "said":
                HandleRoomMsg (msg);
                break;
            case "whispers":
                HandleUserMsg (msg);
                break;
            case "welcome":
                UID = msg.GetArg<string> (0);
                if (OnWelcome != null)
                    OnWelcome ();
                break;
            case "exception":
                HandleExceptionMsg (msg);
                break;
            default:
                HandleSystemMessage (msg);
                break;
            }
        }

        Dictionary<string,Request> requests = new Dictionary<string, NetWrok.Request> ();
        HTTP.WebSocket ws;
        MessageDispatcher dispatcher;
#endregion

    }
}