using System.Text;
using UnityEngine;
using System.Collections;

namespace NetWrok
{
	public class Location : SubAPI
	{
		public void SetObject (string key, object value)
		{
			api.Send ("location.set_object", key, value);
		}
        
		public Request GetObject (string key)
		{
			return api.Request ("location.get_object", key);
		}
        
		public Request GetObjectKeys ()
		{
			return api.Request ("location.get_object_keys");
		}
        
		public Request GetMembers ()
		{
			return api.Request ("location.members");
		}
        
		public void Enter (int member_id)
		{
			api.Send ("location.enter", member_id);
		}
        
		public void Exit ()
		{
			api.Send ("location.exit");
		}

		public void Send (string msg, params object[] args)
		{
			api.Send ("location.send", msg, args);
		}
        

	}






}
