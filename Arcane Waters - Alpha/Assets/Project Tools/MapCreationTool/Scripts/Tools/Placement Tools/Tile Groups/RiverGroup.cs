using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class RiverGroup : TileGroup
   {
      public TileBase[,] allTiles { get; set; }
      public string layer { get; set; }
      public int subLayer { get; set; }

      public override Vector2Int brushSize => new Vector2Int(1, 1);
      public RiverGroup () {
         type = TileGroupType.River;
      }

      public override bool contains (TileBase tile) {
         return TileGroup.contains(allTiles, tile);
      }

      public TileBase pickTile (bool[,] adj, SidesInt sur, int x, int y) {
         Vector2Int tileIndex = new Vector2Int(-1, -1);

         // When bound by 4 tiles
         if (sur.left == 1 && sur.top == 1 && sur.right == 1 && sur.bot == 1)
            tileIndex.Set(1, 1);

         // When bound by 3 tiles
         else if (sur.left == 1 && sur.top == 1 && sur.right == 1)
            tileIndex.Set(4, 1);
         else if (sur.top == 1 && sur.right == 1 && sur.bot == 1)
            tileIndex.Set(3, 2);
         else if (sur.right == 1 && sur.bot == 1 && sur.left == 1)
            tileIndex.Set(3, 1);
         else if (sur.bot == 1 && sur.left == 1 && sur.top == 1)
            tileIndex.Set(4, 2);

         // When bound by 2 tiles
         else if (sur.left == 1 && sur.right == 1)
            tileIndex.Set(1, 2);
         else if (sur.top == 1 && sur.bot == 1)
            tileIndex.Set(0, 1);

         else if (sur.left == 1 && sur.top == 1)
            tileIndex.Set(2, 0);
         else if (sur.left == 1 && sur.bot == 1)
            tileIndex.Set(2, 2);
         else if (sur.right == 1 && sur.top == 1)
            tileIndex.Set(0, 0);
         else if (sur.right == 1 && sur.bot == 1)
            tileIndex.Set(0, 2);

         // When bound by 1 tile
         else if (sur.left == 1)
            tileIndex.Set(4, 0);
         else if (sur.right == 1)
            tileIndex.Set(3, 0);
         else if (sur.top == 1)
            tileIndex.Set(5, 1);
         else if (sur.bot == 1)
            tileIndex.Set(5, 2);

         // When bound by 0 tiles
         else if (sur.left == 0 && sur.top == 0 && sur.right == 0 && sur.bot == 0)
            tileIndex.Set(5, 0);

         // If no tile index was found, add an empty tile
         if (tileIndex.x == -1) {
            Debug.LogError("Could not find target tile in river placement tool.");
            tileIndex.Set(1, 1);
         }
         return allTiles[tileIndex.x, tileIndex.y];
      }
   }
}
