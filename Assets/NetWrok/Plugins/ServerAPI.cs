using System.Text;
using UnityEngine;
using System.Collections;

namespace NetWrok
{
    public class API
    {

        public Connection connection;
        public Contacts contacts;
        public Analytics analytics;
        public Alliance alliance;
        public Clan clan;
        public Inbox inbox;
        public Squad squad;

        public API ()
        {
            contacts = new Contacts () { api = this };
            analytics = new Analytics (){ api = this };
            alliance = new Alliance (){ api = this };
            clan = new Clan (){ api = this };
            inbox = new Inbox (){ api = this };
            squad = new Squad () { api = this };
        }

        public void Join (string room)
        {
            Send ("join", room);
        }
        
        public void Leave (string room)
        {
            Send ("leave", room);
        }
        
        public void Say (string room, string cmd, params object[] args)
        {
            Send ("say", room, cmd, args);
        }
        
        public Request Register (string handle, string email, string password)
        {
            
            var hash = SHA256 (password).ToLower ();
            return Request ("member.register", handle, email, hash);
        }
        
        public Request Authenticate (string email, string password)
        {
            var hash = SHA256 (password).ToLower ();
            
            hash = connection.UID + hash;
            hash = SHA256 (hash).ToLower ();
            return Request ("member.authenticate", email, hash);
        }
        
        public void RequestPasswordReset (string email)
        {
            Send ("member.password_reset_request", email);
        }
        
        public Request ResetPassword (string email, string token, string password)
        {
            password = SHA256 (password);
            return Request ("member.password_reset", email, token, password);
        }

        public void Send (string name, params object[] args)
        {
            connection.Send (name, args);
        }

        public Request Request (string name, params object[] args)
        {
            return connection.Request (name, args);
        }

        string SHA256 (string phrase)
        {
            if (phrase == null)
                return null;
            var encoder = new System.Text.UTF8Encoding ();
            var sha256Hasher = new System.Security.Cryptography.SHA256Managed ();
            var hashedDataBytes = sha256Hasher.ComputeHash (encoder.GetBytes (phrase));
            return ByteArrayToHexString (hashedDataBytes);
        }
        
        string ByteArrayToHexString (byte[] array)
        {
            StringBuilder stringBuilder = new StringBuilder (array.Length * 2);
            byte[] array2 = array;
            for (int i = 0; i < array2.Length; i++) {
                byte b = array2 [i];
                stringBuilder.AppendFormat ("{0:x2}", b);
            }
            return stringBuilder.ToString ().PadLeft (32, '0');
        }


    }

    public class SubAPI
    {
        public API api;
    }

    public class Contacts : SubAPI
    {
        public void Add (int member_id)
        {
            api.Send ("contacts.add", member_id);
        }

        public Request Fetch ()
        {
            return api.Request ("contacts.fetch");
        }
    }

    public class Alliance : SubAPI
    {
        public void SetObject (string key, object value)
        {
            api.Send ("alliance.set_object", key, value);
        }

        public Request GetObject (string key)
        {
            return api.Request ("alliance.get_object", key);
        }

        public Request GetObjectKeys ()
        {
            return api.Request ("alliance.get_object_keys");
        }

        public Request GetMembers ()
        {
            return api.Request ("alliance.members");
        }

        public Request Create (string name, string type)
        {
            return api.Request ("alliance.create", name, type);
        }

        public Request Leave ()
        {
            return api.Request ("alliance.leave");
        }

        public Request Join (int alliance_id)
        {
            return api.Request ("alliance.join", alliance_id);
        }

        public Request List ()
        {
            return api.Request ("alliance.list");
        }
    }

    public class Analytics : SubAPI
    {
        public void RegisterEvent (string path, string evt)
        {
            api.Send ("analytics.register", path, evt);
        }
    }

    public class Clan : SubAPI
    {
        public void SetObject (string key, object value)
        {
            api.Send ("clan.set_object", key, value);
        }

        public Request GetObject (string key)
        {
            return api.Request ("clan.get_object", key);
        }

        public Request GetObjectKeys ()
        {
            return api.Request ("clan.get_object_keys");
        }

        public Request GetMembers ()
        {
            return api.Request ("clan.members");
        }

        public Request Create (string name, string type)
        {
            return api.Request ("clan.create", name, type);
        }

        public Request Leave ()
        {
            return api.Request ("clan.leave");
        }

        public Request Join (int clan_id)
        {
            return api.Request ("clan.join", clan_id);
        }

        public Request List ()
        {
            return api.Request ("clan.list");
        }

        public void Kick (int member_id)
        {
            api.Send ("clan.kick", member_id);
        }
    }

    public class Inbox : SubAPI
    {
        public void Send (int member_id, string type, string text)
        {
            api.Send ("inbox.send", member_id, type, text);
        }

        public Request Fetch ()
        {
            return api.Request ("inbox.fetch");
        }
    }

    public class Squad : SubAPI
    {
        public void SetObject (string key, object value)
        {
            api.Send ("squad.set_object", key, value);
        }
        
        public Request GetObject (string key)
        {
            return api.Request ("squad.get_object", key);
        }
        
        public Request GetObjectKeys ()
        {
            return api.Request ("squad.get_object_keys");
        }
        
        public Request GetMembers ()
        {
            return api.Request ("squad.members");
        }
        
        public void Join (int member_id)
        {
            api.Send ("squad.join", member_id);
        }
        
        public void Leave ()
        {
            api.Send ("squad.leave");
        }
        
        public void Invite (int member_id)
        {
            api.Send ("squad.invite", member_id);
        }

        public void Send (string msg, params object[] args)
        {
            api.Send ("squad.send", msg, args);
        }
        

    }






}
