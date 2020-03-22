using MapCreationTool.PaletteTilesData;
using UnityEngine;
using UnityEngine.Tilemaps;
using TileData = MapCreationTool.PaletteTilesData.TileData;

namespace MapCreationTool
{
   public class WallGroup : TileGroup
   {
      public TileData[,] allTiles { get; set; }
      public string layer { get; set; }

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

      public TileBase pickTile (bool[,] adj, int x, int y) {
         Vector2Int tileIndex = new Vector2Int(-1, -1);

         bool left = adj[x - 1, y] && adj[x - 1, y - 1];
         bool right = adj[x + 1, y] && adj[x + 1, y - 1];

         //Bottom stone row
         if (!adj[x, y - 1]) {
            if (adj[x - 1, y] && adj[x + 1, y])
               tileIndex.Set(2, 1);
            else if (!adj[x - 1, y] && !adj[x + 1, y])
               tileIndex.Set(1, 0);
            else if (!adj[x - 1, y] && adj[x + 1, y])
               tileIndex.Set(0, 1);
            else if (adj[x - 1, y] && !adj[x + 1, y])
               tileIndex.Set(3, 1);
         }

         //Second row - above the stone row, when there is nothing on top
         else if (!adj[x, y - 2] && adj[x, y - 1] && !adj[x, y + 1]) {
            if (left && right)
               tileIndex.Set(2, 2);
            else if (!left && !right)
               tileIndex.Set(1, 4);
            else if (!left && right)
               tileIndex.Set(0, 2);
            else if (left && !right)
               tileIndex.Set(3, 2);
         }

         //Second row - above the stone row, when there are more tiles on top
         else if (!adj[x, y - 2] && adj[x, y - 1] && adj[x, y + 1]) {
            if (left && right)
               tileIndex.Set(3, 0);
            else if (!left && !right)
               tileIndex.Set(1, 3);
            else if (!left && right)
               tileIndex.Set(2, 3);
            else if (left && !right)
               tileIndex.Set(3, 3);
         }

         //Third row - filler, placed when expanding
         else if (adj[x, y - 2] && adj[x, y - 1] && adj[x, y + 1]) {
            if (left && right)
               tileIndex.Set(1, 2);
            else if (!left && !right)
               tileIndex.Set(1, 3);
            else if (!left && right)
               tileIndex.Set(0, 4);
            else if (left && !right)
               tileIndex.Set(0, 3);
         }

         //Top row
         else if (!adj[x, y + 1]) {
            if (left && right)
               tileIndex.Set(2, 0);
            else if (!left && !right)
               tileIndex.Set(1, 4);
            else if (!left && right)
               tileIndex.Set(2, 4);
            else if (left && !right)
               tileIndex.Set(3, 4);
         }
         if (tileIndex.x == -1)
            throw new System.Exception("Target index could not be found when placing wall tiles");
         else
            return allTiles[tileIndex.x, tileIndex.y].tile;
      }
   }
}