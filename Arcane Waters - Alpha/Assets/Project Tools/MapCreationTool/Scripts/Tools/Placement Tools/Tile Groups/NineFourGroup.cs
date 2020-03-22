using MapCreationTool.PaletteTilesData;
using UnityEngine;
using UnityEngine.Tilemaps;
using TileData = MapCreationTool.PaletteTilesData.TileData;

namespace MapCreationTool
{
   public class NineFourGroup : TileGroup
   {
      public TileData[,] mainTiles { get; set; }
      public TileData[,] cornerTiles { get; set; }
      public string layer { get; set; }
      public bool singleLayer { get; set; }
      public bool invertedCorners { get; set; }

      public override Vector2Int brushSize => new Vector2Int(2, 2);

      public NineFourGroup () {
         type = TileGroupType.NineFour;
      }

      public override bool contains (TileBase tile) {
         for (int i = 0; i < mainTiles.GetLength(0); i++)
            for (int j = 0; j < mainTiles.GetLength(1); j++)
               if (mainTiles[i, j] != null && mainTiles[i, j].tile == tile)
                  return true;

         for (int i = 0; i < cornerTiles.GetLength(0); i++)
            for (int j = 0; j < cornerTiles.GetLength(1); j++)
               if (cornerTiles[i, j] != null && cornerTiles[i, j].tile == tile)
                  return true;

         return false;
      }

      public TileBase pickMainTile (bool[,] adj, int x, int y) {
         (int x, int y) index = (1, 1);

         if (adj[x - 1, y] && !adj[x + 1, y])
            index.x = 2;
         else if (!adj[x - 1, y] && adj[x + 1, y])
            index.x = 0;

         if (adj[x, y - 1] && !adj[x, y + 1])
            index.y = 2;
         else if (!adj[x, y - 1] && adj[x, y + 1])
            index.y = 0;

         return mainTiles[index.x, index.y].tile;
      }

      public TileBase pickCornerTile (bool[,] adj, int x, int y) {
         (int x, int y) index = (-1, -1);

         if (!adj[x - 1, y - 1] && adj[x, y - 1] && adj[x - 1, y])
            index = (1, 1);
         else if (!adj[x - 1, y + 1] && adj[x, y + 1] && adj[x - 1, y])
            index = (1, 0);
         else if (!adj[x + 1, y + 1] && adj[x, y + 1] && adj[x + 1, y])
            index = (0, 0);

         else if (!adj[x + 1, y - 1] && adj[x, y - 1] && adj[x + 1, y])
            index = (0, 1);

         if (index.x == -1) {
            return null;
         }

         return cornerTiles[index.x, index.y].tile;
      }
   }
}