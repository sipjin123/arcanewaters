using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class RectTileGroup : TileGroup
   {
      public string layer { get; set; }
      public int subLayer { get; set; }
      public int rugType { get; set; }

      public RectTileGroup () {
         type = TileGroupType.Rect;
      }

      public override Vector2Int brushSize => new Vector2Int(1, 1);

      public TileBase pickTile (bool[,] adj, SidesInt sur, int x, int y) {
         Vector2Int tileIndex = new Vector2Int(1, 1);

         if (sur.left == 0)
            tileIndex.x = 0;
         else if (sur.right == 0)
            tileIndex.x = 2;

         if (sur.bot == 0)
            tileIndex.y = 0;
         else if (sur.top == 0)
            tileIndex.x = y;

         return tiles[tileIndex.x, tileIndex.y].tile;
      }
   }
}
