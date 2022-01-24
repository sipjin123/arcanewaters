using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using System;
using System.Linq;

[Serializable]
public class Instance : NetworkBehaviour
{
   #region Public Variables

   // The id of this instance
   [SyncVar]
   public int id;

   // The key determining the type of area this instance is
   [SyncVar]
   public string areaKey;

   // The seed used for making based on pseudo-random numbers on clients
   [SyncVar]
   public int mapSeed;

   // If shops should show by now, if any
   [SyncVar]
   public bool showShops;

   // The list of Entities in this instance (server only)
   public List<NetworkBehaviour> entities = new List<NetworkBehaviour>();

   // The list of treasure sites in this instance (server only)
   public List<TreasureSite> treasureSites = new List<TreasureSite>();

   // The list of sea structures in this instance (server only)
   public List<SeaStructure> seaStructures = new List<SeaStructure>();

   // The list of pvp waypoints in this instance (server only)
   public List<PvpWaypoint> pvpWaypoints = new List<PvpWaypoint>();

   // The list of pvp loot spawners in this instance (server only)
   public List<PvpLootSpawn> lootSpawners = new List<PvpLootSpawn>();

   // The list of pvp monster spawners in this instance (server only)
   public List<PvpMonsterSpawner> pvpMonsterSpawners = new List<PvpMonsterSpawner>();

   // For debugging in the Editor
   [SyncVar]
   public int entityCount;

   // The maximum number of players allowed in the instance
   [SyncVar]
   public int maxPlayerCount = 50;

   // For the number of enemies in the instance
   [SyncVar]
   public int enemyCount;

   // For the number of seamonsters in the instance
   [SyncVar]
   public int seaMonsterCount;

   // For the number of npc in the instance
   [SyncVar]
   public int npcCount;

   // For the number of sea structures in the instance
   [SyncVar]
   public int seaStructureCount;

   // The number of alive npc enemies in the instance (sea or land enemies)
   [SyncVar]
   public int aliveNPCEnemiesCount = -1;

   // For the number of treasure sites in the instance
   [SyncVar]
   public int treasureSiteCount = 0;

   // For the number of captured treasure sites in the instance
   [SyncVar]
   public int capturedTreasureSiteCount = 0;

   // Gets set to true when the land maps accessed through treasure sites have been cleared of enemies
   [SyncVar]
   public bool areAllTreasureSitesClearedOfEnemies = false;

   // The number assigned to this instance based on the area type
   [SyncVar]
   public int numberInArea;

   // The server address for this Instance
   [SyncVar]
   public string serverAddress;

   // The server port for this Instance
   [SyncVar]
   public int serverPort;

   // The creation time of the instance
   [SyncVar]
   public long creationDate;

   // Gets set to true when the instance holds a voyage area
   [SyncVar]
   public bool isVoyage = false;

   // Gets set to true when the instance holds a voyage area that is part of a league (series of voyage maps)
   [SyncVar]
   public bool isLeague = false;

   // The index of this voyage instance in the league series
   [SyncVar]
   public int leagueIndex = 0;

   // The random seed used to create the league map series
   [SyncVar]
   public int leagueRandomSeed = -1;

   // The area key where users are warped to when exiting a league
   public string leagueExitAreaKey = "";

   // The spawn key where users are warped to when exiting a league
   public string leagueExitSpawnKey = "";

   // The facing direction when exiting a league
   public Direction leagueExitFacingDirection = Direction.South;

   // The unique identifier of the voyage
   [SyncVar]
   public int voyageId = 0;

   // Gets set to true when the voyage instance is player vs player
   [SyncVar]
   public bool isPvP = false;

   // The voyage difficulty
   [SyncVar]
   public int difficulty = 1;

   [SyncVar]
   // The biome of this instance
   public Biome.Type biome = Biome.Type.None;

   // Gets set to true when the NetworkBehaviours specific to this instance are spawned
   [SyncVar]
   public bool isNetworkPrefabInstantiationFinished = false;

   // Player that is currently customizing the area
   public NetEntity playerMakingCustomizations;

   // Our network ident
   public NetworkIdentity netIdent;

   // The user id registered to this area if this is a private area
   public int privateAreaUserId;

   // The number of times the instance must be found empty before being removed (see checkIfInstanceIsEmpty for time between checks)
   public const int CHECKS_BEFORE_REMOVAL = 10;

   #endregion

   public void Awake () {
      // Look up components
      netIdent = GetComponent<NetworkIdentity>();
   }

   private void Start () {
      // On clients, set the instance manager as the parent transform
      if (isClient) {
         transform.SetParent(InstanceManager.self.transform);
      }

      // Spawn all the area prefabs that are specific to this instance
      if (NetworkServer.active) {
         StartCoroutine(CO_SpawnInstanceSpecificPrefabs());
      }

      // Routinely check if the instance is empty
      InvokeRepeating(nameof(checkIfInstanceIsEmpty), 10f, 30f);

      // Routinely count the number of enemies that are still alive
      if (voyageId > 0) {
         InvokeRepeating(nameof(countAliveEnemies), 0f, 1f);
      } else {
         if (this.areaKey.ToLower().Contains("tutorial")) {
            // Enable shops when all enemies have been eliminated
            Area area = AreaManager.self.getArea(this.areaKey);
            if (area != null) {
               area.enableSeaShops();
            }
         }
      }

      // Update this instance in the server network
      if (NetworkServer.active && voyageId > 0) {
         InvokeRepeating(nameof(updateInServerNetwork), UnityEngine.Random.Range(0f, 1f), 1f);
      }
   }

