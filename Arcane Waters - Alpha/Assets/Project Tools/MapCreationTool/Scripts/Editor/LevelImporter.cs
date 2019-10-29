using UnityEngine;
using UnityEditor;
using MapCreationTool.Serialization;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
    public class LevelImporter : AssetPostprocessor
    {
        public const string DataFileExtension = "arcane";

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (string str in importedAssets.Union(movedAssets))
            {
                string extension = Path.GetExtension(str).Trim('.');
                if(extension.CompareTo(DataFileExtension) == 0)
                {
                    ReimportLevel(Path.GetFileNameWithoutExtension(str), Path.GetDirectoryName(str), str);
                }
            }
        }

        static void ReimportLevel(string name, string directory, string dataPath)
        {
            EnsureSerializationMapsLoaded();

            DeserializedProject data = Serializer.Deserialize(File.ReadAllText(dataPath));
            Dictionary<int, Tilemap> layers = new Dictionary<int, Tilemap>();

            GameObject level = new GameObject();
            level.AddComponent<Grid>();
            Transform tilemaps = new GameObject("tilemaps").transform;
            Transform prefabs = new GameObject("prefabs").transform;
            tilemaps.parent = level.transform;
            prefabs.parent = level.transform;

            foreach(var tile in data.tiles)
            {
                if (tile.tile == AssetSerializationMaps.TransparentTile)
                    continue;

                int layerIndex = tile.layer * 100 + (tile.sublayer ?? 0);

                Tilemap layer = null;
                if (layers.ContainsKey(layerIndex))
                    layer = layers[layerIndex];
                else
                {
                    GameObject l = new GameObject("Layer " + layerIndex);
                    float posZ = tile.layer * AssetSerializationMaps.LayerZMultiplier +
                        (tile.sublayer == null ? 0 : tile.sublayer.Value * AssetSerializationMaps.SublayerZMultiplier);

                    l.transform.position = new Vector3(0, 0, posZ);
                    layer = l.AddComponent<Tilemap>();
                    l.AddComponent<TilemapRenderer>();
                    layers.Add(layerIndex, layer);
                    l.transform.parent = tilemaps.transform;
                }

                layer.SetTile(tile.position, tile.tile);
            }

            foreach(var prefab in data.prefabs)
            {
                var go = PrefabUtility.InstantiatePrefab(prefab.prefab) as GameObject;
                go.transform.parent = prefabs;
                go.transform.position = prefab.position + Vector3.back * 10;
                if (go.GetComponent<ZSnap>())
                    go.GetComponent<ZSnap>().snapZ();
            }


            GameObject pref = PrefabUtility.SaveAsPrefabAsset(level, directory + @"\" + name + " Level" + ".prefab", out bool success);
            Object.DestroyImmediate(level);
        }

        static void EnsureSerializationMapsLoaded()
        {
            if (AssetSerializationMaps.BiomeSpecific != null)
                return;

            AssetSerializationMaps.Load();
        }
    }
}

