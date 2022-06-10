using UnityEngine;
using MapCreationTool.Serialization;
using System.Linq;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;
using System;
using MapCustomization;
using FMODUnity;
using Assets.Scripts.Map;
using MapObjectStateVariables;

namespace MapCreationTool
{
   public class MapImporter
   {
      public static ExportedProject001 deserializeMapData (MapInfo mapInfo, string areaKey) {
         try {
            return JsonUtility.FromJson<ExportedProject001>(mapInfo.gameData);
         } catch {
            D.editorLog("Failed to deserialize Map data for: " + areaKey, Color.red);
            return null;
         }
      }

      public static MapChunk instantiateTilemapColliderChunk (string areaKey, ExportedProject001 exportedProject,
         Transform collisionTilemapChunkParent, Biome.Type biome, RectInt bounds) {

         // Instantiate the grid chunk
         var chunk = UnityEngine.Object.Instantiate(AssetSerializationMaps.collisionTilemapChunkTemplate, collisionTilemapChunkParent);
         chunk.transform.localPosition = Vector3.zero;
         chunk.gameObject.name = "Grid " + bounds.xMin + " " + bounds.yMin;

         // Add special case tiles if defined
         ExportedLayer001 atc = exportedProject.additionalTileColliders;
         IEnumerable<ExportedLayer001> layers = atc == null || atc.tiles == null || atc.tiles.Length == 0
            ? exportedProject.layers
            : exportedProject.layers.Union(Enumerable.Repeat(atc, 1));

         foreach (ExportedLayer001 layer in layers) {
            // Instantiate the layer 
            var colTilemap = UnityEngine.Object.Instantiate(AssetSerializationMaps.collisionTilemapTemplate, chunk.transform);
            colTilemap.transform.localPosition = Vector3.zero;
            colTilemap.gameObject.name = layer.name + " " + layer.sublayer;

            // Select the tiles inside the chunk
            Vector3Int[] positions = layer.tiles
            .Where(t => t.c == 1 && t.x >= bounds.xMin && t.x < bounds.xMax && t.y >= bounds.yMin && t.y < bounds.yMax)
            .Select(t => new Vector3Int(t.x, t.y, 0))
            .ToArray();
            TileBase[] tiles = layer.tiles
               .Where(t => t.c == 1 && t.x >= bounds.xMin && t.x < bounds.xMax && t.y >= bounds.yMin && t.y < bounds.yMax)
               .Select(t => AssetSerializationMaps.getTile(t.i, t.j, biome))
               .Where(t => t != null)
               .ToArray();

            // Set the tiles
            colTilemap.SetTiles(positions, tiles);

            // Enable the collider
            colTilemap.GetComponent<TilemapCollider2D>().enabled = true;
         }

         // Initialize the map chunk
         chunk.initialize(bounds);

         return chunk;
      }

