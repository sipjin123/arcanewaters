﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using System;

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

   // For debugging in the Editor
   [SyncVar]
   public int entityCount;

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

   // Our network ident
   public NetworkIdentity netIdent;

   #endregion

   public void Awake () {
      // Look up components
      netIdent = GetComponent<NetworkIdentity>();
   }

   private void Start () {
      // Spawn all the area prefabs that are specific to this instance
      if (NetworkServer.active) {
         StartCoroutine(CO_SpawnInstanceSpecificPrefabs());
      }

      // Routinely check if the instance is empty
      InvokeRepeating("checkIfInstanceIsEmpty", 10f, 30f);
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

   public List<NetworkBehaviour> getEntities () { 
      return entities;
   }

   public int getMaxPlayers () {
      if (Area.FARM.Equals(areaKey) || Area.HOUSE.Equals(areaKey)) {
         return 1;
      }

      return _maxPlayerCount;
   }

   public void updateMaxPlayerCount (bool isSinglePlayer = false) {
      if (isSinglePlayer) {
         _maxPlayerCount = 1;
      } else if (isVoyage) {
         _maxPlayerCount = Voyage.MAX_PLAYERS_PER_INSTANCE;
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

   public int getCapturedTreasureSitesCount () {
      int count = 0;

      foreach (NetworkBehaviour entity in entities) {
         if (entity != null && entity is TreasureSite && ((TreasureSite)entity).isCaptured()) {
            count++;
         }
      }

      return count;
   }

   public int getTreasureSitesCount () {
      int count = 0;

      foreach (NetworkBehaviour entity in entities) {
         if (entity != null && entity is TreasureSite) {
            count++;
         }
      }

      return count;
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
         if (_consecutiveEmptyChecks > 10) {
            InstanceManager.self.removeEmptyInstance(this);
         }

      } else {
         // There's someone in the instance, so reset the counter
         _consecutiveEmptyChecks = 0;
      }
   }

   protected IEnumerator CO_SpawnInstanceSpecificPrefabs () {
      // Wait until the area has been instantiated
      while (AreaManager.self.getArea(this.areaKey) == null) {
         yield return null;
      }

      Area area = AreaManager.self.getArea(this.areaKey);

      if (NetworkServer.active) {
         if (area != null) {
            if (area.enemyDatafields.Count > 0) {
               Transform enemyParent = Instantiate(new GameObject(), area.transform).transform;
               enemyParent.name = "Enemies";
               foreach (ExportedPrefab001 dataField in area.enemyDatafields) {
                  Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 0) * 0.16f + Vector3.back * 10;

                  // Add it to the Instance
                  Enemy enemy = Instantiate(PrefabsManager.self.enemyPrefab, enemyParent);
                  enemy.enemyType = (Enemy.Type) Enemy.fetchReceivedData(dataField.d);

                  BattlerData battleData = MonsterManager.self.getBattler(enemy.enemyType);
                  if (battleData != null) {
                     enemy.isBossType = battleData.isBossType;
                     enemy.animGroupType = battleData.animGroup;
                     enemy.facing = Direction.South;
                  }

                  InstanceManager.self.addEnemyToInstance(enemy, this);

                  enemy.transform.localPosition = targetLocalPos;
                  enemy.desiredPosition = targetLocalPos;

                  NetworkServer.Spawn(enemy.gameObject);
               }
            }

            if (area.npcDatafields.Count > 0) {
               Transform npcParent = Instantiate(new GameObject(), area.transform).transform;
               npcParent.name = "NPCs";
               foreach (ExportedPrefab001 dataField in area.npcDatafields) {
                  Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 0) * 0.16f + Vector3.back * 10;

                  NPC npc = Instantiate(PrefabsManager.self.npcPrefab, npcParent.position + targetLocalPos, Quaternion.identity, npcParent);
                  npc.areaKey = area.areaKey;

                  // Make sure npc has correct data
                  IMapEditorDataReceiver receiver = npc.GetComponent<IMapEditorDataReceiver>();
                  if (receiver != null && dataField.d != null) {
                     receiver.receiveData(dataField.d);
                  }
                  npc.npcId = NPC.fetchDataFieldID(dataField.d);

                  InstanceManager.self.addNPCToInstance(npc, this);

                  foreach (ZSnap snap in npc.GetComponentsInChildren<ZSnap>()) {
                     snap.snapZ();
                  }

                  NetworkServer.Spawn(npc.gameObject);
               }
            }

            if (area.treasureSiteDataFields.Count > 0) {
               foreach (ExportedPrefab001 dataField in area.treasureSiteDataFields) {
                  Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 0) * 0.16f + Vector3.forward * 10;

                  // Instantiate the treasure site
                  TreasureSite site = Instantiate(PrefabsManager.self.treasureSitePrefab, transform);
                  site.instanceId = this.id;
                  site.areaKey = area.areaKey;
                  site.setBiome(area.biome);
                  site.difficulty = difficulty;
                  site.transform.localPosition = targetLocalPos;

                  // Keep a track of the entity
                  InstanceManager.self.addTreasureSiteToInstance(site, this);

                  // Spawn the site on the clients
                  NetworkServer.Spawn(site.gameObject);
               }
            }

            if (area.oreDataFields.Count > 0) {
               Transform oreParent = Instantiate(new GameObject(), area.transform).transform;
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
                  OreNode newOreNode = OreManager.self.createOreNode(this, targetLocalPos, oreType, oreParent);

                  // Make sure the position is synced
                  newOreNode.syncedPosition = newOreNode.transform.position;
               }
            }

            if (area.shipDataFields.Count > 0) {
               Transform shipParent = Instantiate(new GameObject(), area.transform).transform;
               shipParent.name = "Ships";
               foreach (ExportedPrefab001 dataField in area.shipDataFields) {
                  Vector3 targetLocalPos = new Vector3(dataField.x, dataField.y, 0) * 0.16f + Vector3.back * 10;

                  BotShipEntity botShip = Instantiate(PrefabsManager.self.botShipPrefab, shipParent.position + targetLocalPos, Quaternion.identity, shipParent);

                  // Make sure ship has correct data
                  IMapEditorDataReceiver receiver = botShip.GetComponent<IMapEditorDataReceiver>();
                  if (receiver != null && dataField.d != null) {
                     receiver.receiveData(dataField.d);
                  }

                  botShip.faction = Faction.Type.Pirates;

                  InstanceManager.self.addBotShipToInstance(botShip, this);

                  foreach (ZSnap snap in botShip.GetComponentsInChildren<ZSnap>()) {
                     snap.snapZ();
                  }

                  NetworkServer.Spawn(botShip.gameObject);
               }
            }

            // Create the discoveries that could exist in this instance
            DiscoveryManager.self.createDiscoveriesForInstance(this);
         }
      }
   }

   #region Private Variables

   // The number of consecutive times we've checked this instance and found it empty
   protected int _consecutiveEmptyChecks = 0;

   // The maximum number of players allowed in an instance
   protected int _maxPlayerCount = 50;

   #endregion
}