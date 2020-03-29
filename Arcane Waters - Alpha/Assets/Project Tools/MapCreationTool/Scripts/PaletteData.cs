using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool.PaletteTilesData
{
   public class PaletteData
   {
      public TileGroup[,] tileGroups { get; set; }
      public List<PrefabGroup> prefabGroups { get; set; }
      public Biome.Type? type { get; set; }

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

      public Dictionary<TileBase, TileCollisionType> formCollisionDictionary () {
         Dictionary<TileBase, TileCollisionType> result = new Dictionary<TileBase, TileCollisionType>(tileGroups.GetLength(0) * tileGroups.GetLength(1));

         for (int i = 0; i < tileGroups.GetLength(0); i++) {
            for (int j = 0; j < tileGroups.GetLength(1); j++) {
               TileData tile = getTile(i, j);
               if (tile != null && !result.ContainsKey(tile.tile))
                  result.Add(tile.tile, tile.collision);
            }
         }

         return result;
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

   public class PrefabGroup : TileGroup
   {
      public GameObject refPref { get; set; }

      public PrefabGroup () {
         type = TileGroupType.Prefab;
      }

      public virtual GameObject getPrefab () {
         return refPref;
      }
   }

   public class TreePrefabGroup : PrefabGroup
   {
      public GameObject burrowedPref { get; set; }

      public TreePrefabGroup () {
         type = TileGroupType.TreePrefab;
      }

      public override GameObject getPrefab () {
         return Tools.burrowedTrees ? burrowedPref : refPref;
      }
   }

   public class TileData
   {
      public TileBase tile { get; set; }
      public string layer { get; set; }
      public int subLayer { get; set; }
      public int cluster { get; set; }
      public TileCollisionType collision { get; set; }

      public TileData (BiomedPaletteData.BiomedTileData biomedData, Biome.Type biome) {
         tile = biomedData.tile[biome];
         layer = biomedData.layer;
         subLayer = biomedData.subLayer;
         cluster = biomedData.cluster;
         collision = biomedData.collisionType;
      }

      public TileData () {

      }
   }
}