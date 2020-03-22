using MapCreationTool.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class BoardChange
   {
      public List<TileChange> tileChanges { get; set; }
      public List<PrefabChange> prefabChanges { get; set; }
      public Selection selectionToAdd { get; set; }
      public Selection selectionToRemove { get; set; }

      public BoardChange () {
         tileChanges = new List<TileChange>();
         prefabChanges = new List<PrefabChange>();

         selectionToAdd = new Selection();
         selectionToRemove = new Selection();
      }

      public bool empty
      {
         get { return tileChanges.Count == 0 && prefabChanges.Count == 0 && selectionToAdd.empty && selectionToRemove.empty; }
      }

      public void add (BoardChange change) {
         tileChanges.AddRange(change.tileChanges);
         prefabChanges.AddRange(change.prefabChanges);

         selectionToAdd.add(change.selectionToAdd.tiles);
         selectionToAdd.add(change.selectionToAdd.prefabs);
         selectionToRemove.add(change.selectionToRemove.tiles);
         selectionToRemove.add(change.selectionToRemove.prefabs);
      }

      private static BoundsInt getBoardBoundsInt () {
         // We decrease the size by 1 because we want the 'max' vector to be inclusive
         return new BoundsInt(
            DrawBoard.origin.x, DrawBoard.origin.y, 0,
            DrawBoard.size.x - 1, DrawBoard.size.y - 1, 0);
      }



      public static BoardChange calculateClearAllChange (Dictionary<string, Layer> layers, List<PlacedPrefab> prefabs, Selection currentSelection) {
         BoardChange result = new BoardChange();

         foreach (Layer layer in layers.Values) {
            if (layer.hasTilemap)
               result.add(calculateClearAllChange(layer));
            else {
               foreach (Layer sublayer in layer.subLayers) {
                  if (sublayer.hasTilemap) {
                     result.add(calculateClearAllChange(sublayer));
                  }
               }
            }
         }

         foreach (PlacedPrefab pref in prefabs) {
            result.prefabChanges.Add(new PrefabChange { prefabToDestroy = pref.original, positionToDestroy = pref.placedInstance.transform.position });
         }

         foreach (PlacedPrefab pref in currentSelection.prefabs) {
            result.selectionToRemove.add(pref);
         }

         foreach (Vector3Int tile in currentSelection.tiles) {
            result.selectionToRemove.add(tile);
         }

         return result;
      }

      public static BoardChange calculateClearAllChange (Layer layer) {
         BoardChange result = new BoardChange();

         for (int i = 0; i < layer.size.x; i++) {
            for (int j = 0; j < layer.size.y; j++) {
               Vector3Int pos = new Vector3Int(i + layer.origin.x, j + layer.origin.y, 0);
               if (layer.getTile(pos) != null) {
                  result.tileChanges.Add(new TileChange(null, pos, layer));
               }
            }
         }

         return result;
      }

      public static BoardChange calculateDeserializedDataChange (DeserializedProject data, Dictionary<string, Layer> layers, List<PlacedPrefab> prefabs, Selection currentSelection) {
         BoardChange result = calculateClearAllChange(layers, prefabs, currentSelection);
         BoundsInt boundsInt = getBoardBoundsInt();
         Bounds boundsFloat = DrawBoard.getPrefabBounds();

         foreach (var tile in data.tiles) {
            // Check if tile is in bounds of the board
            if (tile.position.x < boundsInt.min.x || tile.position.x > boundsInt.max.x || tile.position.y < boundsInt.min.y || tile.position.y > boundsInt.max.y)
               continue;

            if (tile.sublayer == null)
               result.tileChanges.Add(new TileChange(tile.tile, tile.position, layers[tile.layer]));
            else {
               result.tileChanges.Add(new TileChange(tile.tile, tile.position, layers[tile.layer].subLayers[tile.sublayer.Value]));
            }
         }

         foreach (var prefab in data.prefabs) {
            if (prefab.position.x < boundsFloat.min.x || prefab.position.x > boundsFloat.max.x || prefab.position.y < boundsFloat.min.y || prefab.position.y > boundsFloat.max.y)
               continue;

            result.prefabChanges.Add(new PrefabChange {
               positionToPlace = prefab.position,
               prefabToPlace = prefab.prefab
            });
         }

         return result;
      }

      public static BoardChange calculateEraserChange (
          IEnumerable<Layer> layers,
          List<PlacedPrefab> prefabs,
          EraserLayerMode eraserMode,
          Vector3 worldPos,
          Layer deleteOnlyFrom) {
         BoardChange result = new BoardChange();

         List<PlacedPrefab> overlapPrefs = new List<PlacedPrefab>();
         foreach (PlacedPrefab placedPrefab in prefabs) {
            bool active = placedPrefab.placedInstance.activeSelf;
            placedPrefab.placedInstance.SetActive(true);
            foreach (Collider2D col in placedPrefab.placedInstance.GetComponentsInChildren<Collider2D>(true)) {
               if (col.OverlapPoint(worldPos)) {
                  overlapPrefs.Add(placedPrefab);
                  break;
               }
            }
            placedPrefab.placedInstance.SetActive(active);
         }

         if (overlapPrefs.Count > 0) {
            if (eraserMode == EraserLayerMode.Top) {
               float minZ = overlapPrefs.Min(pp => pp.placedInstance.transform.position.z);
               var prefabToDestroy = overlapPrefs.First(p => p.placedInstance.transform.position.z == minZ);

               result.prefabChanges.Add(new PrefabChange {
                  prefabToDestroy = prefabToDestroy.original,
                  positionToDestroy = prefabToDestroy.placedInstance.transform.position
               });
               return result;
            } else if (eraserMode == EraserLayerMode.All) {
               result.prefabChanges.AddRange(overlapPrefs.Select(p => new PrefabChange {
                  prefabToDestroy = p.original,
                  positionToDestroy = p.placedInstance.transform.position
               }));
            }
         }

         foreach (Layer layer in layers.Reverse()) {
            if (layer.hasTile(DrawBoard.worldToCell(worldPos)) && layer == deleteOnlyFrom) {
               result.tileChanges.Add(new TileChange(null, DrawBoard.worldToCell(worldPos), layer));
               if (eraserMode == EraserLayerMode.Top)
                  return result;
            }
         }

         return result;
      }

      public static BoardChange calculateFillChanges (TileBase tile, Layer layer, Vector3 worldPos) {
         BoardChange change = new BoardChange();
         Point2 boardSize = new Point2(DrawBoard.size.x, DrawBoard.size.y);
         Point2 boardMin = new Point2(DrawBoard.origin.x, DrawBoard.origin.y);
         Point2 boardMax = new Point2(boardMin.x + boardSize.x - 1, boardMin.y + boardSize.y - 1);

         Func<Point2, IEnumerable<Point2>> getNeighbours = (p) => {
            return new Point2[] { new Point2(p.x, p.y + 1), new Point2(p.x, p.y - 1), new Point2(p.x + 1, p.y), new Point2(p.x - 1, p.y),
                    new Point2(p.x + 1, p.y + 1), new Point2(p.x - 1, p.y + 1), new Point2(p.x + 1, p.y - 1), new Point2(p.x - 1, p.y - 1) };
         };

         Func<int, int, int> getUsedIndex = (x, y) => { return boardSize.y * (x - boardMin.x) + y - boardMin.y; };

         Vector3Int startCell = DrawBoard.worldToCell(worldPos);
         Point2 startPos = new Point2 { x = startCell.x, y = startCell.y };

         if (layer.hasTile(startPos.x, startPos.y))
            return change;

         Queue<Point2> q = new Queue<Point2>();
         bool[] used = new bool[boardSize.x * boardSize.y];

         q.Enqueue(startPos);
         used[getUsedIndex(startPos.x, startPos.y)] = true;

         for (int i = 0; i < Tools.MaxFloodFillTileCount && q.Count > 0; i++) {
            Point2 pos = q.Dequeue();

            change.tileChanges.Add(new TileChange { tile = tile, layer = layer, position = new Vector3Int(pos.x, pos.y, 0) });

            if (!layer.hasTile(pos.x + 1, pos.y) && pos.x < boardMax.x && !used[getUsedIndex(pos.x + 1, pos.y)]) {
               q.Enqueue(new Point2(pos.x + 1, pos.y));
               used[getUsedIndex(pos.x + 1, pos.y)] = true;
            }
            if (!layer.hasTile(pos.x, pos.y + 1) && pos.y < boardMax.y && !used[getUsedIndex(pos.x, pos.y + 1)]) {
               q.Enqueue(new Point2(pos.x, pos.y + 1));
               used[getUsedIndex(pos.x, pos.y + 1)] = true;
            }
            if (!layer.hasTile(pos.x + 1, pos.y + 1) && pos.x < boardMax.x && pos.y < boardMax.y && !used[getUsedIndex(pos.x + 1, pos.y + 1)]) {
               q.Enqueue(new Point2(pos.x + 1, pos.y + 1));
               used[getUsedIndex(pos.x + 1, pos.y + 1)] = true;
            }
            if (!layer.hasTile(pos.x - 1, pos.y) && pos.x > boardMin.x && !used[getUsedIndex(pos.x - 1, pos.y)]) {
               q.Enqueue(new Point2(pos.x - 1, pos.y));
               used[getUsedIndex(pos.x - 1, pos.y)] = true;
            }
            if (!layer.hasTile(pos.x, pos.y - 1) && pos.y > boardMin.y && !used[getUsedIndex(pos.x, pos.y - 1)]) {
               q.Enqueue(new Point2(pos.x, pos.y - 1));
               used[getUsedIndex(pos.x, pos.y - 1)] = true;
            }
            if (!layer.hasTile(pos.x - 1, pos.y - 1) && pos.x > boardMin.x && pos.y > boardMin.y && !used[getUsedIndex(pos.x - 1, pos.y - 1)]) {
               q.Enqueue(new Point2(pos.x - 1, pos.y - 1));
               used[getUsedIndex(pos.x - 1, pos.y - 1)] = true;
            }
            if (!layer.hasTile(pos.x - 1, pos.y + 1) && pos.x > boardMin.x && pos.y < boardMax.y && !used[getUsedIndex(pos.x - 1, pos.y + 1)]) {
               q.Enqueue(new Point2(pos.x - 1, pos.y + 1));
               used[getUsedIndex(pos.x - 1, pos.y + 1)] = true;
            }
            if (!layer.hasTile(pos.x + 1, pos.y - 1) && pos.x < boardMax.x && pos.y > boardMin.y && !used[getUsedIndex(pos.x + 1, pos.y - 1)]) {
               q.Enqueue(new Point2(pos.x + 1, pos.y - 1));
               used[getUsedIndex(pos.x + 1, pos.y - 1)] = true;
            }
         }

         return change;
      }

      public static BoardChange calculateFillChanges (PaletteTilesData.TileData tile, Dictionary<string, Layer> layers, Vector3 worldPos) {
         BoardChange change = new BoardChange();

         Vector3Int startCell = DrawBoard.worldToCell(worldPos);
         Point2 startPos = new Point2 { x = startCell.x, y = startCell.y };

         Layer[] sublayersToCheck = layers.Values.SelectMany(v => v.subLayers).Where(l => l.tileCount > 0).ToArray();
         Layer placementLayer = layers[tile.layer].subLayers[tile.subLayer];

         if (sublayersToCheck.Any(l => l.hasTile(startCell)))
            return change;

         Point2 boardSize = new Point2(DrawBoard.size.x, DrawBoard.size.y);
         Point2 boardMin = new Point2(DrawBoard.origin.x, DrawBoard.origin.y);
         Point2 boardMax = new Point2(boardMin.x + boardSize.x - 1, boardMin.y + boardSize.y - 1);

         Func<Point2, IEnumerable<Point2>> getNeighbours = (p) => {
            return new Point2[] { new Point2(p.x, p.y + 1), new Point2(p.x, p.y - 1), new Point2(p.x + 1, p.y), new Point2(p.x - 1, p.y),
                    new Point2(p.x + 1, p.y + 1), new Point2(p.x - 1, p.y + 1), new Point2(p.x + 1, p.y - 1), new Point2(p.x - 1, p.y - 1) };
         };

         Func<int, int, int> getUsedIndex = (x, y) => { return boardSize.y * (x - boardMin.x) + y - boardMin.y; };

         Queue<Point2> q = new Queue<Point2>();
         bool[] used = new bool[boardSize.x * boardSize.y];

         q.Enqueue(startPos);
         used[getUsedIndex(startPos.x, startPos.y)] = true;

         for (int i = 0; i < Tools.MaxFloodFillTileCount && q.Count > 0; i++) {
            Point2 pos = q.Dequeue();

            change.tileChanges.Add(new TileChange { tile = tile.tile, layer = placementLayer, position = new Vector3Int(pos.x, pos.y, 0) });

            if (!sublayersToCheck.Any(l => l.hasTile(pos.x + 1, pos.y)) && pos.x < boardMax.x && !used[getUsedIndex(pos.x + 1, pos.y)]) {
               q.Enqueue(new Point2(pos.x + 1, pos.y));
               used[getUsedIndex(pos.x + 1, pos.y)] = true;
            }
            if (!sublayersToCheck.Any(l => l.hasTile(pos.x, pos.y + 1)) && pos.y < boardMax.y && !used[getUsedIndex(pos.x, pos.y + 1)]) {
               q.Enqueue(new Point2(pos.x, pos.y + 1));
               used[getUsedIndex(pos.x, pos.y + 1)] = true;
            }
            if (!sublayersToCheck.Any(l => l.hasTile(pos.x + 1, pos.y + 1)) && pos.x < boardMax.x && pos.y < boardMax.y && !used[getUsedIndex(pos.x + 1, pos.y + 1)]) {
               q.Enqueue(new Point2(pos.x + 1, pos.y + 1));
               used[getUsedIndex(pos.x + 1, pos.y + 1)] = true;
            }
            if (!sublayersToCheck.Any(l => l.hasTile(pos.x - 1, pos.y)) && pos.x > boardMin.x && !used[getUsedIndex(pos.x - 1, pos.y)]) {
               q.Enqueue(new Point2(pos.x - 1, pos.y));
               used[getUsedIndex(pos.x - 1, pos.y)] = true;
            }
            if (!sublayersToCheck.Any(l => l.hasTile(pos.x, pos.y - 1)) && pos.y > boardMin.y && !used[getUsedIndex(pos.x, pos.y - 1)]) {
               q.Enqueue(new Point2(pos.x, pos.y - 1));
               used[getUsedIndex(pos.x, pos.y - 1)] = true;
            }
            if (!sublayersToCheck.Any(l => l.hasTile(pos.x - 1, pos.y - 1)) && pos.x > boardMin.x && pos.y > boardMin.y && !used[getUsedIndex(pos.x - 1, pos.y - 1)]) {
               q.Enqueue(new Point2(pos.x - 1, pos.y - 1));
               used[getUsedIndex(pos.x - 1, pos.y - 1)] = true;
            }
            if (!sublayersToCheck.Any(l => l.hasTile(pos.x - 1, pos.y + 1)) && pos.x > boardMin.x && pos.y < boardMax.y && !used[getUsedIndex(pos.x - 1, pos.y + 1)]) {
               q.Enqueue(new Point2(pos.x - 1, pos.y + 1));
               used[getUsedIndex(pos.x - 1, pos.y + 1)] = true;
            }
            if (!sublayersToCheck.Any(l => l.hasTile(pos.x + 1, pos.y - 1)) && pos.x < boardMax.x && pos.y > boardMin.y && !used[getUsedIndex(pos.x + 1, pos.y - 1)]) {
               q.Enqueue(new Point2(pos.x + 1, pos.y - 1));
               used[getUsedIndex(pos.x + 1, pos.y - 1)] = true;
            }
         }

         return change;
      }

      public static BoardChange calculateMoveChange (Vector3 from, Vector3 to, IEnumerable<Layer> layers, List<PlacedPrefab> prefabs, Selection selection) {
         bool disabled = true;
         if (disabled) {
            return new BoardChange();
         }
         BoardChange result = new BoardChange();
         BoundsInt boundsInt = getBoardBoundsInt();
         Bounds boundsFloat = DrawBoard.getPrefabBounds();

         Vector3Int fromCell = DrawBoard.worldToCell(from);
         Vector3Int toCell = DrawBoard.worldToCell(to);
         if (fromCell != toCell) {
            Vector3Int cellTransition = toCell - fromCell;
            foreach (Layer layer in layers) {
               HashSet<Vector3Int> arrivingTiles = new HashSet<Vector3Int>(selection.tiles.Where(t => layer.hasTile(t)).Select(t => t + cellTransition));
               foreach (Vector3Int tilePos in selection.tiles) {
                  TileBase tile = layer.getTile(tilePos);
                  if (tile != null) {
                     Vector3Int targetPos = tilePos + cellTransition;
                     if (!(targetPos.x < boundsInt.min.x || targetPos.x > boundsInt.max.x || targetPos.y < boundsInt.min.y || targetPos.y > boundsInt.max.y)) {
                        result.tileChanges.Add(new TileChange(tile, tilePos + cellTransition, layer));
                     }
                     if (!arrivingTiles.Contains(tilePos)) {
                        result.tileChanges.Add(new TileChange(null, tilePos, layer));
                     }
                  }

                  result.selectionToRemove.add(tilePos);
                  result.selectionToAdd.add(tilePos + cellTransition);
               }
            }
         }

         if (from != to) {
            Vector3 transition = to - from;
            foreach (PlacedPrefab prefab in selection.prefabs) {
               Vector3 targetPos = prefab.placedInstance.transform.position + transition;
               if (targetPos.x < boundsFloat.min.x || targetPos.x > boundsFloat.max.x || targetPos.y < boundsFloat.min.y || targetPos.y > boundsFloat.max.y) {
                  result.prefabChanges.Add(new PrefabChange {
                     prefabToDestroy = prefab.original,
                     positionToDestroy = prefab.placedInstance.transform.position
                  });
                  result.selectionToRemove.add(prefab);
               } else {
                  result.prefabChanges.Add(new PrefabChange {
                     prefabToTranslate = prefab.original,
                     positionToPlace = prefab.placedInstance.transform.position,
                     translation = transition
                  });
               }
            }
         }

         return result;
      }

      public static IEnumerable<Vector3Int> getRectTilePositions (Vector3Int p1, Vector3Int p2) {
         BoundsInt bounds = getBoardBoundsInt();

         // Clamp points inside board bounds
         p1.Set(Math.Max(p1.x, bounds.min.x), Math.Max(p1.y, bounds.min.y), 0);
         p1.Set(Math.Min(p1.x, bounds.max.x), Math.Min(p1.y, bounds.max.y), 0);
         p2.Set(Math.Max(p2.x, bounds.min.x), Math.Max(p2.y, bounds.min.y), 0);
         p2.Set(Math.Min(p2.x, bounds.max.x), Math.Min(p2.y, bounds.max.y), 0);

         // Find min and max corners of the bounding box
         Vector3Int min = new Vector3Int(Math.Min(p1.x, p2.x), Mathf.Min(p1.y, p2.y), 0);
         Vector3Int max = new Vector3Int(Math.Max(p1.x, p2.x), Mathf.Max(p1.y, p2.y), 0);
         Vector2Int size = new Vector2Int(max.x - min.x + 1, max.y - min.y + 1);

         for (int i = 0; i < size.x; i++) {
            for (int j = 0; j < size.y; j++) {
               yield return min + new Vector3Int(i, j, 0);
            }
         }
      }

      /// <summary>
      /// Creates bounds that encapsulate given positions
      /// </summary>
      /// <param name="position"></param>
      /// <returns></returns>
      public static BoundsInt encapsulate (List<Vector3Int> positions) {
         Vector3Int min = new Vector3Int(positions.Min(t => t.x), positions.Min(t => t.y), 0);
         Vector3Int max = new Vector3Int(positions.Max(t => t.x), positions.Max(t => t.y), 0);
         return new BoundsInt(min.x, min.y, min.z, max.x - min.x + 1, max.y - min.y + 1, 0);
      }
   }

   public class TileChange
   {
      public TileBase tile { get; set; }
      public Vector3Int position { get; set; }
      public Layer layer { get; set; }

      public TileChange () {

      }
      public TileChange (TileBase tile, Vector3Int position, Layer layer) {
         this.tile = tile;
         this.position = position;
         this.layer = layer;
      }
   }

   public class PrefabChange
   {
      public Vector3 positionToPlace { get; set; }
      public GameObject prefabToPlace { get; set; }

      public Vector3 positionToDestroy { get; set; }
      public GameObject prefabToDestroy { get; set; }

      public GameObject prefabToTranslate { get; set; }
      public Vector3 translation { get; set; }

      public Dictionary<string, string> dataToSet { get; set; }
   }

   public struct Point2
   {
      public int x;
      public int y;

      public Point2 (int x, int y) {
         this.x = x;
         this.y = y;
      }
   }
}
