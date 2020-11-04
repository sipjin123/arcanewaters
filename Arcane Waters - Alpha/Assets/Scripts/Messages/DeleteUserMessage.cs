using System.Runtime.InteropServices;
using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class DeleteUserMessage : NetworkMessage
{
   #region Public Variables

   // The network instance id associated with this message
   public uint netId;

   // The user id
   public int userId;

   #endregion

   public DeleteUserMessage () { }

   public DeleteUserMessage (uint netId) {
      this.netId = netId;
   }

   public DeleteUserMessage (uint netId, int userId) {
      this.netId = netId;
      this.userId = userId;
   }
}