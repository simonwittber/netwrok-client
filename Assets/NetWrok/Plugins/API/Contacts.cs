using System.Text;
using UnityEngine;
using System.Collections;

namespace NetWrok
{

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

		public void Remove (int contact_id)
		{
			api.Send ("contacts.remove", contact_id);
		}
	}

}
