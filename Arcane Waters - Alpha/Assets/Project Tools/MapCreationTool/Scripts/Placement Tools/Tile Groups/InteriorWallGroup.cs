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



         // If no tile index was found, add an empty tile
         if (tileIndex.x == -1) {
            Debug.LogError("Could not find target tile in river placement tool.");
            tileIndex.Set(1, 1);
         }
         return allTiles[tileIndex.x, tileIndex.y];
      }
   }
}

