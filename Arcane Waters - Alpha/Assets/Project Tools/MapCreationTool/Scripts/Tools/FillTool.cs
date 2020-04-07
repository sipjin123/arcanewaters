using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class FillTool : Tool
   {
      protected override ToolType toolType => ToolType.Fill;

      protected override void registerUIEvents () {
         DrawBoardEvents.PointerDown += pointerDown;
      }

      protected override void unregisterUIEvents () {
         DrawBoardEvents.PointerDown -= pointerDown;
      }

      private void pointerDown (Vector3 worldPos) {
         if (Tools.tileGroup != null) {
            if (Tools.tileGroup.maxTileCount == 1) {
               Vector3Int cellPos = DrawBoard.worldToCell(worldPos);
               PaletteTilesData.TileData tile = Tools.tileGroup.tiles[0, 0];

               BoardChange change = Tools.fillBounds == FillBounds.SingleLayer
                  ? calculateFillChanges(tile.tile, DrawBoard.instance.layers[tile.layer].subLayers[tile.subLayer], cellPos)
                  : calculateFillChanges(tile, DrawBoard.instance.layers, cellPos);

               DrawBoard.instance.changeBoard(change);
            }
         }
      }

      public static BoardChange calculateFillChanges (TileBase tile, Layer layer, Vector3Int position) {
         BoardChange change = new BoardChange();
         (int x, int y) boardSize = (DrawBoard.size.x, DrawBoard.size.y);
         (int x, int y) boardMin = (DrawBoard.origin.x, DrawBoard.origin.y);
         (int x, int y) boardMax = (boardMin.x + boardSize.x - 1, boardMin.y + boardSize.y - 1);

         Func<int, int, int> getUsedIndex = (x, y) => { return boardSize.y * (x - boardMin.x) + y - boardMin.y; };

         (int x, int y) startPos = (position.x, position.y);

         if (layer.hasTile(startPos.x, startPos.y))
            return change;

         Queue<(int x, int y)> q = new Queue<(int x, int y)>();
         bool[] used = new bool[boardSize.x * boardSize.y];

         q.Enqueue(startPos);
         used[getUsedIndex(startPos.x, startPos.y)] = true;

         for (int i = 0; i < Tools.MaxFloodFillTileCount && q.Count > 0; i++) {
            var pos = q.Dequeue();

            change.tileChanges.Add(new TileChange { tile = tile, layer = layer, position = new Vector3Int(pos.x, pos.y, 0) });

            if (!layer.hasTile(pos.x + 1, pos.y) && pos.x < boardMax.x && !used[getUsedIndex(pos.x + 1, pos.y)]) {
               q.Enqueue((pos.x + 1, pos.y));
               used[getUsedIndex(pos.x + 1, pos.y)] = true;
            }
            if (!layer.hasTile(pos.x, pos.y + 1) && pos.y < boardMax.y && !used[getUsedIndex(pos.x, pos.y + 1)]) {
               q.Enqueue((pos.x, pos.y + 1));
               used[getUsedIndex(pos.x, pos.y + 1)] = true;
            }
            if (!layer.hasTile(pos.x + 1, pos.y + 1) && pos.x < boardMax.x && pos.y < boardMax.y && !used[getUsedIndex(pos.x + 1, pos.y + 1)]) {
               q.Enqueue((pos.x + 1, pos.y + 1));
               used[getUsedIndex(pos.x + 1, pos.y + 1)] = true;
            }
            if (!layer.hasTile(pos.x - 1, pos.y) && pos.x > boardMin.x && !used[getUsedIndex(pos.x - 1, pos.y)]) {
               q.Enqueue((pos.x - 1, pos.y));
               used[getUsedIndex(pos.x - 1, pos.y)] = true;
            }
            if (!layer.hasTile(pos.x, pos.y - 1) && pos.y > boardMin.y && !used[getUsedIndex(pos.x, pos.y - 1)]) {
               q.Enqueue((pos.x, pos.y - 1));
               used[getUsedIndex(pos.x, pos.y - 1)] = true;
            }
            if (!layer.hasTile(pos.x - 1, pos.y - 1) && pos.x > boardMin.x && pos.y > boardMin.y && !used[getUsedIndex(pos.x - 1, pos.y - 1)]) {
               q.Enqueue((pos.x - 1, pos.y - 1));
               used[getUsedIndex(pos.x - 1, pos.y - 1)] = true;
            }
            if (!layer.hasTile(pos.x - 1, pos.y + 1) && pos.x > boardMin.x && pos.y < boardMax.y && !used[getUsedIndex(pos.x - 1, pos.y + 1)]) {
               q.Enqueue((pos.x - 1, pos.y + 1));
               used[getUsedIndex(pos.x - 1, pos.y + 1)] = true;
            }
            if (!layer.hasTile(pos.x + 1, pos.y - 1) && pos.x < boardMax.x && pos.y > boardMin.y && !used[getUsedIndex(pos.x + 1, pos.y - 1)]) {
               q.Enqueue((pos.x + 1, pos.y - 1));
               used[getUsedIndex(pos.x + 1, pos.y - 1)] = true;
            }
         }

         return change;
      }

      public static BoardChange calculateFillChanges (PaletteTilesData.TileData tile, Dictionary<string, Layer> layers, Vector3Int position) {
         BoardChange change = new BoardChange();

         (int x, int y) startPos = (position.x, position.y);

         Layer[] sublayersToCheck = layers.Values.SelectMany(v => v.subLayers).Where(l => l.tileCount > 0).ToArray();
         Layer placementLayer = layers[tile.layer].subLayers[tile.subLayer];

         if (sublayersToCheck.Any(l => l.hasTile(startPos.x, startPos.y)))
            return change;

         (int x, int y) boardSize = (DrawBoard.size.x, DrawBoard.size.y);
         (int x, int y) boardMin = (DrawBoard.origin.x, DrawBoard.origin.y);
         (int x, int y) boardMax = (boardMin.x + boardSize.x - 1, boardMin.y + boardSize.y - 1);

         Func<int, int, int> getUsedIndex = (x, y) => { return boardSize.y * (x - boardMin.x) + y - boardMin.y; };

         Queue<(int x, int y)> q = new Queue<(int x, int y)>();
         bool[] used = new bool[boardSize.x * boardSize.y];

         q.Enqueue(startPos);
         used[getUsedIndex(startPos.x, startPos.y)] = true;

         for (int i = 0; i < Tools.MaxFloodFillTileCount && q.Count > 0; i++) {
            (int x, int y) pos = q.Dequeue();

            change.tileChanges.Add(new TileChange { tile = tile.tile, layer = placementLayer, position = new Vector3Int(pos.x, pos.y, 0) });

            if (!sublayersToCheck.Any(l => l.hasTile(pos.x + 1, pos.y)) && pos.x < boardMax.x && !used[getUsedIndex(pos.x + 1, pos.y)]) {
               q.Enqueue((pos.x + 1, pos.y));
               used[getUsedIndex(pos.x + 1, pos.y)] = true;
            }
            if (!sublayersToCheck.Any(l => l.hasTile(pos.x, pos.y + 1)) && pos.y < boardMax.y && !used[getUsedIndex(pos.x, pos.y + 1)]) {
               q.Enqueue((pos.x, pos.y + 1));
               used[getUsedIndex(pos.x, pos.y + 1)] = true;
            }
            if (!sublayersToCheck.Any(l => l.hasTile(pos.x + 1, pos.y + 1)) && pos.x < boardMax.x && pos.y < boardMax.y && !used[getUsedIndex(pos.x + 1, pos.y + 1)]) {
               q.Enqueue((pos.x + 1, pos.y + 1));
               used[getUsedIndex(pos.x + 1, pos.y + 1)] = true;
            }
            if (!sublayersToCheck.Any(l => l.hasTile(pos.x - 1, pos.y)) && pos.x > boardMin.x && !used[getUsedIndex(pos.x - 1, pos.y)]) {
               q.Enqueue((pos.x - 1, pos.y));
               used[getUsedIndex(pos.x - 1, pos.y)] = true;
            }
            if (!sublayersToCheck.Any(l => l.hasTile(pos.x, pos.y - 1)) && pos.y > boardMin.y && !used[getUsedIndex(pos.x, pos.y - 1)]) {
               q.Enqueue((pos.x, pos.y - 1));
               used[getUsedIndex(pos.x, pos.y - 1)] = true;
            }
            if (!sublayersToCheck.Any(l => l.hasTile(pos.x - 1, pos.y - 1)) && pos.x > boardMin.x && pos.y > boardMin.y && !used[getUsedIndex(pos.x - 1, pos.y - 1)]) {
               q.Enqueue((pos.x - 1, pos.y - 1));
               used[getUsedIndex(pos.x - 1, pos.y - 1)] = true;
            }
            if (!sublayersToCheck.Any(l => l.hasTile(pos.x - 1, pos.y + 1)) && pos.x > boardMin.x && pos.y < boardMax.y && !used[getUsedIndex(pos.x - 1, pos.y + 1)]) {
               q.Enqueue((pos.x - 1, pos.y + 1));
               used[getUsedIndex(pos.x - 1, pos.y + 1)] = true;
            }
            if (!sublayersToCheck.Any(l => l.hasTile(pos.x + 1, pos.y - 1)) && pos.x < boardMax.x && pos.y > boardMin.y && !used[getUsedIndex(pos.x + 1, pos.y - 1)]) {
               q.Enqueue((pos.x + 1, pos.y - 1));
               used[getUsedIndex(pos.x + 1, pos.y - 1)] = true;
            }
         }

         return change;
      }
   }
}
