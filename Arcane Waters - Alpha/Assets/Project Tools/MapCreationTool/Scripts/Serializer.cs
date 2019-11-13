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
        public static string Serialize(
            Dictionary<int, Layer> layers,
            List<PlacedPrefab> prefabs,
            BiomeType biome,
            bool prettyPrint = false)
        {
            try
            {
                return Serialize001(layers, prefabs, biome, prettyPrint);
            }
            catch(Exception ex)
            {
                Debug.LogError("Failed to serialize map.");
                throw ex;
            }
        }

        private static string Serialize001(
            Dictionary<int, Layer> layers, 
            List<PlacedPrefab> prefabs, 
            BiomeType biome,
            bool prettyPrint = false)
        {
            Func<GameObject, int> prefabToIndex = (go) => { return AssetSerializationMaps.GetIndex(go, biome); };
            Func<TileBase, Vector2Int> tileToIndex = (tile) => { return AssetSerializationMaps.GetIndex(tile, biome); };

            //Make prefab serialization object
            Prefab001[] prefabsSerialized
                = prefabs.Select(p =>
                    new Prefab001
                    {
                        i = prefabToIndex(p.Original),
                        x = p.PlacedInstance.transform.position.x,
                        y = p.PlacedInstance.transform.position.y
                    }
                ).ToArray();

            //Make layer serialization object
            List<Layer001> layersSerialized = new List<Layer001>();

            foreach(var layerkv in layers)
            {
                if(layerkv.Value.HasTilemap)
                {
                    layersSerialized.Add(new Layer001
                    {
                        index = layerkv.Key,
                        tiles = SerializeTiles(layerkv.Value, tileToIndex),
                        sublayers = new SubLayer001[0]
                    });
                }
                else
                {
                    Layer001 ls = new Layer001
                    {
                        index = layerkv.Key,
                        tiles = new Tile001[0],
                        sublayers = new SubLayer001[layerkv.Value.SubLayers.Length]
                    };
                    for(int i = 0; i < layerkv.Value.SubLayers.Length; i++)
                    {
                        ls.sublayers[i] = new SubLayer001
                        {
                            index = i,
                            tiles = SerializeTiles(layerkv.Value.SubLayers[i], tileToIndex)
                        };
                    }
                    layersSerialized.Add(ls);
                }
            }

            Project001 project = new Project001 { 
                version = "0.0.1",
                biome = biome,
                layers = layersSerialized.ToArray(),
                prefabs = prefabsSerialized
            };
           
            return JsonUtility.ToJson(project, prettyPrint);
        }

        public static DeserializedProject Deserialize(string data)
        {
            Project001 dt = JsonUtility.FromJson<Project001>(data);

            if (dt.version != "0.0.1")
                throw new System.ArgumentException($"File version {dt.version} not supported.");

            List<DeserializedProject.DeserializedPrefab> prefabs = new List<DeserializedProject.DeserializedPrefab>();
            List<DeserializedProject.DeserializedTile> tiles = new List<DeserializedProject.DeserializedTile>();

            Func<Vector2Int, TileBase> indexToTile = (index) => { return AssetSerializationMaps.GetTile(index, dt.biome); };
            Func<int, GameObject> indexToPrefab = (index) => { return AssetSerializationMaps.GetPrefab(index, dt.biome); };

            foreach (var pref in dt.prefabs)
            {
                prefabs.Add(new DeserializedProject.DeserializedPrefab
                {
                    position = new Vector3(pref.x, pref.y, 0),
                    prefab = indexToPrefab(pref.i)
                });
            }

            foreach (var layer in dt.layers)
            {
                if (layer.sublayers.Length == 0)
                {
                    foreach (var tile in layer.tiles)
                    {
                        tiles.Add(new DeserializedProject.DeserializedTile
                        {
                            layer = layer.index,
                            sublayer = null,
                            position = new Vector3Int(tile.x, tile.y, 0),
                            tile = indexToTile(new Vector2Int(tile.i, tile.j))
                        });
                    }
                }
                else
                {
                    for (int i = 0; i < layer.sublayers.Length; i++)
                    {
                        foreach (var tile in layer.sublayers[i].tiles)
                        {
                            tiles.Add(new DeserializedProject.DeserializedTile
                            {
                                layer = layer.index,
                                sublayer = i,
                                position = new Vector3Int(tile.x, tile.y, 0),
                                tile = indexToTile(new Vector2Int(tile.i, tile.j))
                            });
                        }
                    }
                }
            }

            return new DeserializedProject
            {
                prefabs = prefabs.ToArray(),
                tiles = tiles.ToArray(),
                biome = dt.biome
            };
        }

        private static Tile001[] SerializeTiles(Layer layer, Func<TileBase, Vector2Int> tileToIndex)
        {
            List<Tile001> tiles = new List<Tile001>();

            for (int i = 0; i < layer.Size.x; i++)
            {
                for (int j = 0; j < layer.Size.y; j++)
                {
                    Vector3Int pos = new Vector3Int(i + layer.Origin.x, j + layer.Origin.y, 0);
                    TileBase tile = layer.GetTile(pos);
                    if (tile != null)
                    {
                        Vector2Int index = tileToIndex(tile);
                        tiles.Add(new Tile001
                        {
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
        public class DeserializedPrefab
        {
            public GameObject prefab;
            public Vector3 position;
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
        public Layer001[] layers;
        public Prefab001[] prefabs;
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
    }
}
