using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class SeaMountainGroup : TileGroup
   {
      public TileBase[,] allTiles { get; set; }
      public int layer { get; set; }

      public override Vector2Int brushSize => new Vector2Int(3, 3);
      public SeaMountainGroup () {
         type = TileGroupType.SeaMountain;
      }

      public override bool contains (TileBase tile) {
         return TileGroup.contains(allTiles, tile);
      }

      public TileBase pickTile(bool[,] adj, SidesInt sur, int x, int y) {
         Vector2Int tileIndex = new Vector2Int(-1, -1);

         if (sur.left == 0 && sur.top == 0 && sur.right > 1 && sur.bot > 0 && !adj[x-1, y-1] && !adj[x - 1, y + 1])
            tileIndex.Set(0, 6);
         else if(sur.left == 0 && sur.top == 0 && sur.right > 1 && sur.bot > 0 && (adj[x - 1, y - 1] || !adj[x - 1, y + 1]))
            tileIndex.Set(1, 6);
         else if (sur.left == 1 && sur.top == 0 && sur.right > 1 && sur.bot > 0)
            tileIndex.Set(2, 6);
         else if (sur.left > 0 && sur.top == 0 && sur.right > 0 && sur.bot > 0)
            tileIndex.Set(3, 6);
         else if (sur.left > 1 && sur.top == 0 && sur.right == 1 && sur.bot > 0)
            tileIndex.Set(4, 6);
         else if (sur.left > 1 && sur.top == 0 && sur.right == 0 && sur.bot > 0 && !adj[x + 1, y - 1] && !adj[x + 1, y + 1])
            tileIndex.Set(5, 6);
         else if (sur.left > 1 && sur.top == 0 && sur.right == 0 && sur.bot > 0 && (adj[x + 1, y - 1] || !adj[x + 1, y + 1]))
            tileIndex.Set(6, 6);
         
         
         
         


         if (tileIndex.x == -1)
            return AssetSerializationMaps.transparentTileBase;
         return allTiles[tileIndex.x, tileIndex.y];
      }
   }
}
