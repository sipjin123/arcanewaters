using UnityEngine;
using MapCreationTool.Serialization;
using System.Linq;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class MapImporter { 
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
      public static Area instantiateMapData(TextAsset data, string areaKey) {

         ensureSerializationMapsLoaded();

         MapTemplate result = Object.Instantiate(AssetSerializationMaps.mapTemplate);
         result.name = areaKey;

         Area area = result.area;
         area.areaKey = areaKey;

         DeserializedProject deserialized = Serializer.deserialize(data.text, false);

         instantiateTilemaps(deserialized.tiles, AssetSerializationMaps.tilemapTemplate, result.tilemapParent);
         instantiatePrefabs(deserialized.prefabs, result.prefabParent);

         setCameraBounds(result, deserialized.tiles);

         // Destroy the template component
         Object.Destroy(result);

         return area;
      }

      /// <summary>
      /// Sets the bounds the the camera can move in inside a map
      /// </summary>
      /// <param name="map"></param>
      /// <param name="tiles"></param>
      static void setCameraBounds(MapTemplate map, DeserializedProject.DeserializedTile[] tiles) {
         Bounds bounds = new Bounds();

         foreach(var tile in tiles) {
            bounds.min = new Vector3(Mathf.Min(tile.position.x, bounds.min.x), Mathf.Min(tile.position.y, bounds.min.y), 0);
            bounds.max = new Vector3(Mathf.Max(tile.position.x, bounds.max.x), Mathf.Max(tile.position.y, bounds.max.y), 0);
         }

         bounds.max += new Vector3(1, 1, 0);

         map.camBounds.points = new Vector2[] { 
            new Vector2(bounds.min.x, bounds.min.y),
            new Vector2(bounds.min.x, bounds.max.y),
            new Vector2(bounds.max.x, bounds.max.y),
            new Vector2(bounds.max.x, bounds.min.y)
         };

         map.confiner.m_BoundingShape2D = map.camBounds;
      }

      static void instantiatePrefabs(DeserializedProject.DeserializedPrefab[] prefabs, Transform parent) {
         foreach (var prefab in prefabs) {
            var pref = Object.Instantiate(prefab.prefab, parent);
            pref.transform.localPosition = prefab.position + Vector3.back * 10;

            ZSnap zsnap = pref.GetComponent<ZSnap>();
            if (zsnap != null)
               zsnap.snapZ();

            IMapEditorDataReceiver receiver = pref.GetComponent<IMapEditorDataReceiver>();
            if (receiver != null && prefab.dataFields != null)
               receiver.receiveData(prefab.dataFields);
         }
      }

      static void instantiateTilemaps(DeserializedProject.DeserializedTile[] tiles, Tilemap tilemapPref, Transform parent) {
         // Set up the layers
         Tilemap[] tms = new Tilemap[10000];
         foreach(var tile in tiles) {
            int layerIndex = tile.layer * 100 + (tile.sublayer ?? 0);
            if (tms[layerIndex] == null) {
               float posZ = AssetSerializationMaps.layerZFirst + tile.layer * AssetSerializationMaps.layerZMultip +
                   (tile.sublayer == null ? 0 : tile.sublayer.Value * AssetSerializationMaps.sublayerZMultip);

               var tilemap = Object.Instantiate(AssetSerializationMaps.tilemapTemplate, parent);
               tilemap.transform.localPosition = new Vector3(0, 0, posZ);

               tilemap.gameObject.name = "Layer " + layerIndex;
               
               //Add water so water checker catches it
               if (tile.layer == PaletteTilesData.PaletteData.WaterLayer)
                  tilemap.gameObject.name += " Water";

               if (!AssetSerializationMaps.layersWithColliders.Contains(tile.layer) && tilemap.GetComponent<Collider>()) {
                  Object.Destroy(tilemap.GetComponent<Collider>());
               }

               tms[layerIndex] = tilemap;
            }
         }

         var groups = tiles
            .Where(t => t.tile != AssetSerializationMaps.transparentTileBase)
            .GroupBy(t => t.layer * 100 + (t.sublayer ?? 0))
            .Select(group => new { group.Key, Tiles = group.Select(t => t.tile), Positions = group.Select(t => t.position) });

         foreach(var group in groups) {
            tms[group.Key].SetTiles(group.Positions.ToArray(), group.Tiles.ToArray());
         }
      }

      public static Direction? ParseDirection(string data) {
         switch(data.Trim(' ')) {
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

