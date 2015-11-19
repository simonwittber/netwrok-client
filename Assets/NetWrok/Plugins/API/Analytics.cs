using System.Text;
using UnityEngine;
using System.Collections;

namespace NetWrok
{

	public class Analytics : SubAPI
	{
		public void RegisterEvent (string path, string evt)
		{
			api.Send ("analytics.register", path, evt);
		}
	}

}
