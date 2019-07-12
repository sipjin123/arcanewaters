using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

[AttributeUsage(AttributeTargets.Method)]
public class ServerOnlyAttribute : Attribute {
   public ServerOnlyAttribute () {

   }
}
