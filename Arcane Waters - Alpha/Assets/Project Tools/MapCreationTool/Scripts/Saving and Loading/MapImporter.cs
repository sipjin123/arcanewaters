using UnityEngine;
using MapCreationTool.Serialization;
using System.Linq;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System;

namespace MapCreationTool
{
   public class MapImporter
   {
      public const string DataFileExtension = "arcane";

      /// <summary>
      /// Creates an instance of a map from the serialized map data
      /// </summary>
      /// <param name="data"></param>
      public static Area instantiateMapData (MapInfo mapInfo, string areaKey, Vector3 position) {

         ensureSerializationMapsLoaded();

         MapTemplate result = UnityEngine.Object.Instantiate(AssetSerializationMaps.mapTemplate, position, Quaternion.identity);
         result.name = areaKey;

         Area area = result.area;
         area.areaKey = areaKey;
         area.version = mapInfo.version;

         ExportedProject001 exportedProject = JsonUtility.FromJson<ExportedProject001>(mapInfo.gameData).fixPrefabFields();

         if (exportedProject.biome == Biome.Type.None) {
            D.warning("Invalid biome type NONE in map data. Setting to 'Forest'.");
            exportedProject.biome = Biome.Type.Forest;
         }

         area.biome = exportedProject.biome;

         if (exportedProject.editorType == EditorType.Sea) {
            area.isSea = true;
         } else if (exportedProject.editorType == EditorType.Interior) {
            area.isInterior = true;
         }

         result.area.setTilemapLayers(instantiateTilemaps(mapInfo, exportedProject, result.tilemapParent, result.collisionTilemapParent));
         instantiatePrefabs(mapInfo, exportedProject, result.prefabParent, result.npcParent, result.area);

         if (exportedProject.gravityEffectors != null) {
            addGravityEffectors(result, exportedProject.gravityEffectors);
         }

         if (exportedProject.vineColliders != null) {
            addVineColliders(result, exportedProject.vineColliders);
         }

         Bounds bounds = calculateBounds(exportedProject);

         setCameraBounds(result, bounds);
         addEdgeColliders(result, bounds);

         // Destroy the template component
         UnityEngine.Object.Destroy(result);

         return area;
      }

      static void addGravityEffectors (MapTemplate map, ExportedGravityEffector[] effectors) {
         foreach (ExportedGravityEffector exportedEffector in effectors) {
            GameObject effector = new GameObject("Gravity Effector");
            effector.transform.parent = map.effectorContainer;
            effector.transform.localPosition = exportedEffector.position;
            effector.transform.localScale = Vector3.one;

            BoxCollider2D collider = effector.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.usedByEffector = true;
            collider.size = exportedEffector.size;

            AreaEffector2D areaEffector = effector.AddComponent<AreaEffector2D>();
            areaEffector.forceAngle = 270;
            areaEffector.forceMagnitude = 15;
            areaEffector.forceVariation = 0;
            areaEffector.forceTarget = EffectorSelection2D.Rigidbody;
         }
      }

      static void addVineColliders (MapTemplate map, ExportedVineCollider[] vineColliders) {
         foreach (ExportedVineCollider vineCollider in vineColliders) {
            GameObject vine = new GameObject("Vine Trigger");
            vine.transform.parent = map.effectorContainer;
            vine.transform.localPosition = vineCollider.position;
            vine.transform.localScale = Vector3.one;

            vine.AddComponent<Vines>();

            BoxCollider2D collider = vine.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = vineCollider.size;
         }
      }

