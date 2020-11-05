using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerObserverManager : NetworkVisibility
{
   #region Public Variables

   #endregion

   void Awake () {
      _entity = GetComponent<NetworkBehaviour>();
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
      Instance instance = InstanceManager.self.getInstance(getInstanceId());

      if (instance == null) {
         return;
      }

      // Ensure that the player can still see themself
      if (_entity != null && _entity.connectionToClient != null) {
         connectionsToObserve.Add(_entity.connectionToClient);
      }

      // Otherwise, allow everything in the instance with a connection to see this entity
      foreach (NetworkBehaviour existingEntity in instance.entities) {
         if (existingEntity != null && existingEntity.connectionToClient != null) {
            connectionsToObserve.Add(existingEntity.connectionToClient);
         }
      }
   }

   // Called hiding and showing objects on the host
   public override void OnSetHostVisibility (bool vis) {
      Util.SetVisibility(gameObject, vis);
   }

   public int getInstanceId () {
      // If we've already cached it, just return that
      if (_instanceId != 0) {
         return _instanceId;
      }

      // Keep track of the instance ID of our associated entity
      if (_entity is NetEntity) {
         _instanceId = ((NetEntity) _entity).instanceId;
      } else if (_entity is TreasureChest) {
         _instanceId = ((TreasureChest) _entity).instanceId;
      } else if (_entity is OreNode) {
         _instanceId = ((OreNode) _entity).instanceId;
      } else if (_entity is Discovery) {
         _instanceId = ((Discovery) _entity).instanceId;
      } 

      return _instanceId;
   }

   #region Private Variables

   // Our associated Entity
   protected NetworkBehaviour _entity;

   // Our instance ID
   protected int _instanceId;

   #endregion
}
