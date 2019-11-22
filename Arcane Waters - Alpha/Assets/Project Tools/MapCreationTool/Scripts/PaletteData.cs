using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool.PaletteTilesData
{
   public class PaletteData
   {
      public const int PathLayer = 2;
      public const int WaterLayer = 9;
      public const int MountainLayer = 6;
      public TileGroup[,] tileGroups { get; set; }
      public List<PrefabGroup> prefabGroups { get; set; }
      public BiomeType? type { get; set; }

      public TileData getTile (int x, int y) {
         TileGroup group = getGroup(x, y);
         if (group == null)
            return null;
         return group.tiles[x - group.start.x, y - group.start.y];
      }

      public TileGroup getGroup (int x, int y) {
         if (x < 0 || x >= tileGroups.GetLength(0) || y < 0 || y >= tileGroups.GetLength(1))
            return null;
         return tileGroups[x, y];
      }

      public Vector2Int indexOf (TileBase tile) {
         for (int i = 0; i < tileGroups.GetLength(0); i++) {
            for (int j = 0; j < tileGroups.GetLength(1); j++) {
               TileData tileData = getTile(i, j);
               if (tileData != null && tileData.tile == tile)
                  return new Vector2Int(i, j);
            }
         }
         return new Vector2Int(-1, -1);
      }

      public IEnumerable<TileGroup> getTileGroupsEnumerator () {
         HashSet<TileGroup> proccessed = new HashSet<TileGroup>();
         for (int i = 0; i < tileGroups.GetLength(0); i++) {
            for (int j = 0; j < tileGroups.GetLength(1); j++) {
               if (tileGroups[i, j] != null && !proccessed.Contains(tileGroups[i, j])) {
                  proccessed.Add(tileGroups[i, j]);
                  yield return tileGroups[i, j];
               }
            }
         }
      }

      public void merge (PaletteData p) {
         TileGroup[,] newG = new TileGroup[
             Mathf.Max(tileGroups.GetLength(0), p.tileGroups.GetLength(0)),
             Mathf.Max(tileGroups.GetLength(1), p.tileGroups.GetLength(1))
         ];

         for (int i = 0; i < newG.GetLength(0); i++) {
            for (int j = 0; j < newG.GetLength(1); j++) {
               TileGroup curGroup = i >= tileGroups.GetLength(0) || j >= tileGroups.GetLength(1) ? null : tileGroups[i, j];
               TileGroup otherGroup = i >= p.tileGroups.GetLength(0) || j >= p.tileGroups.GetLength(1) ? null : p.tileGroups[i, j];

               newG[i, j] = otherGroup ?? curGroup;
            }
         }

         List<PrefabGroup> newPrefs = new List<PrefabGroup>();
         HashSet<TileGroup> used = new HashSet<TileGroup>();

         for (int i = 0; i < newG.GetLength(0); i++) {
            for (int j = 0; j < newG.GetLength(1); j++) {
               if (newG[i, j] != null && newG[i, j] is PrefabGroup && !used.Contains(newG[i, j])) {
                  used.Add(newG[i, j]);
                  newPrefs.Add(newG[i, j] as PrefabGroup);
               }
            }
         }

         tileGroups = newG;
         prefabGroups = newPrefs;
      }
   }

   public class MountainGroup : TileGroup
   {
      public TileBase[,] innerTiles { get; set; }
      public TileBase[,] outerTiles { get; set; }
      public int layer { get; set; }

      public override Vector2Int brushSize => new Vector2Int(5, 6);
      public MountainGroup () {
         type = TileGroupType.Mountain;
      }
   }

   public class NineSliceInOutGroup : TileGroup
   {
      public TileData[,] innerTiles { get; set; }
      public TileData[,] outerTiles { get; set; }

      public override Vector2Int brushSize => new Vector2Int(2, 2);
      public NineSliceInOutGroup () {
         type = TileGroupType.NineSliceInOut;
      }
   }

   public class PrefabGroup : TileGroup
   {
      public GameObject refPref { get; set; }

      public PrefabGroup () {
         type = TileGroupType.Prefab;
      }
   }

   public class TreePrefabGroup : PrefabGroup
   {
      public GameObject burrowedPref { get; set; }

      public TreePrefabGroup () {
         type = TileGroupType.TreePrefab;
      }
   }


   public class NineFourGroup : TileGroup
   {
      public TileData[,] mainTiles { get; set; }
      public TileData[,] cornerTiles { get; set; }
      public int layer { get; set; }
      public bool singleLayer { get; set; }
      public bool invertedCorners { get; set; }

      public override Vector2Int brushSize => new Vector2Int(2, 2);

      public NineFourGroup () {
         type = TileGroupType.NineFour;
      }

      public override bool contains (TileBase tile) {
         for (int i = 0; i < mainTiles.GetLength(0); i++)
            for (int j = 0; j < mainTiles.GetLength(1); j++)
               if (mainTiles[i, j] != null && mainTiles[i, j].tile == tile)
                  return true;

         for (int i = 0; i < cornerTiles.GetLength(0); i++)
            for (int j = 0; j < cornerTiles.GetLength(1); j++)
               if (cornerTiles[i, j] != null && cornerTiles[i, j].tile == tile)
                  return true;

         return false;
      }
   }

   public class WallGroup : TileGroup
   {
      public TileData[,] allTiles { get; set; }
      public int layer { get; set; }

      public override Vector2Int brushSize => new Vector2Int(1, 2);

      public WallGroup () {
         type = TileGroupType.Wall;
      }

      public override bool contains (TileBase tile) {
         for (int i = 0; i < allTiles.GetLength(0); i++)
            for (int j = 0; j < allTiles.GetLength(1); j++)
               if (allTiles[i, j] != null && allTiles[i, j].tile == tile)
                  return true;

         return false;
      }
   }

   public class NineGroup : TileGroup
   {
      public int layer { get; set; }
      public int subLayer { get; set; }

      public override Vector2Int brushSize => new Vector2Int(2, 2);

      public NineGroup () {
         type = TileGroupType.Nine;
      }
   }

   public class DockGroup : TileGroup
   {
      public int layer { get; set; }
      public int subLayer { get; set; }

      public DockGroup () {
         type = TileGroupType.Dock;
      }

      public override Vector2Int brushSize => new Vector2Int(2, 2);
   }

   public class TileGroup
   {
      public TileData[,] tiles { get; set; }
      public Vector3Int start { get; set; }
      public TileGroupType type { get; set; }

      public int maxTileCount
      {
         get { return tiles.GetLength(0) * tiles.GetLength(1); }
      }

      public Vector2Int size
      {
         get { return new Vector2Int(tiles.GetLength(0), tiles.GetLength(1)); }
      }

      public virtual Vector2Int brushSize
      {
         get { return size; }
      }

      public Vector2 centerPoint
      {
         get { return new Vector2(start.x + size.x * 0.5f, start.y + size.y * 0.5f); }
      }

      public bool allInLayer (int layer) {
         for (int i = 0; i < tiles.GetLength(0); i++) {
            for (int j = 0; j < tiles.GetLength(1); j++) {
               if (tiles[i, j] != null && tiles[i, j].layer != layer)
                  return false;
            }
         }
         return true;
      }

      public virtual bool contains (TileBase tile) {
         for (int i = 0; i < tiles.GetLength(0); i++) {
            for (int j = 0; j < tiles.GetLength(1); j++) {
               if (tiles[i, j] != null && tiles[i, j].tile == tile)
                  return true;
            }
         }
         return false;
      }
   }

   public enum TileGroupType
   {
      Regular,
      Prefab,
      NineSliceInOut,
      Nine,
      TreePrefab,
      Mountain,
      NineFour,
      Dock,
      Wall
   }

   public class TileData
   {
      public TileBase tile { get; set; }
      public int layer { get; set; }
      public int subLayer { get; set; }
   }
}
