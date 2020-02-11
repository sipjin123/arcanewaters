using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Text.RegularExpressions;

namespace MapCreationTool.Serialization
{
   /// <summary>
   /// This part of the class is meant to be used the level editor for serialization.
   /// </summary>
   public partial class Serializer
   {
      static string[] layersSea001to002 = new string[] { "ground", "grass", "undf", "undf", "undf", "mountain", "mountain",
         "mountain-top", "mountain-top", "water", "dock", "detail", "detail", "detail", "detail", "detail", "detail", "detail"};
      static string[] layersArea001to002 = new string[] { "ground", "grass", "path", "path", "shrub", "mountain", "mountain",
         "mountain", "shrub", "water", "dock", "detail", "fence", "bush", "stump", "bush", "prop", "stair"};
      static string[] layersInterior001to002 = new string[] { "ground", "floor", "floor", "rug", "shadow", "wall",
         "wall", "furniture", "chair", "mat", "trinket", "ceiling", "undf", "undf", "undf", "undf", "undf", "undf" };

      public static string serialize (
          Dictionary<string, Layer> layers,
          List<PlacedPrefab> prefabs,
          Biome.Type biome,
          EditorType editorType,
          Vector2Int size,
          bool prettyPrint = false) {
         try {
            return serialize002(layers, prefabs, biome, editorType, size, prettyPrint);
         } catch (Exception ex) {
            Debug.LogError("Failed to serialize map.");
            throw ex;
         }
      }

