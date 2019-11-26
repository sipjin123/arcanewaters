using UnityEngine;
using UnityEditor;
using MapCreationTool.Serialization;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Tilemaps;
using Cinemachine;

namespace MapCreationTool
{
   public class LevelImporter : AssetPostprocessor
   {
      public const string DataFileExtension = "arcane";

      static void OnPostprocessAllAssets (string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths) {
         foreach (string str in importedAssets.Union(movedAssets)) {
            string extension = Path.GetExtension(str).Trim('.');
            if (extension.CompareTo(DataFileExtension) == 0) {
               reimportLevel(Path.GetFileNameWithoutExtension(str), Path.GetDirectoryName(str), str);
            }
         }
      }

      static void reimportLevel (string name, string directory, string dataPath) {
         ensureSerializationMapsLoaded();

         DeserializedProject data = Serializer.deserialize(File.ReadAllText(dataPath), false);
         Dictionary<int, Tilemap> layers = new Dictionary<int, Tilemap>();

         GameObject level = new GameObject();
         var area = level.AddComponent<Area>();
         area.areaKey = "TonyTest";

         Transform tilemaps = new GameObject("tilemaps").transform;
         tilemaps.parent = level.transform;
         tilemaps.transform.localScale = new Vector3(0.16f, 0.16f, 1f);
         tilemaps.transform.position = new Vector3(0, 0, 100);

         Grid grid = tilemaps.gameObject.AddComponent<Grid>();
         grid.cellSize = new Vector3(1, 1, 0);

         var rb = tilemaps.gameObject.AddComponent<Rigidbody2D>();
         rb.bodyType = RigidbodyType2D.Static;

         var col = tilemaps.gameObject.AddComponent<CompositeCollider2D>();
         col.generationType = CompositeCollider2D.GenerationType.Manual;

         Transform prefabs = new GameObject("prefabs").transform;
         prefabs.parent = level.transform;

         Transform spawn = new GameObject("spawn").transform;
         spawn.parent = level.transform;
         spawn.transform.localPosition = Vector3.zero;

         var sp = spawn.gameObject.AddComponent<Spawn>();
         sp.spawnKey = "TonyTest";

         foreach (var tile in data.tiles) {
            if (tile.tile == AssetSerializationMaps.transparentTileBase)
               continue;

            int layerIndex = tile.layer * 100 + (tile.sublayer ?? 0);

            Tilemap layer = null;
            if (layers.ContainsKey(layerIndex))
               layer = layers[layerIndex];
            else {
               GameObject l = new GameObject("Layer " + layerIndex);

               //Add water so water checker catches it
               if (tile.layer == PaletteTilesData.PaletteData.WaterLayer)
                  l.name += " Water";

               float posZ = AssetSerializationMaps.layerZFirst + tile.layer * AssetSerializationMaps.layerZMultip +
                   (tile.sublayer == null ? 0 : tile.sublayer.Value * AssetSerializationMaps.sublayerZMultip);

               layer = l.AddComponent<Tilemap>();
               var tlRen = l.AddComponent<TilemapRenderer>();
               tlRen.sortOrder = TilemapRenderer.SortOrder.TopLeft;

               layers.Add(layerIndex, layer);
               l.transform.parent = tilemaps.transform;
               l.transform.localScale = Vector3.one;
               l.transform.localPosition = new Vector3(0, 0, posZ);

               if (AssetSerializationMaps.layersWithColliders.Contains(tile.layer)) {
                  var tmCol = l.AddComponent<TilemapCollider2D>();
                  tmCol.usedByComposite = true;
               }

            }

            layer.SetTile(tile.position, tile.tile);
         }


         foreach (var prefab in data.prefabs) {
            var go = PrefabUtility.InstantiatePrefab(prefab.prefab, prefabs) as GameObject;
            go.transform.parent = prefabs;
            go.transform.position = prefab.position + Vector3.back * 10;
            if (go.GetComponent<ZSnap>())
               go.GetComponent<ZSnap>().snapZ();
         }
         prefabs.localScale = new Vector3(0.16f, 0.16f, 1f);

         var cam = (PrefabUtility.InstantiatePrefab(Resources.Load("Prefabs/StandardMapCamera"), level.transform) as GameObject).GetComponent<CinemachineVirtualCamera>();
         area.vcam = cam;

         level.transform.position = new Vector3(-500, -500, 0);

         GameObject pref = PrefabUtility.SaveAsPrefabAsset(level, directory + @"\" + name + " Level" + ".prefab", out bool success);
         Object.DestroyImmediate(level);
      }

      static void ensureSerializationMapsLoaded () {
         if (AssetSerializationMaps.loaded)
            return;

         AssetSerializationMaps.load();
      }
   }
}

