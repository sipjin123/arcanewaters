using UnityEngine;
using UnityEngine.Tilemaps;
using MapCreationTool.PaletteTilesData;
using TileData = MapCreationTool.PaletteTilesData.TileData;

namespace MapCreationTool
{
   public class NineSliceInOutGroup : TileGroup
   {
      public TileData[,] innerTiles { get; set; }
      public TileData[,] outerTiles { get; set; }

      public override Vector2Int brushSize => new Vector2Int(2, 2);

      public NineSliceInOutGroup () {
         type = TileGroupType.NineSliceInOut;
      }

      public override bool contains (TileBase tile) {
         for (int i = 0; i < innerTiles.GetLength(0); i++)
            for (int j = 0; j < innerTiles.GetLength(1); j++)
               if (innerTiles[i, j] != null && innerTiles[i, j].tile == tile)
                  return true;

         for (int i = 0; i < outerTiles.GetLength(0); i++)
            for (int j = 0; j < outerTiles.GetLength(1); j++)
               if (outerTiles[i, j] != null && outerTiles[i, j].tile == tile)
                  return true;

         return false;
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

         //Override rules for when they try to connect with only 1 tile
         if (adj[x, y + 1] && !adj[x + 1, y] && !adj[x - 1, y + 1])
            tileIndex.Set(2, 2);
         else if (adj[x + 1, y] && !adj[x, y + 1] && !adj[x + 1, y - 1])
            tileIndex.Set(2, 2);
         else if (adj[x + 1, y] && !adj[x, y - 1] && !adj[x + 1, y + 1])
            tileIndex.Set(2, 0);
         else if (adj[x, y - 1] && !adj[x + 1, y] && !adj[x - 1, y - 1])
            tileIndex.Set(2, 0);
         else if (adj[x, y - 1] && !adj[x - 1, y] && !adj[x + 1, y - 1])
            tileIndex.Set(0, 0);
         else if (adj[x - 1, y] && !adj[x, y - 1] && !adj[x - 1, y + 1])
            tileIndex.Set(0, 0);
         else if (adj[x - 1, y] && !adj[x, y + 1] && !adj[x - 1, y - 1])
            tileIndex.Set(0, 2);
         else if (adj[x, y + 1] && !adj[x - 1, y] && !adj[x + 1, y + 1])
            tileIndex.Set(0, 2);

         if (tileIndex.x == 1 && tileIndex.y == 1) {
            //Check if all diagonals are occupied
            if (adj[x - 1, y - 1] && adj[x + 1, y + 1] && adj[x - 1, y + 1] && adj[x + 1, y - 1]) {
               return outerTiles[tileIndex.x, tileIndex.y].tile;
            } else {
               if (!adj[x - 1, y - 1])
                  return innerTiles[2, 2].tile;
               else if (!adj[x + 1, y - 1])
                  return innerTiles[0, 2].tile;
               else if (!adj[x - 1, y + 1])
                  return innerTiles[2, 0].tile;
               else if (!adj[x + 1, y + 1])
                  return innerTiles[0, 0].tile;
            }
         } else
            return outerTiles[tileIndex.x, tileIndex.y].tile;
         throw new System.Exception("Can't resolve tile");
      }
   }
}