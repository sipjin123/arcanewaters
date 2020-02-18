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

      // This was used to automatically generate map files inside the project files. The script must be in the Editor folder for this to work.
      //static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
      //   foreach (string str in importedAssets.Union(movedAssets)) {
      //      string extension = Path.GetExtension(str).Trim('.');
      //      if (extension.CompareTo(DataFileExtension) == 0) {
      //         reimportLevel(Path.GetFileNameWithoutExtension(str), Path.GetDirectoryName(str), str);
      //      }
      //   }
      //}

      /// <summary>
      /// Creates an instance of a map from the serialized map data
      /// </summary>
      /// <param name="data"></param>
      public static Area instantiateMapData (string data, string areaKey, Vector3 position, int instanceID = 0) {

         ensureSerializationMapsLoaded();

         MapTemplate result = UnityEngine.Object.Instantiate(AssetSerializationMaps.mapTemplate, position, Quaternion.identity);
         result.name = areaKey;

         Area area = result.area;
         area.areaKey = areaKey;

         ExportedProject001 exportedProject = JsonUtility.FromJson<ExportedProject001>(data);

         if (exportedProject.biome == Biome.Type.None) {
            D.warning("Invalid biome type NONE in map data. Setting to 'Forest'.");
            exportedProject.biome = Biome.Type.Forest;
         }

         if (exportedProject.editorType == EditorType.Sea) {
            area.isSea = true;
         }

         instantiateTilemaps(exportedProject, result.tilemapParent, result.collisionTilemapParent);
         instantiatePrefabs(exportedProject, result.prefabParent, result.npcParent, instanceID);

         if (exportedProject.gravityEffectors != null) {
            addGravityEffectors(result, exportedProject.gravityEffectors);
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

      static void instantiatePrefabs (ExportedProject001 project, Transform prefabParent, Transform npcParent, int instanceID = 0) {
         Func<int, GameObject> indexToPrefab = (index) => { return AssetSerializationMaps.getPrefab(index, project.biome, false); };

         foreach (var prefab in project.prefabs) {
            GameObject original = indexToPrefab(prefab.i);

            if (original.GetComponent<Enemy>() != null) {
               Transform parent = prefabParent;
               Vector3 targetLocalPos = new Vector3(prefab.x, prefab.y, 0) * 0.16f + Vector3.back * 10;

               // Create an Enemy in this instance
               Enemy enemy = UnityEngine.Object.Instantiate(PrefabsManager.self.enemyPrefab, parent);
               enemy.enemyType = Enemy.Type.Lizard;

               // Add it to the Instance
               Instance instance = InstanceManager.self.getInstance(instanceID);
               InstanceManager.self.addEnemyToInstance(enemy, instance);

               enemy.transform.localPosition = targetLocalPos;
               enemy.desiredPosition = targetLocalPos;
               Mirror.NetworkServer.Spawn(enemy.gameObject);
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
               Debug.LogError(pref.gameObject.name+" __ " +prefab.d + " - " + receiver);
               if (receiver != null && prefab.d != null) {
                  receiver.receiveData(prefab.d);
               } 
            }
         }
      }

      static void instantiateTilemaps (ExportedProject001 project, Transform tilemapParent, Transform collisionTilemapParent) {
         Func<Vector2Int, TileBase> indexToTile = (index) => { return AssetSerializationMaps.getTile(index, project.biome); };

         foreach (ExportedLayer001 layer in project.layers) {
            // Create the tilemap gameobject
            var tilemap = UnityEngine.Object.Instantiate(AssetSerializationMaps.tilemapTemplate, tilemapParent);
            tilemap.transform.localPosition = new Vector3(0, 0, layer.z);
            tilemap.gameObject.name = "Layer " + layer.z;

            var colTilemap = UnityEngine.Object.Instantiate(AssetSerializationMaps.collisionTilemapTemplate, collisionTilemapParent);
            colTilemap.transform.localPosition = Vector3.zero;
            colTilemap.gameObject.name = "Layer " + layer.z;

            if (layer.type == LayerType.Water) {
               colTilemap.gameObject.name += " Water";
            }

            // Add all the tiles
            Vector3Int[] positions = layer.tiles.Select(t => new Vector3Int(t.x, t.y, 0)).ToArray();
            TileBase[] tiles = layer.tiles.Select(t => indexToTile(new Vector2Int(t.i, t.j))).ToArray();

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

            positions = layer.tiles.Where(t => t.c == 1).Select(t => new Vector3Int(t.x, t.y, 0)).ToArray();
            tiles = layer.tiles.Where(t => t.c == 1).Select(t => indexToTile(new Vector2Int(t.i, t.j))).ToArray();
            colTilemap.SetTiles(positions, tiles);

            colTilemap.GetComponent<TilemapCollider2D>().enabled = true;
         }
      }

      public static Direction? ParseDirection (string data) {
         switch (data.Trim(' ')) {
            case "":
               return Direction.North;
            case "North":
               return Direction.NorthEast;
            case "NorthEast":
               return Direction.East;
            case "East":
               return Direction.SouthEast;
            case "SouthEast":
               return Direction.South;
            case "South":
               return Direction.SouthWest;
            case "SouthWest":
               return Direction.West;
            case "West":
               return Direction.NorthWest;
            default:
               Debug.LogWarning($"Unable to parse direction. Data:{data}");
               return null;
         }
      }

      static void ensureSerializationMapsLoaded () {
         if (AssetSerializationMaps.loaded)
            return;

         AssetSerializationMaps.load();
      }
   }
}

