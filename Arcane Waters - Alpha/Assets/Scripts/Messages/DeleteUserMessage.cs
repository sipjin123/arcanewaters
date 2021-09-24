using System.Runtime.InteropServices;
using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class DeleteUserMessage : NetworkMessage
{
   #region Public Variables

   // The user id
   public int userId;

   #endregion

   public DeleteUserMessage () { }

   public DeleteUserMessage (int userId) {
      this.userId = userId;
   }
}