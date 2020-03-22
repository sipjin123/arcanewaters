using System;
using System.Collections.Generic;
using System.Linq;
using MapCreationTool.PaletteTilesData;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class BiomedPaletteData : MonoBehaviour
   {
      public BiomePaletteResources biomePaletteResources = new BiomePaletteResources();

      public PaletteResources sharedResources = new PaletteResources();

      [HideInInspector]
      public TileSetupContainer tileSetupContainer;

      // The dictionary all the different biome palette datas are stored in
      private Dictionary<Biome.Type, PaletteData> datas;

      public PaletteData this[Biome.Type type]
      {
         get { return datas[type]; }
      }


      /// <summary>
      /// Collects and arranges all the asigned information so that the data becomes usable
      /// </summary>
      public void collectInformation () {
         datas = new Dictionary<Biome.Type, PaletteData>();

         // Container for all the special placement tool configs
         Transform specialCon = transform.Find("special");

         // Collect all tile data from the tilemaps and the config
         BiomedTileData[,] tiles = gatherTiles(biomePaletteResources, sharedResources, tileSetupContainer);

         // Form tile groups
         List<BiomedTileGroup> biomedGroups = createGroups(tiles);

         foreach (Biome.Type biome in Enum.GetValues(typeof(Biome.Type))) {
            if (biome == Biome.Type.None) {
               continue;
            }

            // Form special groups
            Transform[] prefCons = new Transform[] { biomePaletteResources.prefabsCon, sharedResources.prefabsCon };
            List<TileGroup> groups = formSpecialGroups(biomedGroups, tiles, specialCon, prefCons, biome);

            // All prefabs will be of the single defined biome, translate it
            foreach (TileGroup group in groups) {
               if (group is PrefabGroup) {
                  PrefabGroup prefGroup = group as PrefabGroup;
                  prefGroup.refPref = AssetSerializationMaps.getPrefab(
                     AssetSerializationMaps.getIndex(prefGroup.refPref, biomePaletteResources.biome),
                     biome,
                     true);
               }

               if (group is TreePrefabGroup) {
                  TreePrefabGroup treeGroup = group as TreePrefabGroup;
                  treeGroup.burrowedPref = AssetSerializationMaps.getPrefab(
                     AssetSerializationMaps.getIndex(treeGroup.burrowedPref, biomePaletteResources.biome),
                     biome,
                     true);
               }
            }

            // Create a palette
            PaletteData palette = new PaletteData {
               tileGroups = new TileGroup[tiles.GetLength(0), tiles.GetLength(1)],
               prefabGroups = groups.Select(g => g as PrefabGroup).Where(g => g != null).ToList(),
               type = biome
            };

            // Set tiles for the palette
            foreach (TileGroup group in groups) {
               for (int i = 0; i < group.tiles.GetLength(0); i++) {
                  for (int j = 0; j < group.tiles.GetLength(1); j++) {
                     palette.tileGroups[group.start.x + i, group.start.y + j] = group;
                  }
               }
            }

            datas.Add(biome, palette);
         }
      }

      private BiomedTileData[,] gatherTiles (BiomePaletteResources biomedResources, PaletteResources sharedPalette, TileSetupContainer setup) {
         BiomedTileData[,] result = new BiomedTileData[setup.size.x, setup.size.y];

         for (int i = 0; i < result.GetLength(0); i++) {
            for (int j = 0; j < result.GetLength(1); j++) {
               // Extract all the data from the config
               result[i, j] = new BiomedTileData {
                  tile = new BiomedTile(),
                  layer = setup[i, j].layer,
                  subLayer = setup[i, j].sublayer,
                  cluster = setup[i, j].cluster,
                  collisionType = setup[i, j].collisionType,
               };

               // Add all the tiles
               TileBase tile = biomedResources.tilesTilemap.GetTile(new Vector3Int(i, j, 0));
               if (tile != null) {
                  // Add the tile from the defined biome
                  result[i, j].tile[biomedResources.biome] = tile;

                  // Add in tiles from other biomes
                  foreach (Biome.Type biome in Enum.GetValues(typeof(Biome.Type))) {
                     if (biome == biomedResources.biome || biome == Biome.Type.None) {
                        continue;
                     }

                     result[i, j].tile[biome] = AssetSerializationMaps.getTile(
                        AssetSerializationMaps.getIndex(tile, biomedResources.biome),
                        biome);
                  }
               }

               // Set tile from the shared tilemap if it exists
               TileBase sTile = sharedPalette.tilesTilemap.GetTile(new Vector3Int(i, j, 0));
               if (sTile != null) {
                  foreach (Biome.Type biome in Enum.GetValues(typeof(Biome.Type))) {
                     result[i, j].tile[biome] = sTile;
                  }
               }

               // Check if resulting tile is empty, remove if so
               if (result[i, j].tile.definedBiomes == 0 && result[i, j].cluster == 0)
                  result[i, j] = null;
            }
         }

         return result;
      }

      private List<BiomedTileGroup> createGroups (BiomedTileData[,] tiles) {
         List<BiomedTileGroup> groups = new List<BiomedTileGroup>();

         // The tiles from the tilemap which were already proccessed
         bool[,] proccessed = new bool[tiles.GetLength(0), tiles.GetLength(1)];

         for (int i = 0; i < tiles.GetLength(0); i++) {
            for (int j = 0; j < tiles.GetLength(1); j++) {
               if (!proccessed[i, j]) {
                  // Check if matrix has an entry, otherwise skip
                  if (tiles[i, j] == null) {
                     proccessed[i, j] = true;
                     continue;
                  }

                  // Find all the tiles that are in a cluster as defined in the cluster map
                  var clusterTiles = searchForCluster(new Vector3Int(i, j, 0), proccessed, tiles);

                  // The left-down most location of the rectangle that binds the group
                  Vector2Int origin = new Vector2Int(clusterTiles.Min(t => t.x), clusterTiles.Min(t => t.y));
                  // The size of the binding rectangle
                  Vector2Int length = new Vector2Int(
                      clusterTiles.Max(t => t.x - origin.x + 1), clusterTiles.Max(t => t.y - origin.y + 1));

                  BiomedTileGroup group = new BiomedTileGroup {
                     tiles = new BiomedTileData[length.x, length.y],
                     start = (Vector3Int) origin
                  };

                  foreach (var pos in clusterTiles) {
                     Vector2Int index = (Vector2Int) pos - origin;
                     group.tiles[index.x, index.y] = tiles[pos.x, pos.y];
                  }

                  groups.Add(group);
               }
            }
         }
         return groups;
      }

      private List<TileGroup> formSpecialGroups (List<BiomedTileGroup> groups, BiomedTileData[,] tileMatrix, Transform specialCon, Transform[] prefabCons, Biome.Type biome) {
         List<TileGroup> result = new List<TileGroup>();
         foreach (var group in groups) {
            //------------------------------------------------
            // Prefabs
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
            // Other special
            t = overlapAnyChild(group, specialCon);
            if (t != null) {
               if (t.GetComponent<NineSliceInOutConfig>()) {
                  result.Add(formSpecialGroup(group, tileMatrix, t.GetComponent<NineSliceInOutConfig>(), biome));
               } else if (t.GetComponent<NineFourGroupConfig>()) {
                  result.Add(formSpecialGroup(group, tileMatrix, t.GetComponent<NineFourGroupConfig>(), biome));
               } else if (t.GetComponent<MountainGroupConfig>()) {
                  result.Add(formSpecialGroup(group, tileMatrix, t.GetComponent<MountainGroupConfig>(), biome));
               } else if (t.GetComponent<NineGroupConfig>()) {
                  result.Add(formSpecialGroup(group, t.GetComponent<NineGroupConfig>(), biome));
               } else if (t.GetComponent<DockGroupConfig>()) {
                  result.Add(FormSpecialGroup(group, t.GetComponent<DockGroupConfig>(), biome));
               } else if (t.GetComponent<WallGroupConfig>()) {
                  result.Add(formSpecialGroup(group, tileMatrix, t.GetComponent<WallGroupConfig>(), biome));
               } else if (t.GetComponent<SeaMountainConfig>()) {
                  result.Add(formSpecialGroup(group, tileMatrix, t.GetComponent<SeaMountainConfig>(), biome));
               } else if (t.GetComponent<RiverGroupConfig>()) {
                  result.Add(formSpecialGroup(group, tileMatrix, t.GetComponent<RiverGroupConfig>(), biome));
               } else if (t.GetComponent<InteriorWallConfig>()) {
                  result.Add(formSpecialGroup(group, tileMatrix, t.GetComponent<InteriorWallConfig>(), biome));
               } else if (t.GetComponent<RectGroupConfig>()) {
                  result.Add(formSpecialGroup(group, tileMatrix, t.GetComponent<RectGroupConfig>(), biome));
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

      private RectTileGroup formSpecialGroup (BiomedTileGroup from, BiomedTileData[,] tileMatrix, RectGroupConfig config, Biome.Type biome) {
         RectTileGroup newGroup = new RectTileGroup {
            tiles = extractBiome(from.tiles, biome),
            start = from.start
         };
         newGroup.layer = newGroup.tiles[1, 1].layer;
         newGroup.subLayer = newGroup.tiles[1, 1].subLayer;
         return newGroup;
      }

      private InteriorWallGroup formSpecialGroup (BiomedTileGroup from, BiomedTileData[,] tileMatrix, InteriorWallConfig config, Biome.Type biome) {
         Tilemap tilemap = config.biomeTilemap.tilemap;

         InteriorWallGroup newGroup = new InteriorWallGroup {
            tiles = extractBiome(from.tiles, biome),
            start = from.start,
            allTiles = new TileBase[config.size.x, config.size.y],
            layer = from.tiles[0, 0].layer,
            subLayer = from.tiles[0, 0].subLayer
         };

         for (int i = 0; i < config.size.x; i++) {
            for (int j = 0; j < config.size.y; j++) {
               Vector3Int index = new Vector3Int(i, j, 0) - tilemap.origin - Vector3Int.RoundToInt(tilemap.transform.position);
               TileBase tile = tilemap.GetTile(index);
               if (tile != null) {
                  newGroup.allTiles[i, j] = AssetSerializationMaps.getTile(
                        AssetSerializationMaps.getIndex(tile, config.biomeTilemap.biome),
                        biome);
               }
            }
         }

         return newGroup;
      }

      private SeaMountainGroup formSpecialGroup (BiomedTileGroup from, BiomedTileData[,] tileMatrix, SeaMountainConfig config, Biome.Type biome) {

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

      private RiverGroup formSpecialGroup (BiomedTileGroup from, BiomedTileData[,] tileMatrix, RiverGroupConfig config, Biome.Type biome) {

         BoundsInt bounds = config.mainBounds;
         bounds.position += Vector3Int.RoundToInt(config.transform.position);

         RiverGroup newGroup = new RiverGroup {
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

         foreach (var data in newGroup.tiles) {
            if (data != null) {
               newGroup.layer = data.layer;
               newGroup.subLayer = data.subLayer;
               break;
            }
         }

         return newGroup;
      }

      private MountainGroup formSpecialGroup (BiomedTileGroup from, BiomedTileData[,] tileMatrix, MountainGroupConfig config, Biome.Type biome) {
         var outerTilemap = config.biomeTileMaps.outerTilemap;
         var innerTilemap = config.biomeTileMaps.innerTilemap;

         BoundsInt bounds = config.allTileBounds;
         bounds.position += Vector3Int.RoundToInt(config.transform.position);

         MountainGroup newGroup = new MountainGroup {
            tiles = extractBiome(from.tiles, biome),
            start = from.start,
            innerTiles = new TileBase[config.innerSize.x, config.innerSize.y],
            outerTiles = new TileBase[config.outerSize.x, config.outerSize.y],
            containsSet = new HashSet<TileBase>()
         };

         newGroup.containsSet.Add(AssetSerializationMaps.transparentTileBase);

         for (int i = 0; i < bounds.size.x; i++) {
            for (int j = 0; j < bounds.size.y; j++) {
               Vector2Int index = new Vector2Int(bounds.position.x + i, bounds.position.y + j);
               if (tileMatrix[index.x, index.y] != null) {
                  TileBase tile = tileMatrix[index.x, index.y].tile[biome];
                  if (!newGroup.containsSet.Contains(tile)) {
                     newGroup.containsSet.Add(tile);
                  }
               }
            }
         }

         for (int i = 0; i < config.innerSize.x; i++) {
            for (int j = 0; j < config.innerSize.y; j++) {
               Vector3Int index = new Vector3Int(i, j, 0) - innerTilemap.origin - Vector3Int.RoundToInt(innerTilemap.transform.position);
               TileBase configTile = innerTilemap.GetTile(index);
               if (configTile != null) {
                  newGroup.innerTiles[i, j] = AssetSerializationMaps.getTile(
                     AssetSerializationMaps.getIndex(configTile, config.biomeTileMaps.biome),
                     biome);
               }
            }
         }

         for (int i = 0; i < config.outerSize.x; i++) {
            for (int j = 0; j < config.outerSize.y; j++) {
               Vector3Int index = new Vector3Int(i, j, 0) - outerTilemap.origin - Vector3Int.RoundToInt(outerTilemap.transform.position);
               TileBase configTile = outerTilemap.GetTile(index);
               if (configTile != null) {
                  newGroup.outerTiles[i, j] = AssetSerializationMaps.getTile(
                     AssetSerializationMaps.getIndex(configTile, config.biomeTileMaps.biome),
                     biome);
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

      private NineSliceInOutGroup formSpecialGroup (BiomedTileGroup from, BiomedTileData[,] tileMatrix, NineSliceInOutConfig config, Biome.Type biome) {
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
                  newGroup.innerTiles[i, j] = new PaletteTilesData.TileData(tileMatrix[index.x, index.y], biome);
               }
            }
         }

         for (int i = 0; i < config.outerBounds.size.x; i++) {
            for (int j = 0; j < config.outerBounds.size.y; j++) {
               Vector2Int index = new Vector2Int(outerBounds.position.x + i, outerBounds.position.y + j);
               if (tileMatrix[index.x, index.y] != null) {
                  newGroup.outerTiles[i, j] = new PaletteTilesData.TileData(tileMatrix[index.x, index.y], biome);
               }
            }
         }
         return newGroup;
      }

      private NineGroup formSpecialGroup (BiomedTileGroup from, NineGroupConfig config, Biome.Type biome) {
         NineGroup newGroup = new NineGroup {
            tiles = extractBiome(from.tiles, biome),
            start = from.start
         };
         newGroup.layer = newGroup.tiles[1, 1].layer;
         newGroup.subLayer = newGroup.tiles[1, 1].subLayer;
         return newGroup;
      }

      private DockGroup FormSpecialGroup (BiomedTileGroup from, DockGroupConfig config, Biome.Type biome) {
         DockGroup newGroup = new DockGroup {
            tiles = extractBiome(from.tiles, biome),
            start = from.start
         };

         newGroup.layer = newGroup.tiles[1, 1].layer;
         newGroup.subLayer = newGroup.tiles[1, 1].subLayer;

         return newGroup;
      }

      private NineFourGroup formSpecialGroup (BiomedTileGroup from, BiomedTileData[,] tileMatrix, NineFourGroupConfig config, Biome.Type biome) {
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
                  newGroup.mainTiles[i, j] = new PaletteTilesData.TileData(tileMatrix[index.x, index.y], biome);
               }
            }
         }

         for (int i = 0; i < cornerBounds.size.x; i++) {
            for (int j = 0; j < cornerBounds.size.y; j++) {
               Vector2Int index = new Vector2Int(cornerBounds.position.x + i, cornerBounds.position.y + j);
               if (tileMatrix[index.x, index.y] != null) {
                  newGroup.cornerTiles[i, j] = new PaletteTilesData.TileData(tileMatrix[index.x, index.y], biome);
               }
            }
         }

         newGroup.layer = newGroup.mainTiles[1, 1].layer;
         return newGroup;
      }

      private WallGroup formSpecialGroup (BiomedTileGroup from, BiomedTileData[,] tileMatrix, WallGroupConfig config, Biome.Type biome) {
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
                  newGroup.allTiles[i, j] = new PaletteTilesData.TileData(tileMatrix[index.x, index.y], biome);
               }
            }
         }

         newGroup.layer = newGroup.allTiles[2, 2].layer;
         return newGroup;
      }

      /// <summary>
      /// Using breadth-first search, finds all tiles in a cluster
      /// </summary>
      /// <param name="start"></param>
      /// <param name="proccessed"></param>
      /// <param name="tiles"></param>
      /// <returns></returns>
      private List<Vector3Int> searchForCluster (Vector3Int start, bool[,] proccessed, BiomedTileData[,] tiles) {
         Vector2Int mSize = new Vector2Int(proccessed.GetLength(0), proccessed.GetLength(1));
         proccessed[start.x, start.y] = true;
         int clusterKey = tiles[start.x, start.y].cluster;
         if (clusterKey == 0) {
            return new List<Vector3Int>() { start };
         }

         List<Vector3Int> result = new List<Vector3Int>();
         Queue<Vector3Int> q = new Queue<Vector3Int>();
         q.Enqueue(start);

         for (int i = 0; i < 1000 && q.Count > 0; i++) {
            Vector3Int cur = q.Dequeue();

            if (cur.x < mSize.x - 1 && !proccessed[cur.x + 1, cur.y] && tiles[cur.x + 1, cur.y]?.cluster == clusterKey) {
               q.Enqueue(cur + Vector3Int.right);
               proccessed[cur.x + 1, cur.y] = true;
            }
            if (cur.x > 0 && !proccessed[cur.x - 1, cur.y] && tiles[cur.x - 1, cur.y]?.cluster == clusterKey) {
               q.Enqueue(cur + Vector3Int.left);
               proccessed[cur.x - 1, cur.y] = true;
            }
            if (cur.y < mSize.y - 1 && !proccessed[cur.x, cur.y + 1] && tiles[cur.x, cur.y + 1]?.cluster == clusterKey) {
               q.Enqueue(cur + Vector3Int.up);
               proccessed[cur.x, cur.y + 1] = true;
            }
            if (cur.y > 0 && !proccessed[cur.x, cur.y - 1] && tiles[cur.x, cur.y - 1]?.cluster == clusterKey) {
               q.Enqueue(cur + Vector3Int.down);
               proccessed[cur.x, cur.y - 1] = true;
            }

            result.Add(cur);

            if (i == 999) {
               throw new System.Exception("Never ending cycle detected!");
            }
         }

         return result;
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

      private PaletteTilesData.TileData[,] extractBiome (BiomedTileData[,] tiles, Biome.Type targetBiome) {
         PaletteTilesData.TileData[,] result = new PaletteTilesData.TileData[tiles.GetLength(0), tiles.GetLength(1)];

         for (int i = 0; i < result.GetLength(0); i++) {
            for (int j = 0; j < result.GetLength(1); j++) {
               if (tiles[i, j] != null && tiles[i, j].tile[targetBiome] != null) {
                  result[i, j] = new PaletteTilesData.TileData(tiles[i, j], targetBiome);
               }
            }
         }

         return result;
      }

      [System.Serializable]
      public class PaletteResources
      {
         public Tilemap tilesTilemap = null;
         public Transform prefabsCon = null;
      }

      [System.Serializable]
      public class BiomePaletteResources : PaletteResources
      {
         public Biome.Type biome = Biome.Type.Forest;
      }

      [System.Serializable]
      public class TileSetupContainer
      {
         public TileSetup[] tileSetups = new TileSetup[0];
         public int matrixHeight = 0;

         public TileSetup this[int x, int y]
         {
            get
            {
               if (x < 0 || y < 0)
                  return null;

               encapsulate(x, y);

               if (tileSetups[x * matrixHeight + y] == null)
                  tileSetups[x * matrixHeight + y] = new TileSetup();

               return tileSetups[x * matrixHeight + y];
            }
         }

         public bool contains (float x, float y) {
            int i = Mathf.FloorToInt(x);
            int j = Mathf.FloorToInt(y);

            return x >= 0 && y >= 0 && i < size.x && j < size.y;
         }

         public Vector2Int size
         {
            get { return new Vector2Int(matrixHeight == 0 ? 0 : tileSetups.Length / matrixHeight, matrixHeight); }
         }

         public void encapsulate (int x, int y) {
            if (y < matrixHeight && x < (matrixHeight == 0 ? 0 : tileSetups.Length / matrixHeight))
               return;

            int oldWidth = matrixHeight == 0 ? 0 : tileSetups.Length / matrixHeight;
            resize(Mathf.Max(x + 1, oldWidth), Mathf.Max(y + 1, matrixHeight));
         }

         public void resize (int width, int height) {
            TileSetup[] old = tileSetups.Clone() as TileSetup[];
            int oldHeight = matrixHeight;
            int oldWidth = oldHeight == 0 ? 0 : old.Length / oldHeight;

            matrixHeight = height;
            int matrixWidth = width;
            tileSetups = new TileSetup[matrixHeight * matrixWidth];

            for (int i = 0; i < tileSetups.Length; i++) {
               // Extract matrix x and y coors
               Vector2Int coors = new Vector2Int(i / (matrixHeight), i % matrixHeight);

               if (coors.x < oldWidth && coors.y < oldHeight) {
                  // Apply old element
                  tileSetups[i] = old[coors.x * oldHeight + coors.y];
               }
            }
         }
      }

      [System.Serializable]
      public class TileSetup
      {
         public string layer;
         public int sublayer;
         public int cluster;
         public TileCollisionType collisionType;
      }
      public class BiomedTile
      {
         private Dictionary<Biome.Type, TileBase> tiles = new Dictionary<Biome.Type, TileBase>();

         public TileBase this[Biome.Type type]
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
         public string layer { get; set; }
         public int subLayer { get; set; }

         public int cluster { get; set; }
         public TileCollisionType collisionType { get; set; }
      }

   }
}

