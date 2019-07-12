using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class InstanceManager : MonoBehaviour {
   #region Public Variables

   // The prefab we use for creating Instances
   public Instance instancePrefab;

   // Self
   public static InstanceManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   public Instance addPlayerToInstance (NetEntity player, Area.Type areaType) {
      // First, look for an open instance
      Instance instance = getOpenInstance(areaType);

      // If there isn't one, we'll have to make it
      if (instance == null) {
         instance = createNewInstance(areaType);
      }

      // Set the player's instance ID
      player.instanceId = instance.id;

      // Add the player to the list
      instance.entities.Add(player);

      // If we just hit the max, we need to recheck which areas are open
      if (instance.getPlayerCount() >= instance.getMaxPlayers()) {
         recalculateOpenAreas();
      }

      // IF they're entering their farm, send them their crops
      if (areaType == Area.Type.Farm) {
         player.cropManager.loadCrops();
      }

      return instance;
   }

   public void addEnemyToInstance (Enemy enemy, Instance instance) {
      instance.entities.Add(enemy);
      enemy.instanceId = instance.id;
   }

   public Instance getInstance (int instanceId) {
      if (_instances.ContainsKey(instanceId)) {
         return _instances[instanceId];
      }

      return null;
   }

   protected Instance createNewInstance(Area.Type areaType) {
      Instance instance = Instantiate(instancePrefab, this.transform);
      instance.id = _id++;
      instance.areaType = areaType;
      instance.numberInArea = getInstanceCount(areaType) + 1;

      // Keep track of it
      _instances.Add(instance.id, instance);

      // Note the open Areas on this server
      recalculateOpenAreas();

      // Create any treasure we might want in the instance
      if (areaType == Area.Type.TreasurePine) {
         TreasureManager.self.createTreasureForInstance(instance);
      }

      // Create any Enemies that exist in this Instance
      EnemyManager.self.spawnEnemiesOnServerForInstance(instance);

      return instance;
   }

   public Instance getOpenInstance (Area.Type areaType) {
      foreach (Instance instance in _instances.Values) {
         if (instance.areaType == areaType && instance.getPlayerCount() < instance.getMaxPlayers()) {
            return instance;
         }
      }

      return null;
   }

   public void removeEntityFromInstance (NetEntity entity) {
      if (entity == null || entity.instanceId == 0) {
         D.log("No need to remove entity from instance: " + entity);
         return;
      }

      // Look up the Instance object, and remove the entity from that
      if (_instances.ContainsKey(entity.instanceId)) {
         Instance instance = _instances[entity.instanceId];
         instance.removeEntityFromInstance(entity);

         // Make the other entities in this instance update their observer list
         rebuildInstanceObservers(entity, instance);
      }

      // Make note of the change on our Server Network
      recalculateOpenAreas();

      // Mark the entity as no longer having an instance
      entity.instanceId = 0;
   }

   public void rebuildInstanceObservers (NetEntity newEntity, Instance instance) {
      // If this entity doesn't have a client connection, then all it needs to do is make itself visible to clients in the instance
      if (newEntity.connectionToClient == null) {
         newEntity.netIdent.RebuildObservers(false);
      } else {
         // Everything in the instance needs to update its observer list to include the new entity
         foreach (NetworkBehaviour instanceEntity in instance.entities) {
            if (instanceEntity != null) {
               instanceEntity.netIdentity.RebuildObservers(false);
            }
         }
      }
   }

   public Area.Type[] getOpenAreas () {
      HashSet<Area.Type> openAreas = new HashSet<Area.Type>();

      foreach (Instance instance in _instances.Values) {
         if (instance.getPlayerCount() < instance.getMaxPlayers()) {
            openAreas.Add(instance.areaType);
         }
      }

      Area.Type[] areaArray = new Area.Type[openAreas.Count];
      openAreas.CopyTo(areaArray);

      return areaArray;
   }

   public int getInstanceCount (Area.Type areaType) {
      int count = 0;

      foreach (Instance instance in _instances.Values) {
         if (instance.areaType == areaType) {
            count++;
         }
      }

      return count;
   }

   public void recalculateOpenAreas () {
      if (ServerNetwork.self.server != null) {
         ServerNetwork.self.server.openAreas = getOpenAreas();
      }
   }

   public void reset () {
      _instances.Clear();
   }

   public bool hasActiveInstanceForArea (Area.Type areaType) {
      foreach (Instance instance in _instances.Values) {
         if (instance.areaType == areaType && instance.entityCount > 0) {
            return true;
         }
      }

      return false;
   }

   public void removeEmptyInstance (Instance instance) {
      // Make sure there's no one inside
      if (instance.getPlayerCount() > 0) {
         D.log("Not going to destroy instance with active connections: " + instance.id);
         return;
      }

      // Make sure it exists in our mapping
      if (!_instances.ContainsKey(instance.id)) {
         D.log("Instance Manager does not contain instance id: " + instance.id);
         return;
      }

      // Destroy any bot entities
      foreach (NetworkBehaviour entity in instance.entities) {
         NetworkServer.Destroy(entity.gameObject);
      }

      // Remove it from our internal mapping
      _instances.Remove(instance.id);

      // Then destroy the instance
      Destroy(instance.gameObject);
   }

   #region Private Variables

   // The instance ID we're up to
   protected int _id = 1;

   // The instances we've created
   protected Dictionary<int, Instance> _instances = new Dictionary<int, Instance>();

   #endregion
}
