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

   // The list of Entities in this instance (server only)
   public List<NetworkBehaviour> entities = new List<NetworkBehaviour>();

   // The list of treasure sites in this instance (server only)
   public List<TreasureSite> treasureSites = new List<TreasureSite>();

   // The list of sea structures in this instance (server only)
   public List<SeaStructure> seaStructures = new List<SeaStructure>();

   // For debugging in the Editor
   [SyncVar]
   public int entityCount;

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

   // The start time
   public long startTime;

   // The user id registered to this area if this is a private area
   public int privateAreaUserId;

   // Number of minutes that this instance can be active when it is a private area
   public const int ACTIVE_MINUTES_CAP = 10;

   #endregion

   public void Awake () {
      // Look up components
      netIdent = GetComponent<NetworkIdentity>();
   }

   private void Start () {
      resetActiveTimer();

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
      }
   }

   public void resetActiveTimer () {
      startTime = DateTime.UtcNow.ToBinary();
   }

   public int getPlayerCount() {
      int count = 0;

      foreach (NetworkBehaviour entity in entities) {
         if (entity != null  && (entity is PlayerBodyEntity || entity is PlayerShipEntity)) {
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
      return entities.FindAll(_=>_ is OreNode);
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
      if (AreaManager.self.tryGetCustomMapManager(areaKey, out CustomMapManager customMapManager)) {
         if (customMapManager is CustomFarmManager || customMapManager is CustomHouseManager) {
            return 1;
         }
      }

      return _maxPlayerCount;
   }

   public void updateMaxPlayerCount (bool isSinglePlayer = false) {
      if (isSinglePlayer) {
         _maxPlayerCount = 1;
      } else if (isVoyage) {
         _maxPlayerCount = Voyage.MAX_PLAYERS_PER_INSTANCE;
      } else if (AreaManager.self.isPrivateArea(areaKey)) {
         _maxPlayerCount = 1;
      } else {
         // Check if we've specified a max player count on the command line
         if (CommandCodes.get(CommandCodes.Type.MAX_INSTANCE_PLAYERS)) {
            _maxPlayerCount = Util.getCommandLineInt(CommandCodes.Type.MAX_INSTANCE_PLAYERS + "");
            D.log("Setting max players-per-instance to: " + _maxPlayerCount);
         }
      }
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
      if ((VoyageManager.isVoyageOrLeagueArea(areaKey) || VoyageManager.isTreasureSiteArea(areaKey)) && VoyageGroupManager.self.isAtLeastOneGroupInVoyage(voyageId)) {
         return;
      }

      // If there's no one in the instance right now, increase the  count
      if (getPlayerCount() <= 0) {
         _consecutiveEmptyChecks++;

         TimeSpan timeSinceStart = DateTime.UtcNow.Subtract(DateTime.FromBinary(startTime));

         // If this is a private area and the active minutes exceeds the cap, respawn player to starting town and remove this instance
         if ((AreaManager.self.isPrivateArea(areaKey) && timeSinceStart.TotalMinutes >= ACTIVE_MINUTES_CAP)) {
            UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
               UserObjects userObj = DB_Main.getUserObjects(privateAreaUserId);

               // If the player saved area is still this instance, Update the player area in the database since this private area will now be removed
               if (userObj.userInfo.areaKey == areaKey) {
                  Vector2 spawnLocalPosition = SpawnManager.self.getDefaultLocalPosition(Area.STARTING_TOWN);
                  DB_Main.setNewLocalPosition(privateAreaUserId, spawnLocalPosition, Direction.South, Area.STARTING_TOWN);
               } 
               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  // Remove the private area
                  InstanceManager.self.removeEmptyInstance(this);
               });
            });
         } else {
            // If the Instance has been empty for long enough, just remove it
            if (_consecutiveEmptyChecks > 10) {
               InstanceManager.self.removeEmptyInstance(this);
            }
         }
      } else {
         // There's someone in the instance, so reset the counter
         _consecutiveEmptyChecks = 0;
         resetActiveTimer();
      }
   }

   private void generateBotShips () {
      BotShipGenerator.generateBotShips(this);
   }

   private void countAliveEnemies () {
      if (!NetworkServer.active || !isNetworkPrefabInstantiationFinished) {
         return;
      }

      aliveNPCEnemiesCount = 0;
      foreach (NetworkBehaviour networkBehaviour in getEntities()) {
         NetEntity entity = networkBehaviour as NetEntity;
         if (entity != null && (entity.isBotShip() || entity.isSeaMonster() || entity.isLandEnemy()) && !entity.isDead()) {
            aliveNPCEnemiesCount++;
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

      if (area.towerDataFields.Count > 0) {
         foreach (ExportedPrefab001 dataField in area.towerDataFields) {
            PvpTower pvpTower = Instantiate(PrefabsManager.self.pvpTowerPrefab);
            pvpTower.areaKey = area.areaKey;
            pvpTower.instanceId = this.id;
            Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 0) * 0.16f + Vector3.forward * 10;
            pvpTower.transform.localPosition = targetLocalPos;
            pvpTower.setAreaParent(area, false);

            // The data set in the map editor will be set to this pvp tower object, this will only occur on the server side, client side data will be set in MapImporter.cs script in the function instantiatePrefabs()
            IMapEditorDataReceiver receiver = pvpTower.GetComponent<IMapEditorDataReceiver>();
            if (receiver != null && dataField.d != null) {
               receiver.receiveData(dataField.d);
            }

            InstanceManager.self.addSeaStructureToInstance(pvpTower, this);
            NetworkServer.Spawn(pvpTower.gameObject);
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

            // The data set in the map editor will be set to this pvp object, this will only occur on the server side, client side data will be set in MapImporter.cs script in the function instantiatePrefabs()
            IMapEditorDataReceiver receiver = pvpBase.GetComponent<IMapEditorDataReceiver>();
            if (receiver != null && dataField.d != null) {
               receiver.receiveData(dataField.d);
            }

            InstanceManager.self.addSeaStructureToInstance(pvpBase, this);
            NetworkServer.Spawn(pvpBase.gameObject);
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

            // The data set in the map editor will be set to this pvp object, this will only occur on the server side, client side data will be set in MapImporter.cs script in the function instantiatePrefabs()
            IMapEditorDataReceiver receiver = pvpShipyard.GetComponent<IMapEditorDataReceiver>();
            if (receiver != null && dataField.d != null) {
               receiver.receiveData(dataField.d);
            }

            pvpShipyard.initSprites();
            pvpShipyard.spawnLocation = PvpGame.getSpawnSideForShipyard(pvpShipyard.pvpTeam, pvpShipyard.laneType);
            InstanceManager.self.addSeaStructureToInstance(pvpShipyard, this);
            NetworkServer.Spawn(pvpShipyard.gameObject);
         }
      }

      // Spawn random enemies
      if (area.isSea) {
         EnemyManager.self.spawnShipsOnServerForInstance(this);
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
         }
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

      xmlId = EnemyManager.self.randomizeShipXmlId(biome);

      foreach (DataField field in dataField.d) {
         if (field.k.CompareTo(DataField.SHIP_GUILD_ID) == 0) {
            guildId = int.Parse(field.v.Split(':')[0]);
         }
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

   #region Private Variables

   // The number of consecutive times we've checked this instance and found it empty
   protected int _consecutiveEmptyChecks = 0;

   // The maximum number of players allowed in an instance
   protected int _maxPlayerCount = 50;

   #endregion
}