      static void addEdgeColliders (MapTemplate map, Bounds bounds) {
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
      static void setCameraBounds (MapTemplate map, Bounds bounds) {
         map.camBounds.points = new Vector2[] {
            new Vector2(bounds.min.x, bounds.min.y),
            new Vector2(bounds.min.x, bounds.max.y),
            new Vector2(bounds.max.x, bounds.max.y),
            new Vector2(bounds.max.x, bounds.min.y)
         };

         map.confiner.m_BoundingShape2D = map.camBounds;
      }

      static Bounds calculateBounds (ExportedProject001 project) {
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

      static void instantiatePrefabs (MapInfo mapInfo, ExportedProject001 project, Transform prefabParent, Transform npcParent, Area area) {
         List<ExportedPrefab001> npcData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> enemyData = new List<ExportedPrefab001>();
         List<ExportedPrefab001> treasureSiteData = new List<ExportedPrefab001>();

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
            } else if (original.GetComponent<TreasureSite>() != null) {
               if (prefab.d != null) {
                  treasureSiteData.Add(prefab);
               }
            } else {
               Transform parent = original.GetComponent<NPC>() ? npcParent : prefabParent;
               Vector3 targetLocalPos = new Vector3(prefab.x, prefab.y, 0) * 0.16f + Vector3.back * 10;

               var pref = UnityEngine.Object.Instantiate(
                  original,
                  parent.TransformPoint(targetLocalPos),
                  Quaternion.identity,
                  parent);

               foreach (IBiomable biomable in pref.GetComponentsInChildren<IBiomable>()) {
                  biomable.setBiome(project.biome);
               }

               foreach (ZSnap snap in pref.GetComponentsInChildren<ZSnap>()) {
                  snap.snapZ();
               }

               IMapEditorDataReceiver receiver = pref.GetComponent<IMapEditorDataReceiver>();
               if (receiver != null && prefab.d != null) {
                  receiver.receiveData(prefab.d);
               }
            }
         }

         if (unrecognizedPrefabs > 0) {
            Utilities.warning($"Could not recognize { unrecognizedPrefabs } prefabs of map { mapInfo.mapName }");
         }

         area.registerNetworkPrefabData(npcData, enemyData, treasureSiteData);
      }

      static List<TilemapLayer> instantiateTilemaps (MapInfo mapInfo, ExportedProject001 project, Transform tilemapParent, Transform collisionTilemapParent) {
         int unrecognizedTiles = 0;

         List<TilemapLayer> result = new List<TilemapLayer>();

         foreach (ExportedLayer001 layer in project.layers) {
            // Create the tilemap gameobject
            var tilemap = UnityEngine.Object.Instantiate(AssetSerializationMaps.tilemapTemplate, tilemapParent);
            tilemap.transform.localPosition = new Vector3(0, 0, layer.z);
            tilemap.gameObject.name = layer.name + " " + layer.sublayer;

            var colTilemap = UnityEngine.Object.Instantiate(AssetSerializationMaps.collisionTilemapTemplate, collisionTilemapParent);
            colTilemap.transform.localPosition = Vector3.zero;
            colTilemap.gameObject.name = layer.name + " " + layer.sublayer;

            // Add all the tiles
            Vector3Int[] positions = layer.tiles.Select(t => new Vector3Int(t.x, t.y, 0)).ToArray();
            TileBase[] tiles = layer.tiles
               .Select(t => AssetSerializationMaps.tryGetTile(new Vector2Int(t.i, t.j), project.biome))
               .Where(t => t != null)
               .ToArray();

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

            tilemap.SetTiles(positions, tiles);

            result.Add(new TilemapLayer {
               tilemap = tilemap,
               name = layer.name ?? "",
               type = layer.type
            });

            positions = layer.tiles.Where(t => t.c == 1).Select(t => new Vector3Int(t.x, t.y, 0)).ToArray();
            tiles = layer.tiles
               .Where(t => t.c == 1)
               .Select(t => AssetSerializationMaps.tryGetTile(new Vector2Int(t.i, t.j), project.biome))
               .Where(t => t != null)
               .ToArray();

            colTilemap.SetTiles(positions, tiles);
            colTilemap.GetComponent<TilemapCollider2D>().enabled = true;
         }

         if (unrecognizedTiles > 0) {
            Utilities.warning($"Could not recognize { unrecognizedTiles } tiles of map { mapInfo.mapName }");
         }

         return result;
      }

      public static Direction? parseDirection (string data) {
         switch (data.Trim(' ')) {
            case "":
            case "North":
               return Direction.North;
            case "NorthEast":
               return Direction.NorthEast;
            case "East":
               return Direction.East;
            case "SouthEast":
               return Direction.SouthEast;
            case "South":
               return Direction.South;
            case "SouthWest":
               return Direction.SouthWest;
            case "West":
               return Direction.West;
            case "NorthWest":
               return Direction.NorthWest;
            default:
               Debug.LogWarning($"Unable to parse direction. Data:{data}");
               return null;
         }
      }

      public static GenericActionTrigger.InteractionType parseInteractionType (string data) {
         switch (data.Trim(' ')) {
            case "Enter":
               return GenericActionTrigger.InteractionType.Enter;
            case "Exit":
               return GenericActionTrigger.InteractionType.Exit;
            case "Stay":
               return GenericActionTrigger.InteractionType.Stay;
            default:
               Debug.LogWarning($"Unable to parse direction. Data:{data}");
               return GenericActionTrigger.InteractionType.Enter;
         }
      }

      static void ensureSerializationMapsLoaded () {
         if (AssetSerializationMaps.loaded)
            return;

         AssetSerializationMaps.load();
      }
   }
}

