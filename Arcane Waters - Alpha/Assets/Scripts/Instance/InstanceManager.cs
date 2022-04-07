using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Linq;

public class InstanceManager : MonoBehaviour
{
   #region Public Variables

   // The prefab we use for creating Instances
   public Instance instancePrefab;

   // Self
   public static InstanceManager self;

   #endregion

   private void Awake () {
      self = this;
   }

   public Instance addPlayerToInstance (NetEntity player, string areaKey, int voyageId, int instanceToVisit = -1) {
      Instance instance = null;

      // Fetch the instance declared by server if there is any
      if (instanceToVisit > 0 && getInstance(instanceToVisit) != null) {
         instance = getInstance(instanceToVisit);
         if (instance != null) {
            D.adminLog("User {" + player.entityName + ":" + player.userId + "} " +
               "successfully fetched visit instance {" + instanceToVisit + ":" + areaKey + "} {" + instance.privateAreaUserId + "}", D.ADMIN_LOG_TYPE.Visit);
         } else {
            D.adminLog("User {" + player.entityName + ":" + player.userId + "} " +
               "Failed fetched visit instance {" + instanceToVisit + ":" + areaKey + "}", D.ADMIN_LOG_TYPE.Visit);
         }
      }

      // If the player is warping to a world map instance, search for it
      if (VoyageManager.isWorldMapArea(areaKey) && WorldMapManager.self.isWorldMapArea(areaKey)) {
         Instance existingInstance = getWorldMapOpenInstance(areaKey);
         if (existingInstance != null) {
            instance = existingInstance;
         }
      }

      // If the player is warping to a voyage instance, search for it
      if (voyageId != -1) {
         if (VoyageManager.isAnyLeagueArea(areaKey) || VoyageManager.isPvpArenaArea(areaKey)) {
            if (!tryGetVoyageInstance(voyageId, out instance)) {
               D.error("Could not find the voyage instance for voyage id " + voyageId + " in area " + areaKey);
            }

            // TODO: Generate a new voyage for user here
            int voyageInstanceId = VoyageManager.self.getPvpInstanceId(areaKey);
            if (voyageInstanceId > 0) {
               Instance inst = getInstance(voyageInstanceId);
               if (inst != null) {
                  instance = inst;
               } else {
                  D.debug("Failed to get voyage instance: " + voyageInstanceId);
               }
            }
         } else if (VoyageManager.isTreasureSiteArea(areaKey)) {
            // Search for the parent sea instance
            if (tryGetVoyageInstance(voyageId, out Instance seaVoyageInstance)) {
               // Search for the treasure site entrance the player is registered in
               foreach (TreasureSite treasureSite in seaVoyageInstance.treasureSites) {
                  if (treasureSite.playerListInSite.Contains(player.userId)) {
                     // Check if the treasure site instance has already been created
                     if (treasureSite.destinationInstanceId > 0) {
                        D.adminLog("Created New Instance for Treasure Site", D.ADMIN_LOG_TYPE.InstanceProcess);
                        instance = getInstance(treasureSite.destinationInstanceId);

                        if (instance != null) {
                           D.adminLog("TreasureSite instance being fetched here, " +
                              "TreasureSite: {" + treasureSite.areaKey + "} {" + treasureSite.instanceId + "} {" + treasureSite.destinationInstanceId + "} " +
                              "PrevArea: {" + areaKey + " } {" + player.areaKey + "}  {" + player.instanceId + "}" +
                              "Instance: {" + instance.id + "}{" + instance.areaKey + "}{" + instance.voyageId + "}", D.ADMIN_LOG_TYPE.POI_WARP);

                           if (areaKey != instance.areaKey) {
                              D.adminLog("Created New Instance for POI, area keys did not match for " +
                                 "InstanceId:{" + instance.id + "} InstanceArea:{" + instance.areaKey + "} TargetArea:{" + areaKey + "}", D.ADMIN_LOG_TYPE.POI_WARP);

                              // If the treasure site instance doesn't exist yet, create it
                              instance = createNewInstance(areaKey, false, voyageId, seaVoyageInstance.leagueExitAreaKey, seaVoyageInstance.leagueExitSpawnKey, seaVoyageInstance.leagueExitFacingDirection);
                              treasureSite.destinationInstanceId = instance.id;
                           }
                        }
                     } else {
                        D.adminLog("Created New Instance for Non-Treasure Site", D.ADMIN_LOG_TYPE.InstanceProcess);
                        // If the treasure site instance doesn't exist yet, create it
                        instance = createNewInstance(areaKey, false, voyageId, seaVoyageInstance.leagueExitAreaKey, seaVoyageInstance.leagueExitSpawnKey, seaVoyageInstance.leagueExitFacingDirection);
                        treasureSite.destinationInstanceId = instance.id;
                     }
                     break;
                  }
               }

               // If the user is not registered in a treasure site entrance, try to find an existing instance with the same voyageId
               if (instance == null) {
                  tryGetTreasureSiteInstance(voyageId, areaKey, out instance);
               }

               // If all else fails, create a new instance (useful for /admin warp commands)
               if (instance == null) {
                  instance = createNewInstance(areaKey, false, voyageId, "", "", Direction.South);
                  D.adminLog("Created New Instance for Voyage", D.ADMIN_LOG_TYPE.InstanceProcess);
               }
            }
         }
      } else {
         if (VoyageManager.isAnyLeagueArea(areaKey) || VoyageManager.isPvpArenaArea(areaKey)) {
            int voyageInstanceId = VoyageManager.self.getPvpInstanceId(areaKey);
            if (voyageInstanceId > 0) {
               Instance inst = getInstance(voyageInstanceId);
               if (inst != null) {
                  instance = inst;
               }
            }
         }
      }

      // Search for an existing private instance assigned to this user or create a new one
      if (AreaManager.self.isPrivateArea(areaKey)) {
         instance = getPlayerPrivateOpenInstance(areaKey, player.userId);
         if (instance == null) {
            instance = createNewInstance(areaKey, player.isSinglePlayer);
            // Register the user id as the privateAreaUserId
            instance.privateAreaUserId = player.userId;
         }
      }

      if (instance == null) {
         // Look for an open instance
         instance = getOpenInstance(areaKey, player.isSinglePlayer);
         if (instance != null) {
            D.adminLog("Get Open Instance", D.ADMIN_LOG_TYPE.InstanceProcess);
         }
      }

      // Fetch existing visitable custom map here if the target area is a custom map, for visit feature
      if (instance == null && CustomMapManager.isPrivateCustomArea(areaKey)) {
         Instance openInst = getVisitablePrivateOpenInstance(areaKey);
         if (openInst) {
            instance = openInst;
         }
      }

      // If there isn't one, we'll have to make it
      if (instance == null) {
         instance = createNewInstance(areaKey, player.isSinglePlayer);
         if (CustomMapManager.isPrivateCustomArea(areaKey)) {
            instance.privateAreaUserId = player.userId;
         }
      }

      // Set the player's instance ID
      player.instanceId = instance.id;

      // Add the player to the list
      instance.entities.Add(player);

      // If player is entering a farm, we need to load plantable trees for him
      if (AreaManager.self.tryGetCustomMapManager(areaKey, out CustomMapManager cmm)) {
         if (cmm is CustomFarmManager || cmm is CustomGuildMapManager) {
            PlantableTreeManager.self.playerEnteredFarm(player, areaKey);
         }
      }

      // Allow discovery manager to react
      DiscoveryManager.self.userEntersInstance(player);

      // If they're entering their farm, send them their crops
      if (AreaManager.self.tryGetCustomMapManager(areaKey, out CustomMapManager customMapManager)) {
         CustomFarmManager farmManager = customMapManager as CustomFarmManager;
         CustomGuildMapManager guildMapManager = customMapManager as CustomGuildMapManager;
         if (farmManager != null) {
            int userId = CustomMapManager.isUserSpecificAreaKey(areaKey) ? CustomMapManager.getUserId(areaKey) : player.userId;
            player.cropManager.loadCrops(userId);
         } else if (guildMapManager != null) {
            int guildId = CustomMapManager.getGuildId(areaKey);
            player.cropManager.loadGuildCrops(guildId);
            D.debug("Entered custom guild map. Loading crops for guild: " + guildId);
         }
      } else {
         // TODO: Update this block of code after setting up farm maps
         if (areaKey.ToLower().Contains("farm")) {
            player.cropManager.loadCrops(player.userId);
         }
      }

      return instance;
   }

