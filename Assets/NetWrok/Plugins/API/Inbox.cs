using System.Text;
using UnityEngine;
using System.Collections;

namespace NetWrok
{

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

}
