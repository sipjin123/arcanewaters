using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class TileGroup
   {
      public PaletteTilesData.TileData[,] tiles { get; set; }
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

      public static bool contains(TileBase[,] matrix, TileBase tile) {
         for (int i = 0; i < matrix.GetLength(0); i++) {
            for (int j = 0; j < matrix.GetLength(1); j++) {
               if (matrix[i, j] == tile)
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
      SeaMountain,
      NineFour,
      Dock,
      Wall,
      River
   }
}
