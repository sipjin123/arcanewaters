using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Steamworks;

public struct SteamFriendData
{
   #region Public Variables

   // Steam id of friend
   public ulong steamId;

   // Name of friend
   public string name;

   // Is friend playing arcane waters
   public bool playingArcaneWaters;

   // Steam status state of friend
   public EPersonaState personaState;

   // The image index of friend avatar - can then be used in SteamUtils.GetImageRGBA
   public int avatarImageIndex;

   #endregion

   public string getStatusDisplay () {
      if (playingArcaneWaters) {
         return "In Arcane Waters";
      }

      switch (personaState) {
         case EPersonaState.k_EPersonaStateOffline:
            return "Offline";
         case EPersonaState.k_EPersonaStateOnline:
            return "Online";
         case EPersonaState.k_EPersonaStateBusy:
            return "Busy";
         case EPersonaState.k_EPersonaStateAway:
            return "Away";
         case EPersonaState.k_EPersonaStateSnooze:
            return "Away";
         case EPersonaState.k_EPersonaStateLookingToTrade:
            return "Online";
         case EPersonaState.k_EPersonaStateLookingToPlay:
            return "Online";
         case EPersonaState.k_EPersonaStateInvisible:
            return "Offline";
      }

      return "Offline";
   }

   #region Private Variables

   #endregion
}