   [Server]
   private void updateInServerNetwork () {
      if (!NetworkServer.active || ServerNetworkingManager.self == null || ServerNetworkingManager.self.server == null) {
         return;
      }

      ServerNetworkingManager.self.server.updateVoyageInstance(this);
   }

   public int getPlayerCount () {
      int count = 0;

      foreach (NetworkBehaviour entity in entities) {
         if (entity != null && (entity is PlayerBodyEntity || entity is PlayerShipEntity)) {
            count++;
         }
      }

      return count;
   }

   public int getTotalNPCEnemyCount () {
      return seaMonsterCount + enemyCount;
   }

   public void registerClientPlayerBody (NetworkBehaviour entity) {
      this.entities.Add(entity);
   }

   public List<NetworkBehaviour> getEntities () {
      return entities;
   }

   public List<NetworkBehaviour> getOreEntities () {
      return entities.FindAll(_ => _ is OreNode);
   }

   public List<PlayerBodyEntity> getPlayerBodyEntities () {
      List<PlayerBodyEntity> newEntityList = new List<PlayerBodyEntity>();

      // Gathers all the player bodies in the area
      foreach (NetworkBehaviour existingEntity in entities) {
         if (existingEntity is PlayerBodyEntity) {
            newEntityList.Add((PlayerBodyEntity) existingEntity);
         }
      }
      return newEntityList;
   }

   public List<PlayerShipEntity> getPlayerShipEntities () {
      List<PlayerShipEntity> newEntityList = new List<PlayerShipEntity>();

      // Gathers all the player ships in the area
      foreach (NetworkBehaviour existingEntity in entities) {
         if (existingEntity is PlayerShipEntity) {
            newEntityList.Add((PlayerShipEntity) existingEntity);
         }
      }
      return newEntityList;
   }

   public List<int> getPlayerUserIds () {
      List<int> userIdList = new List<int>();

      foreach (NetworkBehaviour entity in entities) {
         NetEntity netEntity = entity as NetEntity;
         if (netEntity != null && netEntity.isPlayerEntity()) {
            userIdList.Add(netEntity.userId);
         }
      }

      return userIdList;
   }

   public int getMaxPlayers () {
      return maxPlayerCount;
   }

   public void updateMaxPlayerCount (bool isSinglePlayer = false) {
      maxPlayerCount = getMaxPlayerCount(areaKey, isSinglePlayer);
   }

   public static int getMaxPlayerCount (string areaKey, bool isSinglePlayer = false) {
      if (AreaManager.self.tryGetCustomMapManager(areaKey, out CustomMapManager customMapManager)) {
         if (customMapManager is CustomFarmManager || customMapManager is CustomHouseManager) {
            return 1;
         }
      }

      if (isSinglePlayer || AreaManager.self.isPrivateArea(areaKey)) {
         return 1;
      }

      if (VoyageManager.isAnyLeagueArea(areaKey) || VoyageManager.isPvpArenaArea(areaKey) || VoyageManager.isTreasureSiteArea(areaKey)) {
         return Voyage.MAX_PLAYERS_PER_INSTANCE;
      }

      // Check if we've specified a max player count on the command line
      if (CommandCodes.get(CommandCodes.Type.MAX_INSTANCE_PLAYERS)) {
         return Util.getCommandLineInt(CommandCodes.Type.MAX_INSTANCE_PLAYERS + "");
      }

      return 50;
   }

   public void removeEntityFromInstance (NetworkBehaviour entity) {
      if (entities.Contains(entity)) {
         this.entities.Remove(entity);
      }
   }

   protected void checkIfInstanceIsEmpty () {
      // We only do this on the server
      if (!NetworkServer.active) {
         return;
      }

      // Voyage instances always exist for at least their starting 'open' duration
      if (isVoyage && !isLeague) {
         TimeSpan timeSinceStart = DateTime.UtcNow.Subtract(DateTime.FromBinary(creationDate));
         if (timeSinceStart.TotalSeconds < Voyage.INSTANCE_OPEN_DURATION) {
            return;
         }
      }

      // Sea voyage and treasure site instances exist as long as there is a group linked to them
      if ((VoyageManager.isAnyLeagueArea(areaKey) || VoyageManager.isPvpArenaArea(areaKey) || VoyageManager.isTreasureSiteArea(areaKey))
         && VoyageGroupManager.self.isAtLeastOneGroupInVoyage(voyageId)) {
         return;
      }

      // If there's no one in the instance right now, increase the  count
      if (getPlayerCount() <= 0) {
         _consecutiveEmptyChecks++;

         // If the Instance has been empty for long enough, just remove it
         if (_consecutiveEmptyChecks > CHECKS_BEFORE_REMOVAL) {
            InstanceManager.self.removeEmptyInstance(this);
         }
      } else {
         // There's someone in the instance, so reset the counter
         _consecutiveEmptyChecks = 0;
      }
   }

