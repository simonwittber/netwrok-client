using UnityEngine;
using System.Collections;
using System;

/// <summary>
/// This attribute marks methods which will receive function calls received via NetWrok.Connection.
/// The return value is passed back to the server.
/// </summary>

[AttributeUsage(AttributeTargets.Method)]
public class NetworkFunctionHandlerAttribute : NetworkEventHandlerAttribute {

	
}
