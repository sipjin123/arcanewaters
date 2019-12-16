using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class SeaMountainGroup : TileGroup
   {
      public TileBase[,] allTiles { get; set; }
      public int layer { get; set; }

      public override Vector2Int brushSize => new Vector2Int(5, 5);
      public SeaMountainGroup () {
         type = TileGroupType.SeaMountain;
      }

      public override bool contains (TileBase tile) {
         return TileGroup.contains(allTiles, tile);
      }

      public TileBase pickTile (bool[,] adj, SidesInt sur, int x, int y) {
         Vector2Int tileIndex = new Vector2Int(-1, -1);

         //------------------------------------------
         // top row
         // Prioritize diagonal if mountain is big enough
         if (sur.left == 0 && sur.top == 0 && sur.right > 2 && sur.bot > 2)
            tileIndex.Set(1, 11);
         else if (sur.left > 2 && sur.top == 0 && sur.right == 0 && sur.bot > 2)
            tileIndex.Set(5, 11);

         // Else go for the smaller, squarer version
         else if (sur.left == 0 && sur.top == 0 && sur.right > 1 && sur.bot > 1 && !adj[x + 1, y + 1])
            tileIndex.Set(0, 11);
         else if (sur.left > 1 && sur.top == 0 && sur.right == 0 && sur.bot > 1 && !adj[x - 1, y + 1])
            tileIndex.Set(6, 11);


         else if (sur.left == 1 && sur.top == 0 && sur.right > 1 && sur.bot > 2 && adj[x - 1, y - 3])
            tileIndex.Set(2, 11);
         else if (sur.left > 1 && sur.top == 0 && sur.right == 1 && sur.bot > 2 && adj[x + 1, y - 3])
            tileIndex.Set(4, 11);
         else if (sur.left > 0 && sur.top == 0 && sur.right > 0 && sur.bot > 0 && (!adj[x - 1, y + 1] || !adj[x + 1, y + 1]))
            tileIndex.Set(3, 11);

         //-------------------------------
         // Second row from the top
         // Dont handle the side ones since they are repeating from other rows
         else if (sur.left == 1 && sur.top == 1 && sur.right > 2 && sur.bot > 2 && !adj[x - 1, y + 1])
            tileIndex.Set(1, 10);
         else if (sur.left > 0 && sur.left < 3 && sur.top > 0 && sur.right > 1 && sur.bot > 1
            && adj[x - 1, y - 2] && adj[x + 1, y - 2] && adj[x - 1, y + 1] && !adj[x - 1, y + 2] && !adj[x - 2, y + 1] && adj[x + 2, y + 1])
            tileIndex.Set(2, 10);
         else if (sur.left > 1 && sur.top > 0 && sur.right > 0 && sur.right < 3 && sur.bot > 1
            && adj[x + 1, y - 2] && adj[x - 1, y - 2] && adj[x + 1, y + 1] && !adj[x + 1, y + 2] && !adj[x + 2, y + 1] && adj[x - 2, y + 1])
            tileIndex.Set(4, 10);
         else if (sur.left > 2 && sur.top == 1 && sur.right == 1 && sur.bot > 2 && !adj[x + 1, y + 1])
            tileIndex.Set(5, 10);

         //--------------------------------
         // Third row from the top
         // Dont handle the second from both sides since second row handles those tiles

         else if (sur.left == 0 && sur.top < 2 && sur.top > 0 && sur.right > 2 && sur.bot > 1 && adj[x + 3, y + 1])
            tileIndex.Set(0, 9);
         else if (sur.left > 2 && sur.top < 2 && sur.top > 0 && sur.right == 0 && sur.bot > 1 && adj[x - 3, y + 1])
            tileIndex.Set(6, 9);

         //-----------------------------------
         // Fourth row from the row

         else if (sur.left == 0 && sur.top > 0 && sur.right > 1 && sur.bot > 1)
            tileIndex.Set(0, 8);
         else if (sur.left > 1 && sur.top > 0 && sur.right == 0 && sur.bot > 1)
            tileIndex.Set(6, 8);

         //--------------------------------
         // Fith row from the top

         else if (sur.left == 0 && sur.top > 0 && sur.right > 1 && sur.bot > 0)
            tileIndex.Set(0, 7);
         else if (sur.left > 0 && sur.top > 0 && sur.right > 1 && sur.bot > 1 && !adj[x - 1, y - 2] && adj[x - 1, y - 1])
            tileIndex.Set(1, 7);
         else if (sur.left > 1 && sur.top > 0 && sur.right > 0 && sur.bot > 1 && !adj[x + 1, y - 2] && adj[x + 1, y - 1])
            tileIndex.Set(5, 7);
         else if (sur.left > 1 && sur.top > 0 && sur.right == 0 && sur.bot > 0)
            tileIndex.Set(6, 7);

         //------------------------------
         // Sixth row from the top

         else if (sur.left == 0 && sur.top > 0 && sur.right > 1 && sur.bot == 0)
            tileIndex.Set(0, 6);
         else if (sur.left > 0 && sur.top > 0 && sur.right > 1 && sur.bot == 1 && !adj[x - 1, y - 1])
            tileIndex.Set(1, 6);
         else if (sur.left > 0 && sur.top > 0 && sur.right > 0 && sur.bot == 1 && adj[x - 1, y - 1] && adj[x + 1, y - 1])
            tileIndex.Set(3, 6);
         else if (sur.left > 1 && sur.top > 0 && sur.right > 0 && sur.bot == 1 && !adj[x + 1, y - 1])
            tileIndex.Set(5, 6);
         else if (sur.left > 1 && sur.top > 0 && sur.right == 0 && sur.bot == 0)
            tileIndex.Set(6, 6);

         //-----------------------------
         // The bottom row

         else if (sur.left == 0 && sur.top > 1 && sur.right > 1 && sur.bot == 0)
            tileIndex.Set(1, 5);
         else if (sur.left > 0 && sur.top > 1 && sur.right > 0 && sur.bot == 0)
            tileIndex.Set(3, 5);
         else if (sur.left > 1 && sur.top > 1 && sur.right == 0 && sur.bot == 0)
            tileIndex.Set(5, 5);

         //----------------------------------------------
         // Aditional tiles for when dealing with inner-ish corners
         else if ((sur.left == 1 && sur.top > 1 && sur.right > 1 && sur.bot > 1 && !adj[x - 1, y - 1] && !adj[x - 1, y - 2]) ||
            (sur.left > 0 && sur.top > 1 && sur.right > 1 && sur.bot > 1 && !adj[x - 1, y - 1]))
            tileIndex.Set(5, 2);
         else if ((sur.left == 1 && sur.top > 1 && sur.right > 0 && sur.bot > 1 && !adj[x + 1, y - 1] && !adj[x + 1, y - 2]) || 
            (sur.left > 1 && sur.top > 1 && sur.right > 0 && sur.bot > 1 && !adj[x + 1, y - 1]))
            tileIndex.Set(1, 2);

         else if (sur.left > 1 && sur.top > 1 && sur.right > 1 && sur.bot > 1 && !adj[x - 1, y + 1])
            tileIndex.Set(5, 1);
         else if (sur.left > 1 && sur.top > 1 && sur.right > 1 && sur.bot > 1 && !adj[x + 1, y + 1])
            tileIndex.Set(1, 1);

         else if (
            (sur.left > 0 && sur.top == 0 && sur.bot > 1 && sur.right > 1 && !adj[x - 1, y + 1]) ||
            (sur.left > 0 && sur.top == 1 && sur.bot > 1 && sur.right > 1 && !adj[x - 1, y + 1] && adj[x + 3, y + 1]))
            tileIndex.Set(6, 0);
         else if (
            (sur.left > 1 && sur.top == 0 && sur.bot > 1 && sur.right > 0 && !adj[x + 1, y + 1]) ||
            (sur.left > 2 && sur.top == 1 && sur.bot > 1 && sur.right > 0 && !adj[x + 1, y + 1] && adj[x - 3, y + 1]))
            tileIndex.Set(0, 0);

         else if (
            (sur.left > 1 && sur.top == 1 && sur.right > 1 && sur.bot > 1 && !adj[x - 2, y + 1] && sur.right < 3) ||
            (sur.left > 1 && sur.top == 1 && sur.right > 1 && sur.bot > 1 && !adj[x - 1, y + 1] && sur.right > 2) ||
            (sur.left == 1 && sur.top > 0 && sur.right > 1 && sur.bot > 1 && sur.bot < 3 && !adj[x - 1, y + 1]) ||
            (sur.left > 2 && sur.top > 0 && sur.right > 2 && sur.bot > 1) && !adj[x - 1, y + 2] && !adj[x - 2, y + 1])
            tileIndex.Set(4, 0);
         else if (
            (sur.left > 1 && sur.top == 1 && sur.right > 1 && sur.bot > 1 && !adj[x + 2, y + 1] && sur.left < 3) ||
            (sur.left > 1 && sur.top == 1 && sur.right > 1 && sur.bot > 1 && !adj[x + 1, y + 1] && sur.left > 2) ||
            (sur.left > 1 && sur.top > 0 && sur.right == 1 && sur.bot > 1 && sur.bot < 3 && !adj[x + 1, y + 1]) ||
            (sur.left > 2 && sur.top > 0 && sur.right > 2 && sur.bot > 1) && !adj[x + 1, y + 2] && !adj[x + 2, y + 1])
            tileIndex.Set(2, 0);

         else if (sur.left == 0 & sur.top == 0 && sur.right > 2 && sur.bot > 1 && adj[x + 3, y + 1] && adj[x + 1, y + 2])
            tileIndex.Set(4, 1);
         else if (sur.left > 2 && sur.top == 0 && sur.right == 0 && sur.bot > 1 && adj[x - 3, y + 1] && adj[x - 1, y + 2])
            tileIndex.Set(2, 1);

         else if (sur.left == 0 && sur.top == 0 && sur.right > 2 && sur.bot > 1)
            tileIndex.Set(0, 11);
         else if (sur.left > 2 && sur.top == 0 && sur.right == 0 && sur.bot > 1)
            tileIndex.Set(6, 11);

         else if (sur.left == 1 && sur.top > 0 && sur.right > 1 && sur.bot > 0 && !adj[x - 1, y + 1])
            tileIndex.Set(0, 8);
         else if (sur.left > 1 && sur.top > 0 && sur.right == 1 && sur.bot > 0 && !adj[x + 1, y + 1])
            tileIndex.Set(6, 8);

         // If no tile index was found, add an empty tile
         if (tileIndex.x == -1)
            tileIndex.Set(3, 9);
         return allTiles[tileIndex.x, tileIndex.y];
      }
   }
}
