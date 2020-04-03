using UnityEngine;
using System.Collections.Generic;

namespace MapCreationTool
{
   public class EraserStroke
   {
      public Type type { get; private set; }

      private Layer deletingFromLayer;
      private bool[,] deletedTiles;
      private PlacedPrefab deletedPrefab;

      public EraserStroke () {
         deletedTiles = new bool[Tools.MAX_BOARD_SIZE, Tools.MAX_BOARD_SIZE];
      }

      public void newStroke (PlacedPrefab prefToDelete) {
         deletedPrefab = prefToDelete;
         type = Type.Prefab;
      }

      public void newStroke (Layer deletingFromLayer) {
         this.deletingFromLayer = deletingFromLayer;
         for (int i = 0; i < deletedTiles.GetLength(0); i++) {
            for (int j = 0; j < deletedTiles.GetLength(1); j++) {
               deletedTiles[i, j] = false;
            }
         }
         type = Type.Tile;
      }

      public void paint (Vector3Int position) {
         (int x, int y) index = (position.x + deletedTiles.GetLength(0) / 2, position.y + deletedTiles.GetLength(1) / 2);

         if (index.x < 0 || index.y < 0 || index.x >= deletedTiles.GetLength(0) || index.y > deletedTiles.GetLength(0))
            return;

         deletedTiles[index.x, index.y] = true;
      }

      public void clear () {
         type = Type.None;
      }

      public BoardChange calculateTileChange () {
         if (type == Type.Tile) {
            BoardChange result = new BoardChange();

            for (int i = 0; i < deletedTiles.GetLength(0); i++) {
               for (int j = 0; j < deletedTiles.GetLength(1); j++) {
                  if (deletedTiles[i, j]) {
                     result.tileChanges.Add(new TileChange {
                        tile = null,
                        position = new Vector3Int(i - deletedTiles.GetLength(0) / 2, j - deletedTiles.GetLength(1) / 2, 0),
                        layer = deletingFromLayer
                     });
                  }
               }
            }

            return result;
         }
         return null;
      }

      public BoardChange calculatePrefabChange () {
         if (type == Type.Prefab) {
            return new BoardChange {
               prefabChanges = new List<PrefabChange> {
                  new PrefabChange {
                     positionToDestroy = deletedPrefab.placedInstance.transform.position,
                     prefabToDestroy = deletedPrefab.original
                  }
               }
            };
         }

         return null;
      }

      public enum Type
      {
         None,
         Tile,
         Prefab
      }
   }
}
