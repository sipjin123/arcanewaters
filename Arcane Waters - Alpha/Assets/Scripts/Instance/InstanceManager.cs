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

   public Instance addPlayerToInstance (NetEntity player, string areaKey) {
      // First, look for an open instance
      Instance instance = getOpenInstance(areaKey);

      // If there isn't one, we'll have to make it
      if (instance == null) {
         instance = createNewInstance(areaKey, Biome.Type.None);
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
      if (Area.FARM.Equals(areaKey)) {
         player.cropManager.loadCrops();
      }

      return instance;
   }

   public void addEnemyToInstance (Enemy enemy, Instance instance) {
      instance.entities.Add(enemy);
      enemy.instanceId = instance.id;
   }

   public void addSeaMonsterToInstance (SeaMonsterEntity seaMonster, Instance instance) {
      instance.entities.Add(seaMonster);
      seaMonster.instanceId = instance.id;
   }

   public Instance getInstance (int instanceId) {
      if (_instances.ContainsKey(instanceId)) {
         return _instances[instanceId];
      }

      return null;
   }

   public Instance createNewInstance (string areaKey, Biome.Type biomeType) {
      Instance instance = Instantiate(instancePrefab, this.transform);
      instance.id = _id++;
      instance.areaKey = areaKey;
      instance.biomeType = biomeType;
      instance.numberInArea = getInstanceCount(areaKey) + 1;
      instance.serverAddress = MyNetworkManager.self.networkAddress;
      instance.serverPort = MyNetworkManager.self.telepathy.port;
      instance.mapSeed = Random.Range(0, 1000000);

      // Keep track of it
      _instances.Add(instance.id, instance);

      // Note the open Areas on this server
      recalculateOpenAreas();

      // Create any treasure we might want in the instance
      if (Area.TREASURE_PINE.Equals(areaKey)) {
         // Create any treasure and ore we might want in the instance
         if (Area.isTreasureSite(areaKey)) {
            TreasureManager.self.createTreasureForInstance(instance);
            OreManager.self.createOreNodesForInstance(instance);
         }
      }

      // Create any Enemies that exist in this Instance
      EnemyManager.self.spawnEnemiesOnServerForInstance(instance);

      // Spawn the network object on the Clients
      NetworkServer.Spawn(instance.gameObject);
      return instance;
   }

   public Instance getOpenInstance (string areaKey) {
      foreach (Instance instance in _instances.Values) {
         if (instance.areaKey == areaKey && instance.getPlayerCount() < instance.getMaxPlayers()) {
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

         // We also need the Instance object to be viewable by the new player
         instance.netIdent.RebuildObservers(false);
      }
   }

   public string[] getOpenAreas () {
      HashSet<string> openAreas = new HashSet<string>();

      foreach (Instance instance in _instances.Values) {
         if (instance.getPlayerCount() < instance.getMaxPlayers()) {
            openAreas.Add(instance.areaKey);
         }
      }

      string[] areaArray = new string[openAreas.Count];
      openAreas.CopyTo(areaArray);

      return areaArray;
   }

   public int getInstanceCount (string areaKey) {
      int count = 0;

      foreach (Instance instance in _instances.Values) {
         if (instance.areaKey == areaKey) {
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

   public bool hasActiveInstanceForArea (string areaKey) {
      foreach (Instance instance in _instances.Values) {
         if (instance.areaKey == areaKey && instance.entityCount > 0) {
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
      NetworkServer.Destroy(instance.gameObject);
   }

   #region Private Variables

   // The instance ID we're up to
   protected int _id = 1;

   // The instances we've created
   protected Dictionary<int, Instance> _instances = new Dictionary<int, Instance>();

   #endregion
}