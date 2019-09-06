using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

[RequireComponent(typeof(NetworkIdentity))]
public class InstanceObserverManager : NetworkBehaviour
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

   public override bool OnRebuildObservers (HashSet<NetworkConnection> connectionsToObserve, bool initial) {
      // It seems this has to be cleared out at the start, or else it can contain unwanted connections
      connectionsToObserve.Clear();

      foreach (NetworkBehaviour netBehavior in _instance.entities) {
         if (netBehavior.GetComponent<NetEntity>() != null) {
            NetEntity newEntity = netBehavior.GetComponent<NetEntity>();
            if (newEntity.connectionToClient != null) {
               connectionsToObserve.Add(newEntity.connectionToClient);
            }
         }
      }

      return true;
   }

   // Called hiding and showing objects on the host
   public override void OnSetLocalVisibility (bool vis) {

   }

   #region Private Variables

   // Our associated Instance
   protected Instance _instance;

   #endregion
}
