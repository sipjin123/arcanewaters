using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class InteriorWallGroup : TileGroup
   {
      public TileBase[,] allTiles { get; set; }
      public string layer { get; set; }
      public int subLayer { get; set; }

      public override Vector2Int brushSize => new Vector2Int(3, 3);
      public InteriorWallGroup () {
         type = TileGroupType.InteriorWall;
      }

      public override bool contains (TileBase tile) {
         return TileGroup.contains(allTiles, tile);
      }

      public TileBase pickTile (bool[,] adj, SidesInt sur, int x, int y) {
         Vector2Int tileIndex = new Vector2Int(-1, -1);

         // Bottom row
         if (sur.bot == 0 && sur.left == 0 && sur.right > 1 && !adj[x - 1, y + 1])
            tileIndex.Set(0, 0);
         else if (sur.bot == 0 && sur.left > 1 && sur.right == 0 && !adj[x + 1, y + 1])
            tileIndex.Set(4, 0);
         else if (sur.bot == 0 && sur.left > 0 && sur.right > 0) {
            bool left = true;
            bool right = true;

            if (sur.left == 1 && !adj[x - 2, y + 1])
               left = false;
            if (sur.right == 1 && !adj[x + 2, y + 1])
               right = false;

            if (left && right || !left && !right)
               tileIndex.Set(2, 0);
            else if (right)
               tileIndex.Set(1, 0);
            else
               tileIndex.Set(3, 0);
         }

         // Second from bottom row
         else if (sur.bot == 1 && sur.left == 0 && sur.right > 1)
            tileIndex.Set(0, 1);
         else if (sur.bot == 1 && sur.left == 1 && sur.right > 1 && adj[x - 1, y - 1])
            tileIndex.Set(1, 1);
         else if (sur.bot == 1 && adj[x - 1, y - 1] && adj[x + 1, y - 1] &&
            ((sur.left > 1 && sur.right > 1) || (sur.left == 1 && sur.right == 1)))
            tileIndex.Set(2, 1);
         else if (sur.bot == 1 && sur.left > 1 && sur.right == 1 && adj[x + 1, y - 1])
            tileIndex.Set(3, 1);
         else if (sur.bot == 1 && sur.left > 1 && sur.right == 0)
            tileIndex.Set(4, 1);

         // Third from bottom row
         else if (sur.bot == 2 && sur.left == 0 && sur.right > 1)
            tileIndex.Set(0, 2);
         else if (sur.bot == 2 && sur.left == 1 && sur.right > 1 && adj[x - 1, y - 2])
            tileIndex.Set(1, 2);
         else if (sur.bot == 2 && adj[x - 1, y - 2] && adj[x + 1, y - 2] &&
            ((sur.left > 1 && sur.right > 1) || (sur.left == 1 && sur.right == 1)))
            tileIndex.Set(2, 2);
         else if (sur.bot == 2 && sur.left > 1 && sur.right == 1 && adj[x + 1, y - 2])
            tileIndex.Set(3, 2);
         else if (sur.bot == 2 && sur.left > 1 && sur.right == 0)
            tileIndex.Set(4, 2);

         // Diagonals

         else if (sur.left == 0 && sur.bot == 0 && sur.right > 1 && adj[x + 1, y + 1])
            tileIndex.Set(0, 3);
         else if (sur.left > 0 && sur.bot == 1 && sur.right > 1 && !adj[x - 1, y - 1])
            tileIndex.Set(1, 3);
         else if (sur.left > 1 && sur.bot == 1 && sur.right > 0 && !adj[x + 1, y - 1])
            tileIndex.Set(2, 3);
         else if (sur.left > 1 && sur.bot == 0 && sur.right == 0 && adj[x - 1, y + 1])
            tileIndex.Set(3, 3);

         else if (sur.left > 0 && sur.bot == 2 && sur.right > 0 && !adj[x - 1, y - 2])
            tileIndex.Set(1, 4);
         else if (sur.left > 0 && sur.bot == 3 && !adj[x - 1, y - 3])
            tileIndex.Set(1, 5);
         else if (sur.bot == 3 && sur.right > 0 && !adj[x + 1, y - 3])
            tileIndex.Set(2, 5);
         else if (sur.left > 0 && sur.bot == 2 && sur.right > 0 && !adj[x + 1, y - 2])
            tileIndex.Set(2, 4);

         // If no tile index was found, add a ceiling tile
         if (tileIndex.x == -1) {
            tileIndex.Set(1, 7);
         }
         return allTiles[tileIndex.x, tileIndex.y];
      }
   }
}

