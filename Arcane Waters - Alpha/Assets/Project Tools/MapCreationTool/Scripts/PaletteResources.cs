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

      public Dictionary<BiomeType, PaletteData> gatherData (EditorConfig config) {
         Dictionary<BiomeType, PaletteData> result = new Dictionary<BiomeType, PaletteData>();

         Tilemap clusterMap = transform.Find("cluster_map").GetComponent<Tilemap>();
         Tilemap layerMap = transform.Find("layer_map").GetComponent<Tilemap>();
         Transform specialCon = transform.Find("special");
         Transform sharedPrefabCon = transform.Find("prefabs_shared");

         BiomedTileData[,] tiles = gatherTiles(biomeDatas, layerMap, config, getTileMatrixSize(biomeDatas, new Tilemap[] { sharedTilemap }));

         addSharedTiles(
             tiles,
             sharedTilemap,
             new List<BiomeType> { BiomeType.Desert, BiomeType.Forest, BiomeType.Lava, BiomeType.Pine, BiomeType.Shroom, BiomeType.Snow },
             layerMap,
             config);

         List<BiomedTileGroup> biomedGroups = createGroups(tiles, clusterMap);

         foreach (var container in biomeDatas) {
            Transform prefCon = container.transform.Find("prefabs");

            List<TileGroup> groups = formSpecialGroups(biomedGroups, tiles, specialCon, new Transform[] { prefCon, sharedPrefabCon }, container.biome);

            PaletteData palette = new PaletteData {
               tileGroups = new TileGroup[tiles.GetLength(0), tiles.GetLength(1)],
               prefabGroups = groups.Select(g => g as PrefabGroup).Where(g => g != null).ToList(),
               type = container.biome
            };

            foreach (TileGroup group in groups) {
               for (int i = 0; i < group.tiles.GetLength(0); i++) {
                  for (int j = 0; j < group.tiles.GetLength(1); j++) {
                     palette.tileGroups[group.start.x + i, group.start.y + j] = group;
                  }
               }
            }

            result.Add(container.biome, palette);
         }

         return result;
      }

      private Vector2Int getTileMatrixSize(PaletteDataContainer[] containers, Tilemap[] tilemaps) {
         return new Vector2Int(
            Mathf.Max(containers.Max(c => c.tilemap.size.x), tilemaps.Max(t => t.size.x)), 
            Mathf.Max(containers.Max(c => c.tilemap.size.y), tilemaps.Max(t => t.size.y)));
      }



      private BiomedTileData[,] gatherTiles (PaletteDataContainer[] containers, Tilemap layerMap, EditorConfig config, Vector2Int matrixSize) {
         BiomedTileData[,] result = new BiomedTileData[matrixSize.x, matrixSize.y];

         for (int i = 0; i < result.GetLength(0); i++) {
            for (int j = 0; j < result.GetLength(1); j++) {
               result[i, j] = new BiomedTileData { tile = new BiomedTile(), layer = -1 };
               if (i * 100 + j < subLayers.Length)
                  result[i, j].subLayer = subLayers[i * 100 + j];

               foreach (var con in containers) {
                  TileBase tile = con.tilemap.GetTile(new Vector3Int(i, j, 0));
                  if (tile != null)
                     result[i, j].tile[con.biome] = tile;
               }

               if (result[i, j].tile.definedBiomes == 0)
                  result[i, j] = null;
               else {
                  //Find the layer in the config's layer definitions, if not, leave as -1
                  TileBase layerMarker = layerMap.GetTile(new Vector3Int(i, j, 0));
                  for (int k = 0; k < config.layerTiles.Length; k++) {
                     if (config.layerTiles[k] == layerMarker) {
                        result[i, j].layer = k;
                        break;
                     }
                  }
               }
            }
         }

         return result;
      }

      private void addSharedTiles (BiomedTileData[,] tiles, Tilemap sharedTilemap, List<BiomeType> biomesToSet, Tilemap layerMap, EditorConfig config) {
         for (int i = 0; i < tiles.GetLength(0); i++) {
            for (int j = 0; j < tiles.GetLength(1); j++) {
               TileBase tile = sharedTilemap.GetTile(new Vector3Int(i, j, 0));
               if (tile != null) {
                  tiles[i, j] = new BiomedTileData { tile = new BiomedTile(), layer = -1 };
                  if (i * 100 + j < subLayers.Length)
                     tiles[i, j].subLayer = subLayers[i * 100 + j];

                  foreach (var biome in biomesToSet)
                     tiles[i, j].tile[biome] = tile;

                  //Find the layer in the config's layer definitions, if not, leave as -1
                  TileBase layerMarker = layerMap.GetTile(new Vector3Int(i, j, 0));
                  for (int k = 0; k < config.layerTiles.Length; k++) {
                     if (config.layerTiles[k] == layerMarker) {
                        tiles[i, j].layer = k;
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
      private List<TileGroup> formSpecialGroups (List<BiomedTileGroup> groups, BiomedTileData[,] tileMatrix, Transform specialCon, Transform[] prefabCons, BiomeType biome) {
         List<TileGroup> result = new List<TileGroup>();
         foreach (var group in groups) {
            //------------------------------------------------
            //Prefabs
            Transform t = overlapAnyChild(group, prefabCons);
            if (t != null) {
               if (t.GetComponent<TreePrafabConfig>()) {
                  result.Add(new TreePrefabGroup {
                     tiles = extractBiome(group.tiles, biome),
                     start = group.start,
                     burrowedPref = t.GetComponent<TreePrafabConfig>().burrowedPref,
                     refPref = t.GetComponent<TreePrafabConfig>().regularPref
                  });
               } else if (t.GetComponent<PrefabConfig>()) {
                  result.Add(new PrefabGroup {
                     tiles = extractBiome(group.tiles, biome),
                     start = group.start,
                     refPref = t.GetComponent<PrefabConfig>().prefab
                  });
               } else {
                  result.Add(new PrefabGroup {
                     tiles = extractBiome(group.tiles, biome),
                     start = group.start,
                     refPref = t.gameObject
                  });
               }

               continue;
            }
            //------------------------------------------------
            //Other special
            t = overlapAnyChild(group, specialCon);
            if (t != null) {
               if (t.GetComponent<NineSliceInOutConfig>()) {
                  result.Add(formSpecialGroup(group, tileMatrix, t.GetComponent<NineSliceInOutConfig>(), biome));
               } else if (t.GetComponent<NineFourGroupConfig>()) {
                  result.Add(formSpecialGroup(group, tileMatrix, t.GetComponent<NineFourGroupConfig>(), biome));
               } else if (t.GetComponent<MountainGroupConfig>()) {
                  result.Add(formSpecialGroup(group, t.GetComponent<MountainGroupConfig>(), biome));
               } else if (t.GetComponent<NineGroupConfig>()) {
                  result.Add(formSpecialGroup(group, t.GetComponent<NineGroupConfig>(), biome));
               } else if (t.GetComponent<DockGroupConfig>()) {
                  result.Add(FormSpecialGroup(group, t.GetComponent<DockGroupConfig>(), biome));
               } else if (t.GetComponent<WallGroupConfig>()) {
                  result.Add(formSpecialGroup(group, tileMatrix, t.GetComponent<WallGroupConfig>(), biome));
               } else if (t.GetComponent<SeaMountainConfig>()) {
                  result.Add(formSpecialGroup(group, tileMatrix, t.GetComponent<SeaMountainConfig>(), biome));
               }
               continue;

            }

            result.Add(new TileGroup {
               tiles = extractBiome(group.tiles, biome),
               start = group.start,
               type = TileGroupType.Regular
            });
         }
         return result;
      }

      private SeaMountainGroup formSpecialGroup(BiomedTileGroup from, BiomedTileData[,] tileMatrix, SeaMountainConfig config, BiomeType biome) {

         BoundsInt bounds = config.mainBounds;
         bounds.position += Vector3Int.RoundToInt(config.transform.position);

         SeaMountainGroup newGroup = new SeaMountainGroup {
            tiles = extractBiome(from.tiles, biome),
            start = from.start,
            allTiles = new TileBase[bounds.size.x, bounds.size.y]
         };

         for (int i = 0; i < bounds.size.x; i++) {
            for (int j = 0; j < bounds.size.y; j++) {
               Vector2Int index = new Vector2Int(bounds.position.x + i, bounds.position.y + j);
               if (tileMatrix[index.x, index.y] != null) {
                  newGroup.allTiles[i, j] = tileMatrix[index.x, index.y].tile[biome];
               }
            }
         }

         foreach (var data in from.tiles) {
            if (data != null) {
               newGroup.layer = data.layer;
               break;
            }
         }
         return newGroup;
      }

      private MountainGroup formSpecialGroup (BiomedTileGroup from, MountainGroupConfig config, BiomeType biome) {
         var tm = config.biomeTileMaps.First(bm => bm.biome == biome);
         var outerTilemap = tm.outerTilemap;
         var innerTilemap = tm.innerTilemap;

         MountainGroup newGroup = new MountainGroup {
            tiles = extractBiome(from.tiles, biome),
            start = from.start,
            innerTiles = new TileBase[config.innerSize.x, config.innerSize.y],
            outerTiles = new TileBase[config.outerSize.x, config.outerSize.y]
         };

         for (int i = 0; i < config.innerSize.x; i++) {
            for (int j = 0; j < config.innerSize.y; j++) {
               Vector3Int index = new Vector3Int(i, j, 0) - innerTilemap.origin - Vector3Int.RoundToInt(innerTilemap.transform.position);

               newGroup.innerTiles[i, j] = innerTilemap.GetTile(index);
            }
         }

         //Debug.Log(innerTilemap.size);
         for (int i = 0; i < config.outerSize.x; i++) {
            for (int j = 0; j < config.outerSize.y; j++) {
               Vector3Int index = new Vector3Int(i, j, 0) - outerTilemap.origin - Vector3Int.RoundToInt(outerTilemap.transform.position);

               newGroup.outerTiles[i, j] = outerTilemap.GetTile(index);
            }
         }

         foreach (var data in from.tiles) {
            if (data != null) {
               newGroup.layer = data.layer;
               break;
            }
         }
         return newGroup;
      }

      private NineSliceInOutGroup formSpecialGroup (BiomedTileGroup from, BiomedTileData[,] tileMatrix, NineSliceInOutConfig config, BiomeType biome) {
         BoundsInt innerBounds = config.innerBounds;
         innerBounds.position += Vector3Int.RoundToInt(config.transform.position);
         BoundsInt outerBounds = config.outerBounds;
         outerBounds.position += Vector3Int.RoundToInt(config.transform.position);

         NineSliceInOutGroup newGroup = new NineSliceInOutGroup {
            tiles = extractBiome(from.tiles, biome),
            start = from.start,
            innerTiles = new PaletteTilesData.TileData[innerBounds.size.x, innerBounds.size.y],
            outerTiles = new PaletteTilesData.TileData[outerBounds.size.x, outerBounds.size.y],
         };

         for (int i = 0; i < config.innerBounds.size.x; i++) {
            for (int j = 0; j < config.innerBounds.size.y; j++) {
               Vector2Int index = new Vector2Int(innerBounds.position.x + i, innerBounds.position.y + j);
               if (tileMatrix[index.x, index.y] != null) {
                  newGroup.innerTiles[i, j] = new PaletteTilesData.TileData {
                     tile = tileMatrix[index.x, index.y].tile[biome],
                     layer = tileMatrix[index.x, index.y].layer,
                     subLayer = tileMatrix[index.x, index.y].subLayer
                  };
               }
            }
         }

         for (int i = 0; i < config.outerBounds.size.x; i++) {
            for (int j = 0; j < config.outerBounds.size.y; j++) {
               Vector2Int index = new Vector2Int(outerBounds.position.x + i, outerBounds.position.y + j);
               if (tileMatrix[index.x, index.y] != null) {
                  newGroup.outerTiles[i, j] = new PaletteTilesData.TileData {
                     tile = tileMatrix[index.x, index.y].tile[biome],
                     layer = tileMatrix[index.x, index.y].layer,
                     subLayer = tileMatrix[index.x, index.y].subLayer
                  };
               }
            }
         }
         return newGroup;
      }

      private NineGroup formSpecialGroup (BiomedTileGroup from, NineGroupConfig config, BiomeType biome) {
         NineGroup newGroup = new NineGroup {
            tiles = extractBiome(from.tiles, biome),
            start = from.start
         };
         newGroup.layer = newGroup.tiles[1, 1].layer;
         newGroup.subLayer = newGroup.tiles[1, 1].subLayer;
         return newGroup;
      }

      private DockGroup FormSpecialGroup (BiomedTileGroup from, DockGroupConfig config, BiomeType biome) {
         DockGroup newGroup = new DockGroup {
            tiles = extractBiome(from.tiles, biome),
            start = from.start
         };

         newGroup.layer = newGroup.tiles[1, 1].layer;
         newGroup.subLayer = newGroup.tiles[1, 1].subLayer;

         return newGroup;
      }

      private NineFourGroup formSpecialGroup (BiomedTileGroup from, BiomedTileData[,] tileMatrix, NineFourGroupConfig config, BiomeType biome) {
         BoundsInt mainBounds = config.mainBounds;
         mainBounds.position += Vector3Int.RoundToInt(config.transform.position);
         BoundsInt cornerBounds = config.cornerBounds;
         cornerBounds.position += Vector3Int.RoundToInt(config.transform.position);

         NineFourGroup newGroup = new NineFourGroup {
            tiles = extractBiome(from.tiles, biome),
            start = from.start,
            mainTiles = new PaletteTilesData.TileData[mainBounds.size.x, mainBounds.size.y],
            cornerTiles = new PaletteTilesData.TileData[cornerBounds.size.x, cornerBounds.size.y],
            singleLayer = config.singleLayer
         };

         for (int i = 0; i < mainBounds.size.x; i++) {
            for (int j = 0; j < mainBounds.size.y; j++) {
               Vector2Int index = new Vector2Int(mainBounds.position.x + i, mainBounds.position.y + j);
               if (tileMatrix[index.x, index.y] != null) {
                  newGroup.mainTiles[i, j] = new PaletteTilesData.TileData {
                     tile = tileMatrix[index.x, index.y].tile[biome],
                     layer = tileMatrix[index.x, index.y].layer,
                     subLayer = tileMatrix[index.x, index.y].subLayer
                  };
               }
            }
         }

         for (int i = 0; i < cornerBounds.size.x; i++) {
            for (int j = 0; j < cornerBounds.size.y; j++) {
               Vector2Int index = new Vector2Int(cornerBounds.position.x + i, cornerBounds.position.y + j);
               if (tileMatrix[index.x, index.y] != null) {
                  newGroup.cornerTiles[i, j] = new PaletteTilesData.TileData {
                     tile = tileMatrix[index.x, index.y].tile[biome],
                     layer = tileMatrix[index.x, index.y].layer,
                     subLayer = tileMatrix[index.x, index.y].subLayer
                  };
               }
            }
         }

         newGroup.layer = newGroup.mainTiles[1, 1].layer;
         return newGroup;
      }

      private WallGroup formSpecialGroup (BiomedTileGroup from, BiomedTileData[,] tileMatrix, WallGroupConfig config, BiomeType biome) {
         BoundsInt bounds = config.tileBounds;
         bounds.position += Vector3Int.RoundToInt(config.transform.position);

         WallGroup newGroup = new WallGroup {
            tiles = extractBiome(from.tiles, biome),
            start = from.start,
            allTiles = new PaletteTilesData.TileData[bounds.size.x, bounds.size.y]
         };

         for (int i = 0; i < bounds.size.x; i++) {
            for (int j = 0; j < bounds.size.y; j++) {
               Vector2Int index = new Vector2Int(bounds.position.x + i, bounds.position.y + j);
               if (tileMatrix[index.x, index.y] != null) {
                  newGroup.allTiles[i, j] = new PaletteTilesData.TileData {
                     tile = tileMatrix[index.x, index.y].tile[biome],
                     layer = tileMatrix[index.x, index.y].layer,
                     subLayer = tileMatrix[index.x, index.y].subLayer
                  };
               }
            }
         }

         newGroup.layer = newGroup.allTiles[2, 2].layer;
         return newGroup;
      }

      private Transform overlapAnyChild (BiomedTileGroup group, Transform[] objectContainers) {
         foreach (var con in objectContainers) {
            var child = overlapAnyChild(group, con);
            if (child != null)
               return child;
         }

         return null;
      }

      private Transform overlapAnyChild (BiomedTileGroup group, Transform objectContainer) {
         for (int i = 0; i < objectContainer.childCount; i++) {
            Vector2 point = objectContainer.GetChild(i).transform.position;
            if (group.start.x < point.x && group.start.y < point.y &&
                group.start.x + group.tiles.GetLength(0) > point.x &&
                group.start.y + group.tiles.GetLength(1) > point.y) {
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
      private List<BiomedTileGroup> createGroups (BiomedTileData[,] tiles, Tilemap clusterMap) {
         List<BiomedTileGroup> groups = new List<BiomedTileGroup>();

         //The tiles from the tilemap which were already proccessed
         bool[,] proccessed = new bool[tiles.GetLength(0), tiles.GetLength(1)];

         for (int i = 0; i < tiles.GetLength(0); i++) {
            for (int j = 0; j < tiles.GetLength(1); j++) {
               if (!proccessed[i, j]) {
                  //Check if the tilemap has any tiles, if not, mark as proccessed and ignore
                  if (tiles[i, j] == null) {
                     proccessed[i, j] = true;
                     continue;
                  }

                  //Find all the tiles that are in a cluster as defined in the cluster map
                  var clusterTiles = searchForCluster(new Vector3Int(i, j, 0), proccessed, clusterMap);

                  //The left-down most location of the rectangle that binds the group
                  Vector2Int origin = new Vector2Int(clusterTiles.Min(t => t.x), clusterTiles.Min(t => t.y));
                  //The size of the binding rectangle
                  Vector2Int length = new Vector2Int(
                      clusterTiles.Max(t => t.x - origin.x + 1), clusterTiles.Max(t => t.y - origin.y + 1));

                  BiomedTileGroup group = new BiomedTileGroup {
                     tiles = new BiomedTileData[length.x, length.y],
                     start = (Vector3Int) origin
                  };

                  foreach (var pos in clusterTiles) {
                     if (clusterTiles.Count == 9) { }

                     if (tiles[pos.x, pos.y] == null)
                        continue;

                     Vector2Int index = (Vector2Int) pos - origin;
                     group.tiles[index.x, index.y] = tiles[pos.x, pos.y];
                  }
                  groups.Add(group);
               }
            }
         }
         return groups;
      }

      private PaletteTilesData.TileData[,] extractBiome (BiomedTileData[,] tiles, BiomeType targetBiome) {
         PaletteTilesData.TileData[,] result = new PaletteTilesData.TileData[tiles.GetLength(0), tiles.GetLength(1)];

         for (int i = 0; i < result.GetLength(0); i++) {
            for (int j = 0; j < result.GetLength(1); j++) {
               if (tiles[i, j] != null && tiles[i, j].tile[targetBiome] != null) {
                  result[i, j] = new PaletteTilesData.TileData {
                     tile = tiles[i, j].tile[targetBiome],
                     layer = tiles[i, j].layer,
                     subLayer = tiles[i, j].subLayer
                  };
               }
            }
         }

         return result;
      }

      private List<Vector3Int> searchForCluster (Vector3Int start, bool[,] proccessed, Tilemap clusterMap) {
         proccessed[start.x, start.y] = true;
         TileBase clusterKey = clusterMap.GetTile(start);
         if (clusterKey == null) {
            return new List<Vector3Int>() { start };
         }

         List<Vector3Int> result = new List<Vector3Int>();
         Queue<Vector3Int> q = new Queue<Vector3Int>();
         q.Enqueue(start);

         int breakCounter = 0;

         while (q.Count > 0) {
            Vector3Int current = q.Dequeue();

            if (current.x < proccessed.GetLength(0) - 1 && !proccessed[current.x + 1, current.y] &&
                clusterMap.GetTile(current + Vector3Int.right) == clusterKey) {
               q.Enqueue(current + Vector3Int.right);
               proccessed[current.x + 1, current.y] = true;
            }
            if (current.x > 0 && !proccessed[current.x - 1, current.y] &&
                clusterMap.GetTile(current + Vector3Int.left) == clusterKey) {
               q.Enqueue(current + Vector3Int.left);
               proccessed[current.x - 1, current.y] = true;
            }
            if (current.y < proccessed.GetLength(1) - 1 && !proccessed[current.x, current.y + 1] &&
                clusterMap.GetTile(current + Vector3Int.up) == clusterKey) {
               q.Enqueue(current + Vector3Int.up);
               proccessed[current.x, current.y + 1] = true;
            }
            if (current.y > 0 && !proccessed[current.x, current.y - 1] &&
                clusterMap.GetTile(current + Vector3Int.down) == clusterKey) {
               q.Enqueue(current + Vector3Int.down);
               proccessed[current.x, current.y - 1] = true;
            }

            result.Add(current);

            breakCounter++;
            if (breakCounter > 1000) {
               throw new System.Exception("Never ending cycle detected!");
            }
         }

         return result;
      }

      private void OnDrawGizmosSelected () {
         if (!drawSublayers)
            return;

         if (subLayers == null || subLayers.Length != 50 * 100) {
            int[] prev = subLayers ?? new int[0];
            subLayers = new int[50 * 100];
            for (int i = 0; i < prev.Length; i++)
               subLayers[i] = prev[i];
         }

#if UNITY_EDITOR
         for (int i = 0; i < 50; i++) {
            for (int j = 0; j < 100; j++) {
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

      public int definedBiomes
      {
         get { return tiles.Count; }
      }
   }

   public class BiomedTileGroup
   {
      public BiomedTileData[,] tiles { get; set; }
      public Vector3Int start { get; set; }
   }

   public class BiomedTileData
   {
      public BiomedTile tile { get; set; }
      public int layer { get; set; }
      public int subLayer { get; set; }
   }
}