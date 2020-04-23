using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Steamworks;

public class SteamTester : MonoBehaviour {
   #region Public Variables

   #endregion

   private void OnGUI () {
      if (GUILayout.Button("Check my Friends")) {
         int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagImmediate);
         Debug.LogError("[STEAM-FRIENDS] Listing " + friendCount + " Friends.");
         for (int i = 0; i < friendCount; ++i) {
            CSteamID friendSteamId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagImmediate);
            string friendName = SteamFriends.GetFriendPersonaName(friendSteamId);
            EPersonaState friendState = SteamFriends.GetFriendPersonaState(friendSteamId);

            Debug.LogError(friendName + " is " + friendState);
         }
      }

      if (GUILayout.Button("Check my Steam ID")) {
         CSteamID steamId = SteamUser.GetSteamID();
         Debug.LogError(steamId);
      }

      if (GUILayout.Button("Check my Steam Name")) {

      }
   }

   #region Private Variables

   #endregion
}
