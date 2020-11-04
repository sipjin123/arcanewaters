using System.Runtime.InteropServices;
using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class StoreMessage : NetworkMessage
{
   #region Public Variables

   // The network instance id associated with this message
   public uint netId;

   // The new gems amount we have on hand
   public int gemsOnHand;

   // The new gold amount we have on hand
   public int goldOnHand;

   // Our User Objects
   public UserObjects userObjects;

   #endregion

   public StoreMessage () { }

   public StoreMessage (uint netId, UserObjects userObjects, int goldOnHand, int gemsOnHand) {
      this.netId = netId;
      this.userObjects = userObjects;
      this.goldOnHand = goldOnHand;
      this.gemsOnHand = gemsOnHand;
   }
}