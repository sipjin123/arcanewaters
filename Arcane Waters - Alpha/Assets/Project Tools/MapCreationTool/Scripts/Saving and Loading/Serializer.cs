using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Text.RegularExpressions;
using System.Globalization;
using Newtonsoft.Json;

namespace MapCreationTool.Serialization
{
   public partial class Serializer
   {
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
            size = size,
            nextPrefabId = PlacedPrefab.nextPrefabId
         };

         return JsonUtility.ToJson(project, prettyPrint);
      }

      public static DeserializedProject deserializeForEditor (string data) {
         int version = extractVersion(data);

         switch (version) {
            case 000002:
               return deserialize002(data, true);
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

         Exporter exporter = new Exporter(layers, prefabs, biome, editorType, editorOrigin, editorSize);
         exporter.transformData(collisionDictionary);
         return JsonUtility.ToJson(exporter.toExportedProject001(config));
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
            GameObject original = indexToPrefab(pref.i);
            if (original != AssetSerializationMaps.deletedPrefabMarker) {
               prefabs.Add(new DeserializedProject.DeserializedPrefab {
                  position = new Vector3(pref.x, pref.y, 0),
                  prefab = original,
                  dataFields = pref.data
               });
            }
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
            editorType = dt.editorType,
            nextPrefabId = dt.nextPrefabId
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
      public Biome.Type biome;
      public EditorType editorType;
      public Vector2Int size;
      public int nextPrefabId;

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
      public int nextPrefabId;
   }

   [Serializable]
   public class Layer002
   {
      public string id;
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
      public static CultureInfo US_CULTURE => CultureInfo.CreateSpecificCulture("en-US");

      // For prefabs, the serializable data is saved as key-value pairs
      // Below are they keys for the defined key-value pairs
      public const string WARP_TARGET_MAP_KEY = "target map";
      public const string WARP_TARGET_SPAWN_KEY = "target spawn";
      public const string WARP_WIDTH_KEY = "width";
      public const string WARP_HEIGHT_KEY = "height";
      public const string WARP_ARRIVE_FACING_KEY = "arrive facing";

      public const string TARGET_MAP_INFO_KEY = "target map info";

      public const string SPAWN_NAME_KEY = "name";
      public const string SPAWN_WIDTH_KEY = "width";
      public const string SPAWN_HEIGHT_KEY = "height";

      public const string CRITTER_RUN_DIRECTION_KEY = "run direction";

      public const string LAND_ENEMY_DATA_KEY = "enemy data";

      public const string ORE_SPOT_DATA_KEY = "ore id";
      public const string ORE_TYPE_DATA_KEY = "ore type";

      public const string SEA_ENEMY_DATA_KEY = "enemy data";

      public const string HOUSE_TARGET_MAP_KEY = "target map";
      public const string HOUSE_TARGET_SPAWN_KEY = "target spawn";

      public const string TUTORIAL_ITEM_STEP_ID_KEY = "step id";

      public const string MAP_SIGN_TYPE_KEY = "map sign type";
      public const string MAP_ICON_KEY = "map sign icon";
      public const string MAP_SIGN_LABEL = "map sign label";

      public const string SECRETS_TYPE_ID = "secret type";
      public const string SECRETS_START_SPRITE = "secret start sprite";
      public const string SECRETS_INTERACT_SPRITE = "secret interact sprite";

      public const string NPC_DATA_KEY = "npc data";
      public const string NPC_SHOP_NAME_KEY = "shop name";
      public const string NPC_PANEL_TYPE_KEY = "panel type";
      public const string NPC_DIRECTION_KEY = "direction default";

      public const string SPIDER_WEB_HEIGHT_KEY = "height";

      public const string TREASURE_SPOT_SPAWN_CHANCE_KEY = "spawn chance";
      public const string TREASURE_SPRITE_TYPE_KEY = "treasure sprite type";
      public const string TREASURE_USE_CUSTOM_TYPE_KEY = "treasure use custom sprite";

      public const string GENERIC_ACTION_TRIGGER_INTERACTION_TYPE = "interaction type";
      public const string GENERIC_ACTION_TRIGGER_ACTION_NAME = "action name";
      public const string GENERIC_ACTION_TRIGGER_WIDTH_KEY = "width";
      public const string GENERIC_ACTION_TRIGGER_HEIGHT_KEY = "height";

      public const string BOOK_ID_KEY = "book id";

      public const string LEDGE_WIDTH_KEY = "width";
      public const string LEDGE_HEIGHT_KEY = "height";

      public const string DISCOVERY_SPAWN_CHANCE = "spawn chance";
      public const string POSSIBLE_DISCOVERY = "possible discovery";

      public const string SHIP_DATA_KEY = "ship data";

      public const string PLACED_PREFAB_ID = "id";
      public const string IS_PERMANENT_KEY = "is permanent";

      public const string SHIP_GUILD_ID = "guild id";

      public string k; // Key
      public string v; // Value

      public static int extractId (DataField[] dataFields) {
         if (dataFields == null) return -1;

         foreach (DataField dataField in dataFields) {
            if (dataField.k.CompareTo(PLACED_PREFAB_ID) == 0) {
               return dataField.intValue;
            }
         }

         return -1;
      }

      public T objectValue<T> () {
         if (string.IsNullOrEmpty(v)) {
            return default(T);
         }

         return JsonConvert.DeserializeObject<T>(v);
      }

      public float floatValue
      {
         get { return float.Parse(v, US_CULTURE); }
      }

      public bool tryGetFloatValue (out float value) {
         if (float.TryParse(v, NumberStyles.Float, US_CULTURE, out float val)) {
            value = val;
            return true;
         }
         value = 0;
         return false;
      }

      public int intValue
      {
         get { return int.Parse(v, US_CULTURE); }
      }

      public bool tryGetIntValue (out int value) {
         if (int.TryParse(v, NumberStyles.Integer, US_CULTURE, out int val)) {
            value = val;
            return true;
         }
         value = 0;
         return false;
      }

      public bool tryGetDirectionValue (out Direction value) {
         switch (v.Trim(' ')) {
            case "North":
               value = Direction.North;
               return true;
            case "NorthEast":
               value = Direction.NorthEast;
               return true;
            case "East":
               value = Direction.East;
               return true;
            case "SouthEast":
               value = Direction.SouthEast;
               return true;
            case "South":
               value = Direction.South;
               return true;
            case "SouthWest":
               value = Direction.SouthWest;
               return true;
            case "West":
               value = Direction.West;
               return true;
            case "NorthWest":
               value = Direction.NorthWest;
               return true;
         }

         value = Direction.North;
         return false;
      }

      public bool tryGetInteractionTypeValue (out GenericActionTrigger.InteractionType value) {
         switch (v.Trim(' ')) {
            case "Enter":
               value = GenericActionTrigger.InteractionType.Enter;
               return true;
            case "Exit":
               value = GenericActionTrigger.InteractionType.Exit;
               return true;
            case "Stay":
               value = GenericActionTrigger.InteractionType.Stay;
               return true;
         }
         value = GenericActionTrigger.InteractionType.Enter;
         return false;
      }
   }
}