      public static void addSpecialTileChunks (MapTemplate map, SpecialTileChunk[] chunks) {
         foreach (SpecialTileChunk chunk in chunks) {
            switch (chunk.type) {
               case SpecialTileChunk.Type.Stair:
               case SpecialTileChunk.Type.StairNorth:
                  GameObject stairs = UnityEngine.Object.Instantiate(AssetSerializationMaps.stairsEffector, map.effectorContainer);
                  stairs.transform.localPosition = chunk.position;
                  stairs.transform.localScale = Vector3.one;
                  stairs.GetComponent<BoxCollider2D>().size = chunk.size;
                  if (chunk.type == SpecialTileChunk.Type.StairNorth) {
                     // For north effectors, increase the drag so player moves slower
                     AreaEffector2D effector = stairs.GetComponentInChildren<AreaEffector2D>();
                     if (effector != null) {
                        effector.drag *= 10f;
                     }
                  }
                  break;
               case SpecialTileChunk.Type.Vine:
                  Vines vines = UnityEngine.Object.Instantiate(AssetSerializationMaps.vinesTrigger, map.effectorContainer);
                  vines.transform.localPosition = chunk.position - Vector2.up * 0.4f;
                  vines.transform.localScale = Vector3.one;
                  vines.GetComponent<BoxCollider2D>().size = chunk.size - new Vector2(0.6f, 0.9f);
                  break;
               case SpecialTileChunk.Type.Waterfall:
                  Ledge waterfall = UnityEngine.Object.Instantiate(AssetSerializationMaps.ledgePrefab, map.prefabParent);
                  waterfall.name = "Waterfall Ledge";
                  waterfall.transform.localPosition = chunk.position * 0.16f + Vector2.down * 0.16f * 0.5f;
                  waterfall.transform.localScale = Vector3.one;
                  waterfall.setSize(chunk.size + Vector2.up * 0f + Vector2.left * 0.9f);

                  // Attaching a FMOD event emitter
                  if (SoundEffectManager.self != null) {
                     BoxCollider2D waterfallCollider = waterfall.GetComponent<BoxCollider2D>();
                     Bounds bounds = waterfallCollider.bounds;

                     // FMOD 3D emitter position
                     GameObject emitterGo = new GameObject();
                     emitterGo.name = "Waterfall Emitter";
                     emitterGo.transform.SetParent(waterfall.transform);

                     Vector2 emitterPos = new Vector2(bounds.center.x, bounds.center.y);
                     emitterGo.transform.position = emitterPos;

                     StudioEventEmitter emitterScript = emitterGo.AddComponent<StudioEventEmitter>();
                     emitterScript.PlayEvent = EmitterGameEvent.ObjectStart;
                     emitterScript.StopEvent = EmitterGameEvent.ObjectDestroy;
                     emitterScript.Event = SoundEffectManager.CALMING_WATERFALL;
                  }

                  break;
               case SpecialTileChunk.Type.Current:
                  GameObject current = UnityEngine.Object.Instantiate(AssetSerializationMaps.currentEffector, map.effectorContainer);
                  current.transform.localPosition = chunk.position;
                  current.transform.localScale = Vector3.one;
                  current.GetComponentInChildren<AreaEffector2D>().forceAngle = ((int) chunk.effectorDirection - 3) * -45f;
                  PolygonCollider2D poly = current.GetComponentInChildren<PolygonCollider2D>();
                  poly.pathCount = chunk.paths.Length;
                  for (int i = 0; i < chunk.paths.Length; i++) {
                     poly.SetPath(i, chunk.paths[i].points);
                  }
                  break;
               case SpecialTileChunk.Type.Rug1:
               case SpecialTileChunk.Type.Rug2:
               case SpecialTileChunk.Type.Rug3:
               case SpecialTileChunk.Type.Rug4:
                  GameObject rug = new GameObject("Rug Marker");
                  rug.transform.parent = map.rugMarkerParent;
                  RugMarker marker = rug.AddComponent<RugMarker>();
                  marker.type = (int) chunk.type - 4;
                  marker.center = new Vector2(chunk.position.x * 0.16f, chunk.position.y * 0.16f);
                  marker.size = new Vector2(chunk.size.x * 0.16f, chunk.size.y * 0.16f);
                  rug.transform.localPosition = marker.center;
                  marker.processData();
                  break;
            }
         }
      }

      public static void addEdgeColliders (MapTemplate map, Bounds bounds) {
         GameObject container = new GameObject("Edge Colliders");
         container.transform.parent = map.transform;
         container.transform.localPosition = Vector2.zero;
         container.transform.localScale = 0.16f * Vector2.one;

         MapEdges edges = container.AddComponent<MapEdges>();

         edges.top = container.AddComponent<EdgeCollider2D>();
         edges.top.points = new Vector2[] { new Vector2(bounds.min.x, bounds.max.y), bounds.max };

         edges.right = container.AddComponent<EdgeCollider2D>();
         edges.right.points = new Vector2[] { bounds.max, new Vector2(bounds.max.x, bounds.min.y) };

         edges.bottom = container.AddComponent<EdgeCollider2D>();
         edges.bottom.points = new Vector2[] { new Vector2(bounds.max.x, bounds.min.y), bounds.min };

         edges.left = container.AddComponent<EdgeCollider2D>();
         edges.left.points = new Vector2[] { bounds.min, new Vector2(bounds.min.x, bounds.max.y) };
      }

