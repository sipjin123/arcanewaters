using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

[RequireComponent(typeof(NetworkIdentity))]
public class BattlerObserverManager : NetworkBehaviour {
   #region Public Variables

   #endregion

   void Awake () {
      _battler = GetComponent<Battler>();
   }

   // Called when a new player enters
   public override bool OnCheckObserver (NetworkConnection newObserver) {
      // This function is basically useless because it's called too early in the process
      return false;
   }

   public override bool OnRebuildObservers (HashSet<NetworkConnection> connectionsToObserve, bool initial) {
      // It seems this has to be cleared out at the start, or else it can contain unwanted connections
      connectionsToObserve.Clear();

      foreach (Battler battler in _battler.battle.getParticipants()) {
         if (battler.player != null && battler.player.connectionToClient != null) {
            connectionsToObserve.Add(battler.player.connectionToClient);
         }
      }

      return true;
   }

   // Called hiding and showing objects on the host
   public override void OnSetLocalVisibility (bool vis) {

   }

   #region Private Variables

   // Our associated Battler
   protected Battler _battler;

   #endregion
}
