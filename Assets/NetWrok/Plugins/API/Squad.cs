using System.Text;
using UnityEngine;
using System.Collections;

namespace NetWrok
{







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