   private void generateBotShips () {
      BotShipGenerator.generateBotShips(this);
   }

   private void enableVoyageShops () {
      if (showShops) {
         // Enable shops when all enemies have been eliminated
         Area area = AreaManager.self.getArea(this.areaKey);
         if (area != null) {
            area.enableSeaShops();
         }
      }
   }

   private void countAliveEnemies () {
      if (!NetworkServer.active || !isNetworkPrefabInstantiationFinished) {
         enableVoyageShops();
         return;
      }

      aliveNPCEnemiesCount = 0;
      foreach (NetworkBehaviour networkBehaviour in getEntities()) {
         NetEntity entity = networkBehaviour as NetEntity;
         if (entity != null && (entity.isBotShip() || entity.isSeaMonster() || entity.isLandEnemy()) && !entity.isDead()) {
            aliveNPCEnemiesCount++;
            showShops = false;
            _hasSpawnedEnemies = true;
         }
      }

      // Check if all enemies inside treasure sites have been defeated
      areAllTreasureSitesClearedOfEnemies = true;
      foreach (TreasureSite treasureSite in treasureSites) {
         if (treasureSite.isActive() && !treasureSite.isClearedOfEnemies) {
            areAllTreasureSitesClearedOfEnemies = false;
            break;
         }
      }

      // Enable shops after enemies have been cleared out
      if (_hasSpawnedEnemies && aliveNPCEnemiesCount < 1) {
         showShops = true;
         _hasSpawnedEnemies = false;
      }
   }

   protected IEnumerator CO_SpawnInstanceSpecificPrefabs () {
      // Wait until the area has been instantiated
      while (AreaManager.self.getArea(this.areaKey) == null) {
         yield return null;
      }

      Area area = AreaManager.self.getArea(this.areaKey);

      if (!NetworkServer.active || area == null) {
         yield break;
      }

      if (area.seaMonsterDataFields.Count > 0 && seaMonsterCount < 1) {
         foreach (ExportedPrefab001 dataField in area.seaMonsterDataFields) {
            Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 0) * 0.16f + Vector3.back * 10;
            SeaMonsterEntity.Type seaMonsterType = (SeaMonsterEntity.Type) SeaMonsterEntity.fetchReceivedData(dataField.d);

            SeaMonsterEntity seaMonster = spawnSeaMonster((SeaMonsterEntity.Type) SeaMonsterEntity.fetchReceivedData(dataField.d), targetLocalPos, area);

            // Special case of the 'horror' boss
            if (seaMonsterType == SeaMonsterEntity.Type.Horror) {
               seaMonster.chestDropCount = UnityEngine.Random.Range(3, 5);

               float distanceGap = 1.2f;
               float diagonalDistanceGap = Vector2.Distance(new Vector2(0, 0), new Vector2(distanceGap, distanceGap));

               SeaMonsterEntity childSeaMonster1 = spawnSeaMonsterChild(SeaMonsterEntity.Type.Horror_Tentacle, targetLocalPos + new Vector3(diagonalDistanceGap, -diagonalDistanceGap, 0), area, seaMonster, 1, -1, distanceGap, 1);
               SeaMonsterEntity childSeaMonster2 = spawnSeaMonsterChild(SeaMonsterEntity.Type.Horror_Tentacle, targetLocalPos + new Vector3(-diagonalDistanceGap, -diagonalDistanceGap, 0), area, seaMonster, -1, -1, distanceGap, 0);
               SeaMonsterEntity childSeaMonster3 = spawnSeaMonsterChild(SeaMonsterEntity.Type.Horror_Tentacle, targetLocalPos + new Vector3(diagonalDistanceGap, diagonalDistanceGap, 0), area, seaMonster, 1, 1, distanceGap, 1);
               SeaMonsterEntity childSeaMonster4 = spawnSeaMonsterChild(SeaMonsterEntity.Type.Horror_Tentacle, targetLocalPos + new Vector3(-diagonalDistanceGap, diagonalDistanceGap, 0), area, seaMonster, -1, 1, distanceGap, 0);
               SeaMonsterEntity childSeaMonster5 = spawnSeaMonsterChild(SeaMonsterEntity.Type.Horror_Tentacle, targetLocalPos + new Vector3(-distanceGap, 0, 0), area, seaMonster, -1, 0, distanceGap, 1);
               SeaMonsterEntity childSeaMonster6 = spawnSeaMonsterChild(SeaMonsterEntity.Type.Horror_Tentacle, targetLocalPos + new Vector3(distanceGap, 0, 0), area, seaMonster, 1, 0, distanceGap, 0);
            }
         }
      }

