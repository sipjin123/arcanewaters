using MapCreationTool.PaletteTilesData;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
    public class PaletteResources : MonoBehaviour
    {
        [SerializeField]
        public PaletteDataContainer[] biomeDatas = new PaletteDataContainer[0];
        [SerializeField]
        private Tilemap sharedTilemap = null;

        [SerializeField]
        private bool drawSublayers = true;
        [HideInInspector]
        public int[] subLayers = new int[50 * 100];

        public Dictionary<BiomeType, PaletteData> GatherData(EditorConfig config)
        {
            Dictionary<BiomeType, PaletteData> result = new Dictionary<BiomeType, PaletteData>();

            Tilemap clusterMap = transform.Find("cluster_map").GetComponent<Tilemap>();
            Tilemap layerMap = transform.Find("layer_map").GetComponent<Tilemap>();
            Transform specialCon = transform.Find("special");

            BiomedTileData[,] tiles = GatherTiles(biomeDatas, layerMap, config);
            AddSharedTiles(
                tiles,
                sharedTilemap,
                new List<BiomeType> { BiomeType.Desert, BiomeType.Forest, BiomeType.Lava, BiomeType.Pine, BiomeType.Shroom, BiomeType.Snow },
                layerMap,
                config);

            List<BiomedTileGroup> biomedGroups = CreateGroups(tiles, clusterMap);

            foreach (var container in biomeDatas)
            {
                Transform prefCon = container.transform.Find("prefabs");
                List<TileGroup> groups = FormSpecialGroups(biomedGroups, tiles, specialCon, prefCon, container.biome);

                PaletteData palette = new PaletteData
                {
                    TileGroups = new TileGroup[tiles.GetLength(0), tiles.GetLength(1)],
                    PrefabGroups = groups.Select(g => g as PrefabGroup).Where(g => g != null).ToList(),
                    Type = container.biome
                };

                foreach (TileGroup group in groups)
                {
                    for (int i = 0; i < group.Tiles.GetLength(0); i++)
                    {
                        for (int j = 0; j < group.Tiles.GetLength(1); j++)
                        {
                            palette.TileGroups[group.Start.x + i, group.Start.y + j] = group;
                        }
                    }
                }

                result.Add(container.biome, palette);
            }

            return result;
        }

        private BiomedTileData[,] GatherTiles(PaletteDataContainer[] containers, Tilemap layerMap, EditorConfig config)
        {
            BiomedTileData[,] result = new BiomedTileData[containers.Max(c => c.tilemap.size.x), containers.Max(c => c.tilemap.size.y)];

            for (int i = 0; i < result.GetLength(0); i++)
            {
                for (int j = 0; j < result.GetLength(1); j++)
                {
                    result[i, j] = new BiomedTileData { Tile = new BiomedTile(), Layer = -1 };
                    if (i * 100 + j < subLayers.Length)
                        result[i, j].SubLayer = subLayers[i * 100 + j];

                    foreach (var con in containers)
                    {
                        TileBase tile = con.tilemap.GetTile(new Vector3Int(i, j, 0));
                        if (tile != null)
                            result[i, j].Tile[con.biome] = tile;
                    }

                    if (result[i, j].Tile.DefinedBiomes == 0)
                        result[i, j] = null;
                    else
                    {
                        //Find the layer in the config's layer definitions, if not, leave as -1
                        TileBase layerMarker = layerMap.GetTile(new Vector3Int(i, j, 0));
                        for (int k = 0; k < config.layerTiles.Length; k++)
                        {
                            if (config.layerTiles[k] == layerMarker)
                            {
                                result[i, j].Layer = k;
                                break;
                            }
                        }
                    }
                }
            }

            return result;
        }

        private void AddSharedTiles(BiomedTileData[,] tiles, Tilemap sharedTilemap, List<BiomeType> biomesToSet, Tilemap layerMap, EditorConfig config)
        {
            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                for (int j = 0; j < tiles.GetLength(1); j++)
                {
                    TileBase tile = sharedTilemap.GetTile(new Vector3Int(i, j, 0));
                    if (tile != null)
                    {
                        tiles[i, j] = new BiomedTileData { Tile = new BiomedTile(), Layer = -1 };
                        if (i * 100 + j < subLayers.Length)
                            tiles[i, j].SubLayer = subLayers[i * 100 + j];

                        foreach (var biome in biomesToSet)
                            tiles[i, j].Tile[biome] = tile;

                        //Find the layer in the config's layer definitions, if not, leave as -1
                        TileBase layerMarker = layerMap.GetTile(new Vector3Int(i, j, 0));
                        for (int k = 0; k < config.layerTiles.Length; k++)
                        {
                            if (config.layerTiles[k] == layerMarker)
                            {
                                tiles[i, j].Layer = k;
                                break;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Takes a list of regular groups and form a list of groups with special groups included
        /// </summary>
        /// <param name="data"></param>
        /// <param name="specialCon"></param>
        /// <param name="prefabCon"></param>
        private List<TileGroup> FormSpecialGroups(List<BiomedTileGroup> groups, BiomedTileData[,] tileMatrix, Transform specialCon, Transform prefabCon, BiomeType biome)
        {
            List<TileGroup> result = new List<TileGroup>();
            foreach (var group in groups)
            {
                //------------------------------------------------
                //Prefabs
                Transform t = OverlapAnyChild(group, prefabCon);
                if (t != null)
                {
                    if (t.GetComponent<TreePrafabConfig>())
                    {
                        result.Add(new TreePrefabGroup
                        {
                            Tiles = ExtractBiome(group.Tiles, biome),
                            Start = group.Start,
                            BurrowedPref = t.GetComponent<TreePrafabConfig>().burrowedPref,
                            RefPref = t.GetComponent<TreePrafabConfig>().regularPref
                        });
                    }
                    else
                    {
                        result.Add(new PrefabGroup
                        {
                            Tiles = ExtractBiome(group.Tiles, biome),
                            Start = group.Start,
                            RefPref = t.gameObject
                        });
                    }

                    continue;
                }
                //------------------------------------------------
                //Other special
                t = OverlapAnyChild(group, specialCon);
                if (t != null)
                {
                    if (t.GetComponent<NineSliceInOutConfig>())
                    {
                        result.Add(FormSpecialGroup(group, tileMatrix, t.GetComponent<NineSliceInOutConfig>(), biome));
                    }
                    else if (t.GetComponent<NineFourGroupConfig>())
                    {
                        result.Add(FormSpecialGroup(group, tileMatrix, t.GetComponent<NineFourGroupConfig>(), biome));
                    }
                    else if (t.GetComponent<MountainGroupConfig>())
                    {
                        result.Add(FormSpecialGroup(group, t.GetComponent<MountainGroupConfig>(), biome));
                    }
                    else if (t.GetComponent<NineGroupConfig>())
                    {
                        result.Add(FormSpecialGroup(group, t.GetComponent<NineGroupConfig>(), biome));
                    }
                    else if (t.GetComponent<DockGroupConfig>())
                    {
                        result.Add(FormSpecialGroup(group, t.GetComponent<DockGroupConfig>(), biome));
                    }
                    else if (t.GetComponent<WallGroupConfig>())
                    {
                        result.Add(FormSpecialGroup(group, tileMatrix, t.GetComponent<WallGroupConfig>(), biome));
                    }
                    continue;

                }

                result.Add(new TileGroup
                {
                    Tiles = ExtractBiome(group.Tiles, biome),
                    Start = group.Start,
                    Type = TileGroupType.Regular
                });
            }
            return result;
        }

        private MountainGroup FormSpecialGroup(BiomedTileGroup from, MountainGroupConfig config, BiomeType biome)
        {
            var tm = config.biomeTileMaps.First(bm => bm.biome == biome);
            var outerTilemap = tm.outerTilemap;
            var innerTilemap = tm.innerTilemap;

            MountainGroup newGroup = new MountainGroup
            {
                Tiles = ExtractBiome(from.Tiles, biome),
                Start = from.Start,
                InnerTiles = new TileBase[config.innerSize.x, config.innerSize.y],
                OuterTiles = new TileBase[config.outerSize.x, config.outerSize.y]
            };

            for (int i = 0; i < config.innerSize.x; i++)
            {
                for (int j = 0; j < config.innerSize.y; j++)
                {
                    Vector3Int index = new Vector3Int(i, j, 0) - innerTilemap.origin - Vector3Int.RoundToInt(innerTilemap.transform.position);

                    newGroup.InnerTiles[i, j] = innerTilemap.GetTile(index);
                }
            }

            //Debug.Log(innerTilemap.size);
            for (int i = 0; i < config.outerSize.x; i++)
            {
                for (int j = 0; j < config.outerSize.y; j++)
                {
                    Vector3Int index = new Vector3Int(i, j, 0) - outerTilemap.origin - Vector3Int.RoundToInt(outerTilemap.transform.position);

                    newGroup.OuterTiles[i, j] = outerTilemap.GetTile(index);
                }
            }

            foreach (var data in from.Tiles)
            {
                if (data != null)
                {
                    newGroup.Layer = data.Layer;
                    break;
                }
            }
            return newGroup;
        }

        private NineSliceInOutGroup FormSpecialGroup(BiomedTileGroup from, BiomedTileData[,] tileMatrix, NineSliceInOutConfig config, BiomeType biome)
        {
            BoundsInt innerBounds = config.innerBounds;
            innerBounds.position += Vector3Int.RoundToInt(config.transform.position);
            BoundsInt outerBounds = config.outerBounds;
            outerBounds.position += Vector3Int.RoundToInt(config.transform.position);

            NineSliceInOutGroup newGroup = new NineSliceInOutGroup
            {
                Tiles = ExtractBiome(from.Tiles, biome),
                Start = from.Start,
                InnerTiles = new PaletteTilesData.TileData[innerBounds.size.x, innerBounds.size.y],
                OuterTiles = new PaletteTilesData.TileData[outerBounds.size.x, outerBounds.size.y],
            };

            for (int i = 0; i < config.innerBounds.size.x; i++)
            {
                for (int j = 0; j < config.innerBounds.size.y; j++)
                {
                    Vector2Int index = new Vector2Int(innerBounds.position.x + i, innerBounds.position.y + j);
                    if (tileMatrix[index.x, index.y] != null)
                    {
                        newGroup.InnerTiles[i, j] = new PaletteTilesData.TileData
                        {
                            Tile = tileMatrix[index.x, index.y].Tile[biome],
                            Layer = tileMatrix[index.x, index.y].Layer,
                            SubLayer = tileMatrix[index.x, index.y].SubLayer
                        };
                    }
                }
            }

            for (int i = 0; i < config.outerBounds.size.x; i++)
            {
                for (int j = 0; j < config.outerBounds.size.y; j++)
                {
                    Vector2Int index = new Vector2Int(outerBounds.position.x + i, outerBounds.position.y + j);
                    if (tileMatrix[index.x, index.y] != null)
                    {
                        newGroup.OuterTiles[i, j] = new PaletteTilesData.TileData
                        {
                            Tile = tileMatrix[index.x, index.y].Tile[biome],
                            Layer = tileMatrix[index.x, index.y].Layer,
                            SubLayer = tileMatrix[index.x, index.y].SubLayer
                        };
                    }
                }
            }
            return newGroup;
        }

        private NineGroup FormSpecialGroup(BiomedTileGroup from, NineGroupConfig config, BiomeType biome)
        {
            NineGroup newGroup = new NineGroup
            {
                Tiles = ExtractBiome(from.Tiles, biome),
                Start = from.Start
            };
            newGroup.Layer = newGroup.Tiles[1, 1].Layer;
            newGroup.SubLayer = newGroup.Tiles[1, 1].SubLayer;
            return newGroup;
        }

        private DockGroup FormSpecialGroup(BiomedTileGroup from, DockGroupConfig config, BiomeType biome)
        {
            DockGroup newGroup = new DockGroup
            {
                Tiles = ExtractBiome(from.Tiles, biome),
                Start = from.Start
            };

            newGroup.Layer = newGroup.Tiles[1, 1].Layer;
            newGroup.SubLayer = newGroup.Tiles[1, 1].SubLayer;

            return newGroup;
        }

        private NineFourGroup FormSpecialGroup(BiomedTileGroup from, BiomedTileData[,] tileMatrix, NineFourGroupConfig config, BiomeType biome)
        {
            BoundsInt mainBounds = config.mainBounds;
            mainBounds.position += Vector3Int.RoundToInt(config.transform.position);
            BoundsInt cornerBounds = config.cornerBounds;
            cornerBounds.position += Vector3Int.RoundToInt(config.transform.position);

            NineFourGroup newGroup = new NineFourGroup
            {
                Tiles = ExtractBiome(from.Tiles, biome),
                Start = from.Start,
                MainTiles = new PaletteTilesData.TileData[mainBounds.size.x, mainBounds.size.y],
                CornerTiles = new PaletteTilesData.TileData[cornerBounds.size.x, cornerBounds.size.y],
                SingleLayer = config.singleLayer
            };

            for (int i = 0; i < mainBounds.size.x; i++)
            {
                for (int j = 0; j < mainBounds.size.y; j++)
                {
                    Vector2Int index = new Vector2Int(mainBounds.position.x + i, mainBounds.position.y + j);
                    if (tileMatrix[index.x, index.y] != null)
                    {
                        newGroup.MainTiles[i, j] = new PaletteTilesData.TileData
                        {
                            Tile = tileMatrix[index.x, index.y].Tile[biome],
                            Layer = tileMatrix[index.x, index.y].Layer,
                            SubLayer = tileMatrix[index.x, index.y].SubLayer
                        };
                    }
                }
            }

            for (int i = 0; i < cornerBounds.size.x; i++)
            {
                for (int j = 0; j < cornerBounds.size.y; j++)
                {
                    Vector2Int index = new Vector2Int(cornerBounds.position.x + i, cornerBounds.position.y + j);
                    if (tileMatrix[index.x, index.y] != null)
                    {
                        newGroup.CornerTiles[i, j] = new PaletteTilesData.TileData
                        {
                            Tile = tileMatrix[index.x, index.y].Tile[biome],
                            Layer = tileMatrix[index.x, index.y].Layer,
                            SubLayer = tileMatrix[index.x, index.y].SubLayer
                        };
                    }
                }
            }

            newGroup.Layer = newGroup.MainTiles[1, 1].Layer;
            return newGroup;
        }

        private WallGroup FormSpecialGroup(BiomedTileGroup from, BiomedTileData[,] tileMatrix, WallGroupConfig config, BiomeType biome)
        {
            BoundsInt bounds = config.tileBounds;
            bounds.position += Vector3Int.RoundToInt(config.transform.position);

            WallGroup newGroup = new WallGroup
            {
                Tiles = ExtractBiome(from.Tiles, biome),
                Start = from.Start,
                AllTiles = new PaletteTilesData.TileData[bounds.size.x, bounds.size.y]
            };

            for (int i = 0; i < bounds.size.x; i++)
            {
                for (int j = 0; j < bounds.size.y; j++)
                {
                    Vector2Int index = new Vector2Int(bounds.position.x + i, bounds.position.y + j);
                    if (tileMatrix[index.x, index.y] != null)
                    {
                        newGroup.AllTiles[i, j] = new PaletteTilesData.TileData
                        {
                            Tile = tileMatrix[index.x, index.y].Tile[biome],
                            Layer = tileMatrix[index.x, index.y].Layer,
                            SubLayer = tileMatrix[index.x, index.y].SubLayer
                        };
                    }
                }
            }

            newGroup.Layer = newGroup.AllTiles[2, 2].Layer;
            return newGroup;
        }

        private Transform OverlapAnyChild(BiomedTileGroup group, Transform objectContainer)
        {
            for (int i = 0; i < objectContainer.childCount; i++)
            {
                Vector2 point = objectContainer.GetChild(i).transform.position;
                if (group.Start.x < point.x && group.Start.y < point.y &&
                    group.Start.x + group.Tiles.GetLength(0) > point.x &&
                    group.Start.y + group.Tiles.GetLength(1) > point.y)
                {
                    return objectContainer.GetChild(i);
                }
            }
            return null;
        }

        /// <summary>
        /// Creates tile groups with layers based on cluster data
        /// </summary>
        /// <param name="tileMap"></param>
        /// <param name="clusterMap"></param>
        /// <param name="layerMap"></param>
        /// <param name="config"></param>
        /// <returns></returns>
        private List<BiomedTileGroup> CreateGroups(BiomedTileData[,] tiles, Tilemap clusterMap)
        {
            List<BiomedTileGroup> groups = new List<BiomedTileGroup>();

            //The tiles from the tilemap which were already proccessed
            bool[,] proccessed = new bool[tiles.GetLength(0), tiles.GetLength(1)];

            for (int i = 0; i < tiles.GetLength(0); i++)
            {
                for (int j = 0; j < tiles.GetLength(1); j++)
                {
                    if (!proccessed[i, j])
                    {
                        //Check if the tilemap has any tiles, if not, mark as proccessed and ignore
                        if (tiles[i, j] == null)
                        {
                            proccessed[i, j] = true;
                            continue;
                        }

                        //Find all the tiles that are in a cluster as defined in the cluster map
                        var clusterTiles = SearchForCluster(new Vector3Int(i, j, 0), proccessed, clusterMap);

                        //The left-down most location of the rectangle that binds the group
                        Vector2Int origin = new Vector2Int(clusterTiles.Min(t => t.x), clusterTiles.Min(t => t.y));
                        //The size of the binding rectangle
                        Vector2Int length = new Vector2Int(
                            clusterTiles.Max(t => t.x - origin.x + 1), clusterTiles.Max(t => t.y - origin.y + 1));

                        BiomedTileGroup group = new BiomedTileGroup
                        {
                            Tiles = new BiomedTileData[length.x, length.y],
                            Start = (Vector3Int)origin
                        };

                        foreach (var pos in clusterTiles)
                        {
                            if (clusterTiles.Count == 9) { }

                            if (tiles[pos.x, pos.y] == null)
                                continue;

                            Vector2Int index = (Vector2Int)pos - origin;
                            group.Tiles[index.x, index.y] = tiles[pos.x, pos.y];
                        }
                        groups.Add(group);
                    }
                }
            }
            return groups;
        }

        private PaletteTilesData.TileData[,] ExtractBiome(BiomedTileData[,] tiles, BiomeType targetBiome)
        {
            PaletteTilesData.TileData[,] result = new PaletteTilesData.TileData[tiles.GetLength(0), tiles.GetLength(1)];

            for (int i = 0; i < result.GetLength(0); i++)
            {
                for (int j = 0; j < result.GetLength(1); j++)
                {
                    if (tiles[i, j] != null && tiles[i, j].Tile[targetBiome] != null)
                    {
                        result[i, j] = new PaletteTilesData.TileData
                        {
                            Tile = tiles[i, j].Tile[targetBiome],
                            Layer = tiles[i, j].Layer,
                            SubLayer = tiles[i, j].SubLayer
                        };
                    }
                }
            }

            return result;
        }

        private List<Vector3Int> SearchForCluster(Vector3Int start, bool[,] proccessed, Tilemap clusterMap)
        {
            proccessed[start.x, start.y] = true;
            TileBase clusterKey = clusterMap.GetTile(start);
            if (clusterKey == null)
            {
                return new List<Vector3Int>() { start };
            }

            List<Vector3Int> result = new List<Vector3Int>();
            Queue<Vector3Int> q = new Queue<Vector3Int>();
            q.Enqueue(start);

            int breakCounter = 0;

            while (q.Count > 0)
            {
                Vector3Int current = q.Dequeue();

                if (current.x < proccessed.GetLength(0) - 1 && !proccessed[current.x + 1, current.y] &&
                    clusterMap.GetTile(current + Vector3Int.right) == clusterKey)
                {
                    q.Enqueue(current + Vector3Int.right);
                    proccessed[current.x + 1, current.y] = true;
                }
                if (current.x > 0 && !proccessed[current.x - 1, current.y] &&
                    clusterMap.GetTile(current + Vector3Int.left) == clusterKey)
                {
                    q.Enqueue(current + Vector3Int.left);
                    proccessed[current.x - 1, current.y] = true;
                }
                if (current.y < proccessed.GetLength(1) - 1 && !proccessed[current.x, current.y + 1] &&
                    clusterMap.GetTile(current + Vector3Int.up) == clusterKey)
                {
                    q.Enqueue(current + Vector3Int.up);
                    proccessed[current.x, current.y + 1] = true;
                }
                if (current.y > 0 && !proccessed[current.x, current.y - 1] &&
                    clusterMap.GetTile(current + Vector3Int.down) == clusterKey)
                {
                    q.Enqueue(current + Vector3Int.down);
                    proccessed[current.x, current.y - 1] = true;
                }

                result.Add(current);

                breakCounter++;
                if (breakCounter > 1000)
                {
                    throw new System.Exception("Never ending cycle detected!");
                }
            }

            return result;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawSublayers)
                return;

            if (subLayers == null || subLayers.Length != 50 * 100)
            {
                int[] prev = subLayers ?? new int[0];
                subLayers = new int[50 * 100];
                for (int i = 0; i < prev.Length; i++)
                    subLayers[i] = prev[i];
            }

#if UNITY_EDITOR
            for (int i = 0; i < 50; i++)
            {
                for (int j = 0; j < 100; j++)
                {
                    GUIStyle style = UnityEditor.EditorStyles.whiteMiniLabel;
                    var oldAlign = style.alignment;
                    style.alignment = TextAnchor.MiddleCenter;

                    UnityEditor.Handles.Label(new Vector3(i, j, 0) + Vector3.one * 0.5f, subLayers[i * 100 + j].ToString(), style);

                    style.alignment = oldAlign;
                }
            }
#endif
        }
    }

    public class BiomedTile
    {
        private Dictionary<BiomeType, TileBase> tiles = new Dictionary<BiomeType, TileBase>();

        public TileBase this[BiomeType type]
        {
            get
            {
                if (tiles.TryGetValue(type, out TileBase value))
                    return value;
                return null;
            }
            set
            {
                if (tiles.ContainsKey(type))
                    tiles[type] = value;
                else
                    tiles.Add(type, value);
            }
        }

        public int DefinedBiomes
        {
            get { return tiles.Count; }
        }
    }

    public class BiomedTileGroup
    {
        public BiomedTileData[,] Tiles { get; set; }
        public Vector3Int Start { get; set; }
    }

    public class BiomedTileData
    {
        public BiomedTile Tile { get; set; }
        public int Layer { get; set; }
        public int SubLayer { get; set; }
    }
}