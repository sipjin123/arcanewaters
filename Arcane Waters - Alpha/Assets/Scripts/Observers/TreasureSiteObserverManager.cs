using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class TreasureSiteObserverManager : NetworkBehaviour
{
   #region Public Variables

   #endregion

   void Awake () {
      _treasureSite = GetComponent<TreasureSite>();
      _canvasGroup = GetComponentInChildren<CanvasGroup>();
   }

   // Called when a new player enters
   public override bool OnCheckObserver (NetworkConnection newObserver) {
      // This function is basically useless because it's called too early in the process
      return false;
   }

   public override bool OnRebuildObservers (HashSet<NetworkConnection> connectionsToObserve, bool initial) {
      // It seems this has to be cleared out at the start, or else it can contain unwanted connections
      connectionsToObserve.Clear();

      // Look up our instance
      Instance instance = InstanceManager.self.getInstance(getInstanceId());

      if (instance == null) {
         return true;
      }

      // Allow everything in the instance with a connection to see this entity
      foreach (NetworkBehaviour entity in instance.entities) {
         if (entity != null && entity.connectionToClient != null) {
            connectionsToObserve.Add(entity.connectionToClient);
         }
      }

      return true;
   }

   // Called hiding and showing objects on the host
   public override void OnSetHostVisibility (bool vis) {
      Util.SetVisibility(gameObject, vis);

      // Show or hide the UI
      _canvasGroup.alpha = vis ? 1.0f : 0.0f;
   }

   public int getInstanceId () {
      return _treasureSite.instanceId;
   }

   #region Private Variables

   // Our associated site
   protected TreasureSite _treasureSite;

   // The canvas group of the UI bars
   protected CanvasGroup _canvasGroup;

   #endregion
}
