using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using Random = UnityEngine.Random;
using Pathfinding;
using UnityEngine.Tilemaps;
using System.Linq;

public class EnemyManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static EnemyManager self;
   
   #endregion

   public void Awake () {
      self = this;
   }

   public void Start () {
      // Create empty lists for each Area Type
      foreach (string areaKey in AreaManager.self.getAreaKeys()) {
         _spawners[areaKey] = new List<Enemy_Spawner>();
      }
   }

   public void storeSpawner (Enemy_Spawner spawner, string areaKey) {
      if (!_spawners.ContainsKey(areaKey)) {
         _spawners.Add(areaKey, new List<Enemy_Spawner>());
      }
      List<Enemy_Spawner> list = _spawners[areaKey];
      list.Add(spawner);
      _spawners[areaKey] = list;
   }

   public void removeSpawners (string areaKey) {
      List<Enemy_Spawner> list;
      if (_spawners.TryGetValue(areaKey, out list)) {
         list.Clear();
         _spawners[areaKey] = list;
      }
   }

   public void spawnOpenWorldEnemies (Instance instance, string areaKey, int targetSpawns) {
      processSpawnOpenWorldEnemies(instance, areaKey, targetSpawns);
   }

   private void processSpawnOpenWorldEnemies (Instance instance, string areaKey, int targetSpawns) {
      double initialTime = NetworkTime.time;
      Area areaTarget = AreaManager.self.getArea(areaKey);
      if (areaTarget == null) {
         D.adminLog("NULL Area {"+areaKey+"}", D.ADMIN_LOG_TYPE.EnemyWaterSpawn);
      } else {
         List<TilemapLayer> waterLayers = areaTarget.getWaterLayer();

         // Reverse the layers so that deepest layer is processed first and is set as hardest difficulty
         waterLayers.Reverse();

         HashSet<Vector3Int> occupiedTileOverall = new HashSet<Vector3Int>();
         D.adminLog("Generating Layers: {" + waterLayers.Count + "} water layers for area {" + areaKey + "} TargetSpwns: {" + targetSpawns + "}", D.ADMIN_LOG_TYPE.EnemyWaterSpawn);

         // Make sure that the generated deeper water layer is cached so it does not check again on shallow tiles, this prevents
         List<Vector3Int> deepWaterTiles = new List<Vector3Int>();
         List<Vector3Int> shallowWaterTiles = new List<Vector3Int>();
         List<Vector3Int> midWaterTiles = new List<Vector3Int>();

         Dictionary<TilemapLayer,int> deepWaterLayer = new Dictionary<TilemapLayer, int>();
         Dictionary<TilemapLayer, int> midWaterLayer = new Dictionary<TilemapLayer, int>();
         Dictionary<TilemapLayer, int> shallowWaterLayer = new Dictionary<TilemapLayer, int>();
         foreach (TilemapLayer layer in waterLayers) {
            if (layer.fullName == "water 8") {
               continue;
            }

            // Extract all available tiles in this layer
            BoundsInt.PositionEnumerator allTilesInLayer = layer.tilemap.cellBounds.allPositionsWithin;

            foreach (Vector3Int currentTile in allTilesInLayer) {
               Vector3 worldCoord = layer.tilemap.CellToWorld(currentTile);
               if (!occupiedTileOverall.Contains(currentTile) && !Util.hasLandTile(worldCoord)) {
                  TileBase newTile = layer.tilemap.GetTile(currentTile); 
                  float scaler = .035f;
                  bool createObjectMarkers = false;
                  if (newTile != null) {
                     if (areaTarget.hasTileAttribute(TileAttributes.Type.DeepWater, worldCoord)) {
                        // Remove after enemy spawner is finalized
                        if (createObjectMarkers) {
                           GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                           obj.transform.localScale = new Vector3(scaler, scaler, scaler);
                           obj.transform.position = worldCoord;
                        }

                        occupiedTileOverall.Add(currentTile);
                        deepWaterTiles.Add(currentTile);
                        if (deepWaterLayer.ContainsKey(layer)) {
                           deepWaterLayer[layer]++;
                        } else {
                           deepWaterLayer.Add(layer, 1);
                        }
                     }
                     else if (areaTarget.hasTileAttribute(TileAttributes.Type.MidWater, worldCoord)) {
                        // Remove after enemy spawner is finalized
                        if (createObjectMarkers) {
                           GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                           obj.transform.localScale = new Vector3(scaler / 2, scaler, scaler);
                           obj.transform.position = worldCoord;
                           obj.transform.eulerAngles = new Vector3(45, 45, 45);
                        }

                        occupiedTileOverall.Add(currentTile);
                        midWaterTiles.Add(currentTile);
                        if (midWaterLayer.ContainsKey(layer)) {
                           midWaterLayer[layer]++;
                        } else {
                           midWaterLayer.Add(layer, 1);
                        }
                     }
                     else if (areaTarget.hasTileAttribute(TileAttributes.Type.ShallowWater, worldCoord)) {
                        // Remove after enemy spawner is finalized
                        if (createObjectMarkers) {
                           GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                           obj.transform.localScale = new Vector3(scaler, scaler, scaler);
                           obj.transform.position = worldCoord;
                        }

                        occupiedTileOverall.Add(currentTile);
                        shallowWaterTiles.Add(currentTile);
                        if (shallowWaterLayer.ContainsKey(layer)) {
                           shallowWaterLayer[layer]++;
                        } else {
                           shallowWaterLayer.Add(layer, 1);
                        }
                     }
                  }
               }
            }
         }

         int spawnsPerLayer = (int) targetSpawns / 3;
         if (deepWaterLayer.Count > 0 && deepWaterTiles.Count > 0) {
            int maxTiles = 0;
            TilemapLayer mostCompatibleLayer = null;
            foreach (KeyValuePair<TilemapLayer, int> currLayer in deepWaterLayer) {
               if (currLayer.Value > maxTiles) {
                  mostCompatibleLayer = currLayer.Key;
                  maxTiles = currLayer.Value;
               }
            }

            if (mostCompatibleLayer != null) {
               StartCoroutine(CO_OpenWorldSpawnByDifficulty(instance, areaTarget, Voyage.Difficulty.Hard, spawnsPerLayer, mostCompatibleLayer, deepWaterTiles));
            } else {
               D.debug("No compatible layer for Hard difficulty");
            }
         }
         if (midWaterLayer.Count > 0 && midWaterTiles.Count > 0) {
            int maxTiles = 0;
            TilemapLayer mostCompatibleLayer = null;
            foreach (KeyValuePair<TilemapLayer, int> currLayer in midWaterLayer) {
               if (currLayer.Value > maxTiles) {
                  mostCompatibleLayer = currLayer.Key;
                  maxTiles = currLayer.Value;
               }
            }

            if (mostCompatibleLayer != null) {
               StartCoroutine(CO_OpenWorldSpawnByDifficulty(instance, areaTarget, Voyage.Difficulty.Medium, spawnsPerLayer, mostCompatibleLayer, midWaterTiles));
            } else {
               D.debug("No compatible layer for Medium difficulty");
            }
         }
         if (shallowWaterLayer.Count > 0 && shallowWaterTiles.Count > 0) {
            int maxTiles = 0;
            TilemapLayer mostCompatibleLayer = null;
            foreach (KeyValuePair<TilemapLayer, int> currLayer in shallowWaterLayer) {
               if (currLayer.Value > maxTiles) {
                  mostCompatibleLayer = currLayer.Key;
                  maxTiles = currLayer.Value;
               }
            }

            if (mostCompatibleLayer != null) {
               StartCoroutine(CO_OpenWorldSpawnByDifficulty(instance, areaTarget, Voyage.Difficulty.Easy, spawnsPerLayer, mostCompatibleLayer, shallowWaterTiles));
            } else {
               D.debug("No compatible layer for Easy difficulty");
            }
         }
         D.adminLog("Open World {" + areaKey + "} Enemies Finished spawning: {" + (NetworkTime.time - initialTime).ToString("f1") + "}", D.ADMIN_LOG_TYPE.EnemyWaterSpawn);
      }
   }

   private IEnumerator CO_OpenWorldSpawnByDifficulty (Instance instance, Area areaTarget, Voyage.Difficulty difficulty, int spawnsPerLayer, TilemapLayer layer, List<Vector3Int> availableTiles) {
      yield return new WaitForSeconds(.1f);
      int maxAttempts = 30;
      int successfulSpawns = 0;
      int indexCount = 0;
      HashSet<Vector3Int> occupiedTilesThisLayer = new HashSet<Vector3Int>();
      while (successfulSpawns < spawnsPerLayer && maxAttempts > 0) {
         indexCount++;
         maxAttempts--;
         if (maxAttempts < 1) {
            D.adminLog("Last attempt! Layer {" + layer.fullName + "} Cycles: {" + indexCount + "/" + maxAttempts + "} " +
               "Breaking cycle due to limitations Spawned: {" + successfulSpawns + "}", D.ADMIN_LOG_TYPE.EnemyWaterSpawn);
            break;
         }

         // Find a random value within the available list
         if (availableTiles.Count > 0) {
            Vector3Int randomSelectedTile = availableTiles.ChooseRandom();
            if (!occupiedTilesThisLayer.Contains(randomSelectedTile)) {
               // Mark occupied tiles and count the successful spawns 
               occupiedTilesThisLayer.Add(randomSelectedTile);

               // Fetch the tile information
               Vector3Int newVector = randomSelectedTile;
               TileBase newTile = layer.tilemap.GetTile(newVector);
               if (newTile != null) {
                  // Make sure tile is a water tile
                  Vector3 worldCoord = layer.tilemap.CellToWorld(newVector);
                  if (areaTarget.hasWaterTile(worldCoord) && !areaTarget.hasLandTile(worldCoord)) {
                     // Make sure that the new coord selected does not spawn within spawn blocker bounds
                     bool spawnsInBlocker = false;
                     foreach (OpenWorldSpawnBlocker spawnBlocker in areaTarget.openWorldSpawnBlockers) {
                        if (spawnBlocker.isWithinBounds(worldCoord)) {
                           spawnsInBlocker = true;
                        }
                     }

                     if (!spawnsInBlocker) {
                        successfulSpawns++;

                        // Initialize spawn values
                        float spawnShipChance = 60;
                        int randomEnemyTypeVal = Random.Range(0, 100);
                        int guildId = BotShipEntity.PIRATES_GUILD_ID;
                        bool forceSpawnSeamonsters = false;

                        // Override random value to fix ship spawning only if the map does not allow seamonsters
                        MapCreationTool.Serialization.Map mapInfo = AreaManager.self.getMapInfo(areaTarget.areaKey);
                        if (mapInfo != null) {
                           // Some maps do not allow spawning of sea monsters
                           if (!mapInfo.spawnsSeaMonsters && randomEnemyTypeVal >= spawnShipChance) {
                              D.debug("Map Data override! This map {" + areaTarget.areaKey + "} does not allow spawning of SeaMonsters!");
                              randomEnemyTypeVal = 0;
                           }

                           // Some maps only spawns seamonsters such as the areas between all the biomes
                           if ((Area.SpecialState) mapInfo.specialState == Area.SpecialState.SeaMonstersOnly) {
                              randomEnemyTypeVal = 100;
                              forceSpawnSeamonsters = true;
                           }
                        }

                        // Spawning ships has a 60% chance
                        Vector3 newSpawnPost = layer.tilemap.CellToWorld(newVector);
                        if (randomEnemyTypeVal < spawnShipChance) {
                           spawnBotShip(instance, areaTarget, newSpawnPost, guildId, false, true, difficulty);
                        } else {
                           if (mapInfo.spawnsSeaMonsters || forceSpawnSeamonsters) {
                              spawnSeaMonster(instance, areaTarget, newSpawnPost, false, true, guildId, difficulty);
                           } else {
                              spawnBotShip(instance, areaTarget, newSpawnPost, guildId, false, true, difficulty);
                           }
                        }

                        D.adminLog("Spawned and Found tile: " + newTile.name + " at " + newVector + "__" + difficulty, D.ADMIN_LOG_TYPE.EnemyWaterSpawn);
                     }
                  }
               }
            }
         }
      }

      D.adminLog("Processing A:{"+areaTarget.areaKey+":"+instance.id+"} " +
         "D:{" + difficulty + "} TCount:{" + availableTiles.Count + "} SCount:{"+successfulSpawns+"} Cycles:{"+indexCount+"}", D.ADMIN_LOG_TYPE.EnemyWaterSpawn);
   }

   public void spawnEnemyAtLocation(Enemy.Type enemyType, Instance instance, Vector2 location) {
        // If we don't have any spawners defined for this Area, then we're done
        if (instance == null)
        {
            D.warning("Invalid instance received when trying to spawn enemy " + enemyType);
            return;
        }

        // Create an Enemy in this instance
        Enemy enemy = Instantiate(PrefabsManager.self.enemyPrefab);
        enemy.transform.localPosition = location;
        enemy.enemyType = enemyType;
        enemy.areaKey = instance.areaKey;
        enemy.setAreaParent(AreaManager.self.getArea(instance.areaKey), false);

        BattlerData battlerData = MonsterManager.self.getBattlerData(enemy.enemyType);
        if (battlerData != null)
        {
            enemy.isBossType = battlerData.isBossType;
            enemy.isSupportType = battlerData.isSupportType;
            enemy.animGroupType = battlerData.animGroup;
            enemy.facing = Direction.South;
            enemy.displayNameText.text = battlerData.enemyName;
        }

        // Add it to the Instance
        InstanceManager.self.addEnemyToInstance(enemy, instance);
        NetworkServer.Spawn(enemy.gameObject);
    }

    public void spawnEnemiesOnServerForInstance (Instance instance) {
       // If we don't have any spawners defined for this Area, then we're done
       if (instance == null || !_spawners.ContainsKey(instance.areaKey)) {
          return;
       }

       foreach (Enemy_Spawner spawner in _spawners[instance.areaKey]) {
         Enemy.Type enemyType = spawner.getEnemyType(instance.biome);
         if (enemyType != Enemy.Type.PlayerBattler) {
            // Create an Enemy in this instance
            Enemy enemy = Instantiate(PrefabsManager.self.enemyPrefab);
            enemy.transform.localPosition = spawner.transform.localPosition;
            enemy.enemyType = enemyType;
            enemy.areaKey = instance.areaKey;
            enemy.setAreaParent(AreaManager.self.getArea(instance.areaKey), false);

            BattlerData battlerData = MonsterManager.self.getBattlerData(enemy.enemyType);
            if (battlerData != null) {
               enemy.isBossType = battlerData.isBossType;
               enemy.isSupportType = battlerData.isSupportType;
               enemy.animGroupType = battlerData.animGroup;
               enemy.facing = Direction.South;
               enemy.displayNameText.text = battlerData.enemyName;
            }

            // Add it to the Instance
            InstanceManager.self.addEnemyToInstance(enemy, instance);
            NetworkServer.Spawn(enemy.gameObject);
         }
       }
    }

   public void spawnShipsOnServerForInstance (Instance instance) {
      // If we don't have any spawners defined for this Area, then we're done
      if (instance == null || !_spawners.ContainsKey(instance.areaKey)) {
         return;
      }

      int guildId = BotShipEntity.PIRATES_GUILD_ID;
      Area area = AreaManager.self.getArea(instance.areaKey);
      int randomEnemyTypeVal = Random.Range(0, 100);
      int spawnsPerSpot = getSpawnsPerSpot(instance.difficulty);
      float spawnShipChance = 60;

      // Override random value to fix ship spawning only if the map does not allow seamonsters
      MapCreationTool.Serialization.Map mapInfo = AreaManager.self.getMapInfo(instance.areaKey);
      if (mapInfo != null) {
         if (!mapInfo.spawnsSeaMonsters && randomEnemyTypeVal >= spawnShipChance) {
            D.debug("Map Data override! This map {" + instance.areaKey + "} does not allow spawning of SeaMonsters!");
            randomEnemyTypeVal = 0;
         }
      }

      foreach (Enemy_Spawner spawner in _spawners[instance.areaKey]) {
         // Spawning ships has a 60% chance
         if (randomEnemyTypeVal < spawnShipChance) {
            for (int i = 0; i < spawnsPerSpot; i++) {
               spawnBotShip(instance, area, spawner.transform.localPosition, guildId, i!=0, false);
            }
         } else {
            for (int i = 0; i < spawnsPerSpot; i++) {
               if (mapInfo.spawnsSeaMonsters) {
                  spawnSeaMonster(instance, area, spawner.transform.localPosition, i != 0, false, guildId);
               } else {
                  spawnBotShip(instance, area, spawner.transform.localPosition, guildId, i != 0, false);
               }
            }
         }
      }
   }

   private void spawnBotShip (Instance instance, Area area, Vector2 localPosition, int guildId, bool isPositionRandomized, bool useWorldPosition, Voyage.Difficulty difficulty = Voyage.Difficulty.None) {
      Ship.Type shipType = Ship.Type.Type_1;
      int shipXmlId = SeaMonsterEntityData.DEFAULT_SHIP_ID;

      if (difficulty == Voyage.Difficulty.None) {
         shipType = randomizeShipType(instance.biome);
      } else {
         SeaMonsterEntityData randomShipByDifficulty = randomizeShipByDifficulty(difficulty, instance.biome);
         if (randomShipByDifficulty != null) {
            shipType = (Ship.Type) randomShipByDifficulty.subVarietyTypeId;
            shipXmlId = randomShipByDifficulty.xmlId;
         } else {
            shipType = randomizeShipType(instance.biome);
         }
      }

      SeaMonsterEntityData seaEnemyData = SeaMonsterManager.self.getAllSeaMonsterData().Find(ent => ent.subVarietyTypeId == (int) shipType && ent.xmlId == shipXmlId);
      if (seaEnemyData == null) {
         D.debug("Ship type {" + shipType + "} does not have matching data registered in sea enemy manager!");
         return;
      }

      if (isPositionRandomized && !useWorldPosition) {
         localPosition = getRandomWalkableLocalPositionAround(localPosition, area);
      }

      BotShipEntity botShip = Instantiate(PrefabsManager.self.botShipPrefab);
      botShip.areaKey = instance.areaKey;
      botShip.facing = Direction.South;
      if (useWorldPosition) {
         botShip.transform.position = localPosition;
         botShip.setAreaParent(area, true);
      } else {
         botShip.setAreaParent(area, false);
         botShip.transform.localPosition = localPosition;
      }
      botShip.seaEntityData = seaEnemyData;
      botShip.maxHealth = seaEnemyData.maxHealth;
      botShip.currentHealth = seaEnemyData.maxHealth;

      botShip.shipType = shipType;
      if (seaEnemyData.skillIdList.Count > 0) {
         botShip.primaryAbilityId = seaEnemyData.skillIdList[0];
         foreach (int abilityIdNew in seaEnemyData.skillIdList) {
            botShip.abilityList.Add(abilityIdNew);
         }
      }
      botShip.guildId = guildId;
      botShip.setShipData(seaEnemyData.xmlId, shipType, instance.difficulty);

      InstanceManager.self.addSeaMonsterToInstance(botShip, instance);
      NetworkServer.Spawn(botShip.gameObject);
   }

   private void spawnSeaMonster (Instance instance, Area area, Vector2 localPosition, bool isPositionRandomized, bool useWorldPosition, int guildId, Voyage.Difficulty difficulty = Voyage.Difficulty.None) {
      // Spawn sea monster type based on biome
      SeaMonsterEntity.Type seaMonsterType = SeaMonsterEntity.Type.None;

      if (difficulty == Voyage.Difficulty.None) {
         switch (instance.biome) {
            case Biome.Type.Forest:
            case Biome.Type.Pine:
               seaMonsterType = SeaMonsterEntity.Type.Fishman;
               break;
            case Biome.Type.Lava:
               seaMonsterType = SeaMonsterEntity.Type.Reef_Giant;
               break;
            case Biome.Type.Desert:
               seaMonsterType = SeaMonsterEntity.Type.Worm;
               break;
            default:
               D.debug("No specific monster for biome: {" + instance.biome + "} Spawning default Fishman");
               seaMonsterType = SeaMonsterEntity.Type.Fishman;
               break;
         }
      } else {
         SeaMonsterEntityData randomEnemyByDifficulty = randomizeMonsterByDifficulty(difficulty, instance.biome);
         if (randomEnemyByDifficulty != null) {
            seaMonsterType = randomEnemyByDifficulty.seaMonsterType;
         } else {
            spawnBotShip(instance, area, localPosition, guildId, false, true, difficulty);
            return;
         }
      }

      if (isPositionRandomized && !useWorldPosition) {
         localPosition = getRandomWalkableLocalPositionAround(localPosition, area);
      }

      SeaMonsterEntity seaEntity = Instantiate(PrefabsManager.self.seaMonsterPrefab);
      SeaMonsterEntityData data = SeaMonsterManager.self.getMonster(seaMonsterType);
      if (data == null) {
         D.debug("Failed to spawn seamonster type {" + seaMonsterType + "}");
         return;
      }

      // Basic setup
      seaEntity.monsterType = data.seaMonsterType;
      seaEntity.areaKey = instance.areaKey;
      seaEntity.facing = Direction.South;
      seaEntity.setAreaParent(area, true);

      // Transform setup
      if (useWorldPosition) {
         seaEntity.transform.position = localPosition;
      } else {
         seaEntity.transform.localPosition = localPosition;
      }

      // Network Setup
      InstanceManager.self.addSeaMonsterToInstance(seaEntity, instance);
      NetworkServer.Spawn(seaEntity.gameObject);
   }

   private Vector2 getRandomWalkableLocalPositionAround (Vector2 localPosition, Area area) {
      Vector2 spawnWorldPosition = localPosition + (Vector2)area.transform.position + Random.insideUnitCircle.normalized * Random.Range(SPAWN_POSITION_DISTANCE_MIN, SPAWN_POSITION_DISTANCE_MAX);
      NNInfo nodeInfo = AstarPath.active.GetNearest(spawnWorldPosition, NNConstraint.Default);
      if (nodeInfo.node != null) {
         return (Vector3) nodeInfo.node.position - area.transform.position;
      } else {
         return localPosition;
      }
   }

   private SeaMonsterEntityData randomizeShipByDifficulty (Voyage.Difficulty difficulty, Biome.Type biomeType) {
      List<SeaMonsterEntityData> seaMonsterList = SeaMonsterManager.self.getAllSeaMonsterData().FindAll(
         _ => _.difficultyLevel == difficulty
         // Ships have variety types
         && _.subVarietyTypeId > 0
         // There are biome filters that needs consideration
         && _.biomes.Contains(biomeType));
      if (seaMonsterList.Count > 0) {
         return seaMonsterList.ChooseRandom();
      }

      return null;
   }

   private SeaMonsterEntityData randomizeMonsterByDifficulty (Voyage.Difficulty difficulty, Biome.Type biomeType) {
      List<SeaMonsterEntityData> seaMonsterList = SeaMonsterManager.self.getAllSeaMonsterData().FindAll(
            _ => _.difficultyLevel == difficulty
            // Sea monsters do not have variety types (for now)
            && _.subVarietyTypeId < 1 
            // There are biome filters that needs consideration
            && _.biomes.Contains(biomeType)
            // Only pick standalone monsters
            && _.roleType == RoleType.Standalone);
      if (seaMonsterList.Count > 0) {
         return seaMonsterList.ChooseRandom();
      }

      return null;
   }

   private Ship.Type randomizeShipType (Biome.Type biome) {
      Ship.Type shipType = Ship.Type.Type_1;

      switch (biome) {
         case Biome.Type.Forest:
            shipType = Ship.Type.Type_1;
            break;
         case Biome.Type.Desert:
            shipType = new List<Ship.Type> { Ship.Type.Type_1, Ship.Type.Type_2, Ship.Type.Type_3 }.ChooseRandom();
            break;
         case Biome.Type.Pine:
            shipType = new List<Ship.Type> { Ship.Type.Type_1, Ship.Type.Type_2 }.ChooseRandom();
            break;
         case Biome.Type.Snow:
            shipType = new List<Ship.Type> { Ship.Type.Type_2, Ship.Type.Type_3, Ship.Type.Type_4 }.ChooseRandom();
            break;
         case Biome.Type.Lava:
            shipType = new List<Ship.Type> { Ship.Type.Type_4, Ship.Type.Type_5, Ship.Type.Type_6 }.ChooseRandom();
            break;
         case Biome.Type.Mushroom:
            shipType = new List<Ship.Type> { Ship.Type.Type_6, Ship.Type.Type_7, Ship.Type.Type_8 }.ChooseRandom();
            break;
      }

      return shipType;
   }

   public int randomizeShipXmlId (Biome.Type biome) {
      int xmlId = 0;
      List<SeaMonsterEntityData> seaEnemyList = SeaMonsterManager.self.seaMonsterDataList.FindAll(_ => _.seaMonsterType == SeaMonsterEntity.Type.PirateShip);

      switch (biome) {
         case Biome.Type.Forest:
            xmlId = 17;
            break;
         case Biome.Type.Desert:
            xmlId = new List<int> { 
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_1).xmlId,
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_2).xmlId,
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_3).xmlId
            }.ChooseRandom();
            break;
         case Biome.Type.Pine:
            xmlId = new List<int> {
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_1).xmlId,
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_2).xmlId
            }.ChooseRandom();
            break;
         case Biome.Type.Snow:
            xmlId = new List<int> {
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_2).xmlId,
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_3).xmlId,
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_4).xmlId
            }.ChooseRandom();
            break;
         case Biome.Type.Lava:
            xmlId = new List<int> {
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_4).xmlId,
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_5).xmlId,
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_6).xmlId
            }.ChooseRandom();
            break;
         case Biome.Type.Mushroom:
            xmlId = new List<int> {
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_6).xmlId,
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_7).xmlId,
               seaEnemyList.Find(_ => _.subVarietyTypeId == (int) Ship.Type.Type_8).xmlId
            }.ChooseRandom();
            break;
      }

      return xmlId;
   }

   public static int getSpawnsPerSpot (int difficulty) {
      return getSpawnsPerSpot(difficulty, AdminGameSettingsManager.self.settings.seaSpawnsPerSpot);
   }

   public static int getSpawnsPerSpot (int difficulty, float adminParameter) {
      return Mathf.CeilToInt(((float)difficulty / 2f) * adminParameter);
   }

   #region Private Variables

   // The distance from the spawn point where enemies can spawn, when the position is randomized
   private static float SPAWN_POSITION_DISTANCE_MIN = 0.5f;
   private static float SPAWN_POSITION_DISTANCE_MAX = 1f;

   // Stores a list of Enemy Spawners for each type of Site
   protected Dictionary<string, List<Enemy_Spawner>> _spawners = new Dictionary<string, List<Enemy_Spawner>>();

   #endregion
}