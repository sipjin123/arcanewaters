using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class GenericObserverManager : NetworkVisibility {
   #region Public Variables

   #endregion

   private void Awake () {
      _observer = GetComponent<IObserver>();
   }

   public override bool OnCheckObserver (NetworkConnection conn) {
      // This function is basically useless because it's called too early in the process
      return false;
   }

   public override void OnRebuildObservers (HashSet<NetworkConnection> connectionsToObserve, bool initialize) {
      // It seems this has to be cleared out at the start, or else it can contain unwanted connections
      connectionsToObserve.Clear();

      // Look up our instance
      Instance instance = InstanceManager.self.getInstance(_observer.getInstanceId());

      if (instance == null) {
         return;
      }

      foreach (NetworkBehaviour entity in instance.entities) {
         if (entity.connectionToClient != null) {
            connectionsToObserve.Add(entity.connectionToClient);
         }
      }
   }

   public override void OnSetHostVisibility (bool visible) {
      Util.SetVisibility(gameObject, visible);
   }

   #region Private Variables

   // A reference to our associated observer
   protected IObserver _observer;

   #endregion
}
