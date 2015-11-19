using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UniExtensions.Serialization;

namespace NetWrok {

    public class Message {

        public string name = "";
        public string type = "ev";
        public string id = null;
        public List<object> args = new List<object>();

        public Message() {
            this.id = Random.Range(int.MinValue, int.MaxValue).ToString("X");
        }

        public Message(string name) {
            this.id = Random.Range(int.MinValue, int.MaxValue).ToString("X");
            this.name = name;
        }

        public Message(string name, params object[] args) {
            this.id = Random.Range(int.MinValue, int.MaxValue).ToString("X");
            this.name = name;
            this.args.AddRange(args);
        }

        public void AddArg(object arg) {
            args.Add(arg);
        }

        public bool HasArg(int index) {
            if(args == null) return false;
            return args.Count > index;
        }

        public T GetArg<T>(int index) {
            try {
                return (T)args[index];
            } catch(System.InvalidCastException e) {
                Debug.LogWarning(e);
                return default(T);
            }
        }

        override public string ToString() {
            return JsonSerializer.Encode(this);
        }

        public static Message FromString(string json) {
            return JsonSerializer.Decode<Message>(json);
        }
    	
    }
}
