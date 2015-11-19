using System.Text;
using UnityEngine;
using System.Collections;

namespace NetWrok
{

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

}
