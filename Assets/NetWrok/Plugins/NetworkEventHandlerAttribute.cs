using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// This attribute marks methods which will receive events received via NetWrok.Connection.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class NetworkEventHandlerAttribute : Attribute {

    public string name { private set; get; }

    public NetworkEventHandlerAttribute() {
        this.name = null;
    }
    public NetworkEventHandlerAttribute(string name) {
        this.name = name;
    }

}
