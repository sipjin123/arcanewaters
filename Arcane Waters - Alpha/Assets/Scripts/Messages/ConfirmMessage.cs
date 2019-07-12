using System.Runtime.InteropServices;
using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class ConfirmMessage : MessageBase {
   #region Public Variables

   // The network instance id associated with this message
   public uint netId;

   // The Type of confirmation
   public enum Type {
      SeaWarp = 1, BoughtCargo = 2, BugReport = 3, AddGold = 4, General = 5,
      ConfirmDeleteItem = 6, ContainerOpened = 7, StoreItemBought = 8, UsedHairDye = 9,
      DeletedUser = 10, UsedShipSkin = 11, UsedHaircut = 12, CreatedGuild = 13,
      ShipBought = 14
   }

   // The Type of confirmation
   public Type confirmType;

   // The time of the action on the server
   public long timestamp;

   // A custom confirmation message that can be provided
   public string customMessage;

   #endregion

   public ConfirmMessage () { }

   public ConfirmMessage (uint netId) {
      this.netId = netId;
   }

   public ConfirmMessage (uint netId, Type confirmType, long timestamp, string customMessage = "") {
      this.netId = netId;
      this.confirmType = confirmType;
      this.timestamp = timestamp;
      this.customMessage = customMessage;
   }
}