      private static string serialize002 (
          Dictionary<string, Layer> layers,
          List<PlacedPrefab> prefabs,
          Biome.Type biome,
          EditorType editorType,
          Vector2Int size,
          bool prettyPrint = false) {
         Func<GameObject, int> prefabToIndex = (go) => { return AssetSerializationMaps.getIndex(go, biome); };
         Func<TileBase, Vector2Int> tileToIndex = (tile) => { return AssetSerializationMaps.getIndex(tile, biome); };

         //Make prefab serialization object
         Prefab001[] prefabsSerialized
             = prefabs.Select(p =>
                 new Prefab001 {
                    i = prefabToIndex(p.original),
                    x = p.placedInstance.transform.position.x,
                    y = p.placedInstance.transform.position.y,
                    data = p.data.Select(data => new DataField { k = data.Key, v = data.Value }).ToArray()
                 }
             ).ToArray();

         //Make layer serialization object
         List<Layer002> layersSerialized = new List<Layer002>();

         foreach (var layerkv in layers) {
            if (layerkv.Value.hasTilemap) {
               layersSerialized.Add(new Layer002 {
                  id = layerkv.Key,
                  tiles = serializeTiles(layerkv.Value, tileToIndex),
                  sublayers = new SubLayer001[0]
               });
            } else {
               Layer002 ls = new Layer002 {
                  id = layerkv.Key,
                  tiles = new Tile001[0],
                  sublayers = new SubLayer001[layerkv.Value.subLayers.Length]
               };
               for (int i = 0; i < layerkv.Value.subLayers.Length; i++) {
                  ls.sublayers[i] = new SubLayer001 {
                     index = i,
                     tiles = serializeTiles(layerkv.Value.subLayers[i], tileToIndex)
                  };
               }
               layersSerialized.Add(ls);
            }
         }

         Project002 project = new Project002 {
            version = "0.0.2",
            biome = biome,
            layers = layersSerialized.ToArray(),
            prefabs = prefabsSerialized,
            editorType = editorType,
            size = size
         };

         return JsonUtility.ToJson(project, prettyPrint);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="data"></param>
      /// <param name="forEditor">Whether the desirelized project will be used by the editor, or the main game</param>
      /// <returns></returns>
      public static DeserializedProject deserialize (string data, bool forEditor) {
         int version = extractVersion(data);

         switch (version) {
            case 000001:
               return deserialize001(data, forEditor);
            case 000002:
               return deserialize002(data, forEditor);
            default:
               throw new Exception("Failed to identify the file version.");
         }
      }

      public static string serializeExport (
         Dictionary<string, Layer> layers,
         List<PlacedPrefab> prefabs,
         Biome.Type biome,
         EditorType editorType,
         EditorConfig config,
         Dictionary<TileBase, TileCollisionType> collisionDictionary,
         Vector2Int editorOrigin,
         Vector2Int editorSize) {
         return serializeExport001(layers, prefabs, biome, editorType, config, collisionDictionary, editorOrigin, editorSize);
      }

      private static string serializeExport001 (
         Dictionary<string, Layer> layers,
         List<PlacedPrefab> prefabs,
         Biome.Type biome,
         EditorType editorType,
         EditorConfig config,
         Dictionary<TileBase, TileCollisionType> collisionDictionary,
         Vector2Int editorOrigin,
         Vector2Int editorSize) {
         Func<GameObject, int> prefabToIndex = (go) => { return AssetSerializationMaps.getIndex(go, biome); };
         Func<TileBase, Vector2Int> tileToIndex = (tile) => { return AssetSerializationMaps.getIndex(tile, biome); };

         //Make prefab serialization object
         ExportedPrefab001[] prefabsSerialized
             = prefabs.Select(p =>
                 new ExportedPrefab001 {
                    i = prefabToIndex(p.original),
                    x = p.placedInstance.transform.position.x,
                    y = p.placedInstance.transform.position.y,
                    d = p.data.Select(data => new DataField { k = data.Key, v = data.Value }).ToArray()
                 }
             ).ToArray();

         List<MidExportLayer> midExportLayers = formMidExportLayers(layers, tileToIndex, collisionDictionary, config, editorType, editorOrigin, editorSize);

         ExportedProject001 project = new ExportedProject001 {
            version = "0.0.1",
            biome = biome,
            layers = formExportedLayers(midExportLayers, editorSize).ToArray(),
            gravityEffectors = formGravityEffectors(midExportLayers, editorSize),
            editorType = editorType,
            prefabs = prefabsSerialized,
         };

         return JsonUtility.ToJson(project);
      }

      private static List<ExportedLayer001> formExportedLayers (List<MidExportLayer> midLayers, Vector2Int editorSize) {
         List<ExportedLayer001> result = new List<ExportedLayer001>();

         // Set collision flag to tiles inside mid export data structure
         for (int i = 0; i < editorSize.x; i++) {
            for (int j = 0; j < editorSize.y; j++) {
               bool hasWater = false;
               for (int k = midLayers.Count - 1; k >= 0; k--) {
                  if (midLayers[k].layer.CompareTo("water") == 0 && midLayers[k].tileMatrix[i, j] != null) {
                     hasWater = true;
                  }
               }

               for (int k = midLayers.Count - 1; k >= 0; k--) {

                  if (midLayers[k].tileMatrix[i, j] == null)
                     continue;

                  // If docks have no water under them, don't place colliders
                  if (midLayers[k].layer.CompareTo("dock") == 0 && !hasWater) {
                     if (midLayers[k].tileMatrix[i, j].collisionType == TileCollisionType.CancelEnabled || midLayers[k].tileMatrix[i, j].collisionType == TileCollisionType.CancelDisabled) {
                        break;
                     } else {
                        continue;
                     }
                  }

                  if (midLayers[k].tileMatrix[i, j].collisionType == TileCollisionType.Enabled) {
                     midLayers[k].tileMatrix[i, j].tile.c = 1;
                  } else if (midLayers[k].tileMatrix[i, j].collisionType == TileCollisionType.CancelEnabled) {
                     midLayers[k].tileMatrix[i, j].tile.c = 1;
                     break;
                  } else if (midLayers[k].tileMatrix[i, j].collisionType == TileCollisionType.CancelDisabled) {
                     break;
                  }
               }
            }
         }

         // Form final layers
         foreach (MidExportLayer midLayer in midLayers) {
            List<ExportedTile001> exportedTiles = new List<ExportedTile001>();
            for (int i = 0; i < midLayer.tileMatrix.GetLength(0); i++) {
               for (int j = 0; j < midLayer.tileMatrix.GetLength(1); j++) {
                  if (midLayer.tileMatrix[i, j] != null)
                     exportedTiles.Add(midLayer.tileMatrix[i, j].tile);
               }
            }
            result.Add(new ExportedLayer001 {
               z = midLayer.z,
               tiles = exportedTiles.ToArray(),
               type = midLayer.type
            });
         }

         return result;
      }

      private static List<MidExportLayer> formMidExportLayers (
         Dictionary<string, Layer> layers,
         Func<TileBase, Vector2Int> tileToIndex,
         Dictionary<TileBase, TileCollisionType> collisionDictionary,
         EditorConfig config,
         EditorType editorType,
         Vector2Int editorOrigin,
         Vector2Int editorSize) {
         // Form mid export layers. These are layers with tile matrixes, whose tiles also have the collision setup info attached
         List<MidExportLayer> midLayers = new List<MidExportLayer>();

         foreach (var layerkv in layers) {
            // Set offset for layers that are supposed to be on top of player
            float zOffset = config.getLayers(editorType).First(l => l.layer.CompareTo(layerkv.Key) == 0).zOffset;

            if (layerkv.Value.hasTilemap) {
               if (layerkv.Value.tileCount == 0)
                  continue;

               midLayers.Add(new MidExportLayer {
                  z = getZ(config.getIndex(layerkv.Key, editorType), 0) + zOffset,
                  layer = layerkv.Key,
                  tileMatrix = getTilesWithCollisions(layerkv.Value, tileToIndex, collisionDictionary, editorOrigin, editorSize),
                  subLayer = 0
               });
            } else {
               for (int i = 0; i < layerkv.Value.subLayers.Length; i++) {
                  if (layerkv.Value.subLayers[i].tileCount == 0)
                     continue;

                  midLayers.Add(new MidExportLayer {
                     z = getZ(config.getIndex(layerkv.Key, editorType), i) + zOffset,
                     layer = layerkv.Key,
                     tileMatrix = getTilesWithCollisions(layerkv.Value.subLayers[i], tileToIndex, collisionDictionary, editorOrigin, editorSize),
                     type = LayerType.Water,
                     subLayer = i
                  });
               }
            }
         }

         return midLayers;
      }

      private static ExportedGravityEffector[] formGravityEffectors (List<MidExportLayer> midLayers, Vector2Int editorSize) {
         List<ExportedGravityEffector> effectors = new List<ExportedGravityEffector>();
         // Find out which tiles have to be effected
         bool[,] effectorMatrix = new bool[editorSize.x, editorSize.y];

         for (int i = 0; i < editorSize.x; i++) {
            for (int j = 0; j < editorSize.y; j++) {
               foreach (MidExportLayer layer in midLayers) {
                  if (layer.tileMatrix[i, j] != null) {
                     if (layer.layer.CompareTo("stair") == 0 || (layer.layer.CompareTo("water") == 0 && layer.subLayer == 4)) {
                        effectorMatrix[i, j] = true;
                        break;
                     }
                  }
               }
            }
         }

         bool[,] used = new bool[editorSize.x, editorSize.y];

         for (int i = 0; i < editorSize.x; i++) {
            for (int j = 0; j < editorSize.y; j++) {
               if (!effectorMatrix[i, j] || used[i, j]) {
                  continue;
               }

               Vector2Int size = new Vector2Int(1, 1);
               used[i, j] = true;

               // Get the height of effected clump
               while (j + size.y < editorSize.y && effectorMatrix[i, j + size.y] && !used[i, j + size.y]) {
                  used[i, j + size.y] = true;
                  size.y++;
               }

               // Extend the effected clump horizontally
               while (i + size.x < editorSize.x) {
                  bool hasColumn = true;
                  // Check if next column is full
                  for (int h = 0; h < size.y; h++) {
                     if (!effectorMatrix[i + size.x, j + h] || used[i + size.x, j + h]) {
                        hasColumn = false;
                        break;
                     }
                  }

                  if (!hasColumn) {
                     break;
                  }

                  // If next column is full, extend and mark as used
                  for (int h = 0; h < size.y; h++) {
                     used[i + size.x, j + h] = true;
                  }
                  size.x++;
               }

               effectors.Add(new ExportedGravityEffector {
                  position = new Vector2(i, j) + (Vector2) size * 0.5f - (Vector2) editorSize * 0.5f,
                  size = size
               });
            }
         }

         return effectors.ToArray();
      }

      private static TileIndexWithCollision[,] getTilesWithCollisions (
         Layer layer,
         Func<TileBase, Vector2Int> tileToIndex,
         Dictionary<TileBase, TileCollisionType> tileToCollision,
         Vector2Int editorOrigin,
         Vector2Int editorSize) {
         TileIndexWithCollision[,] tiles = new TileIndexWithCollision[editorSize.x, editorSize.y];

         for (int i = 0; i < layer.size.x; i++) {
            for (int j = 0; j < layer.size.y; j++) {
               Vector3Int pos = new Vector3Int(i + layer.origin.x, j + layer.origin.y, 0);
               TileBase tile = layer.getTile(pos);
               if (tile != null) {
                  Vector2Int index = tileToIndex(tile);

                  TileCollisionType col = TileCollisionType.Disabled;
                  if (tileToCollision.TryGetValue(tile, out TileCollisionType foundCol))
                     col = foundCol;

                  tiles[pos.x - editorOrigin.x, pos.y - editorOrigin.y] = new TileIndexWithCollision {
                     tile = new ExportedTile001 {
                        i = index.x,
                        j = index.y,
                        x = pos.x,
                        y = pos.y
                     },
                     collisionType = col
                  };
               }
            }
         }

         return tiles;
      }

      static float getZ (int layer, int sublayer) {
         return
            AssetSerializationMaps.layerZFirst +
            layer * AssetSerializationMaps.layerZMultip +
            sublayer * AssetSerializationMaps.sublayerZMultip;
      }

      private static DeserializedProject deserialize001 (string data, bool forEditor) {
         Project001 dt = JsonUtility.FromJson<Project001>(data);

         if (dt.biome == Biome.Type.None) {
            dt.biome = Biome.Type.Forest;
         }

         List<DeserializedProject.DeserializedPrefab> prefabs = new List<DeserializedProject.DeserializedPrefab>();
         List<DeserializedProject.DeserializedTile> tiles = new List<DeserializedProject.DeserializedTile>();

         Func<Vector2Int, TileBase> indexToTile = (index) => { return AssetSerializationMaps.getTile(index, dt.biome); };
         Func<int, GameObject> indexToPrefab = (index) => { return AssetSerializationMaps.getPrefab(index, dt.biome, forEditor); };

         string[] layerVersionMap = getLayerMaps001to002(dt.editorType);

         foreach (var pref in dt.prefabs) {
            prefabs.Add(new DeserializedProject.DeserializedPrefab {
               position = new Vector3(pref.x, pref.y, 0),
               prefab = indexToPrefab(pref.i),
               dataFields = pref.data
            });
         }

         foreach (var layer in dt.layers) {
            if (layer.sublayers.Length == 0) {
               foreach (var tile in layer.tiles) {
                  tiles.Add(new DeserializedProject.DeserializedTile {
                     layer = layerVersionMap[layer.index],
                     sublayer = null,
                     position = new Vector3Int(tile.x, tile.y, 0),
                     tile = indexToTile(new Vector2Int(tile.i, tile.j))
                  });
               }
            } else {
               for (int i = 0; i < layer.sublayers.Length; i++) {
                  foreach (var tile in layer.sublayers[i].tiles) {
                     tiles.Add(new DeserializedProject.DeserializedTile {
                        layer = layerVersionMap[layer.index],
                        sublayer = i,
                        position = new Vector3Int(tile.x, tile.y, 0),
                        tile = indexToTile(new Vector2Int(tile.i, tile.j))
                     });
                  }
               }
            }
         }

         return new DeserializedProject {
            prefabs = prefabs.ToArray(),
            tiles = tiles.ToArray(),
            biome = dt.biome,
            size = dt.size,
            editorType = dt.editorType
         };
      }

      private static DeserializedProject deserialize002 (string data, bool forEditor) {
         Project002 dt = JsonUtility.FromJson<Project002>(data);

         if (dt.biome == Biome.Type.None) {
            dt.biome = Biome.Type.Forest;
         }

         List<DeserializedProject.DeserializedPrefab> prefabs = new List<DeserializedProject.DeserializedPrefab>();
         List<DeserializedProject.DeserializedTile> tiles = new List<DeserializedProject.DeserializedTile>();

         Func<Vector2Int, TileBase> indexToTile = (index) => { return AssetSerializationMaps.getTile(index, dt.biome); };
         Func<int, GameObject> indexToPrefab = (index) => { return AssetSerializationMaps.getPrefab(index, dt.biome, forEditor); };

         foreach (var pref in dt.prefabs) {
            prefabs.Add(new DeserializedProject.DeserializedPrefab {
               position = new Vector3(pref.x, pref.y, 0),
               prefab = indexToPrefab(pref.i),
               dataFields = pref.data
            });
         }

         foreach (var layer in dt.layers) {
            if (layer.sublayers.Length == 0) {
               foreach (var tile in layer.tiles) {
                  tiles.Add(new DeserializedProject.DeserializedTile {
                     layer = layer.id,
                     sublayer = null,
                     position = new Vector3Int(tile.x, tile.y, 0),
                     tile = indexToTile(new Vector2Int(tile.i, tile.j))
                  });
               }
            } else {
               for (int i = 0; i < layer.sublayers.Length; i++) {
                  foreach (var tile in layer.sublayers[i].tiles) {
                     tiles.Add(new DeserializedProject.DeserializedTile {
                        layer = layer.id,
                        sublayer = i,
                        position = new Vector3Int(tile.x, tile.y, 0),
                        tile = indexToTile(new Vector2Int(tile.i, tile.j))
                     });
                  }
               }
            }
         }

         return new DeserializedProject {
            prefabs = prefabs.ToArray(),
            tiles = tiles.ToArray(),
            biome = dt.biome,
            size = dt.size,
            editorType = dt.editorType
         };
      }


      private static int extractVersion (string data) {
         Regex rx = new Regex("\"[0-9].[0-9].[0-9]\"");
         Match match = rx.Match(data);

         if (!match.Success)
            throw new Exception("Could not identify the file version");

         string versionString = match.Groups[0].Value.Replace('\"', ' ');
         string[] versionNumbers = versionString.Split('.');

         return int.Parse(versionNumbers[0]) * 10000 + int.Parse(versionNumbers[1]) * 100 + int.Parse(versionNumbers[2]);
      }

      private static string[] getLayerMaps001to002 (EditorType editorType) {
         switch (editorType) {
            case EditorType.Area:
               return layersArea001to002;
            case EditorType.Sea:
               return layersSea001to002;
            case EditorType.Interior:
               return layersInterior001to002;
            default:
               throw new Exception("Unhandled editor type.");
         }
      }

      private static Tile001[] serializeTiles (Layer layer, Func<TileBase, Vector2Int> tileToIndex) {
         List<Tile001> tiles = new List<Tile001>();

         for (int i = 0; i < layer.size.x; i++) {
            for (int j = 0; j < layer.size.y; j++) {
               Vector3Int pos = new Vector3Int(i + layer.origin.x, j + layer.origin.y, 0);
               TileBase tile = layer.getTile(pos);
               if (tile != null) {
                  Vector2Int index = tileToIndex(tile);
                  tiles.Add(new Tile001 {
                     i = index.x,
                     j = index.y,
                     x = pos.x,
                     y = pos.y
                  });
               }
            }
         }
         return tiles.ToArray();
      }

      /// <summary>
      /// Used in the middle of map export process as a temporary layer
      /// </summary>
      private class MidExportLayer
      {
         public float z { get; set; }
         public string layer { get; set; }
         public int subLayer { get; set; }
         public TileIndexWithCollision[,] tileMatrix { get; set; }
         public LayerType type { get; set; }
      }

      /// <summary>
      /// Used in the middle of map export process
      /// </summary>
      private class TileIndexWithCollision
      {
         public ExportedTile001 tile { get; set; }
         public TileCollisionType collisionType { get; set; }
      }
   }

