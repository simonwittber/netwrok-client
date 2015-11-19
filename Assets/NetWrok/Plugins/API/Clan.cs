using System.Text;
using UnityEngine;
using System.Collections;

namespace NetWrok
{

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

}