   public void addCustomizationManagerToInstance (MapCustomizationManager mapCustomizationManager, Instance instance) {
      instance.entities.Add(instance);
      instance.mapCustomizationManager = mapCustomizationManager;
      mapCustomizationManager.instanceId = instance.id;
   }

   public void addDroppedItemToInstance (DroppedItem item, Instance instance) {
      instance.entities.Add(item);
      item.instanceId = instance.id;
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

   public void addInteractableToInstance (InteractableObjEntity objEntity, Instance instance) {
      instance.entities.Add(objEntity);
      instance.interactableObject.Add(objEntity);
      objEntity.instanceId = instance.id;
   }

   public void addSeaStructureToInstance (SeaStructure seaStructure, Instance instance) {
      instance.entities.Add(seaStructure);
      instance.seaStructures.Add(seaStructure);
      seaStructure.instanceId = instance.id;
      instance.seaStructureCount++;
   }

   public void addSeaEntityToInstance (SeaEntity seaEntity, Instance instance) {
      instance.entities.Add(seaEntity);
      seaEntity.instanceId = instance.id;
   }

   public void addSeaMonsterSpawnerToInstance (PvpMonsterSpawner monsterSpawner, Instance instance) {
      instance.entities.Add(monsterSpawner);
      instance.pvpMonsterSpawners.Add(monsterSpawner);
      monsterSpawner.instanceId = instance.id;
   }

   public void addWindowToInstance (WindowInteractable windowInteractable, Instance instance) {
      instance.entities.Add(windowInteractable);
      windowInteractable.instanceId = instance.id;
   }

   public void addPvpWaypointToInstance (PvpWaypoint waypoint, Instance instance) {
      instance.pvpWaypoints.Add(waypoint);
   }

   public void addLootSpawnerToInstance (PvpLootSpawn lootSpawn, Instance instance) {
      instance.entities.Add(lootSpawn);
      instance.lootSpawners.Add(lootSpawn);
      lootSpawn.instanceId = instance.id;
   }

   public Instance getInstance (int instanceId) {
      if (_instances.ContainsKey(instanceId)) {
         return _instances[instanceId];
      }

      return null;
   }

   public bool tryGetInstance (int instanceId, out Instance instance) {
      if (_instances.TryGetValue(instanceId, out instance)) {
         return true;
      }

      return false;
   }

   public List<Instance> getAllInstances () {
      return _instances.Values.ToList();
   }

   public Instance createNewInstance (string areaKey, bool isSinglePlayer) {
      return createNewInstance(areaKey, isSinglePlayer, -1, "", "", Direction.South);
   }

   public Instance createNewInstance (string areaKey, bool isSinglePlayer, int voyageId, string leagueExitAreaKey, string leagueExitSpawnKey, Direction leagueExitFacingDirection) {
      int difficulty = getDifficultyForInstance(voyageId);
      return createNewInstance(areaKey, isSinglePlayer, voyageId, difficulty, leagueExitAreaKey, leagueExitSpawnKey, leagueExitFacingDirection);
   }

   public Instance createNewInstance (string areaKey, bool isSinglePlayer, int voyageId, int difficulty, string leagueExitAreaKey, string leagueExitSpawnKey, Direction leagueExitFacingDirection) {
      Biome.Type biome = getBiomeForInstance(areaKey, voyageId);
      return createNewInstance(areaKey, isSinglePlayer, false, voyageId, false, false, 0, -1, leagueExitAreaKey, leagueExitSpawnKey, leagueExitFacingDirection, difficulty, biome);
   }

   public Instance createNewInstance (string areaKey, bool isSinglePlayer, bool isVoyage, int voyageId, bool isPvP, bool isLeague, int leagueIndex, int leagueRandomSeed, string leagueExitAreaKey, string leagueExitSpawnKey, Direction leagueExitFacingDirection, int difficulty, Biome.Type biome) {
      Instance instance = Instantiate(instancePrefab, this.transform);
      instance.id = _id++;
      instance.areaKey = areaKey;
      instance.numberInArea = getInstanceCount(areaKey) + 1;
      instance.serverAddress = MyNetworkManager.self.networkAddress;
      instance.serverPort = MyNetworkManager.self.telepathy.port;
      instance.mapSeed = UnityEngine.Random.Range(0, 1000000);
      instance.creationDate = DateTime.UtcNow.ToBinary();
      instance.isVoyage = isVoyage;
      instance.isLeague = isLeague;
      instance.leagueIndex = leagueIndex;
      instance.leagueRandomSeed = leagueRandomSeed;
      instance.leagueExitAreaKey = leagueExitAreaKey;
      instance.leagueExitSpawnKey = leagueExitSpawnKey;
      instance.leagueExitFacingDirection = leagueExitFacingDirection;
      instance.voyageId = voyageId;
      instance.biome = biome == Biome.Type.None ? AreaManager.self.getDefaultBiome(areaKey) : biome;
      instance.isPvP = isPvP;
      instance.difficulty = difficulty;

      instance.updateMaxPlayerCount(isSinglePlayer);

      if (CustomMapManager.isPrivateCustomArea(areaKey)) {
         int ownerUserId = CustomMapManager.getUserId(areaKey);

         if (instance.areaKey.Contains(CustomFarmManager.GROUP_AREA_KEY)) {
            ServerNetworkingManager.self.addPrivateFarmInstance(ownerUserId);
         } else if (instance.areaKey.Contains(CustomHouseManager.GROUP_AREA_KEY)) {
            ServerNetworkingManager.self.addPrivateHouseInstance(ownerUserId);
         }

         D.adminLog("Generated new instance: {" + areaKey + "} Owner:{" + ownerUserId + "} InstanceId:{" + instance.id + "}", D.ADMIN_LOG_TYPE.Visit);
      }

      // Keep track of it
      _instances.Add(instance.id, instance);

      // Update the instance count in the server network
      if (ServerNetworkingManager.self != null && ServerNetworkingManager.self.server != null) {
         D.adminLog("Registering Instance {" + ServerNetworkingManager.self.server.networkedPort.Value + "} for area {" + areaKey + "}", D.ADMIN_LOG_TYPE.InstanceProcess);
         ServerNetworkingManager.self.server.areaToInstanceCount[areaKey] = getInstanceCount(areaKey);
      }

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

   public Instance getWorldMapOpenInstance (string areaKey) {
      foreach (Instance instance in _instances.Values) {
         if (WorldMapManager.self.isWorldMapArea(instance.areaKey)) {
            if (instance.areaKey == areaKey && instance.getPlayerCount() < instance.getMaxPlayers()) {
               return instance;
            }
         }
      }

      return null;
   }

   public Instance getPlayerPrivateOpenInstance (string areaKey, int userId) {
      foreach (Instance instance in _instances.Values) {
         if (instance.areaKey == areaKey && instance.getPlayerCount() < instance.getMaxPlayers() && instance.privateAreaUserId == userId) {
            return instance;
         }
      }

      return null;
   }

   public Instance getVisitablePrivateOpenInstance (string areaKey) {
      foreach (Instance instance in _instances.Values) {
         if (instance.areaKey == areaKey) {
            return instance;
         }
      }

      return null;
   }

   public Instance getVisitablePrivateOpenInstance (int userId) {
      foreach (Instance instance in _instances.Values) {
         if (instance.getPlayerUserIds().Exists(_ => _ == userId)) {
            return instance;
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

   public string[] getAreas () {
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

   public List<Instance> getTreasureSiteInstancesLinkedToVoyages () {
      List<Instance> voyages = new List<Instance>();

      foreach (Instance instance in _instances.Values) {
         if (VoyageManager.isTreasureSiteArea(instance.areaKey) && instance.voyageId > 0) {
            voyages.Add(instance);
         }
      }

      return voyages;
   }

   public bool tryGetVoyageInstance (int voyageId, out Instance instance) {
      instance = null;

      foreach (Instance i in _instances.Values) {
         if (i.isVoyage && i.voyageId == voyageId) {
            instance = i;
            return true;
         }
      }

      return false;
   }

   public bool tryGetTreasureSiteInstance (int voyageId, string areaKey, out Instance instance) {
      instance = null;

      foreach (Instance i in _instances.Values) {
         if (VoyageManager.isTreasureSiteArea(i.areaKey) && i.voyageId == voyageId && string.Equals(i.areaKey, areaKey)) {
            instance = i;
            return true;
         }
      }

      return false;
   }

   public static Biome.Type getBiomeForInstance (string areaKey, int voyageId) {
      // Voyages can have a biome different than the default for the area
      if (VoyageManager.isAnyLeagueArea(areaKey) || VoyageManager.isPvpArenaArea(areaKey) || VoyageManager.isTreasureSiteArea(areaKey)) {
         if (VoyageManager.self.tryGetVoyage(voyageId, out Voyage voyage)) {
            return voyage.biome;
         }
      }

      return AreaManager.self.getDefaultBiome(areaKey);
   }

   public static int getDifficultyForInstance (int voyageId) {
      if (VoyageManager.self.tryGetVoyage(voyageId, out Voyage voyage)) {
         return voyage.difficulty;
      }

      return 1;
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

   public Instance getClientInstance (int playerInstanceId) {
      // TODO: Confirm if registering each instance on startup for each client is ideal than getting component in children, since instance registry only takes place in server side(createNewInstance())
      List<Instance> instances = GetComponentsInChildren<Instance>().ToList();
      return instances.Find(_ => _.id == playerInstanceId);
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

      // Destroy any instance-specific entities
      foreach (NetworkBehaviour entity in instance.entities) {
         if (entity != null) {
            NetworkServer.Destroy(entity.gameObject);
         }
      }

      // If it was a pvp instance, remove it from the pvp manager's internal mapping
      if (instance.isPvP) {
         PvpManager.self.tryRemoveEmptyGame(instance.id);
      }

      // Remove it from our internal mapping
      if (CustomMapManager.isPrivateCustomArea(instance.areaKey)) {
         if (instance.areaKey.Contains(CustomFarmManager.GROUP_AREA_KEY)) {
            ServerNetworkingManager.self.releasePrivateFarmInstance(CustomMapManager.getUserId(instance.areaKey));
         } else if (instance.areaKey.Contains(CustomHouseManager.GROUP_AREA_KEY)) {
            ServerNetworkingManager.self.releasePrivateHouseInstance(CustomMapManager.getUserId(instance.areaKey));
         }
      }
      _instances.Remove(instance.id);

      // Remove the instance from the server network if it is a voyage
      ServerNetworkingManager.self.server.removeVoyageInstance(instance);

      // Update the instance count in the server network
      ServerNetworkingManager.self.server.areaToInstanceCount[instance.areaKey] = getInstanceCount(instance.areaKey);

      // Then destroy the instance
      NetworkServer.Destroy(instance.gameObject);
   }

   public int getPlayerCountInInstance (int instanceId) {
      Instance instance = getInstance(instanceId);

      if (instance == null) {
         return 0;
      }

      int playerCount = instance.getPlayerCount();
      return playerCount;
   }

   public int getPlayerCountAllInstances () {
      int total = 0;
      foreach (KeyValuePair<int, Instance> pair in _instances) {
         total += getPlayerCountInInstance(pair.Key);
      }
      return total;
   }

   public List<InstanceOverview> createOverviewForAllInstances () {
      return _instances
         .Select(kv => new InstanceOverview { area = kv.Value.areaKey, count = getPlayerCountInInstance(kv.Key) })
         .ToList();
   }

   #region Private Variables

   // The instance ID we're up to
   protected int _id = 1;

   // The instances we've created
   protected Dictionary<int, Instance> _instances = new Dictionary<int, Instance>();

   #endregion
}