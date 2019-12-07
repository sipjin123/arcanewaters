using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool.Serialization
{
   /// <summary>
   /// This part of the class is meant to be used the level editor for serialization.
   /// </summary>
   public partial class Serializer
   {
      public static string serialize (
          Dictionary<int, Layer> layers,
          List<PlacedPrefab> prefabs,
          BiomeType biome,
          EditorType editorType,
          Vector2Int size,
          bool prettyPrint = false) {
         try {
            return serialize001(layers, prefabs, biome, editorType, size, prettyPrint);
         } catch (Exception ex) {
            Debug.LogError("Failed to serialize map.");
            throw ex;
         }
      }

      private static string serialize001 (
          Dictionary<int, Layer> layers,
          List<PlacedPrefab> prefabs,
          BiomeType biome,
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
         List<Layer001> layersSerialized = new List<Layer001>();

         foreach (var layerkv in layers) {
            if (layerkv.Value.hasTilemap) {
               layersSerialized.Add(new Layer001 {
                  index = layerkv.Key,
                  tiles = serializeTiles(layerkv.Value, tileToIndex),
                  sublayers = new SubLayer001[0]
               });
            } else {
               Layer001 ls = new Layer001 {
                  index = layerkv.Key,
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

         Project001 project = new Project001 {
            version = "0.0.1",
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
         Project001 dt = JsonUtility.FromJson<Project001>(data);


         if (dt.version != "0.0.1")
            throw new ArgumentException($"File version {dt.version} not supported.");

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
                     layer = layer.index,
                     sublayer = null,
                     position = new Vector3Int(tile.x, tile.y, 0),
                     tile = indexToTile(new Vector2Int(tile.i, tile.j))
                  });
               }
            } else {
               for (int i = 0; i < layer.sublayers.Length; i++) {
                  foreach (var tile in layer.sublayers[i].tiles) {
                     tiles.Add(new DeserializedProject.DeserializedTile {
                        layer = layer.index,
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
   }

   public class DeserializedProject
   {
      public DeserializedPrefab[] prefabs;
      public DeserializedTile[] tiles;
      public BiomeType biome;
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
         public int layer;
         public int? sublayer;
         public Vector3Int position;
      }
   }

   [Serializable]
   public class Project001
   {
      public string version;
      public BiomeType biome;
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
      public int i; //Tile index x
      public int j; //Tile index y
      public int x; //Tile position x
      public int y; //Tile position y
   }

   [Serializable]
   public class Prefab001
   {
      public int i; //Prefab index
      public float x; //Prefab position x
      public float y; //Prefab position y
      public DataField[] data; //The custom data of the prefab, defined as key-value pairs
   }

   [Serializable]
   public class DataField
   {
      public string k; //Key
      public string v; //Value
   }
}
