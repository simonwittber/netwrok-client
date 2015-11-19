using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace NetWrok
{

    [System.Serializable]
    public class Room {
        public string name = "";
        public List<string> clients = new List<string>();
        public void AddMember(string uid) {
            if(!clients.Contains(uid)) clients.Add(uid);
        }
        public void RemoveMember(string uid) {
            if(!clients.Contains(uid)) clients.Remove(uid);
        }
    }
}