using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class MountainGroup : TileGroup
   {
      public TileBase[,] innerTiles { get; set; }
      public TileBase[,] outerTiles { get; set; }
      public string layer { get; set; }

      public override Vector2Int brushSize => new Vector2Int(5, 6);
      public MountainGroup () {
         type = TileGroupType.Mountain;
      }

      public TileBase pickTile (bool[,] adj, SidesInt sur, int x, int y) {
         Vector2Int outerIndex = new Vector2Int(-1, -1);
         Vector2Int innerIndex = new Vector2Int(-1, -1);

         //row 0
         if (sur.left == 0 && sur.bot == 0 && !adj[x + 1, y - 1])
            outerIndex.Set(1, 0);
         else if (sur.left >= 1 && sur.right >= 1 && sur.bot == 0)
            outerIndex.Set(2, 0);
         else if (sur.right == 0 && sur.bot == 0 && !adj[x - 1, y - 1])
            outerIndex.Set(3, 0);
         //row1
         else if (sur.left == 0 && sur.bot == 0 && adj[x + 1, y - 1])
            outerIndex.Set(0, 1);
         else if (sur.left == 1 && sur.bot == 1)
            innerIndex.Set(4, 4);
         else if (sur.left >= 2 && sur.right >= 2 && sur.bot == 1)
            outerIndex.Set(2, 1);
         else if (sur.right == 1 && sur.bot == 1)
            innerIndex.Set(2, 4);
         else if (sur.right == 0 && sur.bot == 0 && adj[x - 1, y - 1])
            outerIndex.Set(4, 1);
         //row2
         else if (sur.left == 0 && sur.bot == 1)
            outerIndex.Set(0, 2);
         else if (sur.left == 1 && sur.bot == 2)
            outerIndex.Set(1, 2);
         else if (sur.left >= 2 && sur.right >= 2 && sur.bot == 2)
            outerIndex.Set(2, 2);
         else if (sur.right == 1 && sur.bot == 2)
            outerIndex.Set(3, 2);
         else if (sur.right == 0 && sur.bot == 1)
            outerIndex.Set(4, 2);
         //row3
         else if (sur.left == 0 && sur.top >= 2 && sur.bot >= 2)
            outerIndex.Set(0, 3);
         else if (sur.left == 1 && sur.top >= 2 && sur.bot >= 3)
            outerIndex.Set(1, 3);
         else if (sur.left >= 2 && sur.right >= 2 && sur.top >= 2 && sur.bot >= 3)
            outerIndex.Set(2, 3);
         else if (sur.right == 1 && sur.top >= 2 && sur.bot >= 3)
            outerIndex.Set(3, 3);
         else if (sur.right == 0 && sur.top >= 2 && sur.bot >= 2)
            outerIndex.Set(4, 3);
         //row4
         else if (sur.left == 0 && sur.top == 1)
            outerIndex.Set(0, 4);
         else if (sur.left == 1 && sur.top == 1)
            outerIndex.Set(1, 4);
         else if (sur.left >= 2 && sur.right >= 2 && sur.top == 1)
            outerIndex.Set(2, 4);
         else if (sur.right == 1 && sur.top == 1)
            outerIndex.Set(3, 4);
         else if (sur.right == 0 && sur.top == 1)
            outerIndex.Set(4, 4);
         //row5
         else if (sur.left == 0 && sur.top == 0)
            outerIndex.Set(0, 5);
         else if (sur.left == 1 && sur.top == 0)
            outerIndex.Set(1, 5);
         else if (sur.left >= 2 && sur.right >= 2 && sur.top == 0)
            outerIndex.Set(2, 5);
         else if (sur.right == 1 && sur.top == 0)
            outerIndex.Set(3, 5);
         else if (sur.right == 0 && sur.top == 0)
            outerIndex.Set(4, 5);

         //----------------------------------------
         //Handle corners 
         //Mountain corners
         if (sur.right == 1 && sur.top == 1 && !adj[x + 1, y + 1])
            outerIndex.Set(4, 4);
         else if (sur.left == 1 && sur.top == 1 && !adj[x - 1, y + 1])
            outerIndex.Set(0, 4);

         else if (sur.right >= 2 && sur.top >= 1 && !adj[x + 1, y + 1])
            innerIndex.Set(2, 1);
         else if (sur.left >= 2 && sur.top >= 1 && !adj[x - 1, y + 1])
            innerIndex.Set(4, 1);
         else if (sur.right >= 2 && sur.bot == 1 && !adj[x + 1, y - 1])
            innerIndex.Set(2, 4);
         else if (sur.left >= 2 && sur.bot == 1 && !adj[x - 1, y - 1])
            innerIndex.Set(4, 4);

         else if (sur.right == 1 && sur.bot >= 2 && !adj[x + 1, y - 1])
            innerIndex.Set(1, 3);
         else if (sur.left == 1 && sur.bot >= 2 && !adj[x - 1, y - 1])
            innerIndex.Set(5, 3);

         //Other pieces
         else if (sur.left >= 1 && sur.bot >= 2 && !adj[x - 1, y - 1])
            innerIndex.Set(5, 3);
         else if (sur.right >= 1 && sur.bot >= 2 && !adj[x + 1, y - 1])
            innerIndex.Set(1, 3);

         else if (sur.bot >= 3 && sur.top >= 2 && sur.right == 1 && !adj[x + 1, y + 1])
            innerIndex.Set(1, 2);
         else if (sur.bot >= 3 && sur.top >= 2 && sur.left == 1 && !adj[x - 1, y + 1])
            innerIndex.Set(5, 2);

         //big grass corner pieces
         else if (!adj[x + 1, y + 2] && !adj[x + 2, y + 1] && adj[x + 1, y + 1] && sur.top >= 1 && sur.right >= 1)
            innerIndex.Set(1, 1);
         else if (!adj[x - 1, y + 2] && !adj[x - 2, y + 1] && adj[x - 1, y + 1] && sur.top >= 1 && sur.left >= 1)
            innerIndex.Set(5, 1);
         else if (!adj[x + 1, y - 2] && adj[x + 1, y - 1] && sur.bot >= 2 && sur.right >= 1)
            innerIndex.Set(1, 4);
         else if (!adj[x - 1, y - 2] && adj[x - 1, y - 1] && sur.bot >= 2 && sur.left >= 1)
            innerIndex.Set(5, 4);

         //Straight grass pieces
         else if (sur.top >= 2 && sur.bot >= 3 && sur.right >= 2 && (!adj[x + 2, y + 1] || !adj[x + 2, y - 1]))
            outerIndex.Set(3, 3);
         else if (sur.top >= 2 && sur.bot >= 3 && sur.left >= 2 && (!adj[x - 2, y + 1] || !adj[x - 2, y - 1]))
            outerIndex.Set(1, 3);
         else if (sur.left >= 2 && sur.right >= 2 && sur.top == 2 && (!adj[x - 1, y + 2] || !adj[x + 1, y + 2]))
            outerIndex.Set(2, 4);
         //else if (sur.left >= 2 && sur.right >= 2 && sur.bot == 2 && (!adj[i - 1, j - 2] || !adj[i + 1, j - 2]))
         //    outerIndex.Set(2, 2);


         //small grass corner pieces
         else if (!adj[x + 2, y + 2] && adj[x + 1, y + 2] && adj[x + 2, y + 1] && sur.top >= 2 && sur.right >= 2)
            innerIndex.Set(0, 1);
         else if (!adj[x - 2, y + 2] && adj[x - 1, y + 2] && adj[x - 2, y + 1] && sur.top >= 2 && sur.left >= 2)
            innerIndex.Set(5, 0);
         else if (sur.bot >= 3 && sur.right >= 2 &&
             ((adj[x + 2, y - 2] && !adj[x + 1, y - 3] && adj[x - 0, y - 3] && adj[x + 1, y - 2]) ||
             (!adj[x + 2, y - 2] && adj[x + 1, y - 2] && adj[x + 2, y - 1])))
            innerIndex.Set(0, 4);
         else if (sur.bot >= 3 && sur.left >= 2 &&
             ((adj[x - 2, y - 2] && !adj[x - 1, y - 3] && adj[x - 0, y - 3] && adj[x - 1, y - 2]) ||
             (!adj[x - 2, y - 2] && adj[x - 1, y - 2] && adj[x - 2, y - 1])))
            innerIndex.Set(6, 4);

         // Override for bottom slope tiles
         if (innerIndex.x == 4 && innerIndex.y == 4 && !adj[x + 1, y - 2]) {
            innerIndex.Set(-1, -1);
            outerIndex.Set(1, 1);
         } else if (innerIndex.x == 2 && innerIndex.y == 4 && !adj[x - 1, y - 2]) {
            innerIndex.Set(-1, -1);
            outerIndex.Set(3, 1);
         }

         if (innerIndex.x == -1 && outerIndex.x == 2 && outerIndex.y == 1) {
            bool leftMost = false;
            bool rightMost = false;

            if (adj[x - 1, y - 2])
               leftMost = true;
            if (adj[x + 1, y - 2])
               rightMost = true;

            if (leftMost && rightMost)
               innerIndex.Set(3, 5);
            else if (leftMost && !rightMost)
               innerIndex.Set(0, 5);
            else if (!leftMost && rightMost)
               innerIndex.Set(6, 5);
         }

         if (innerIndex.x != -1)
            return innerTiles[innerIndex.x, innerIndex.y];
         else if (outerIndex.x != -1 && outerTiles[outerIndex.x, outerIndex.y] != null)
            return outerTiles[outerIndex.x, outerIndex.y];
         else {
            Debug.LogError("Could not find tile index!");
            return null;
         }
      }
   }
}
