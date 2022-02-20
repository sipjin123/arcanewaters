﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class InteractableObjObserverManager : NetworkVisibility {
   #region Public Variables

   #endregion

   void Awake () {
      _interactableObjEntity = GetComponent<InteractableObjEntity>();
   }

   // Called when a new player enters
   public override bool OnCheckObserver (NetworkConnection newObserver) {
      // This function is basically useless because it's called too early in the process
      return false;
   }

   public override void OnRebuildObservers (HashSet<NetworkConnection> connectionsToObserve, bool initial) {
      // It seems this has to be cleared out at the start, or else it can contain unwanted connections
      connectionsToObserve.Clear();

      // Look up our instance
      Instance instance = InstanceManager.self.getInstance(_interactableObjEntity.getInstanceId());

      if (instance == null) {
         return;
      }

      foreach (NetworkBehaviour entity in instance.entities) {
         if (entity.connectionToClient != null) {
            connectionsToObserve.Add(entity.connectionToClient);
         }
      }
   }

   // Called hiding and showing objects on the host
   public override void OnSetHostVisibility (bool vis) {
      Util.SetVisibility(gameObject, vis);
   }

   #region Private Variables

   // Our associated sea projectile
   protected InteractableObjEntity _interactableObjEntity;

   #endregion
}