      /// <summary>
      /// Sets the bounds the the camera can move in inside a map
      /// </summary>
      /// <param name="map"></param>
      /// <param name="tiles"></param>
      public static void setCameraBounds (MapTemplate map, Bounds bounds) {
         map.mapCameraBounds.setMapBoundingBox(bounds);

         map.confiner.m_BoundingShape2D = map.mapCameraBounds.getCameraBoundsShape();

         Vector3 confinerPosition = map.area.cameraBounds.transform.localPosition;
         map.area.cameraBounds.transform.localScale = new Vector3(0.16f, 0.16f, 0.16f);
         map.area.cameraBounds.transform.localPosition = new Vector3(confinerPosition.x, confinerPosition.y, confinerPosition.z);
      }

      public static Bounds calculateBounds (ExportedProject001 project) {
         int minX = int.MaxValue;
         int minY = int.MaxValue;
         int maxX = int.MinValue;
         int maxY = int.MinValue;

         foreach (var layer in project.layers) {
            foreach (var tile in layer.tiles) {
               minX = Mathf.Min(tile.x, minX);
               minY = Mathf.Min(tile.y, minY);
               maxX = Mathf.Max(tile.x, maxX);
               maxY = Mathf.Max(tile.y, maxY);
            }
         }

         if (minX == int.MaxValue) {
            // Project has no tiles
            minX = 0;
            minY = 0;
            maxX = 0;
            maxY = 0;
         }

         Bounds bounds = new Bounds();

         bounds.min = new Vector3(minX, minY, 0);
         bounds.max = new Vector3(maxX, maxY, 0);

         bounds.max += new Vector3(1, 1, 0);

         return bounds;
      }

