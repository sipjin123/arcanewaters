using MapCreationTool.PaletteTilesData;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
    public class PaletteDataContainer : MonoBehaviour
    {
        public PaletteData GatherData(EditorConfig config, BiomeType? type)
        {
            Tilemap clusterMap = transform.parent.Find("cluster_map").GetComponent<Tilemap>();
            Tilemap layerMap = transform.parent.Find("layer_map").GetComponent<Tilemap>();
            Tilemap tileMap = transform.Find("tile_map").GetComponent<Tilemap>();
            Transform prefCon = transform.Find("prefabs");
            Transform specialCon = transform.parent.Find("special");

            List<TileGroup> groups = CreateGroups(tileMap, clusterMap, layerMap, config);
            groups = FormSpecialGroups(groups, specialCon, prefCon, type);

            PaletteData result = new PaletteData
            {
                TileGroups = new TileGroup[tileMap.size.x, tileMap.size.y],
                PrefabGroups = groups.Select(g => g as PrefabGroup).Where(g => g != null).ToList(),
                Type = type
            };

            foreach (TileGroup group in groups)
            {
                for (int i = 0; i < group.Tiles.GetLength(0); i++)
                {
                    for (int j = 0; j < group.Tiles.GetLength(1); j++)
                    {
                        result.TileGroups[group.Start.x + i, group.Start.y + j] = group;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Takes a list of regular groups and form a list of groups with special groups included
        /// </summary>
        /// <param name="data"></param>
        /// <param name="specialCon"></param>
        /// <param name="prefabCon"></param>
        private List<TileGroup> FormSpecialGroups(List<TileGroup> groups, Transform specialCon, Transform prefabCon, BiomeType? biome)
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
                            Tiles = group.Tiles,
                            Start = group.Start,
                            BurrowedPref = t.GetComponent<TreePrafabConfig>().burrowedPref,
                            RefPref = t.GetComponent<TreePrafabConfig>().regularPref
                        });
                    }
                    else
                    {
                        result.Add(new PrefabGroup
                        {
                            Tiles = group.Tiles,
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
                        result.Add(FormSpecialGroup(group, t.GetComponent<NineSliceInOutConfig>()));
                    }
                    else if (t.GetComponent<NineFourGroupConfig>())
                    {
                        result.Add(FormSpecialGroup(group, t.GetComponent<NineFourGroupConfig>()));
                    }
                    else if (t.GetComponent<MountainGroupConfig>() && biome != null)
                    {
                        result.Add(FormSpecialGroup(group, t.GetComponent<MountainGroupConfig>(), biome.Value));
                    }
                    else if (t.GetComponent<NineGroupConfig>())
                    {
                        result.Add(FormSpecialGroup(group, t.GetComponent<NineGroupConfig>()));
                    }
                    continue;
                }
                result.Add(group);
            }
            return result;
        }

        private MountainGroup FormSpecialGroup(TileGroup from, MountainGroupConfig config, BiomeType biome)
        {
            var tm = config.biomeTileMaps.First(bm => bm.biome == biome);
            var outerTilemap = tm.outerTilemap;
            var innerTilemap = tm.innerTilemap;

            BoundsInt innerBounds = config.innerBounds;
            innerBounds.position += Vector3Int.RoundToInt(config.transform.position);
            BoundsInt outerBounds = config.outerBounds;
            outerBounds.position += Vector3Int.RoundToInt(config.transform.position);

            MountainGroup newGroup = new MountainGroup
            {
                Tiles = from.Tiles,
                Start = from.Start,
                InnerTiles = new TileBase[innerBounds.size.x, innerBounds.size.y],
                OuterTiles = new TileBase[outerBounds.size.x, outerBounds.size.y]
            };

            for (int i = 0; i < config.innerBounds.size.x; i++)
            {
                for (int j = 0; j < config.innerBounds.size.y; j++)
                {
                    Vector3Int index = new Vector3Int(i, j, 0) + innerBounds.min - innerTilemap.origin - Vector3Int.RoundToInt(innerTilemap.transform.position);

                    newGroup.InnerTiles[i, j] = innerTilemap.GetTile(index);
                }
            }
            
            //Debug.Log(innerTilemap.size);
            for (int i = 0; i < config.outerBounds.size.x; i++)
            {
                for (int j = 0; j < config.outerBounds.size.y; j++)
                {
                    Vector3Int index = new Vector3Int(i, j, 0) + outerBounds.min - outerTilemap.origin - Vector3Int.RoundToInt(outerTilemap.transform.position);
                    
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

        private NineSliceInOutGroup FormSpecialGroup(TileGroup from, NineSliceInOutConfig config)
        {
            BoundsInt innerBounds = config.innerBounds;
            innerBounds.position += Vector3Int.RoundToInt(config.transform.position);
            BoundsInt outerBounds = config.outerBounds;
            outerBounds.position += Vector3Int.RoundToInt(config.transform.position);

            NineSliceInOutGroup newGroup = new NineSliceInOutGroup
            {
                Tiles = from.Tiles,
                Start = from.Start,
                InnerTiles = new PaletteTilesData.TileData[innerBounds.size.x, innerBounds.size.y],
                OuterTiles = new PaletteTilesData.TileData[outerBounds.size.x, outerBounds.size.y],
            };

            for (int i = 0; i < config.innerBounds.size.x; i++)
            {
                for (int j = 0; j < config.innerBounds.size.y; j++)
                {
                    newGroup.InnerTiles[i, j] = from.Tiles[
                        innerBounds.position.x + i - from.Start.x,
                        innerBounds.position.y + j - from.Start.y
                    ];
                }
            }

            for (int i = 0; i < config.outerBounds.size.x; i++)
            {
                for (int j = 0; j < config.outerBounds.size.y; j++)
                {
                    newGroup.OuterTiles[i, j] = from.Tiles[
                        outerBounds.position.x + i - from.Start.x,
                        outerBounds.position.y + j - from.Start.y
                    ];
                }
            }
            return newGroup;
        }

        private NineGroup FormSpecialGroup(TileGroup from, NineGroupConfig config)
        {
            NineGroup newGroup = new NineGroup
            {
                Tiles = from.Tiles,
                Start = from.Start
            };
            newGroup.Layer = newGroup.Tiles[1, 1].Layer;

            return newGroup;
        }

        private NineFourGroup FormSpecialGroup(TileGroup from, NineFourGroupConfig config)
        {
            BoundsInt pathBounds = config.pathBounds;
            pathBounds.position += Vector3Int.RoundToInt(config.transform.position);
            BoundsInt cornerBounds = config.cornerBounds;
            cornerBounds.position += Vector3Int.RoundToInt(config.transform.position);

            NineFourGroup newGroup = new NineFourGroup
            {
                Tiles = from.Tiles,
                Start = from.Start,
                MainTiles = new PaletteTilesData.TileData[pathBounds.size.x, pathBounds.size.y],
                CornerTiles = new PaletteTilesData.TileData[cornerBounds.size.x, cornerBounds.size.y]
            };

            for (int i = 0; i < pathBounds.size.x; i++)
            {
                for (int j = 0; j < pathBounds.size.y; j++)
                {
                    newGroup.MainTiles[i, j] = from.Tiles[
                        pathBounds.position.x + i - from.Start.x,
                        pathBounds.position.y + j - from.Start.y
                    ];
                }
            }

            for (int i = 0; i < cornerBounds.size.x; i++)
            {
                for (int j = 0; j < cornerBounds.size.y; j++)
                {
                    newGroup.CornerTiles[i, j] = from.Tiles[
                        cornerBounds.position.x + i - from.Start.x,
                        cornerBounds.position.y + j - from.Start.y
                    ];
                }
            }

            newGroup.Layer = newGroup.MainTiles[1, 1].Layer;

            return newGroup;
        }
        private Transform OverlapAnyChild(TileGroup group, Transform objectContainer)
        {
            for (int i = 0; i < objectContainer.childCount; i++)
            {
                Vector2 point = objectContainer.GetChild(i).transform.position;
                if (group.Start.x < point.x && group.Start.y < point.y &&
                    group.Start.x + group.Size.x > point.x &&
                    group.Start.y + group.Size.y > point.y)
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
        private List<TileGroup> CreateGroups(Tilemap tileMap, Tilemap clusterMap, Tilemap layerMap, EditorConfig config)
        {
            List<TileGroup> groups = new List<TileGroup>();

            //The tiles from the tilemap which were already proccessed
            bool[,] proccessed = new bool[tileMap.size.x, tileMap.size.y];

            for (int i = 0; i < tileMap.size.x; i++)
            {
                for (int j = 0; j < tileMap.size.y; j++)
                {
                    if (!proccessed[i, j])
                    {
                        //Check if the tilemap has any tiles, if not, mark as proccessed and ignore
                        if (!tileMap.HasTile(new Vector3Int(i, j, 0)))
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

                        TileGroup group = new TileGroup
                        {
                            Tiles = new PaletteTilesData.TileData[length.x, length.y],
                            Start = (Vector3Int)origin
                        };

                        foreach (var pos in clusterTiles)
                        {
                            if (tileMap.GetTile(pos) == null)
                                continue;
                            Vector2Int index = (Vector2Int)pos - origin;
                            group.Tiles[index.x, index.y] = new PaletteTilesData.TileData
                            {
                                Tile = tileMap.GetTile(pos),
                                Layer = -1
                            };

                            //Find the layer in the config's layer definitions, if not, leave as -1
                            TileBase layerMarker = layerMap.GetTile(pos);
                            for (int k = 0; k < config.layerTiles.Length; k++)
                            {
                                if (config.layerTiles[k] == layerMarker)
                                {
                                    group.Tiles[index.x, index.y].Layer = k;
                                    break;
                                }
                            }
                        }
                        groups.Add(group);
                    }
                }
            }
            return groups;
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
    }
}
