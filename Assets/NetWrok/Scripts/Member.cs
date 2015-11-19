using UnityEngine;
using System.Collections;
using System.Linq;


namespace NetWrok
{
    public class Member : MonoBehaviour
    {
        public int member_id, clan_id, alliance_id;
        public string handle, clan_name, alliance_name;
        public string[] roles;

        [NetworkEventHandler("member.info")]
        public void OnAuthInfo(Hashtable msg) {
            member_id = (int)msg["id"];
            alliance_id = (int)(msg["alliance_id"]==null?0:msg["alliance_id"]);
            clan_id = (int)(msg["clan_id"]==null?0:msg["clan_id"]);
            handle = (string)msg["handle"];
            clan_name = (string)msg["clan_name"];
            alliance_name = (string)msg["alliance_name"];
            roles = (from i in ((ArrayList)msg["roles"]).ToArray() select (string)i).ToArray();
        }
 
    }
}
