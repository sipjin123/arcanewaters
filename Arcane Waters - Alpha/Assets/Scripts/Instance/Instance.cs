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

   // For the number of treasure sites in the instance
   [SyncVar]
   public int treasureSiteCount = 0;

   // For the number of captured treasure sites in the instance
   [SyncVar]
   public int capturedTreasureSiteCount = 0;

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

   // The unique identifier of the voyage
   [SyncVar]
   public int voyageId = 0;

   // Gets set to true when the voyage instance is player vs player
   [SyncVar]
   public bool isPvP = false;

   // The voyage difficulty
   [SyncVar]
   public Voyage.Difficulty difficulty = Voyage.Difficulty.None;

   // Player that is currently customizing the area
   public NetEntity playerMakingCustomizations;

   // Our network ident
   public NetworkIdentity netIdent;

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

   public void registerClientPlayerBody (NetworkBehaviour entity) {
      this.entities.Add(entity);
   }

   public List<NetworkBehaviour> getEntities () { 
      return entities;
   }

   public List<NetworkBehaviour> getPlayerEntities () {
      List<NetworkBehaviour> newEntityList = new List<NetworkBehaviour>();
      
      // Gathers all the player bodies in the area
      foreach (NetworkBehaviour existingEntity in entities) {
         if (existingEntity is PlayerBodyEntity) {
            newEntityList.Add(existingEntity);
         }
      }
      return newEntityList;
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
      if (isVoyage) {
         TimeSpan timeSinceStart = DateTime.UtcNow.Subtract(DateTime.FromBinary(creationDate));
         if (timeSinceStart.TotalSeconds < Voyage.INSTANCE_OPEN_DURATION) {
            return;
         }
      }

      // If there's no one in the instance right now, increase the  count
      if (getPlayerCount() <= 0) {
         _consecutiveEmptyChecks++;

         // If the Instance has been empty for long enough, just remove it
         if (_consecutiveEmptyChecks > 10 || AreaManager.self.isPrivateArea(areaKey)) {
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
               seaMonster.isStationary = true;

               float distanceGap = .25f;
               float diagonalDistanceGap = .35f;

               SeaMonsterEntity childSeaMonster1 = spawnSeaMonsterChild(SeaMonsterEntity.Type.Horror_Tentacle, targetLocalPos + new Vector3(distanceGap, -distanceGap, 0), area, seaMonster, 1, -1, 1);
               SeaMonsterEntity childSeaMonster2 = spawnSeaMonsterChild(SeaMonsterEntity.Type.Horror_Tentacle, targetLocalPos + new Vector3(-distanceGap, -distanceGap, 0), area, seaMonster, -1, -1, 0);
               SeaMonsterEntity childSeaMonster3 = spawnSeaMonsterChild(SeaMonsterEntity.Type.Horror_Tentacle, targetLocalPos + new Vector3(distanceGap, distanceGap, 0), area, seaMonster, 1, 1, 1);
               SeaMonsterEntity childSeaMonster4 = spawnSeaMonsterChild(SeaMonsterEntity.Type.Horror_Tentacle, targetLocalPos + new Vector3(-distanceGap, distanceGap, 0), area, seaMonster, -1, 1, 0);
               SeaMonsterEntity childSeaMonster5 = spawnSeaMonsterChild(SeaMonsterEntity.Type.Horror_Tentacle, targetLocalPos + new Vector3(-diagonalDistanceGap, 0, 0), area, seaMonster, -1, 0, 1);
               SeaMonsterEntity childSeaMonster6 = spawnSeaMonsterChild(SeaMonsterEntity.Type.Horror_Tentacle, targetLocalPos + new Vector3(diagonalDistanceGap, 0, 0), area, seaMonster, 1, 0, 0);

               childSeaMonster1.isStationary = true;
               childSeaMonster2.isStationary = true;
               childSeaMonster3.isStationary = true;
               childSeaMonster4.isStationary = true;
               childSeaMonster5.isStationary = true;
               childSeaMonster6.isStationary = true;
            }
         }
      }

      if (area.enemyDatafields.Count > 0 && enemyCount < 1) {
         foreach (ExportedPrefab001 dataField in area.enemyDatafields) {
            Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 0) * 0.16f + Vector3.back * 10;

            // Add it to the Instance
            Enemy enemy = Instantiate(PrefabsManager.self.enemyPrefab);
            enemy.enemyType = (Enemy.Type) Enemy.fetchReceivedData(dataField.d);
            enemy.areaKey = area.areaKey;
            enemy.transform.localPosition = targetLocalPos;
            enemy.setAreaParent(area, false);
                  
            BattlerData battleData = MonsterManager.self.getBattler(enemy.enemyType);
            if (battleData != null) {
               enemy.isBossType = battleData.isBossType;
               enemy.isSupportType = battleData.isSupportType;
               enemy.animGroupType = battleData.animGroup;
               enemy.facing = Direction.South;
               enemy.displayNameText.text = battleData.enemyName;
            }

            InstanceManager.self.addEnemyToInstance(enemy, this);
                  
            NetworkServer.Spawn(enemy.gameObject);
         }
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

            // Instantiate the treasure site
            TreasureSite site = Instantiate(PrefabsManager.self.treasureSitePrefab);
            site.instanceId = this.id;
            site.areaKey = area.areaKey;
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

      if (area.secretsEntranceDataFields.Count > 0) {
         int spawnIdCounter = 0;
         foreach (ExportedPrefab001 dataField in area.secretsEntranceDataFields) {
            Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 0) * 0.16f + Vector3.forward * 10;

            SecretEntranceHolder secretObjNode = Instantiate(PrefabsManager.self.secretEntrancePrefab);

            // Make sure obj has correct data
            IMapEditorDataReceiver receiver = secretObjNode.GetComponent<IMapEditorDataReceiver>();
            if (receiver != null && dataField.d != null) {
               receiver.receiveData(dataField.d);
            }

            secretObjNode.areaKey = area.areaKey;

            // Transform Setup
            secretObjNode.transform.localPosition = targetLocalPos;
            secretObjNode.setAreaParent(area, false);

            // Id Setup
            secretObjNode.instanceId = id;
            secretObjNode.spawnId = spawnIdCounter;

            entities.Add(secretObjNode);

            NetworkServer.Spawn(secretObjNode.gameObject);
            spawnIdCounter++;

            try {
               area.registerWarpFromSecretEntrance(secretObjNode.cachedSecretEntrance.warp);
            } catch {
               D.debug("No warp assigned to secret entrance!");
            }
         }
      }

      if (area.shipDataFields.Count > 0) {
         foreach (ExportedPrefab001 dataField in area.shipDataFields) {
            Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 0) * 0.16f + Vector3.back * 10;
            BotShipEntity botShip = spawnBotShip(dataField, targetLocalPos, area);
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
   }

   private SeaMonsterEntity spawnSeaMonster (SeaMonsterEntity.Type seaMonsterType, Vector3 localPos, Area area) {
      if (SeaMonsterManager.self.getMonster(seaMonsterType) == null) {
         D.debug("Sea monster is null! " + seaMonsterType);
         return null;
      }

      SeaMonsterEntityData seaMonsterData = SeaMonsterManager.self.getMonster(seaMonsterType);

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

   public BotShipEntity spawnBotShip (ExportedPrefab001 dataField, Vector3 localPos, Area area) {
      int xmlId = 0;
      int guildId = 1;
      foreach (DataField field in dataField.d) {
         if (field.k.CompareTo(DataField.SHIP_DATA_KEY) == 0) {
            int type = int.Parse(field.v.Split(':')[0]);
            xmlId = type;
         }
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
      botShip.setShipData(shipType);

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

   private SeaMonsterEntity spawnSeaMonsterChild (SeaMonsterEntity.Type seaMonsterType, Vector2 localPos, Area area, SeaMonsterEntity parentSeaMonster, int xVal, int yVal, int variety) {
      SeaMonsterEntity childSeaMonster = spawnSeaMonster(seaMonsterType, localPos, area);

      childSeaMonster.distanceFromSpawnPoint = new Vector2(xVal, yVal);
      childSeaMonster.variety = (variety);

      // Link the parent and child monsters
      childSeaMonster.seaMonsterParentEntity = parentSeaMonster;
      parentSeaMonster.seaMonsterChildrenList.Add(childSeaMonster);

      return childSeaMonster;
   }

   #region Private Variables

   // The number of consecutive times we've checked this instance and found it empty
   protected int _consecutiveEmptyChecks = 0;

   // The maximum number of players allowed in an instance
   protected int _maxPlayerCount = 50;

   #endregion
}