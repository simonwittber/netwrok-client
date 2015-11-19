using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;


namespace NetWrok.HTTP {
	public class Scheduler : MonoBehaviour {
		
        static Scheduler _instance = null;
		
        static public Scheduler Instance {
            get {
				if(_instance == null) {
					_instance = new GameObject("HTTP.Scheduler", typeof(Scheduler)).GetComponent<Scheduler>();
					_instance.gameObject.hideFlags = HideFlags.HideAndDontSave;
				}
				return _instance;
			}
		}
		
		public void Send(Request request, System.Action<HTTP.Request> requestDelegate) {
			StartCoroutine(_Send(request, requestDelegate));
		}
		
		public void Send(Request request, System.Action<HTTP.Response> responseDelegate) {
			StartCoroutine(_Send(request, responseDelegate));
		}

		public void OnQuit(System.Action fn) {
			onQuit.Add(fn);	
		}
		
		void Awake()
		{
			if (_instance != null)
			{
				Destroy(gameObject);
			}
		}

		IEnumerator _Send(Request request, System.Action<HTTP.Response> responseDelegate) {
			request.Send();
			while(!request.isDone)
				yield return new WaitForEndOfFrame();
			if(request.exception != null) {
				Debug.LogError(request.exception);	
			} else {
				responseDelegate(request.response);
			}
		}
		
		IEnumerator _Send(Request request, System.Action<HTTP.Request> requestDelegate) {
			request.Send();
			while(!request.isDone)
				yield return new WaitForEndOfFrame();
			requestDelegate(request);
		}

		void OnApplicationQuit() {
			foreach(var fn in onQuit) {
				try {
					fn();
				} catch(System.Exception e) {
					Debug.LogError(e);	
				}
			}
		}

		List<System.Action> onQuit = new List<System.Action>();
	}
}