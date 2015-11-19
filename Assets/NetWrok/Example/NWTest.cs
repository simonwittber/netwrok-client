using UnityEngine;
using System.Collections;


public class NWTest : MonoBehaviour {

    NetWrok.Connection conn;

    [NetworkEventHandler("Hooray")]
	public void hooray(int a, int b, int c) {
        Debug.Log("HOORAY");
        Debug.Log(a);
        Debug.Log(b);
        Debug.Log(c);
    }


    public void OnWelcome() {
        Debug.Log(conn);
        StartCoroutine(DoStuff());
    }

    void Start() {
        conn = GetComponent<NetWrok.Connection>();
        conn.OnWelcome += OnWelcome;
    }

    IEnumerator DoStuff() {

        {
            var req = conn.server.Register("boris", "boris@wittber.com", "boris");
            yield return req.Wait();
            Debug.Log (req.Result);
        }
        {
            var req = conn.server.Authenticate("boris@wittber.com", "boris");
            yield return req.Wait();
            Debug.Log (req.Result);
        }


        {
            var req = conn.Request ("clan.create", "Knobs", null);
            yield return req.Wait();
            Debug.Log(req.Result);
        }

        {
			var req = conn.server.clan.Create("QQ", null);
            yield return req.Wait();
            Debug.Log(req.Result);
        }
    }


}