      if (area.enemyDatafields.Count > 0 && enemyCount < 1) {
         foreach (ExportedPrefab001 dataField in area.enemyDatafields) {
            Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 0) * 0.16f + Vector3.back * 10;

            // Add it to the Instance
            Enemy enemy = Instantiate(PrefabsManager.self.enemyPrefab);
            foreach (DataField field in dataField.d) {
               if (field.k.CompareTo(DataField.LAND_ENEMY_DATA_KEY) == 0) {
                  // Get ID from npc data field
                  if (field.tryGetIntValue(out int id)) {
                     enemy.enemyType = (Enemy.Type) id;
                  }
               }
               if (field.k.CompareTo(DataField.NPC_STATIONARY_KEY) == 0) {
                  string isStationaryData = field.v.Split(':')[0];
                  enemy.isStationary = isStationaryData.ToLower() == "true" ? true : false;
               }
            }
            enemy.areaKey = area.areaKey;
            enemy.transform.localPosition = targetLocalPos;
            enemy.setAreaParent(area, false);
            BattlerData battlerData = MonsterManager.self.getBattlerData(enemy.enemyType);
            if (battlerData != null) {
               enemy.isBossType = battlerData.isBossType;
               enemy.isSupportType = battlerData.isSupportType;
               enemy.animGroupType = battlerData.animGroup;
               enemy.facing = Direction.South;
               enemy.displayNameText.text = battlerData.enemyName;
            }

            InstanceManager.self.addEnemyToInstance(enemy, this);

            NetworkServer.Spawn(enemy.gameObject);
         }
      }

      if (area.bossSpawnerDataFields.Count > 0) {
         Enemy.Type bossToSpawn = Enemy.Type.None;
         switch (biome) {
            case Biome.Type.Forest:
            case Biome.Type.Pine:
               bossToSpawn = Enemy.Type.Lizard_King;
               break;
            case Biome.Type.Lava:
            case Biome.Type.Desert:
               bossToSpawn = Enemy.Type.Golem_Boss;
               break;
            default:
               bossToSpawn = UnityEngine.Random.Range(0, 2) == 0 ? Enemy.Type.Golem_Boss : Enemy.Type.Lizard_King;
               break;
         }

         if (bossToSpawn != Enemy.Type.None) {
            foreach (ExportedPrefab001 dataField in area.bossSpawnerDataFields) {
               // Add it to the Instance
               Enemy enemy = Instantiate(PrefabsManager.self.enemyPrefab);
               enemy.enemyType = bossToSpawn;
               enemy.isStationary = true;
               enemy.areaKey = area.areaKey;
               Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 0) * 0.16f + Vector3.back * 10;
               enemy.transform.localPosition = targetLocalPos;
               enemy.setAreaParent(area, false);
               BattlerData battlerData = MonsterManager.self.getBattlerData(enemy.enemyType);
               if (battlerData != null) {
                  enemy.isBossType = battlerData.isBossType;
                  enemy.isSupportType = battlerData.isSupportType;
                  enemy.animGroupType = battlerData.animGroup;
                  enemy.facing = Direction.South;
                  enemy.displayNameText.text = battlerData.enemyName;
               }

               InstanceManager.self.addEnemyToInstance(enemy, this);
               NetworkServer.Spawn(enemy.gameObject);
            }
         }
      }

      int pvpStructuresSpawned = 0;

      if (area.towerDataFields.Count > 0) {
         foreach (ExportedPrefab001 dataField in area.towerDataFields) {
            PvpTower pvpTower = Instantiate(PrefabsManager.self.pvpTowerPrefab);
            pvpTower.areaKey = area.areaKey;
            pvpTower.instanceId = this.id;
            Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 0) * 0.16f + Vector3.forward * 10;
            pvpTower.transform.localPosition = targetLocalPos;
            pvpTower.setAreaParent(area, false);
            pvpTower.setIsInvulnerable(true);

            // The data set in the map editor will be set to this pvp tower object, this will only occur on the server side, client side data will be set in MapImporter.cs script in the function instantiatePrefabs()
            IMapEditorDataReceiver receiver = pvpTower.GetComponent<IMapEditorDataReceiver>();
            if (receiver != null && dataField.d != null) {
               receiver.receiveData(dataField.d);
            }

            InstanceManager.self.addSeaStructureToInstance(pvpTower, this);
            NetworkServer.Spawn(pvpTower.gameObject);
            pvpStructuresSpawned++;
         }
      }

      if (area.pvpCaptureTargetHolders.Count > 0) {
         foreach (ExportedPrefab001 dataField in area.pvpCaptureTargetHolders) {
            PvpCaptureTargetHolder captureTargetHolder = Instantiate(PrefabsManager.self.pvpCaptureTargetHolderPrefab);
            captureTargetHolder.areaKey = area.areaKey;
            captureTargetHolder.instanceId = this.id;
            Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 0) * 0.16f + Vector3.forward * 10;
            captureTargetHolder.transform.localPosition = targetLocalPos;
            captureTargetHolder.setAreaParent(area, false);
            captureTargetHolder.setIsInvulnerable(true);

            IMapEditorDataReceiver receiver = captureTargetHolder.GetComponent<IMapEditorDataReceiver>();
            if (receiver != null && dataField.d != null) {
               receiver.receiveData(dataField.d);
            }

            InstanceManager.self.addSeaStructureToInstance(captureTargetHolder, this);
            NetworkServer.Spawn(captureTargetHolder.gameObject);
            pvpStructuresSpawned++;
         }
      }

      if (area.pvpLootSpawners.Count > 0) {
         int index = 0;
         foreach (ExportedPrefab001 dataField in area.pvpLootSpawners) {
            PvpLootSpawn pvpLootSpawner = Instantiate(PrefabsManager.self.pvpLootSpawnerPrefab);
            Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 0) * 0.16f + Vector3.forward * 10;
            pvpLootSpawner.transform.localPosition = targetLocalPos;
            pvpLootSpawner.setAreaParent(area, false);

            pvpLootSpawner.instanceId = this.id;
            pvpLootSpawner.areaKey = areaKey;
            index++;

            IMapEditorDataReceiver receiver = pvpLootSpawner.GetComponent<IMapEditorDataReceiver>();
            if (receiver != null && dataField.d != null) {
               receiver.receiveData(dataField.d);
            }

            InstanceManager.self.addLootSpawnerToInstance(pvpLootSpawner, this);
            NetworkServer.Spawn(pvpLootSpawner.gameObject);
         }
      }

      if (area.pvpMonsterSpawnerDataFields.Count > 0) {
         int index = 0;
         foreach (ExportedPrefab001 dataField in area.pvpMonsterSpawnerDataFields) {
            PvpMonsterSpawner pvpMonsterSpawner = Instantiate(PrefabsManager.self.pvpMonsterSpawnerPrefab);
            pvpMonsterSpawner.transform.SetParent(area.transform, false);
            Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 0) * 0.16f + Vector3.forward * 10;
            pvpMonsterSpawner.transform.localPosition = targetLocalPos;

            pvpMonsterSpawner.instanceId = this.id;
            pvpMonsterSpawner.spawnId = index;
            index++;

            IMapEditorDataReceiver receiver = pvpMonsterSpawner.GetComponent<IMapEditorDataReceiver>();
            if (receiver != null && dataField.d != null) {
               receiver.receiveData(dataField.d);
            }
            InstanceManager.self.addSeaMonsterSpawnerToInstance(pvpMonsterSpawner, this);
            NetworkServer.Spawn(pvpMonsterSpawner.gameObject);
         }
      }

      if (area.windowDataFields.Count > 0) {
         int indexId = 0;
         foreach (ExportedPrefab001 dataField in area.windowDataFields) {
            WindowInteractable windowInteractable = Instantiate(PrefabsManager.self.WindowInteractable);
            windowInteractable.instanceId = this.id;
            windowInteractable.areaKey = areaKey;
            windowInteractable.id = indexId;
            indexId++;

            windowInteractable.transform.SetParent(area.transform, false);
            Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 5) * 0.16f + Vector3.forward * 10;
            windowInteractable.transform.localPosition = targetLocalPos;

            InstanceManager.self.addWindowToInstance(windowInteractable, this);
            NetworkServer.Spawn(windowInteractable.gameObject);
         }
      }

      if (area.waypointsDataFields.Count > 0) {
         foreach (ExportedPrefab001 dataField in area.waypointsDataFields) {
            // TODO: Process waypoint info here
            PvpWaypoint pvpWaypoint = Instantiate(new GameObject("PvpWaypoint")).AddComponent<PvpWaypoint>();
            Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 0) * 0.16f + Vector3.forward * 10;
            pvpWaypoint.transform.localPosition = targetLocalPos;
            pvpWaypoint.transform.SetParent(area.transform, false);

            IMapEditorDataReceiver receiver = pvpWaypoint.GetComponent<IMapEditorDataReceiver>();
            if (receiver != null && dataField.d != null) {
               receiver.receiveData(dataField.d);
            }

            InstanceManager.self.addPvpWaypointToInstance(pvpWaypoint, this);
         }
      }

      if (area.baseDataFields.Count > 0) {
         foreach (ExportedPrefab001 dataField in area.baseDataFields) {
            PvpBase pvpBase = Instantiate(PrefabsManager.self.pvpBasePrefab);
            pvpBase.areaKey = area.areaKey;
            pvpBase.instanceId = this.id;
            Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 0) * 0.16f + Vector3.forward * 10;
            pvpBase.transform.localPosition = targetLocalPos;
            pvpBase.setAreaParent(area, false);
            pvpBase.setIsInvulnerable(true);

            // The data set in the map editor will be set to this pvp object, this will only occur on the server side, client side data will be set in MapImporter.cs script in the function instantiatePrefabs()
            IMapEditorDataReceiver receiver = pvpBase.GetComponent<IMapEditorDataReceiver>();
            if (receiver != null && dataField.d != null) {
               receiver.receiveData(dataField.d);
            }

            InstanceManager.self.addSeaStructureToInstance(pvpBase, this);
            NetworkServer.Spawn(pvpBase.gameObject);
            pvpStructuresSpawned++;
         }
      }

      if (area.shipyardDataFields.Count > 0) {
         foreach (ExportedPrefab001 dataField in area.shipyardDataFields) {
            PvpShipyard pvpShipyard = Instantiate(PrefabsManager.self.pvpShipyardPrefab);
            pvpShipyard.areaKey = area.areaKey;
            pvpShipyard.instanceId = this.id;
            Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 0) * 0.16f + Vector3.forward * 10;
            pvpShipyard.transform.localPosition = targetLocalPos;
            pvpShipyard.setAreaParent(area, false);
            pvpShipyard.setIsInvulnerable(true);

            // The data set in the map editor will be set to this pvp object, this will only occur on the server side, client side data will be set in MapImporter.cs script in the function instantiatePrefabs()
            IMapEditorDataReceiver receiver = pvpShipyard.GetComponent<IMapEditorDataReceiver>();
            if (receiver != null && dataField.d != null) {
               receiver.receiveData(dataField.d);
            }

            pvpShipyard.spawnLocation = PvpGame.getSpawnSideForShipyard(pvpShipyard.pvpTeam, pvpShipyard.laneType);
            InstanceManager.self.addSeaStructureToInstance(pvpShipyard, this);
            NetworkServer.Spawn(pvpShipyard.gameObject);
            pvpStructuresSpawned++;
         }
      }

      if (pvpStructuresSpawned > 0) {
         area.rescanGraph();
      }

      // Spawn random enemies
      if (area.isSea) {
         EnemyManager.self.spawnShipsOnServerForInstance(this);

         if (Area.isWorldMap(area.areaKey) && area.openWorldController != null) {
            D.adminLog("Generating open world enemies at area {" + area.areaKey + "}, max of {" + area.openWorldController.maxEnemyCount + "}", D.ADMIN_LOG_TYPE.EnemyWaterSpawn);
            EnemyManager.self.spawnOpenWorldEnemies(this, area.areaKey, area.openWorldController.maxEnemyCount);
         } else {
            D.adminLog("Failed Because map {" + area.areaKey + "} is not valid!", D.ADMIN_LOG_TYPE.EnemyWaterSpawn);
         }
      } else {
         EnemyManager.self.spawnEnemiesOnServerForInstance(this);
      }

      if (area.npcDatafields.Count > 0 && npcCount < 1) {
         string disabledNpcLog = "";

         foreach (ExportedPrefab001 dataField in area.npcDatafields) {
            Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 0) * 0.16f + Vector3.back * 10;
            int npcId = NPC.fetchDataFieldID(dataField.d);

            // If npc data equivalent does not exist, do not process this entity
            if (NPCManager.self.getNPCData(npcId) == null) {
               disabledNpcLog += (disabledNpcLog == "" ? "" : " : ") + npcId;
            } else {
               NPC npc = Instantiate(PrefabsManager.self.npcPrefab);
               npc.areaKey = area.areaKey;
               npc.npcId = npcId;
               npc.transform.localPosition = targetLocalPos;
               npc.setAreaParent(area, false);

               NPCManager.self.storeNpcIdPerArea(npc.npcId, areaKey);

               // Make sure npc has correct data
               IMapEditorDataReceiver receiver = npc.GetComponent<IMapEditorDataReceiver>();
               if (receiver != null && dataField.d != null) {
                  receiver.receiveData(dataField.d);
               }

               InstanceManager.self.addNPCToInstance(npc, this);

               foreach (ZSnap snap in npc.GetComponentsInChildren<ZSnap>()) {
                  snap.snapZ();
               }

               NetworkServer.Spawn(npc.gameObject);
            }
         }
         if (disabledNpcLog.Length > 1) {
            D.editorLog("Disabled Npc's for area: (" + this.areaKey + ") : " + disabledNpcLog, Color.red);
         }
      }

      if (area.treasureSiteDataFields.Count > 0) {
         foreach (ExportedPrefab001 dataField in area.treasureSiteDataFields) {
            Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 0) * 0.16f + Vector3.forward * 10;

            // League treasure sites have a custom behavior (TreasureSiteLeague.cs)
            TreasureSite site;
            if (this.isLeague) {
               site = Instantiate(PrefabsManager.self.treasureSiteLeaguePrefab);
               ((TreasureSiteLeague) site).leagueIndex = this.leagueIndex;
            } else {
               site = Instantiate(PrefabsManager.self.treasureSitePrefab);
            }

            site.instanceId = this.id;
            site.areaKey = area.areaKey;
            site.instanceBiome = this.biome;
            site.difficulty = difficulty;
            site.transform.localPosition = targetLocalPos;
            site.setAreaParent(area, false);

            // Keep a track of the entity
            InstanceManager.self.addTreasureSiteToInstance(site, this);

            // Spawn the site on the clients
            NetworkServer.Spawn(site.gameObject);
         }
      }

      List<OreNode> oreNodeList = new List<OreNode>();
      if (area.oreDataFields.Count > 0) {
         foreach (ExportedPrefab001 dataField in area.oreDataFields) {
            Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 0) * 0.16f + Vector3.forward * 10;
            int oreId = 0;
            OreNode.Type oreType = OreNode.Type.None;

            foreach (DataField field in dataField.d) {
               if (field.k.CompareTo(DataField.ORE_SPOT_DATA_KEY) == 0) {
                  // Get ID from ore data field
                  if (field.tryGetIntValue(out int id)) {
                     oreId = id;
                  }
               }
               if (field.k.CompareTo(DataField.ORE_TYPE_DATA_KEY) == 0) {
                  // Get ore type from ore data field
                  if (field.tryGetIntValue(out int type)) {
                     oreType = (OreNode.Type) type;
                  }
               }
            }

            // Create the ore node locally and through the network
            OreNode newOreNode = OreManager.self.createOreNode(this, targetLocalPos, oreType);
            oreNodeList.Add(newOreNode);
            Map map = AreaManager.self.getMapInfo(areaKey);
            if (map != null) {
               newOreNode.mapSpecialType = map.specialType;
            }
         }
      }

      // Register the ore spots to the ore controller if it exists
      if (area.oreNodeController) {
         area.oreNodeController.setOreSpotList(oreNodeList);
      }

      if (area.shipDataFields.Count > 0) {
         foreach (ExportedPrefab001 dataField in area.shipDataFields) {
            Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 0) * 0.16f + Vector3.back * 10;
            BotShipEntity botShip = spawnBotShip(dataField, targetLocalPos, area, biome);
         }
      }

      // Create the discoveries that could exist in this instance
      DiscoveryManager.self.createDiscoveriesForInstance(this);

      // Spawn potential treasure chests for this instance
      TreasureManager.self.createTreasureForInstance(this);

      // Regularly re-create the bot ships that are killed
      if (BotShipGenerator.shouldGenerateBotShips(areaKey)) {
         InvokeRepeating(nameof(generateBotShips), UnityEngine.Random.Range(10f, 14f), 30f);
      }

      isNetworkPrefabInstantiationFinished = true;
   }

   private SeaMonsterEntity spawnSeaMonster (SeaMonsterEntity.Type seaMonsterType, Vector3 localPos, Area area) {
      if (SeaMonsterManager.self.getMonster(seaMonsterType) == null) {
         D.debug("Sea monster is null! " + seaMonsterType);
         return null;
      }

      SeaMonsterEntity seaMonster = Instantiate(PrefabsManager.self.seaMonsterPrefab);
      seaMonster.monsterType = seaMonsterType;
      seaMonster.areaKey = this.areaKey;
      seaMonster.facing = Direction.South;
      seaMonster.transform.localPosition = localPos;
      seaMonster.setAreaParent(area, false);

      InstanceManager.self.addSeaMonsterToInstance(seaMonster, this);

      NetworkServer.Spawn(seaMonster.gameObject);

      return seaMonster;
   }

   public BotShipEntity spawnBotShip (ExportedPrefab001 dataField, Vector3 localPos, Area area, Biome.Type biome) {
      int xmlId = SeaMonsterEntityData.DEFAULT_SHIP_ID;
      int guildId = 1;
      bool randomizeShip = true;

      // Randomize xml id of ships by biome as default
      xmlId = EnemyManager.self.randomizeShipXmlId(biome);
      int xmlIdOverride = 0;
      foreach (DataField field in dataField.d) {
         if (field.k.CompareTo(DataField.SHIP_GUILD_ID) == 0) {
            guildId = int.Parse(field.v.Split(':')[0]);
         }
         if (field.k.CompareTo(DataField.SHIP_DATA_KEY) == 0) {
            xmlIdOverride = int.Parse(field.v.Split(':')[0]);
         }
         if (field.k.CompareTo(DataField.RANDOMIZE_SHIP) == 0) {
            string randomizeShipData = field.v.Split(':')[0];
            randomizeShip = randomizeShipData.ToLower() == "true" ? true : false;
         }
      }

      // If randomize ship is set in web tool, override randomized xml id
      if (!randomizeShip) {
         xmlId = xmlIdOverride;
      }

      if (SeaMonsterManager.self.getMonster(xmlId) == null) {
         D.debug("Sea monster is null! " + xmlId);
         return null;
      }

      SeaMonsterEntityData seaMonsterData = SeaMonsterManager.self.getMonster(xmlId);
      BotShipEntity botShip = Instantiate(PrefabsManager.self.botShipPrefab);
      botShip.areaKey = this.areaKey;
      botShip.facing = Direction.South;
      botShip.transform.localPosition = localPos;

      botShip.seaEntityData = seaMonsterData;
      botShip.maxHealth = seaMonsterData.maxHealth;
      botShip.currentHealth = seaMonsterData.maxHealth;

      Ship.Type shipType = (Ship.Type) seaMonsterData.subVarietyTypeId;
      botShip.shipType = shipType;
      if (seaMonsterData.skillIdList.Count > 0) {
         botShip.primaryAbilityId = seaMonsterData.skillIdList[0];
      }
      botShip.guildId = guildId;
      botShip.setShipData(xmlId, shipType, this.difficulty);

      botShip.setAreaParent(area, false);

      InstanceManager.self.addSeaMonsterToInstance(botShip, this);

      NetworkServer.Spawn(botShip.gameObject);

      if (botShip != null) {
         IMapEditorDataReceiver receiver = botShip.GetComponent<IMapEditorDataReceiver>();
         if (receiver != null && dataField.d != null) {
            receiver.receiveData(dataField.d);
         }
      } else {
         D.debug("Failed to spawn bot ship: " + xmlId);
      }

      return botShip;
   }

   private SeaMonsterEntity spawnSeaMonsterChild (SeaMonsterEntity.Type seaMonsterType, Vector2 localPos, Area area, SeaMonsterEntity parentSeaMonster, int xVal, int yVal, float distanceToParent, int variety) {
      SeaMonsterEntity childSeaMonster = spawnSeaMonster(seaMonsterType, localPos, area);

      childSeaMonster.directionFromSpawnPoint = new Vector2(xVal, yVal);
      childSeaMonster.distanceFromSpawnPoint = distanceToParent;
      childSeaMonster.variety = (variety);

      // Link the parent and child monsters
      childSeaMonster.seaMonsterParentEntity = parentSeaMonster;
      parentSeaMonster.seaMonsterChildrenList.Add(childSeaMonster);

      return childSeaMonster;
   }

   public SeaEntity spawnSeaEnemy (SeaMonsterEntityData data, Vector3 position) {
      Area area = AreaManager.self.getArea(this.areaKey);

      SeaEntity seaEntity;

      if (data.seaMonsterType == SeaMonsterEntity.Type.PirateShip) {
         seaEntity = Instantiate(PrefabsManager.self.botShipPrefab);
      } else {
         seaEntity = Instantiate(PrefabsManager.self.seaMonsterPrefab);
      }

      seaEntity.areaKey = this.areaKey;
      seaEntity.facing = Direction.South;
      seaEntity.transform.localPosition = position;
      seaEntity.setAreaParent(area, true);
      seaEntity.maxHealth = data.maxHealth;
      seaEntity.currentHealth = data.maxHealth;

      // Handle as ship
      if (data.seaMonsterType == SeaMonsterEntity.Type.PirateShip) {
         BotShipEntity botShip = seaEntity as BotShipEntity;
         botShip.seaEntityData = data;
         Ship.Type shipType = (Ship.Type) data.subVarietyTypeId;
         botShip.shipType = shipType;
         if (data.skillIdList.Count > 0) {
            botShip.primaryAbilityId = data.skillIdList[0];
         }

         // All ships in the web tool currently are pirates
         botShip.guildId = 2;
         botShip.setShipData(data.xmlId, shipType, this.difficulty);

         // Handle as sea monster
      } else {
         SeaMonsterEntity seaMonster = seaEntity as SeaMonsterEntity;
         seaMonster.monsterType = data.seaMonsterType;
      }

      InstanceManager.self.addSeaMonsterToInstance(seaEntity, this);
      NetworkServer.Spawn(seaEntity.gameObject);

      return seaEntity;
   }

   /// <summary>
   /// Drops a new item in the instance. It will be created in the database on pick up.
   /// If not picked up, the item will cease to exist when the instance is destroyed.
   /// </summary>
   /// <param name="newItem">The item to create</param>
   /// <param name="localPosition">At what position to drop item in relation to the instance</param>
   /// <param name="appearDirection">Direction of travel when the item spawns</param>
   /// <param name="limitToUserId">If not 0, this item will only be able to be picked up by this user</param>
   /// <param name="lifetimeSeconds">After what time in seconds will this item despawn. 0 If no time limit</param>
   /// <param name="spinWhileDropping">Should the item spin when it's dropping to the ground</param>
   /// <param name="configureBeforeSpawn">Action that's called on the item before it's spawned to the network, can be used to fine configure the item</param>
   [Server]
   public void dropNewItem (Item newItem, Vector3 localPosition, Vector2 appearDirection, bool spinWhileDropping = true, int limitToUserId = 0, Action<DroppedItem> configureBeforeSpawn = null, float lifetimeSeconds = 600) {
      // Get area
      Area area = AreaManager.self.getArea(areaKey);
      if (area == null) {
         return;
      }

      DroppedItem droppedItem = Instantiate(PrefabsManager.self.droppedItemPrefab);
      droppedItem.transform.position = area.transform.TransformPoint(localPosition);

      droppedItem.limitToUserId = limitToUserId;
      droppedItem.lifetimeSeconds = lifetimeSeconds;
      droppedItem.targetItem = newItem;
      droppedItem.appearDirection = appearDirection;

      if (!spinWhileDropping) {
         droppedItem.setNoRotationsDuringAppearance();
      }

      NetworkServer.Spawn(droppedItem.gameObject);
   }

   #region Private Variables

   // The number of consecutive times we've checked this instance and found it empty
   protected int _consecutiveEmptyChecks = 0;

   // If enemies have started spawning
   private bool _hasSpawnedEnemies;

   #endregion
}