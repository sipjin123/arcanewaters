using System.Runtime.InteropServices;
using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class LogInCompleteMessage : NetworkMessage
{
   #region Public Variables

   // The network instance id associated with this message
   public uint netId;

   // The direction we should start off facing
   public Direction initialFacingDirection = Direction.South;

   // The email associated with this login, needed for Paymentwall
   public string accountEmail;

   // The account creation date for this login, needed for Paymentwall
   public long accountCreationTime;

   #endregion

   public LogInCompleteMessage () { }

   public LogInCompleteMessage (uint netId, Direction initialFacingDirection, string accountEmail, long accountCreationTime) {
      this.netId = netId;
      this.initialFacingDirection = initialFacingDirection;
      this.accountEmail = accountEmail;
      this.accountCreationTime = accountCreationTime;
   }
}