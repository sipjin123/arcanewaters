using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class DockGroup : TileGroup
   {
      public string layer { get; set; }
      public int subLayer { get; set; }

      public DockGroup () {
         type = TileGroupType.Dock;
      }

      public override Vector2Int brushSize => new Vector2Int(2, 2);

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

         //Override for lower corners
         if (adj[x + 1, y] && adj[x - 1, y] && adj[x, y + 1] && adj[x, y - 1]) {
            if (!adj[x + 1, y - 1])
               tileIndex = new Vector2Int(2, 1);
            else if (!adj[x - 1, y - 1])
               tileIndex = new Vector2Int(0, 1);
         }

         return tiles[tileIndex.x, tileIndex.y].tile;
      }
   }
}