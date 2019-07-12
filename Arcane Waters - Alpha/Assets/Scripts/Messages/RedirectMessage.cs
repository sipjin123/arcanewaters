using System.Runtime.InteropServices;
using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class RedirectMessage : MessageBase {
   #region Public Variables

   // The network instance id associated with this message
   public uint netId;

   // The new ip address to connect to
   public string newIpAddress;

   // The new port to connect to
   public int newPort;

   #endregion

   public RedirectMessage () { }

   public RedirectMessage (uint netId, string newIpAddress, int newPort) {
      this.netId = netId;
      this.newIpAddress = newIpAddress;
      this.newPort = newPort;
   }
}