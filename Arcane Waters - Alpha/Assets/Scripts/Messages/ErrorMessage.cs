using System.Runtime.InteropServices;
using UnityEngine;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class ErrorMessage : NetworkMessage
{
   #region Public Variables

   // The network instance id associated with this message
   public uint netId;

   // The Type of error
   public enum Type {
      None = 0,
      FailedUserOrPass = 1 , Disconnected = 2, NameTaken = 3, NoGoldForCargo = 4, PortOutOfCargo = 5,
      OutOfCargoSpace = 6, PlayerNotEnoughCargo = 7, TooManyCargoTypes = 8, NoTradePermits = 9,
      UsernameNotFound = 10, ServerStartingUp = 11, AlreadyOnline = 12, Banned = 13, ClientOutdated = 14,
      InvalidUsername = 15, ServerDown = 16, NoCropsOfThatType = 17, NotEnoughGems = 18, NotEnoughGold = 19,
      Misc = 20, Kicked = 21
   }

   // The Type of error
   public Type errorType;

   // A custom message that can be specified
   public string customMessage;

   #endregion

   public ErrorMessage () { }

   public ErrorMessage (uint netId) {
      this.netId = netId;
   }

   public ErrorMessage (uint netId, Type errorType, string customMessage = "") {
      this.netId = netId;
      this.errorType = errorType;
      this.customMessage = customMessage;
   }
}