      public static IEnumerator CO_InstantiatePrefabs (MapInfo mapInfo, ExportedProject001 project, Transform prefabParent, Transform npcParent, Area area) {
         List<ExportedPrefab001> npcData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> enemyData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> seaMonstersData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> oreData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> treasureSiteData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> shipData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> bossSpawnerData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> pvpTowerData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> windowInteractData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> largeWindowInteractData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> pvpBaseData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> pvpShipyardTowerData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> pvpWaypointsData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> pvpMonsterSpawnerData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> pvpLootSpawnData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> pvpCaptureTargetHolderData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> varyingStateObjectData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> whirlpoolData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> pvpNpcData = new List<ExportedPrefab001>();
         OpenWorldController openWorldController = null;
         OreNodeMapController oreController = null;

         int unrecognizedPrefabs = 0;
         int cropSpotCounter = 0;
         int spawnIdCounter = 0;

         int waitFrameCounter = 0;
         foreach (var prefab in project.prefabs) {
            // Wait a frame periodically to avoid freezing for a long time
            waitFrameCounter++;
            if (waitFrameCounter > 100) {
               waitFrameCounter = 0;
               yield return null;
            }

            GameObject original = AssetSerializationMaps.tryGetPrefabGame(prefab.i, project.biome);
            if (original == null) {
               unrecognizedPrefabs++;
               continue;
            }

            if (original.TryGetComponent(out CropSpot csp)) {
               cropSpotCounter++;
               csp.cropNumber = cropSpotCounter;
               csp.areaKey = area.areaKey;
            }

            if (original.TryGetComponent(out Enemy _)) {
               if (prefab.d != null) {
                  enemyData.Add(prefab);
               }
            } else if (original.TryGetComponent(out SeaMonsterEntity _)) {
               if (prefab.d != null) {
                  seaMonstersData.Add(prefab);
               }
            } else if (original.TryGetComponent(out TreasureSite _)) {
               if (prefab.d != null) {
                  treasureSiteData.Add(prefab);
               }
            } else if (original.TryGetComponent(out OreSpot _)) {
               if (prefab.d != null) {
                  oreData.Add(prefab);
               }
            } else if (original.TryGetComponent(out PvpTower _)) {
               if (prefab.d != null) {
                  pvpTowerData.Add(prefab);
               }
            } else if (original.TryGetComponent(out PvpBase _)) {
               if (prefab.d != null) {
                  pvpBaseData.Add(prefab);
               }
            } else if (original.TryGetComponent(out PvpShipyard _)) {
               if (prefab.d != null) {
                  pvpShipyardTowerData.Add(prefab);
               }
            } else if (original.TryGetComponent(out PvpWaypoint _)) {
               if (prefab.d != null) {
                  pvpWaypointsData.Add(prefab);
               }
            } else if (original.TryGetComponent(out BossSpawner _)) {
               if (prefab.d != null) {
                  bossSpawnerData.Add(prefab);
               }
            } else if (original.TryGetComponent(out WindowInteractable wi)) {
               if (prefab.d != null) {
                  if (wi.isLargeWindow) {
                     largeWindowInteractData.Add(prefab);
                  } else {
                     windowInteractData.Add(prefab);
                  }
               }
            } else if (original.TryGetComponent(out VaryingStateObject _)) {
               if (prefab.d != null) {
                  varyingStateObjectData.Add(prefab);
               }
            } else if (original.TryGetComponent(out NPC _)) {
               if (prefab.d != null) {
                  npcData.Add(prefab);
               }
            } else if (original.TryGetComponent(out PvpMonsterSpawner _)) {
               if (prefab.d != null) {
                  pvpMonsterSpawnerData.Add(prefab);
               }
            } else if (original.TryGetComponent(out ShipEntity _)) {
               if (prefab.d != null) {
                  shipData.Add(prefab);
               }
            } else if (original.TryGetComponent(out PvpLootSpawn _)) {
               if (prefab.d != null) {
                  pvpLootSpawnData.Add(prefab);
               }
            } else if (original.TryGetComponent(out PvpCaptureTargetHolder _)) {
               if (prefab.d != null) {
                  pvpCaptureTargetHolderData.Add(prefab);
               }
            } else if (original.TryGetComponent(out WhirlpoolEffector _)) {
               if (prefab.d != null) {
                  whirlpoolData.Add(prefab);
               }
            } else if (original.TryGetComponent(out PvpNpc _)) {
               if (prefab.d != null) {
                  pvpNpcData.Add(prefab);
               }
            } else {
               Vector3 targetLocalPos = new Vector3(prefab.x, prefab.y, 0) * 0.16f + Vector3.back * 10;

               GameObject pref = UnityEngine.Object.Instantiate(
                  original,
                  prefabParent.TransformPoint(targetLocalPos),
                  Quaternion.identity,
                  prefabParent);

               // Register treasure spot prefab id 
               if (original.TryGetComponent(out TreasureSpot _)) {
                  int prefabId = 0;
                  foreach (DataField field in prefab.d) {
                     if (field.k.CompareTo(DataField.PLACED_PREFAB_ID) == 0) {
                        // Get ID from the data field
                        if (field.tryGetIntValue(out int id)) {
                           prefabId = id;
                           break;
                        }
                     }
                  }
                  pref.GetComponent<TreasureSpot>().mapDataId = prefabId;
               } else if (original.TryGetComponent(out DisplayAnvil _)) {
                  if (prefab.d != null) {
                     foreach (DataField field in prefab.d) {
                        if (field.k.CompareTo(DataField.IS_FUNCTIONAL_ANVIL) == 0) {
                           if (field.v == "True") {
                              pref.GetComponent<DisplayAnvil>().loadFunctionalAnvil();
                           }
                        }
                     }
                  }
               } else if (original.TryGetComponent(out SecretEntranceHolder _)) {
                  SecretEntranceHolder secretEntranceObj = pref.GetComponent<SecretEntranceHolder>();
                  if (prefab.d != null) {
                     // Make sure obj has correct data
                     IMapEditorDataReceiver receiver = pref.GetComponent<IMapEditorDataReceiver>();
                     if (receiver != null && prefab.d != null) {
                        receiver.receiveData(prefab.d);
                     }

                     secretEntranceObj.areaKey = area.areaKey;

                     // Transform Setup
                     secretEntranceObj.transform.localPosition = targetLocalPos;
                     secretEntranceObj.setAreaParent(area, false);

                     secretEntranceObj.spawnId = spawnIdCounter;
                     spawnIdCounter++;

                     try {
                        area.registerWarpFromSecretEntrance(secretEntranceObj.cachedSecretEntrance.warp);
                     } catch {
                        D.debug("No warp assigned to secret entrance!");
                     }
                  }
               } else if (original.TryGetComponent(out Spawn _)) {
                  pref.transform.localScale = new Vector3(0.16f, 0.16f, 1f);
               } else if (original.TryGetComponent(out SpiderWeb _)) {
                  pref.GetComponent<SpiderWeb>().initializeBiome(project.biome);
               } else if (original.TryGetComponent(out GenericActionTrigger _)) {
                  pref.GetComponent<GenericActionTrigger>().biomeType = project.biome;
               } else if (original.TryGetComponent(out OreNodeMapController _)) {
                  oreController = pref.GetComponent<OreNodeMapController>();
               } else if (original.TryGetComponent(out OpenWorldController _)) {
                  openWorldController = pref.GetComponent<OpenWorldController>();
               } else if (original.TryGetComponent(out OpenWorldSpawnBlocker _)) {
                  OpenWorldSpawnBlocker openWorldBlocker = pref.GetComponent<OpenWorldSpawnBlocker>();
                  area.openWorldSpawnBlockers.Add(openWorldBlocker);
               } else if (original.TryGetComponent(out PvpShopEntity _)) {
                  PvpShopEntity shopEntity = pref.GetComponent<PvpShopEntity>();
                  if (area.isSea && VoyageManager.isAnyLeagueArea(area.areaKey) && !VoyageManager.isPvpArenaArea(area.areaKey)) {
                     shopEntity.enableShop(false);
                  }
                  shopEntity.isSeaShop = area.isSea;
               } else if (original.TryGetComponent(out Butterfly _)) {
                  Butterfly butterfly = pref.GetComponent<Butterfly>();
                  butterfly.setAreaKey(area.areaKey);
               }

               foreach (IBiomable biomable in pref.GetComponentsInChildren<IBiomable>()) {
                  biomable.setBiome(project.biome, !Mirror.NetworkClient.active);
               }

               foreach (ZSnap snap in pref.GetComponentsInChildren<ZSnap>()) {
                  snap.inheritedOffsetZ = prefab.iz;
                  snap.snapZ();

                  // Remove colliders if the prefab is placed on something
                  if (snap.inheritedOffsetZ != 0) {
                     foreach (Collider2D col in pref.GetComponents<Collider2D>()) {
                        col.isTrigger = true;
                     }
                  }
               }

               if (pref.TryGetComponent(out SetTileAttributeInArea setTiles)) {
                  if (setTiles.areaCollider != null) {
                     for (float x = setTiles.areaCollider.bounds.min.x; x < setTiles.areaCollider.bounds.max.x + 0.16f; x += 0.16f) {
                        for (float y = setTiles.areaCollider.bounds.min.y; y < setTiles.areaCollider.bounds.max.y + 0.16f; y += 0.16f) {
                           if (setTiles.areaCollider.OverlapPoint(new Vector2(x, y))) {
                              area.setTileAttribute(setTiles.type, new Vector2(x, y));
                           }
                        }
                     }
                  }
               }

               if (pref.TryGetComponent(out CustomizablePrefab customizablePrefab)) {
                  customizablePrefab.isPermanent = true;

                  customizablePrefab.mapEditorState.localPosition = pref.transform.localPosition;
                  customizablePrefab.mapEditorState.id = DataField.extractId(prefab.d);
                  customizablePrefab.mapEditorState.created = true;
                  customizablePrefab.mapEditorState.serializationId = prefab.i;

                  customizablePrefab.customizedState = customizablePrefab.mapEditorState;
                  customizablePrefab.unappliedChanges = customizablePrefab.mapEditorState;
                  customizablePrefab.unappliedChanges.clearAll();
               }

               if (prefab.d != null) {
                  foreach (IMapEditorDataReceiver receiver in pref.GetComponents<IMapEditorDataReceiver>()) {
                     receiver.receiveData(prefab.d);
                  }
               }
            }
         }

         if (unrecognizedPrefabs > 0) {
            Utilities.warning($"Could not recognize { unrecognizedPrefabs } prefabs of map { mapInfo.mapName }");
         }

         area.registerNetworkPrefabData(npcData, enemyData, oreData, treasureSiteData,
            shipData, seaMonstersData, bossSpawnerData, pvpTowerData,
            pvpBaseData, pvpShipyardTowerData, pvpWaypointsData, pvpMonsterSpawnerData,
            pvpLootSpawnData, pvpCaptureTargetHolderData, oreController, openWorldController,
            windowInteractData, largeWindowInteractData, varyingStateObjectData, whirlpoolData,
            pvpNpcData);
      }
   }
}

