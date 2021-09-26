using System.Runtime.InteropServices;
using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class RedirectMessage : NetworkMessage
{
   #region Public Variables
   // The new ip address to connect to
   public string newIpAddress;

   // The new port to connect to
   public int newPort;

   #endregion

   public RedirectMessage () { }

   public RedirectMessage (string newIpAddress, int newPort) {
      this.newIpAddress = newIpAddress;
      this.newPort = newPort;
   }
}