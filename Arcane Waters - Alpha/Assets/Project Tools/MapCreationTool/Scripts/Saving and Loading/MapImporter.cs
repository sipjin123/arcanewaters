using UnityEngine;
using MapCreationTool.Serialization;
using System.Linq;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System;
using MapCustomization;

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

      public static void instantiateTilemapLayer (List<TilemapLayer> tilemaps, MapInfo mapInfo, ExportedLayer001 layer,
            Transform tilemapParent, Transform collisionTilemapParent, Biome.Type biome, ref int unrecognizedTiles) {

         // Create the tilemap gameobject
         var tilemap = UnityEngine.Object.Instantiate(AssetSerializationMaps.tilemapTemplate, tilemapParent);
         tilemap.transform.localPosition = new Vector3(0, 0, layer.z);
         tilemap.gameObject.name = layer.name + " " + layer.sublayer;

         // Add all the tiles
         Vector3Int[] positions = layer.tiles.Select(t => new Vector3Int(t.x, t.y, 0)).ToArray();
         TileBase[] tiles = layer.tiles
            .Select(t => AssetSerializationMaps.tryGetTile(new Vector2Int(t.i, t.j), biome))
            .Where(t => t != null)
            .ToArray();

         // Check if the layer is recognized
         unrecognizedTiles += layer.tiles.Length - tiles.Length;

         // Ensure the 'sprite' of animated tiles is set
         foreach (TileBase tileBase in tiles) {
            AnimatedTile aTile = tileBase as AnimatedTile;
            if (aTile != null) {
               if (aTile.sprite == null && aTile.m_AnimatedSprites.Length > 0) {
                  aTile.sprite = aTile.m_AnimatedSprites[0];
               }
            }
         }

         // Set the tiles in the tilemap
         tilemap.SetTiles(positions, tiles);

         // Add the layer to the list
         tilemaps.Add(new TilemapLayer {
            tilemap = tilemap,
            fullName = layer.name + " " + layer.sublayer ?? "",
            name = layer.name ?? "",
            type = layer.type
         });
      }

      public static MapChunk instantiateTilemapColliderChunk (ExportedProject001 exportedProject,
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
               .Select(t => AssetSerializationMaps.tryGetTile(new Vector2Int(t.i, t.j), biome))
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
                  GameObject stairs = UnityEngine.Object.Instantiate(AssetSerializationMaps.stairsEffector, map.effectorContainer);
                  stairs.transform.localPosition = chunk.position;
                  stairs.transform.localScale = Vector3.one;
                  stairs.GetComponent<BoxCollider2D>().size = chunk.size;
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
                  waterfall.transform.localPosition = chunk.position * 0.16f + Vector2.up * (chunk.size.y - 1) * 0.5f * 0.16f;
                  waterfall.transform.localScale = Vector3.one;
                  waterfall.setSize(chunk.size);
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

         container.AddComponent<EdgeCollider2D>().points = new Vector2[] { new Vector2(bounds.min.x, bounds.max.y), bounds.max };
         container.AddComponent<EdgeCollider2D>().points = new Vector2[] { bounds.max, new Vector2(bounds.max.x, bounds.min.y) };
         container.AddComponent<EdgeCollider2D>().points = new Vector2[] { new Vector2(bounds.max.x, bounds.min.y), bounds.min };
         container.AddComponent<EdgeCollider2D>().points = new Vector2[] { bounds.min, new Vector2(bounds.min.x, bounds.max.y) };
      }

      /// <summary>
      /// Sets the bounds the the camera can move in inside a map
      /// </summary>
      /// <param name="map"></param>
      /// <param name="tiles"></param>
      public static void setCameraBounds (MapTemplate map, Bounds bounds) {
         map.camBounds.points = new Vector2[] {
            new Vector2(bounds.min.x, bounds.min.y),
            new Vector2(bounds.min.x, bounds.max.y),
            new Vector2(bounds.max.x, bounds.max.y),
            new Vector2(bounds.max.x, bounds.min.y)
         };

         map.confiner.m_BoundingShape2D = map.camBounds;
      }

      public static Bounds calculateBounds (ExportedProject001 project) {
         Bounds bounds = new Bounds();

         foreach (var layer in project.layers) {
            foreach (var tile in layer.tiles) {
               bounds.min = new Vector3(Mathf.Min(tile.x, bounds.min.x), Mathf.Min(tile.y, bounds.min.y), 0);
               bounds.max = new Vector3(Mathf.Max(tile.x, bounds.max.x), Mathf.Max(tile.y, bounds.max.y), 0);
            }
         }

         bounds.max += new Vector3(1, 1, 0);

         return bounds;
      }

      public static void instantiatePrefabs (MapInfo mapInfo, ExportedProject001 project, Transform prefabParent, Transform npcParent, Area area) {
         List<ExportedPrefab001> npcData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> enemyData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> seaMonstersData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> oreData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> treasureSiteData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> shipData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> secretsData = new List<ExportedPrefab001>();

         int unrecognizedPrefabs = 0;
         int cropSpotCounter = 0;

         foreach (var prefab in project.prefabs) {
            GameObject original = AssetSerializationMaps.tryGetPrefabGame(prefab.i, project.biome);
            if (original == null) {
               unrecognizedPrefabs++;
               continue;
            }

            if (original.GetComponent<CropSpot>() != null) {
               cropSpotCounter++;
               original.GetComponent<CropSpot>().cropNumber = cropSpotCounter;
            }

            if (original.GetComponent<Enemy>() != null) {
               if (prefab.d != null) {
                  enemyData.Add(prefab);
               }
            } else if (original.GetComponent<SeaMonsterEntity>() != null) {
               if (prefab.d != null) {
                  seaMonstersData.Add(prefab);
               }
            } else if (original.GetComponent<TreasureSite>() != null) {
               if (prefab.d != null) {
                  treasureSiteData.Add(prefab);
               }
            } else if (original.GetComponent<OreSpot>() != null) {
               if (prefab.d != null) {
                  oreData.Add(prefab);
               }
            } else if (original.GetComponent<NPC>() != null) {
               if (prefab.d != null) {
                  npcData.Add(prefab);
               }
            } else if (original.GetComponent<ShipEntity>() != null) {
               if (prefab.d != null) {
                  shipData.Add(prefab);
               }
            } else if (original.GetComponent<SecretEntrance>() != null) {
               if (prefab.d != null) {
                  secretsData.Add(prefab);
               }
            } else {
               Vector3 targetLocalPos = new Vector3(prefab.x, prefab.y, 0) * 0.16f + Vector3.back * 10;

               var pref = UnityEngine.Object.Instantiate(
                  original,
                  prefabParent.TransformPoint(targetLocalPos),
                  Quaternion.identity,
                  prefabParent);

               foreach (IBiomable biomable in pref.GetComponentsInChildren<IBiomable>()) {
                  biomable.setBiome(project.biome);
               }

               foreach (ZSnap snap in pref.GetComponentsInChildren<ZSnap>()) {
                  snap.inheritedOffsetZ = prefab.iz;
                  snap.snapZ();

                  // Remove colliders if the prefab is placed on something
                  if (snap.inheritedOffsetZ != 0) {
                     foreach (Collider2D col in pref.GetComponents<Collider2D>()) {
                        UnityEngine.Object.Destroy(col);
                     }
                  }
               }

               if (prefab.d != null) {
                  foreach (IMapEditorDataReceiver receiver in pref.GetComponents<IMapEditorDataReceiver>()) {
                     receiver.receiveData(prefab.d);
                  }
               }

               CustomizablePrefab customizablePrefab = pref.GetComponent<CustomizablePrefab>();
               if (customizablePrefab != null) {
                  customizablePrefab.mapEditorState.localPosition = pref.transform.localPosition;
                  customizablePrefab.mapEditorState.id = DataField.extractId(prefab.d);
                  customizablePrefab.mapEditorState.created = true;

                  customizablePrefab.customizedState = customizablePrefab.mapEditorState;
                  customizablePrefab.unappliedChanges = customizablePrefab.mapEditorState;
                  customizablePrefab.unappliedChanges.clearAll();
               }
            }
         }

         if (unrecognizedPrefabs > 0) {
            Utilities.warning($"Could not recognize { unrecognizedPrefabs } prefabs of map { mapInfo.mapName }");
         }

         area.registerNetworkPrefabData(npcData, enemyData, oreData, treasureSiteData, shipData, secretsData, seaMonstersData);
      }

      public static void ensureSerializationMapsLoaded () {
         if (AssetSerializationMaps.loaded)
            return;

         AssetSerializationMaps.load();
      }
   }
}

