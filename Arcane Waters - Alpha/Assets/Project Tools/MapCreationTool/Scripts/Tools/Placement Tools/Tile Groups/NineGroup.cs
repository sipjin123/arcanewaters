using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class NineGroup : TileGroup
   {
      public string layer { get; set; }
      public int subLayer { get; set; }

      public override Vector2Int brushSize => new Vector2Int(2, 2);

      public NineGroup () {
         type = TileGroupType.Nine;
      }

      public TileBase pickTile (bool[,] adj, int x, int y) {
         Vector2Int tileIndex = new Vector2Int(1, 1);
         if (adj[x - 1, y] && !adj[x + 1, y])
            tileIndex.x = 2;
         else if (!adj[x - 1, y] && adj[x + 1, y])
            tileIndex.x = 0;

         if (adj[x, y - 1] && !adj[x, y + 1])
            tileIndex.y = 2;
         else if (!adj[x, y - 1] && adj[x, y + 1])
            tileIndex.y = 0;

         return tiles[tileIndex.x, tileIndex.y].tile;
      }
   }
}