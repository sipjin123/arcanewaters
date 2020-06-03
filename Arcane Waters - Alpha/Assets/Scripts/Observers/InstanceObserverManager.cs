using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

[RequireComponent(typeof(NetworkIdentity))]
public class InstanceObserverManager : NetworkVisibility
{
   #region Public Variables

   #endregion

   void Awake () {
      _instance = GetComponent<Instance>();
   }

   // Called when a new player enters
   public override bool OnCheckObserver (NetworkConnection newObserver) {
      // This function is basically useless because it's called too early in the process
      return false;
   }

   public override void OnRebuildObservers (HashSet<NetworkConnection> connectionsToObserve, bool initial) {
      // It seems this has to be cleared out at the start, or else it can contain unwanted connections
      connectionsToObserve.Clear();

      foreach (NetworkBehaviour entity in _instance.entities) {
         if (entity != null && entity.connectionToClient != null) {
            connectionsToObserve.Add(entity.connectionToClient);
         }
      }
   }

   // Called hiding and showing objects on the host
   public override void OnSetHostVisibility (bool vis) {

   }

   #region Private Variables

   // Our associated Instance
   protected Instance _instance;

   #endregion
}
