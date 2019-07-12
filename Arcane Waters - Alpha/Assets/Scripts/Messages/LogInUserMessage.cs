using System.Runtime.InteropServices;
using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class LogInUserMessage : MessageBase {
   #region Public Variables

   // The network instance id associated with this message
   public uint netId;

   // The credentials
   public string accountName;
   public string accountPassword;

   // The client's version of the game
   public int clientGameVersion;

   // The selected user
   public int selectedUserId;

   #endregion

   public LogInUserMessage () { }

   public LogInUserMessage (uint netId) {
      this.netId = netId;
   }

   public LogInUserMessage (uint netId, string accountName, string accountPassword, int clientGameVersion, int selectedUserId) {
      this.netId = netId;
      this.selectedUserId = selectedUserId;
      this.accountName = accountName;
      this.clientGameVersion = clientGameVersion;
      this.accountPassword = accountPassword;
   }
}