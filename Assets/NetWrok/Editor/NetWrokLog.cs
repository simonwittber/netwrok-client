using UnityEngine;
using System.Collections;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;
using UniExtensions.Serialization;

public class NetWrokLog : EditorWindow
{
    const int MAX_LOGGED_MSGS = 100;

    List<MsgEntry> sent = new List<MsgEntry> ();
    List<MsgEntry> recv = new List<MsgEntry> ();
    string cmd = "";
    List<string> cmdHistory = new List<string>();

    [MenuItem ("Window/NetWrok Log")]
    static void Init ()
    {
        EditorWindow.GetWindow (typeof(NetWrokLog));
    }

    GUIStyle rowStyle, keyStyle, typeStyle, oddStyle, evenStyle;

    NetWrok.Connection _conn = null;
    NetWrok.Connection Conn {
        get {
            if(_conn == null) 
                _conn = GameObject.FindObjectOfType<NetWrok.Connection>();
            return _conn;
        }
    }

    void OnEnable ()
    {
        NetWrok.Connection.SendHook += CatchSentMessages;
        NetWrok.Connection.RecvHook += CatchRecvMessages;

    }

    void OnDisable ()
    {
        NetWrok.Connection.SendHook -= CatchSentMessages;
        NetWrok.Connection.RecvHook -= CatchRecvMessages;
    }

    void CreateStyles() {
        rowStyle = new GUIStyle(GUI.skin.button);
        rowStyle.alignment = TextAnchor.MiddleLeft;
        keyStyle = new GUIStyle(GUI.skin.label);
        keyStyle.fontStyle = FontStyle.Bold;
        typeStyle = new GUIStyle(GUI.skin.label);
        typeStyle.fontStyle = FontStyle.Italic;
        evenStyle = new GUIStyle(GUI.skin.textField);
        oddStyle = new GUIStyle(GUI.skin.textField);
        evenStyle.margin = new RectOffset(0,0,0,0);
        oddStyle.margin = new RectOffset(0,0,0,0);
    }

    void CatchSentMessages (NetWrok.Connection conn, NetWrok.Message msg)
    {
        if (msg == null)
            return;

        sent.Add (new MsgEntry () { timestamp = System.DateTime.Now, msg=msg, connection = conn });
        if(sent.Count > MAX_LOGGED_MSGS) {
            sent.RemoveAt(0);
        }
        Repaint ();

    }

    void CatchRecvMessages (NetWrok.Connection conn, NetWrok.Message msg)
    {
        if (msg == null)
            return;
        recv.Add (new MsgEntry () { timestamp = System.DateTime.Now, msg=msg, connection = conn });
        if(recv.Count > MAX_LOGGED_MSGS) {
            recv.RemoveAt(0);
        }
        Repaint ();

    }

    Vector2 sentScroll, recvScroll;

    void OnGUI ()
    {
        var width = Screen.width / 2;
        CreateStyles();

        GUILayout.BeginHorizontal ();
        GUILayout.BeginVertical ("box", GUILayout.MaxWidth(width));
        GUILayout.Label("Sent Messages");
        sentScroll = GUILayout.BeginScrollView (sentScroll);
        foreach (var m in sent) {
            DrawMsg (m);
        }
        GUILayout.EndScrollView ();
        GUILayout.EndVertical ();
        GUILayout.BeginVertical ("box", GUILayout.MaxWidth(width));
        GUILayout.Label("Received Messages");
        recvScroll = GUILayout.BeginScrollView (recvScroll);
        foreach (var m in recv) {
            DrawMsg (m);
        }
        GUILayout.EndScrollView ();
        GUILayout.EndVertical ();
        GUILayout.EndHorizontal ();

        DrawCommandLine ();
    }

    void DrawCommandLine ()
    {
        if (Event.current.type == EventType.KeyDown) {
            if(Event.current.character == '\n') {
                cmd = cmd.Trim();
                var request = false;
                if(cmd.StartsWith("r:")) {
                    request = true;
                    cmd = cmd.Substring(2);
                }
                string name;
                object[] parms;
                var firstSpace = cmd.IndexOf(' ');
                if(firstSpace > 0) {
                    name = cmd.Substring(0, firstSpace).Trim();
                    cmd = cmd.Substring(name.Length).Trim();
                    parms = new object[] {};
                } else {
                    name = cmd.Trim ();
                    parms = new object[] {};
                }
                if(request)
                    Conn.Request(name, parms);
                else
                    Conn.Send (name, parms);
                cmd = "";
                GUI.FocusControl("CMD");
            }
            if(Event.current.keyCode == KeyCode.UpArrow) {

            }

        }
        GUI.SetNextControlName("CMD");
        cmd = GUILayout.TextField (cmd);
    }

    void DrawMsg (MsgEntry m)
    {
        GUILayout.BeginVertical ();
        GUILayout.BeginHorizontal ();
        var heading = (m.timestamp.ToString ("HH:mm:ss.ff") + " " + m.connection.url);
        heading += " " + m.msg.name;
        var color = GUI.backgroundColor;
        if(m.msg.name == "exception") {
            GUI.backgroundColor = new Color(1f, 0.85f, 0f);
        }
        m.visible = GUILayout.Toggle (m.visible, heading, rowStyle, GUILayout.ExpandWidth (true));
        GUI.backgroundColor = color;
        GUILayout.EndHorizontal ();
        if(m.visible) {
            DrawDetail(m);
        }


        GUILayout.EndVertical ();

        GUILayout.Space (5);
    }

    void DrawDetail (MsgEntry m)
    {
        if(m.repr == null) {
            m.repr = UniExtensions.Serialization.JsonPrettyPrint.Format(m.msg.ToString());
        }
        GUILayout.TextArea(m.repr);

    }


    bool IsContainerType (object o)
    {
        return o is Hashtable || o is ArrayList;
    }


    class MsgEntry
    {
        public System.DateTime timestamp;
        public NetWrok.Connection connection;
        public NetWrok.Message msg;
        public bool visible = false;
        public string repr = null;
    }
}