using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace NetWrok
{
    public class MessageDispatcher
    {
        public Connection connection;
        Dictionary<string, List<MethodReference>> dispatchTable = new Dictionary<string, List<MethodReference>> ();
        Dictionary<string, List<MethodReference>> functionTable = new Dictionary<string, List<MethodReference>> ();

        public void Reset ()
        {
            Disable ();
            Enable ();
        }

        public void Enable ()
        {
            if (connection != null) {
                RegisterAllNetworkMethods ();
                connection.OnRoomMessage += HandleOnRoomMessage;
                connection.OnSystemMessage += HandleOnSystemMessage;
                connection.OnConnected += HandleOnConnected;
                connection.OnUserMessage += HandleOnUserMessage;
            }
        }

        public void Disable ()
        {
            if (connection != null) {
                UnregisterAllNetworkMethods ();
                connection.OnRoomMessage -= HandleOnRoomMessage;
                connection.OnSystemMessage -= HandleOnSystemMessage;
                connection.OnConnected -= HandleOnConnected;
                connection.OnUserMessage -= HandleOnUserMessage;
            }
        }

        void HandleOnConnected ()
        {

        }

        void HandleOnUserMessage (string cmd, string uid, Message msg)
        {
            Dispatch (msg.type, msg.id, cmd, msg.args.ToArray());
        }

        void HandleOnRoomMessage (string room, string uid, string cmd, Message msg)
        {
            Dispatch (msg.type, msg.id, cmd, msg.args.ToArray());
        }

        void HandleOnLeftRoom (string room, string UID)
        {

        }

        void HandleOnJoinedRoom (string room, string UID)
        {

        }

        void Dispatch (string type, string id, string cmd, object[] arguments)
        {
            if (type == "ev") {
                if (dispatchTable.ContainsKey (cmd)) {
                    foreach (var mr in dispatchTable[cmd]) {
                        try {
                            mr.mi.Invoke (mr.c, arguments);
                        } catch (TargetParameterCountException) {
                            Debug.LogError(string.Format("Tring to call event ({0}) with invalid parameters ({1})", cmd, (arguments==null?"NULL":string.Join(", ", (from i in arguments select i.ToString()).ToArray()))));
                        }
                    }
                } else {
                    Debug.Log (string.Format ("Unknown message ({0})", cmd));
                }
            }
            if (type == "fn") {
                if (functionTable.ContainsKey (cmd)) {
                    foreach (var mr in functionTable[cmd]) {
                        var result = mr.mi.Invoke (mr.c, arguments);
                        connection.Send("return", id, result);
                    }
                } else {
                    Debug.Log (string.Format ("Unknown function ({0})", cmd));
                }
            }

        }

        void HandleOnSystemMessage (Message msg)
        {
            Dispatch (msg.type, msg.id, msg.name, msg.args.ToArray());
        }

        void UnregisterAllNetworkMethods ()
        {
            dispatchTable.Clear ();
            functionTable.Clear ();
        }

        void RegisterAllNetworkMethods ()
        {
            dispatchTable.Clear ();
            functionTable.Clear ();
            var components = GameObject.FindObjectsOfType<Component> ();
            foreach (var c in components) {
                foreach (var mi in c.GetType().GetMethods()) {
                    var evattrs = mi.GetCustomAttributes (typeof(NetworkEventHandlerAttribute), false);
                    if (evattrs.Length > 0) {
                        var att = evattrs[0] as NetworkEventHandlerAttribute;
                        var key = att.name;
                        AddMethodToTable (c, mi, key, dispatchTable);
                    }
                    var fnattrs = mi.GetCustomAttributes (typeof(NetworkFunctionHandlerAttribute), false);
                    if (fnattrs.Length > 0) {
                        var att = evattrs[0] as NetworkFunctionHandlerAttribute;
                        var key = att.name;
                        AddMethodToTable (c, mi, key, functionTable);
                    }
                }
            }
        }
    
        void AddMethodToTable (Component c, MethodInfo mi, string key, Dictionary<string, List<MethodReference>> table)
        {
            if(key == null)
                key = string.Format ("{0}.{1}", c.GetType ().Name, mi.Name);
            if (!table.ContainsKey (key))
                table [key] = new List<MethodReference> ();
            Debug.Log ("Registering Network Method: " + key);
            table [key].Add (new MethodReference () { c = c, mi = mi });
        }
    }


}