   [Serializable]
   public class ExportedProject001
   {
      public string version;
      public Biome.Type biome;
      public EditorType editorType;
      public Vector2Int size;
      public ExportedPrefab001[] prefabs;
      public ExportedLayer001[] layers;
      public ExportedGravityEffector[] gravityEffectors;
   }

   [Serializable]
   public class ExportedPrefab001
   {
      public int i; // Prefab index
      public float x; // Prefab position x
      public float y; // Prefab position y
      public DataField[] d; // The custom data of the prefab, defined as key-value pairs
   }

   [Serializable]
   public class ExportedLayer001
   {
      public float z;
      public ExportedTile001[] tiles;
      public LayerType type;
   }

   [Serializable]
   public class ExportedTile001
   {
      public int i; // Tile index x
      public int j; // Tile index y
      public int x; // Tile position x
      public int y; // Tile position y
      public int c; // Whether the tile should collide
   }

   [Serializable]
   public class ExportedGravityEffector
   {
      public Vector2 position;
      public Vector2 size;
   }

   public class DeserializedProject
   {
      public DeserializedPrefab[] prefabs;
      public DeserializedTile[] tiles;
      public Biome.Type biome;
      public EditorType editorType;
      public Vector2Int size;

      public class DeserializedPrefab
      {
         public GameObject prefab;
         public Vector3 position;
         public DataField[] dataFields;
      }

