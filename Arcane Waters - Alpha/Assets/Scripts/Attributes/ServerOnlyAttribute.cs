using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

/// <summary>
/// When executing a build, the <strong>contents</strong> of this method will be 
/// wrapped around a '#if <strong>IS_SERVER_ONLY</strong> #endif' directive.
/// </summary>
[AttributeUsage(AttributeTargets.Method)]
public class ServerOnlyAttribute : Attribute {
   public ServerOnlyAttribute () {

   }
}
