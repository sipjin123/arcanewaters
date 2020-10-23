using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Linq;

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

   public Instance addPlayerToInstance (NetEntity player, string areaKey, int voyageId) {
      Instance instance = null;

      // If the player is warping to a voyage instance, search for it
      if (voyageId != -1 && VoyageManager.self.isVoyageArea(areaKey)) {
         instance = getVoyageInstance(voyageId);
         if (instance == null) {
            D.error("Could not find the voyage instance for voyage id " + voyageId);
         }
      }

      // For private areas, we always create a new instance, to ensure that players don't join instances of other players
      if (AreaManager.self.isPrivateArea(areaKey)) {
         instance = createNewInstance(areaKey, player.isSinglePlayer);
      }

      if (instance == null) {
         // Look for an open instance
         instance = getOpenInstance(areaKey, player.isSinglePlayer);
      }

      // If there isn't one, we'll have to make it
      if (instance == null) {
         instance = createNewInstance(areaKey, player.isSinglePlayer);
      }

      // Set the player's instance ID
      player.instanceId = instance.id;

      // Add the player to the list
      instance.entities.Add(player);

      // If we just hit the max number of players, we need to recheck which areas are open
      if (instance.getPlayerCount() >= instance.getMaxPlayers()) {
         recalculateOpenAreas();
      }

      // If they're entering their farm, send them their crops
      if (AreaManager.self.tryGetCustomMapManager(areaKey, out CustomMapManager customMapManager)) {
         CustomFarmManager farmManager = customMapManager as CustomFarmManager;
         if (farmManager != null) {
            int userId = CustomMapManager.isUserSpecificAreaKey(areaKey) ? CustomMapManager.getUserId(areaKey) : player.userId;
            if (userId == player.userId) {
               player.cropManager.loadCrops();
            } 
         }
      } else {
         // TODO: Update this block of code after setting up farm maps
         if (areaKey.ToLower().Contains("farm")) {
            player.cropManager.loadCrops();
         }
      }
      return instance;
   }

   public void addDiscoveryToInstance (Discovery discovery, Instance instance) {
      instance.entities.Add(discovery);
      discovery.instanceId = instance.id;      
   }

   public void addEnemyToInstance (Enemy enemy, Instance instance) {
      instance.entities.Add(enemy);
      enemy.instanceId = instance.id;
      instance.enemyCount++;
   }

   public void addNPCToInstance (NPC npc, Instance instance) {
      instance.entities.Add(npc);
      npc.instanceId = instance.id;
      instance.npcCount++;
   }

   public void addBotShipToInstance (BotShipEntity botShip, Instance instance) {
      instance.entities.Add(botShip);
      botShip.instanceId = instance.id;
   }

   public SecretEntranceHolder getSecretEntranceInstance (int instanceId, int spawnId) {
      NetworkBehaviour secretEntrance = getInstance(instanceId).entities.Find(_ => _ is SecretEntranceHolder && ((SecretEntranceHolder)_).spawnId == spawnId);
      if (secretEntrance == null) {
         D.debug("Cant find secret entrance with spawn id: {" + spawnId + "} in instance {" + instanceId + "}");
      }
      return (SecretEntranceHolder) secretEntrance;
   }

   public void addSeaMonsterToInstance (SeaEntity seaMonster, Instance instance) {
      instance.entities.Add(seaMonster);
      seaMonster.instanceId = instance.id;
      instance.seaMonsterCount++;
   }

   public void addTreasureSiteToInstance (TreasureSite treasureSite, Instance instance) {
      instance.entities.Add(treasureSite);
      instance.treasureSites.Add(treasureSite);
      treasureSite.instanceId = instance.id;
      instance.treasureSiteCount++;
   }

   public Instance getInstance (int instanceId) {
      if (_instances.ContainsKey(instanceId)) {
         return _instances[instanceId];
      }

      return null;
   }

   public Instance createNewInstance (string areaKey, bool isSinglePlayer) {
      return createNewInstance(areaKey, isSinglePlayer, false, -1, false, Voyage.Difficulty.None);
   }

   public Instance createNewInstance (string areaKey, bool isSinglePlayer, bool isVoyage, int voyageId, bool isPvP,
      Voyage.Difficulty difficulty) {
      Instance instance = Instantiate(instancePrefab, this.transform);
      instance.id = _id++;
      instance.areaKey = areaKey;
      instance.numberInArea = getInstanceCount(areaKey) + 1;
      instance.serverAddress = MyNetworkManager.self.networkAddress;
      instance.serverPort = MyNetworkManager.self.telepathy.port;
      instance.mapSeed = UnityEngine.Random.Range(0, 1000000);
      instance.creationDate = DateTime.UtcNow.ToBinary();
      instance.isVoyage = isVoyage;
      instance.voyageId = voyageId;
      instance.isPvP = isPvP;
      instance.difficulty = difficulty;

      instance.updateMaxPlayerCount(isSinglePlayer);

      // Keep track of it
      _instances.Add(instance.id, instance);

      // Note the open Areas on this server
      recalculateOpenAreas();

      // Spawn the network object on the Clients
      if (NetworkServer.active) {
         NetworkServer.Spawn(instance.gameObject);
      }
      return instance;
   }

   public Instance getOpenInstance (string areaKey, bool isSinglePlayer) {
      if (!isSinglePlayer) {
         foreach (Instance instance in _instances.Values) {
            if (instance.areaKey == areaKey && instance.getPlayerCount() < instance.getMaxPlayers()) {
               return instance;
            }
         }
      }

      return null;
   }

   public void removeEntityFromInstance (NetEntity entity) {
      if (entity == null || entity.instanceId == 0) {
         D.debug("No need to remove entity from instance: " + entity);
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

   public string[] getAreas() {
      HashSet<string> areas = new HashSet<string>();

      foreach (Instance instance in _instances.Values) {
            areas.Add(instance.areaKey);
      }

      string[] areaArray = new string[areas.Count];
      areas.CopyTo(areaArray);
      
      return areaArray;
   }

   public List<Instance> getVoyageInstances () {
      List<Instance> voyages = new List<Instance>();

      foreach (Instance instance in _instances.Values) {
         if (instance.isVoyage) {
            voyages.Add(instance);
         }
      }

      return voyages;
   }

   public Instance getVoyageInstance (int voyageId) {
      foreach (Instance instance in _instances.Values) {
         if (instance.isVoyage && instance.voyageId == voyageId) {
            return instance;
         }
      }

      return null;
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