      public class DeserializedTile
      {
         public TileBase tile;
         public string layer;
         public int? sublayer;
         public Vector3Int position;
      }
   }

   [Serializable]
   public class Project002
   {
      public string version;
      public Biome.Type biome;
      public EditorType editorType;
      public Layer002[] layers;
      public Prefab001[] prefabs;
      public Vector2Int size;
   }

   [Serializable]
   public class Layer002
   {
      public string id;
      public Tile001[] tiles;
      public SubLayer001[] sublayers;
   }

   [Serializable]
   public class Project001
   {
      public string version;
      public Biome.Type biome;
      public EditorType editorType;
      public Layer001[] layers;
      public Prefab001[] prefabs;
      public Vector2Int size;
   }

   [Serializable]
   public class Layer001
   {
      public int index;
      public Tile001[] tiles;
      public SubLayer001[] sublayers;
   }

   [Serializable]
   public class SubLayer001
   {
      public int index;
      public Tile001[] tiles;
   }

   [Serializable]
   public class Tile001
   {
      public int i; // Tile index x
      public int j; // Tile index y
      public int x; // Tile position x
      public int y; // Tile position y
   }

   [Serializable]
   public class Prefab001
   {
      public int i; // Prefab index
      public float x; // Prefab position x
      public float y; // Prefab position y
      public DataField[] data; // The custom data of the prefab, defined as key-value pairs
   }

   [Serializable]
   public class DataField
   {
      public string k; // Key
      public string v; // Value
   }
}
