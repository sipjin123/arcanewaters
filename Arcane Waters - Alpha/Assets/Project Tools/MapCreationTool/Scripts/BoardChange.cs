using MapCreationTool.PaletteTilesData;
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

      public BoardChange () {
         tileChanges = new List<TileChange>();
         prefabChanges = new List<PrefabChange>();
      }

      public bool empty
      {
         get { return tileChanges.Count == 0 && prefabChanges.Count == 0; }
      }

      public void add (BoardChange change) {
         tileChanges.AddRange(change.tileChanges);
         prefabChanges.AddRange(change.prefabChanges);
      }

      private static BoundsInt getBoardBoundsInt () {
         // We decrease the size by 1 because we want the 'max' vector to be inclusive
         return new BoundsInt(
            DrawBoard.origin.x, DrawBoard.origin.y, 0,
            DrawBoard.size.x - 1, DrawBoard.size.y - 1, 0);
      }

      private static Bounds getPrefabBounds () {
         Bounds result = new Bounds(Vector3.zero, (Vector3Int) DrawBoard.size);
         result.Expand(6f);
         return result;
      }

      public static BoardChange calculateClearAllChange (Dictionary<string, Layer> layers, List<PlacedPrefab> prefabs) {
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

         return result; 
      }

      public static BoardChange calculatePrefabChange (GameObject prefabToPlace, Vector3 positionToPlace) {
         Bounds bounds = getPrefabBounds();
         if (positionToPlace.x < bounds.min.x || positionToPlace.x > bounds.max.x || positionToPlace.y < bounds.min.y || positionToPlace.y > bounds.max.y)
            return new BoardChange();

         return new BoardChange {
            prefabChanges = new List<PrefabChange> {
               new PrefabChange {
                  prefabToPlace = prefabToPlace,
                  positionToPlace = positionToPlace
               }
            }
         };
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

      public static BoardChange calculateDeserializedDataChange (DeserializedProject data, Dictionary<string, Layer> layers, List<PlacedPrefab> prefabs) {
         BoardChange result = calculateClearAllChange(layers, prefabs);
         BoundsInt boundsInt = getBoardBoundsInt();
         Bounds boundsFloat = getPrefabBounds();

         foreach (var tile in data.tiles) {
            // Check if tile is in bounds of the board
            if (tile.position.x < boundsInt.min.x || tile.position.x > boundsInt.max.x || tile.position.y < boundsInt.min.y || tile.position.y > boundsInt.max.y)
               continue;

            Layer layer = layers[tile.layer];
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
          Vector3 worldPos) {
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
            if (layer.hasTile(DrawBoard.worldToCell(worldPos))) {
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

      public static BoardChange calculateRectChanges (RectTileGroup group, Layer layer, Vector3Int p1, Vector3Int p2) {
         BoundsInt bounds = getBoardBoundsInt();
         BoardChange change = new BoardChange();

         // Clamp points inside board bounds
         p1.Set(Math.Max(p1.x, bounds.min.x), Math.Max(p1.y, bounds.min.y), 0);
         p1.Set(Math.Min(p1.x, bounds.max.x), Math.Min(p1.y, bounds.max.y), 0);
         p2.Set(Math.Max(p2.x, bounds.min.x), Math.Max(p2.y, bounds.min.y), 0);
         p2.Set(Math.Min(p2.x, bounds.max.x), Math.Min(p2.y, bounds.max.y), 0);

         // Find min and max corners of the bounding box
         Vector3Int min = new Vector3Int(Math.Min(p1.x, p2.x), Mathf.Min(p1.y, p2.y), 0);
         Vector3Int max = new Vector3Int(Math.Max(p1.x, p2.x), Mathf.Max(p1.y, p2.y), 0);
         Vector2Int size = new Vector2Int(max.x - min.x + 1, max.y - min.y + 1);

         // Add left corners
         change.tileChanges.Add(new TileChange { layer = layer, position = min, tile = group.tiles[0, 0].tile });
         change.tileChanges.Add(new TileChange { layer = layer, position = new Vector3Int(min.x, max.y, 0), tile = group.tiles[0, 2].tile });

         // Add rest of left column
         for (int i = 1; i < size.y - 1; i++) {
            change.tileChanges.Add(new TileChange { layer = layer, position = min + Vector3Int.up * i, tile = group.tiles[0, 1].tile });
         }

         // Add middle rows
         for (int i = 1; i < size.x - 1; i++) {
            // Add top and bot tiles
            change.tileChanges.Add(new TileChange { layer = layer, position = new Vector3Int(min.x + i, min.y, 0), tile = group.tiles[1, 0].tile });
            change.tileChanges.Add(new TileChange { layer = layer, position = new Vector3Int(min.x + i, max.y, 0), tile = group.tiles[1, 2].tile });

            // Add rest of tiles
            for (int j = 1; j < size.y - 1; j++) {
               change.tileChanges.Add(new TileChange { layer = layer, position = min + new Vector3Int(i, j, 0), tile = group.tiles[1, 1].tile });
            }
         }

         // Add right corners
         change.tileChanges.Add(new TileChange { layer = layer, position = new Vector3Int(max.x, min.y, 0), tile = group.tiles[2, 0].tile });
         change.tileChanges.Add(new TileChange { layer = layer, position = max, tile = group.tiles[2, 2].tile });

         // Add rest of right tiles
         for (int i = 1; i < size.y - 1; i++) {
            change.tileChanges.Add(new TileChange { layer = layer, position = max - Vector3Int.up * i, tile = group.tiles[2, 1].tile });
         }

         return change;
      }

      public static BoardChange calculateNineSliceInOutchanges (NineSliceInOutGroup group, Layer layer, Vector3 worldPos) {
         BoardChange change = new BoardChange();

         Vector3Int targetCenter = DrawBoard.worldToCell(worldPos - new Vector3(group.brushSize.x * 0.5f, group.brushSize.y * 0.5f));

         BoundsInt bounds = getBoardBoundsInt();

         //Adjacency matrix where the target brush tile is at the center,
         //Tiles of the same group are set to true, null and other tiles false
         int n = 7; //Size of adjacency matrix
         bool[,] adj = new bool[n, n];

         //Fill adjacency matrix according to relavent tile info
         for (int i = 0; i < n; i++) {
            for (int j = 0; j < n; j++) {
               Vector3Int index = new Vector3Int(targetCenter.x - (n / 2) + i, targetCenter.y - (n / 2) + j, 0);
               TileBase tile = layer.getTile(index);

               adj[i, j] = tile != null; // && group.Contains(tile);

               // If the position is outside of the map, we will treat this position as if it contains a tile
               if (index.x < bounds.min.x || index.x > bounds.max.x || index.y < bounds.min.y || index.y > bounds.max.y)
                  adj[i, j] = true;
            }
         }

         //Set the tiles that must be placed as true
         adj[3, 3] = adj[3, 4] = adj[4, 3] = adj[4, 4] = true;

         //Determine tiles based on the adjacency matrix
         for (int i = 1; i < n - 1; i++) {
            for (int j = 1; j < n - 1; j++) {
               if (adj[i, j]) {
                  Vector3Int position = targetCenter + new Vector3Int(i - (n / 2), j - (n / 2), 0);

                  // If the position is outside the map, continue
                  if (position.x < bounds.min.x || position.x > bounds.max.x || position.y < bounds.min.y || position.y > bounds.max.y)
                     continue;

                  Vector2Int tileIndex = new Vector2Int(1, 1);
                  if (adj[i - 1, j] && !adj[i + 1, j])
                     tileIndex.x = 2;
                  else if (!adj[i - 1, j] && adj[i + 1, j])
                     tileIndex.x = 0;

                  if (adj[i, j - 1] && !adj[i, j + 1])
                     tileIndex.y = 2;
                  else if (!adj[i, j - 1] && adj[i, j + 1])
                     tileIndex.y = 0;

                  //Override rules for when they try to connect with only 1 tile
                  if (adj[i, j + 1] && !adj[i + 1, j] && !adj[i - 1, j + 1])
                     tileIndex.Set(2, 2);
                  else if (adj[i + 1, j] && !adj[i, j + 1] && !adj[i + 1, j - 1])
                     tileIndex.Set(2, 2);
                  else if (adj[i + 1, j] && !adj[i, j - 1] && !adj[i + 1, j + 1])
                     tileIndex.Set(2, 0);
                  else if (adj[i, j - 1] && !adj[i + 1, j] && !adj[i - 1, j - 1])
                     tileIndex.Set(2, 0);
                  else if (adj[i, j - 1] && !adj[i - 1, j] && !adj[i + 1, j - 1])
                     tileIndex.Set(0, 0);
                  else if (adj[i - 1, j] && !adj[i, j - 1] && !adj[i - 1, j + 1])
                     tileIndex.Set(0, 0);
                  else if (adj[i - 1, j] && !adj[i, j + 1] && !adj[i - 1, j - 1])
                     tileIndex.Set(0, 2);
                  else if (adj[i, j + 1] && !adj[i - 1, j] && !adj[i + 1, j + 1])
                     tileIndex.Set(0, 2);

                  if (tileIndex.x == 1 && tileIndex.y == 1) {
                     //Check if all diagonals are occupied
                     if (adj[i - 1, j - 1] && adj[i + 1, j + 1] && adj[i - 1, j + 1] && adj[i + 1, j - 1]) {
                        change.tileChanges.Add(new TileChange(group.outerTiles[tileIndex.x, tileIndex.y].tile, position, layer));
                     } else {
                        if (!adj[i - 1, j - 1])
                           change.tileChanges.Add(new TileChange(group.innerTiles[2, 2].tile, position, layer));
                        else if (!adj[i + 1, j - 1])
                           change.tileChanges.Add(new TileChange(group.innerTiles[0, 2].tile, position, layer));
                        else if (!adj[i - 1, j + 1])
                           change.tileChanges.Add(new TileChange(group.innerTiles[2, 0].tile, position, layer));
                        else if (!adj[i + 1, j + 1])
                           change.tileChanges.Add(new TileChange(group.innerTiles[0, 0].tile, position, layer));
                     }
                  } else
                     change.tileChanges.Add(new TileChange(group.outerTiles[tileIndex.x, tileIndex.y].tile, position, layer));
               }
            }
         }
         return change;
      }

      public static BoardChange calculateNineGroupChanges (NineGroup group, Layer layer, Vector3 worldPos) {
         BoardChange change = new BoardChange();

         Vector3Int targetCenter = DrawBoard.worldToCell(worldPos - new Vector3(group.brushSize.x * 0.5f, group.brushSize.y * 0.5f));
         BoundsInt bounds = getBoardBoundsInt();

         //Adjacency matrix where the target brush tile is at the center,
         //Tiles of the same group are set to true, null and other tiles false
         int n = 7; //Size of adjacency matrix
         bool[,] adj = new bool[n, n];

         //Fill adjacency matrix according to relavent tile info
         for (int i = 0; i < n; i++) {
            for (int j = 0; j < n; j++) {
               Vector3Int index = new Vector3Int(targetCenter.x - (n / 2) + i, targetCenter.y - (n / 2) + j, 0);
               TileBase tile = layer.getTile(index);

               adj[i, j] = tile != null && group.contains(tile);

               // If the position is outside of the map, we will treat this position as if it contains a tile
               if (index.x < bounds.min.x || index.x > bounds.max.x || index.y < bounds.min.y || index.y > bounds.max.y)
                  adj[i, j] = true;
            }
         }

         //Set the tiles that must be placed as true
         adj[3, 3] = true;
         adj[3, 4] = true;
         adj[4, 3] = true;
         adj[4, 4] = true;

         //Determine tiles based on the adjacency matrix
         for (int i = 1; i < n - 1; i++) {
            for (int j = 1; j < n - 1; j++) {
               if (adj[i, j]) {
                  Vector3Int position = targetCenter + new Vector3Int(i - (n / 2), j - (n / 2), 0);

                  // If the position is outside the map, continue
                  if (position.x < bounds.min.x || position.x > bounds.max.x || position.y < bounds.min.y || position.y > bounds.max.y)
                     continue;

                  Vector2Int tileIndex = new Vector2Int(1, 1);
                  if (adj[i - 1, j] && !adj[i + 1, j])
                     tileIndex.x = 2;
                  else if (!adj[i - 1, j] && adj[i + 1, j])
                     tileIndex.x = 0;

                  if (adj[i, j - 1] && !adj[i, j + 1])
                     tileIndex.y = 2;
                  else if (!adj[i, j - 1] && adj[i, j + 1])
                     tileIndex.y = 0;

                  change.tileChanges.Add(new TileChange(group.tiles[tileIndex.x, tileIndex.y].tile, position, layer));
               }
            }
         }
         return change;
      }

      /// <summary>
      /// If a user would make input at a given position with the nine four tile group selected,
      /// what kind of changes would be made.
      /// </summary>
      /// <param name="group"></param>
      /// <param name="worldPos"></param>
      /// <returns></returns>
      public static BoardChange calculateNineFourChanges (NineFourGroup group, Layer layer, Vector3 worldPos) {
         BoardChange change = new BoardChange();

         BoundsInt bounds = getBoardBoundsInt();
         Vector3Int targetCenter = DrawBoard.worldToCell(worldPos - new Vector3(group.brushSize.x * 0.5f, group.brushSize.y * 0.5f));
         Vector3Int targetCorner = targetCenter - Vector3Int.one * 4;
         targetCorner.z = 0;
         int n = 2;

         List<Vector3Int> addedTiles = new List<Vector3Int>();

         for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
               if (group.mainTiles[i, j] != null)
                  addedTiles.Add(new Vector3Int(i + targetCenter.x, j + targetCenter.y, 0));

         change.add(calculateNineFourRearangementChanges(group, addedTiles, layer, targetCorner, new Vector3Int(n * 5, n * 5, 0), 0));

         return change;
      }

      public static BoardChange calculateNineFourRearangementChanges (
          NineFourGroup group, List<Vector3Int> addedTiles, Layer layer, Vector3Int from, Vector3Int size, int depth) {
         if (depth > 1000)
            throw new Exception("Potential infinite recursion!");

         BoardChange change = new BoardChange();
         BoundsInt bounds = getBoardBoundsInt();

         Layer mainLayer = layer.subLayers[group.mainTiles[0, 0].subLayer];
         Layer cornerLayer = layer.subLayers[group.cornerTiles[0, 0].subLayer];

         bool[,] adj = new bool[size.x, size.y];

         //Fill adjacency matrix according to relavent tile info
         for (int i = 0; i < size.x; i++) {
            for (int j = 0; j < size.y; j++) {
               Vector3Int index = new Vector3Int(i, j, 0) + from;
               TileBase tile = mainLayer.getTile(index);

               adj[i, j] = tile != null && group.contains(tile);

               if (index.x < bounds.min.x || index.x > bounds.max.x || index.y < bounds.min.y || index.y > bounds.max.y)
                  adj[i, j] = true;
            }
         }

         foreach (Vector3Int pos in addedTiles) {
            if (pos.x >= from.x && pos.y >= from.y && pos.x < from.x + size.x && pos.y < from.y + size.y)
               adj[pos.x - from.x, pos.y - from.y] = true;
         }

         //--------------------------------------------
         //Ensure no edge cases are present
         List<Vector3Int> added = new List<Vector3Int>();
         int lastAddedCount = -1;

         int infiniteCounter = 0;
         while (lastAddedCount != added.Count) {
            infiniteCounter++;
            if (infiniteCounter > 10000)
               throw new System.Exception("Infinite cycle!");

            lastAddedCount = added.Count;
            var aj = adj.Clone() as bool[,];


            for (int i = 0; i < size.x; i++) {
               for (int j = 0; j < size.y; j++) {
                  //short diagonals
                  if (i < size.x - 2 && j < size.y - 2 && !adj[i, j] && adj[i + 1, j + 1] && (!adj[i + 2, j + 2] || !adj[i + 2, j + 1] || !adj[i + 1, j + 2]) && adj[i, j + 1] && adj[i + 1, j])
                     added.Add(new Vector3Int(i, j, 0));
                  else if (i > 1 && j < size.y - 2 && !adj[i, j] && adj[i - 1, j + 1] && (!adj[i - 2, j + 2] || !adj[i - 2, j + 1] || !adj[i - 1, j + 2]) && adj[i, j + 1] && adj[i - 1, j])
                     added.Add(new Vector3Int(i, j, 0));
                  if (i < size.x - 2 && j > 1 && !adj[i, j] && adj[i + 1, j - 1] && (!adj[i + 2, j - 2] || !adj[i + 2, j - 1] || !adj[i + 1, j - 2]) && adj[i, j - 1] && adj[i + 1, j])
                     added.Add(new Vector3Int(i, j, 0));
                  else if (i > 1 && j > 1 && !adj[i, j] && adj[i - 1, j - 1] && (!adj[i - 2, j - 2] || !adj[i - 2, j - 1] || !adj[i - 1, j - 2]) && adj[i, j - 1] && adj[i - 1, j])
                     added.Add(new Vector3Int(i, j, 0));

                  //not completely diagonal diagonals

               }
            }

            for (int i = lastAddedCount; i < added.Count; i++) {
               adj[added[i].x, added[i].y] = true;
            }
         }

         addedTiles.AddRange(added.Select(p => p + from));

         //Determine tiles based on the adjacency matrix
         for (int i = 1; i < size.x - 1; i++) {
            for (int j = 1; j < size.y - 1; j++) {
               if (adj[i, j]) {
                  Vector3Int position = from + new Vector3Int(i, j, 0);

                  // If the position is outside the map, continue
                  if (position.x < bounds.min.x || position.x > bounds.max.x || position.y < bounds.min.y || position.y > bounds.max.y)
                     continue;

                  //-------------------------------------
                  //Main
                  Vector2Int mainIndex = new Vector2Int(1, 1);
                  if (adj[i - 1, j] && !adj[i + 1, j])
                     mainIndex.x = 2;
                  else if (!adj[i - 1, j] && adj[i + 1, j])
                     mainIndex.x = 0;

                  if (adj[i, j - 1] && !adj[i, j + 1])
                     mainIndex.y = 2;
                  else if (!adj[i, j - 1] && adj[i, j + 1])
                     mainIndex.y = 0;

                  //-------------------------------------
                  //Path corners
                  Vector2Int cornerIndex = new Vector2Int(-1, -1);
                  PaletteTilesData.TileData[,] cornerTiles = group.cornerTiles;

                  if (!adj[i - 1, j - 1] && adj[i, j - 1] && adj[i - 1, j])
                     cornerIndex = new Vector2Int(1, 1);
                  else if (!adj[i - 1, j + 1] && adj[i, j + 1] && adj[i - 1, j])
                     cornerIndex = new Vector2Int(1, 0);
                  else if (!adj[i + 1, j + 1] && adj[i, j + 1] && adj[i + 1, j])
                     cornerIndex = new Vector2Int(0, 0);


                  else if (!adj[i + 1, j - 1] && adj[i, j - 1] && adj[i + 1, j])
                     cornerIndex = new Vector2Int(0, 1);

                  if (group.singleLayer) {
                     if (cornerIndex.x == -1)
                        change.tileChanges.Add(new TileChange(group.tiles[mainIndex.x, mainIndex.y].tile, position, mainLayer));
                     else
                        change.tileChanges.Add(new TileChange(cornerTiles[cornerIndex.x, cornerIndex.y].tile, position, mainLayer));
                  } else {
                     change.tileChanges.Add(new TileChange(group.tiles[mainIndex.x, mainIndex.y].tile, position, mainLayer));

                     TileBase cornerTile = cornerIndex.x == -1 ? null : cornerTiles[cornerIndex.x, cornerIndex.y].tile;
                     change.tileChanges.Add(new TileChange(cornerTile, position, cornerLayer));
                  }
               }
            }
         }

         foreach (Vector3Int pos in added) {
            change.add(calculateNineFourRearangementChanges(
                group,
                addedTiles,
                layer,
                pos + from - new Vector3Int(size.x / 2, size.y / 2, 0),
                size,
                depth + 1));
         }
         return change;
      }

      public static BoardChange calculateDockChanges (DockGroup group, Layer layer, Vector3 worldPos) {
         BoardChange change = new BoardChange();
         BoundsInt bounds = getBoardBoundsInt();

         Vector3Int targetCenter = DrawBoard.worldToCell(worldPos - new Vector3(group.brushSize.x * 0.5f, group.brushSize.y * 0.5f));
         Vector3Int targetCorner = targetCenter - Vector3Int.one * 4;
         targetCorner.z = 0;
         int n = 2;

         List<Vector3Int> addedTiles = new List<Vector3Int>();

         for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
               addedTiles.Add(new Vector3Int(i + targetCenter.x, j + targetCenter.y, 0));

         //Ensure no edge cases are present
         addedTiles = tilesToRemoveThinParts(layer, addedTiles, 2);

         //Get bounds in which to check for rearangements
         BoundsInt rearrangeBounds = encapsulate(addedTiles);

         //Expand bounds include border and edge tiles to check
         rearrangeBounds.min += new Vector3Int(1, 1, 0) * -2;
         rearrangeBounds.max += new Vector3Int(1, 1, 0) * 2;

         Vector3Int from = rearrangeBounds.min;
         Vector3Int size = rearrangeBounds.size;

         bool[,] adj = new bool[size.x, size.y];

         //Fill adjacency matrix according to relavent tile info
         for (int i = 0; i < size.x; i++) {
            for (int j = 0; j < size.y; j++) {
               Vector3Int index = new Vector3Int(i, j, 0) + from;

               TileBase tile = layer.getTile(index);
               adj[i, j] = tile != null && group.contains(tile);

               // If the position is outside of the map, we will treat this position as if it contains a tile
               if (index.x < bounds.min.x || index.x > bounds.max.x || index.y < bounds.min.y || index.y > bounds.max.y)
                  adj[i, j] = true;
            }
         }

         //Fill in tiles that will be added
         foreach (Vector3Int pos in addedTiles)
            adj[pos.x - from.x, pos.y - from.y] = true;

         //Determine tiles based on the adjacency matrix
         for (int i = 1; i < size.x - 1; i++) {
            for (int j = 1; j < size.y - 1; j++) {
               if (adj[i, j]) {
                  Vector3Int position = from + new Vector3Int(i, j, 0);

                  // If the position is outside the map, continue
                  if (position.x < bounds.min.x || position.x > bounds.max.x || position.y < bounds.min.y || position.y > bounds.max.y)
                     continue;

                  Vector2Int tileIndex = new Vector2Int(1, 1);
                  if (adj[i - 1, j] && !adj[i + 1, j])
                     tileIndex.x = 2;
                  else if (!adj[i - 1, j] && adj[i + 1, j])
                     tileIndex.x = 0;

                  if (adj[i, j - 1] && !adj[i, j + 1])
                     tileIndex.y = 2;
                  else if (!adj[i, j - 1] && adj[i, j + 1])
                     tileIndex.y = 0;

                  //Override for lower corners
                  if (adj[i + 1, j] && adj[i - 1, j] && adj[i, j + 1] && adj[i, j - 1]) {
                     if (!adj[i + 1, j - 1])
                        tileIndex = new Vector2Int(2, 1);
                     else if (!adj[i - 1, j - 1])
                        tileIndex = new Vector2Int(0, 1);
                  }

                  change.tileChanges.Add(new TileChange(group.tiles[tileIndex.x, tileIndex.y].tile, position, layer));
               }
            }
         }

         return change;
      }

      public static BoardChange calculateSeaMountainChanges (SeaMountainGroup group, Vector3 worldPos, Layer layer) {
         BoardChange change = new BoardChange();
         BoundsInt bounds = getBoardBoundsInt();

         Vector3Int targetCenter = DrawBoard.worldToCell(worldPos - new Vector3(group.brushSize.x / 2, group.brushSize.y / 2));
         int n = 5;

         List<Vector3Int> addedTiles = new List<Vector3Int>();

         for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
               addedTiles.Add(new Vector3Int(i + targetCenter.x, j + targetCenter.y, 0));

         //Ensure no edge cases are present
         addedTiles = tilesToRemoveThinParts(layer, addedTiles, 5);
         addedTiles = tilesToRoundOutCorners(layer, addedTiles);

         //Get bounds in which to check for rearangements
         BoundsInt rearrangeBounds = encapsulate(addedTiles);

         //Expand bounds include border and edge tiles to check
         rearrangeBounds.min += new Vector3Int(1, 1, 0) * -6;
         rearrangeBounds.max += new Vector3Int(1, 1, 0) * 6;

         Vector3Int from = rearrangeBounds.min;
         Vector3Int size = rearrangeBounds.size;

         bool[,] adj = new bool[size.x, size.y];

         //Fill adjacency matrix according to relavent tile info
         for (int i = 0; i < size.x; i++) {
            for (int j = 0; j < size.y; j++) {
               Vector3Int index = new Vector3Int(i, j, 0) + from;

               TileBase tile = layer.getTile(index);
               adj[i, j] = tile != null && group.contains(tile);

               // If the position is outside of the map, we will treat this position as if it contains a tile
               if (index.x < bounds.min.x || index.x > bounds.max.x || index.y < bounds.min.y || index.y > bounds.max.y)
                  adj[i, j] = true;
            }
         }

         //Fill in tiles that will be added
         foreach (Vector3Int pos in addedTiles)
            adj[pos.x - from.x, pos.y - from.y] = true;

         //Determine tiles based on the adjacency matrix
         for (int i = 3; i < size.x - 3; i++) {
            for (int j = 3; j < size.y - 3; j++) {
               if (adj[i, j]) {
                  Vector3Int position = from + new Vector3Int(i, j, 0);

                  // If the position is outside the map, continue
                  if (position.x < bounds.min.x || position.x > bounds.max.x || position.y < bounds.min.y || position.y > bounds.max.y)
                     continue;

                  change.tileChanges.Add(new TileChange(
                     group.pickTile(adj, surroundingCount(adj, new Vector2Int(i, j), SidesInt.uniform(3)), i, j),
                     position,
                     layer));
               }
            }
         }

         return change;
      }

      public static BoardChange calculateRiverChanges (RiverGroup group, Vector3 worldPos, Layer layer) {
         BoardChange change = new BoardChange();
         BoundsInt bounds = getBoardBoundsInt();

         Vector3Int targetCenter = DrawBoard.worldToCell(worldPos);

         //Get bounds in which to check for rearangements
         BoundsInt rearrangeBounds = encapsulate(new List<Vector3Int> { targetCenter });

         //Expand bounds include border and edge tiles to check
         rearrangeBounds.min += new Vector3Int(1, 1, 0) * -2;
         rearrangeBounds.max += new Vector3Int(1, 1, 0) * 2;

         Vector3Int from = rearrangeBounds.min;
         Vector3Int size = rearrangeBounds.size;

         bool[,] adj = new bool[size.x, size.y];

         //Fill adjacency matrix according to relavent tile info
         for (int i = 0; i < size.x; i++) {
            for (int j = 0; j < size.y; j++) {
               Vector3Int index = new Vector3Int(i, j, 0) + from;

               TileBase tile = layer.getTile(index);
               adj[i, j] = tile != null && group.contains(tile);

               // If the position is outside of the map, we will treat this position as if it contains a tile
               if (index.x < bounds.min.x || index.x > bounds.max.x || index.y < bounds.min.y || index.y > bounds.max.y)
                  adj[i, j] = true;
            }
         }

         //Fill in tiles that will be added
         adj[targetCenter.x - from.x, targetCenter.y - from.y] = true;


         //Determine tiles based on the adjacency matrix
         for (int i = 1; i < size.x - 1; i++) {
            for (int j = 1; j < size.y - 1; j++) {
               if (adj[i, j]) {
                  Vector3Int position = from + new Vector3Int(i, j, 0);

                  // If the position is outside the map, continue
                  if (position.x < bounds.min.x || position.x > bounds.max.x || position.y < bounds.min.y || position.y > bounds.max.y)
                     continue;

                  change.tileChanges.Add(new TileChange(
                     group.pickTile(adj, surroundingCount(adj, new Vector2Int(i, j), SidesInt.uniform(1)), i, j),
                     position,
                     layer));
               }
            }
         }

         return change;
      }

      public static BoardChange calculateInteriorWallChanges (InteriorWallGroup group, Vector3 worldPos, Layer layer) {
         BoardChange change = new BoardChange();
         BoundsInt bounds = getBoardBoundsInt();

         Vector3Int targetCenter = DrawBoard.worldToCell(worldPos - new Vector3(group.brushSize.x / 2, group.brushSize.y / 2));

         List<Vector3Int> addedTiles = new List<Vector3Int>();

         for (int i = 0; i < group.brushSize.x; i++)
            for (int j = 0; j < group.brushSize.y; j++)
               addedTiles.Add(new Vector3Int(i + targetCenter.x, j + targetCenter.y, 0));

         //Ensure no edge cases are present
         addedTiles = tilesToRemoveThinParts(layer, addedTiles, group.brushSize, false);
         addedTiles = tilesToRoundOutCorners(layer, addedTiles, false);
         addedTiles = tilesForWallEdgeCases(layer, addedTiles, false);

         //Get bounds in which to check for rearangements
         BoundsInt rearrangeBounds = encapsulate(addedTiles);

         //Expand bounds include border and edge tiles to check
         rearrangeBounds.min += new Vector3Int(1, 1, 0) * -8;
         rearrangeBounds.max += new Vector3Int(1, 1, 0) * 8;

         Vector3Int from = rearrangeBounds.min;
         Vector3Int size = rearrangeBounds.size;

         bool[,] adj = new bool[size.x, size.y];

         //Fill adjacency matrix according to relavent tile info
         for (int i = 0; i < size.x; i++) {
            for (int j = 0; j < size.y; j++) {
               Vector3Int index = new Vector3Int(i, j, 0) + from;

               TileBase tile = layer.getTile(index);
               adj[i, j] = tile != null;// && group.contains(tile);

               // If the position is outside of the map, we will treat this position as if it contains a tile
               if (index.x < bounds.min.x || index.x > bounds.max.x || index.y < bounds.min.y || index.y > bounds.max.y)
                  adj[i, j] = true;
            }
         }

         //Fill in tiles that will be added
         foreach (Vector3Int pos in addedTiles)
            adj[pos.x - from.x, pos.y - from.y] = true;

         //Determine tiles based on the adjacency matrix
         for (int i = 4; i < size.x - 4; i++) {
            for (int j = 4; j < size.y - 4; j++) {
               if (adj[i, j]) {
                  Vector3Int position = from + new Vector3Int(i, j, 0);

                  // If the position is outside the map, continue
                  if (position.x < bounds.min.x || position.x > bounds.max.x || position.y < bounds.min.y || position.y > bounds.max.y)
                     continue;

                  change.tileChanges.Add(new TileChange(
                     group.pickTile(adj, surroundingCount(adj, new Vector2Int(i, j), SidesInt.uniform(4)), i, j),
                     position,
                     layer));
               }
            }
         }

         return change;
      }

      public static BoardChange calculateWallChanges (WallGroup group, Layer layer, Vector3 worldPos) {
         BoardChange change = new BoardChange();
         BoundsInt bounds = getBoardBoundsInt();

         Vector3Int targetCenter = DrawBoard.worldToCell(worldPos - new Vector3(group.brushSize.x / 2, group.brushSize.y / 2));
         Vector3Int targetCorner = targetCenter - Vector3Int.one * 4;
         targetCorner.z = 0;
         Vector2Int n = new Vector2Int(1, 2);

         List<Vector3Int> addedTiles = new List<Vector3Int>();

         for (int i = 0; i < n.x; i++)
            for (int j = 0; j < n.y; j++)
               addedTiles.Add(new Vector3Int(i + targetCenter.x, j + targetCenter.y, 0));

         //Ensure no edge cases are present
         addedTiles = tilesToRemoveThinParts(layer, addedTiles, n);

         //Get bounds in which to check for rearangements
         BoundsInt rearrangeBounds = encapsulate(addedTiles);

         //Expand bounds include border and edge tiles to check
         rearrangeBounds.min += new Vector3Int(-2, -4, 0);
         rearrangeBounds.max += new Vector3Int(2, 4, 0);

         Vector3Int from = rearrangeBounds.min;
         Vector3Int size = rearrangeBounds.size;

         bool[,] adj = new bool[size.x, size.y];

         //Fill adjacency matrix according to relavent tile info
         for (int i = 0; i < size.x; i++) {
            for (int j = 0; j < size.y; j++) {
               Vector3Int index = new Vector3Int(i, j, 0) + from;
               TileBase tile = layer.getTile(index);
               adj[i, j] = tile != null && group.contains(tile);

               // If the position is outside of the map, we will treat this position as if it contains a tile
               if (index.x < bounds.min.x || index.x > bounds.max.x || index.y < bounds.min.y || index.y > bounds.max.y)
                  adj[i, j] = true;
            }
         }

         //Fill in tiles that will be added
         foreach (Vector3Int pos in addedTiles)
            adj[pos.x - from.x, pos.y - from.y] = true;

         //Determine tiles based on the adjacency matrix
         for (int i = 1; i < size.x - 1; i++) {
            for (int j = 2; j < size.y - 2; j++) {
               if (adj[i, j]) {
                  Vector3Int position = from + new Vector3Int(i, j, 0);

                  // If the position is outside the map, continue
                  if (position.x < bounds.min.x || position.x > bounds.max.x || position.y < bounds.min.y || position.y > bounds.max.y)
                     continue;

                  Vector2Int tileIndex = new Vector2Int(-1, -1);

                  bool left = adj[i - 1, j] && adj[i - 1, j - 1];
                  bool right = adj[i + 1, j] && adj[i + 1, j - 1];

                  //Bottom stone row
                  if (!adj[i, j - 1]) {
                     if (adj[i - 1, j] && adj[i + 1, j])
                        tileIndex.Set(2, 1);
                     else if (!adj[i - 1, j] && !adj[i + 1, j])
                        tileIndex.Set(1, 0);
                     else if (!adj[i - 1, j] && adj[i + 1, j])
                        tileIndex.Set(0, 1);
                     else if (adj[i - 1, j] && !adj[i + 1, j])
                        tileIndex.Set(3, 1);
                  }

                  //Second row - above the stone row, when there is nothing on top
                  else if (!adj[i, j - 2] && adj[i, j - 1] && !adj[i, j + 1]) {
                     if (left && right)
                        tileIndex.Set(2, 2);
                     else if (!left && !right)
                        tileIndex.Set(1, 4);
                     else if (!left && right)
                        tileIndex.Set(0, 2);
                     else if (left && !right)
                        tileIndex.Set(3, 2);
                  }

                  //Second row - above the stone row, when there are more tiles on top
                  else if (!adj[i, j - 2] && adj[i, j - 1] && adj[i, j + 1]) {
                     if (left && right)
                        tileIndex.Set(3, 0);
                     else if (!left && !right)
                        tileIndex.Set(1, 3);
                     else if (!left && right)
                        tileIndex.Set(2, 3);
                     else if (left && !right)
                        tileIndex.Set(3, 3);
                  }

                  //Third row - filler, placed when expanding
                  else if (adj[i, j - 2] && adj[i, j - 1] && adj[i, j + 1]) {
                     if (left && right)
                        tileIndex.Set(1, 2);
                     else if (!left && !right)
                        tileIndex.Set(1, 3);
                     else if (!left && right)
                        tileIndex.Set(0, 4);
                     else if (left && !right)
                        tileIndex.Set(0, 3);
                  }

                  //Top row
                  else if (!adj[i, j + 1]) {
                     if (left && right)
                        tileIndex.Set(2, 0);
                     else if (!left && !right)
                        tileIndex.Set(1, 4);
                     else if (!left && right)
                        tileIndex.Set(2, 4);
                     else if (left && !right)
                        tileIndex.Set(3, 4);
                  }
                  if (tileIndex.x == -1)
                     throw new Exception("Target index could not be found when placing wall tiles");
                  else
                     change.tileChanges.Add(new TileChange(group.allTiles[tileIndex.x, tileIndex.y].tile, position, layer));
               }
            }
         }

         return change;
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

      /// <summary>
      /// If a user would make input at a given position with the REGULAR tile group selected,
      /// what kind of changes would be made.
      /// </summary>
      /// <param name="group"></param>
      /// <param name="worldPos"></param>
      /// <returns></returns>
      public static BoardChange calculateRegularTileGroupChanges (TileGroup group, Dictionary<string, Layer> layers, Vector3 worldPos) {
         BoardChange change = new BoardChange();
         BoundsInt bounds = getBoardBoundsInt();

         Vector3 originWorldPos = new Vector3(worldPos.x - group.size.x / 2, worldPos.y - group.size.y / 2, 0);
         Vector3Int originCellPos = DrawBoard.worldToCell(originWorldPos);

         for (int i = 0; i < group.tiles.GetLength(0); i++) {
            for (int j = 0; j < group.tiles.GetLength(1); j++) {
               if (group.tiles[i, j] != null) {
                  Vector3Int position = originCellPos + new Vector3Int(i, j, 0);

                  // If the position is outside the map, continue
                  if (position.x < bounds.min.x || position.x > bounds.max.x || position.y < bounds.min.y || position.y > bounds.max.y)
                     continue;

                  Layer layer = null;

                  if (group.tiles[i, j].layer == PaletteData.MountainLayer)
                     layer = layers[group.tiles[i, j].layer].subLayers[Tools.mountainLayer];
                  else if (layers.TryGetValue(group.tiles[i, j].layer, out Layer outlayer))
                     layer = outlayer.subLayers[group.tiles[i, j].subLayer];
                  else
                     Debug.Log($"Layer {group.tiles[i, j].layer} not set up!");

                  change.tileChanges.Add(new TileChange(group.tiles[i, j].tile, position, layer));
               }
            }
         }

         return change;
      }

      public static BoardChange calculateMountainChanges (MountainGroup group, Vector3 worldPos, Layer layer) {
         BoardChange change = new BoardChange();

         SidesInt sides = new SidesInt { left = 2, top = 2, right = 2, bot = 3 };
         Vector2Int brush_n = new Vector2Int(sides.left + sides.right + 1, sides.top + sides.bot + 1);

         Vector3Int targetCenter = DrawBoard.worldToCell(worldPos);
         Vector3Int targetCorner = targetCenter - new Vector3Int(sides.left * 5, sides.bot * 5, 0);

         //Adjacency matrix where the target brush tile is at the center,
         //Tiles of the same group are set to true, null and other tiles false
         //Size of adjacency matrix
         Vector3Int n = new Vector3Int(
             sides.left * 4 + brush_n.x + sides.right * 4,
             sides.bot * 4 + brush_n.y + sides.top * 4,
             0);

         List<Vector3Int> addedTiles = new List<Vector3Int>();

         for (int i = 0; i < brush_n.x; i++)
            for (int j = 0; j < brush_n.y; j++)
               if (group.outerTiles[i, j] != null)
                  addedTiles.Add(new Vector3Int(i + targetCorner.x + sides.left * 4, j + targetCorner.y + sides.bot * 4, 0));

         change.add(calculateMountainRearangementChanges(group, addedTiles, layer, targetCorner, n, sides, 0));

         return change;
      }

      public static BoardChange calculateMountainRearangementChanges (
          MountainGroup group, List<Vector3Int> addedTiles, Layer layer, Vector3Int from, Vector3Int size, SidesInt sides, int depth) {
         if (depth > 1000)
            throw new Exception("Potential infinite recursion!");

         BoardChange change = new BoardChange();
         BoundsInt bounds = getBoardBoundsInt();

         //Adjacency matrix where the target brush tile is at the center,
         //Tiles of the same group are set to true, null and other tiles false
         //Size of adjacency matrix
         bool[,] adj = new bool[size.x, size.y];

         //Fill adjacency matrix according to relavent tile info
         for (int i = 0; i < size.x; i++) {
            for (int j = 0; j < size.y; j++) {
               Vector3Int index = new Vector3Int(i, j, 0) + from;
               TileBase tile = layer.getTile(index);
               adj[i, j] = tile != null;// && group.Contains(tile);
            }
         }

         foreach (Vector3Int pos in addedTiles) {
            if (pos.x >= from.x && pos.y >= from.y && pos.x < from.x + size.x && pos.y < from.y + size.y)
               adj[pos.x - from.x, pos.y - from.y] = true;
         }

         //--------------------------------------------
         //Ensure no edge cases are present
         List<Vector3Int> added = new List<Vector3Int>();
         int lastAddedCount = -1;

         int infiniteCounter = 0;
         while (lastAddedCount != added.Count) {
            infiniteCounter++;
            if (infiniteCounter > 10000)
               throw new System.Exception("Infinite cycle!");

            lastAddedCount = added.Count;
            var aj = adj.Clone() as bool[,];


            for (int i = 0; i < size.x; i++) {
               for (int j = 0; j < size.y; j++) {
                  //2x2 corners
                  if (i > 0 && i < size.x - 1 && j > 0 && j < size.y - 1) {
                     //bottom left
                     if (!aj[i, j] && !aj[i - 1, j] && !aj[i, j - 1] &&
                         aj[i - 1, j + 1] && aj[i, j + 1] && aj[i + 1, j + 1] && aj[i + 1, j] && aj[i + 1, j - 1])
                        added.Add(new Vector3Int(i, j, 0));

                     //bottom right
                     if (!aj[i, j] && !aj[i + 1, j] && !aj[i, j - 1] &&
                         aj[i + 1, j + 1] && aj[i, j + 1] && aj[i - 1, j + 1] && aj[i - 1, j] && aj[i - 1, j - 1])
                        added.Add(new Vector3Int(i, j, 0));

                     //top right
                     if (!aj[i, j] && !aj[i + 1, j] && !aj[i, j + 1] &&
                         aj[i + 1, j - 1] && aj[i, j - 1] && aj[i - 1, j - 1] && aj[i - 1, j] && aj[i - 1, j + 1])
                        added.Add(new Vector3Int(i, j, 0));

                     //top left
                     if (!aj[i, j] && !aj[i - 1, j] && !aj[i, j + 1] &&
                         aj[i - 1, j - 1] && aj[i, j - 1] && aj[i + 1, j - 1] && aj[i + 1, j] && aj[i + 1, j + 1])
                        added.Add(new Vector3Int(i, j, 0));
                  }

                  //short diagonals
                  if (i < size.x - 4 && j < size.y - 4) {
                     if (!aj[i, j] && aj[i + 1, j + 1] && aj[i, j + 1] && aj[i + 1, j] &&
                         (!aj[i + 2, j + 2] || !aj[i + 3, j + 3] || !aj[i + 4, j + 4]))
                        added.Add(new Vector3Int(i, j, 0));

                  }
                  if (i > 3 && j > 3) {
                     if (!aj[i, j] && aj[i - 1, j - 1] && aj[i, j - 1] && aj[i - 1, j] &&
                         (!aj[i - 2, j - 2] || !aj[i - 3, j - 3] || !aj[i - 4, j - 4]))
                        added.Add(new Vector3Int(i, j, 0));
                  }
                  if (i < size.x - 4 && j > 3) {
                     if (!aj[i, j] && aj[i + 1, j - 1] && aj[i, j - 1] && aj[i + 1, j] &&
                         (!aj[i + 2, j - 2] || !aj[i + 3, j - 3] || !aj[i + 4, j - 4]))
                        added.Add(new Vector3Int(i, j, 0));
                  }
                  if (i > 3 && j < size.y - 4) {
                     if (!aj[i, j] && aj[i - 1, j + 1] && aj[i, j + 1] && aj[i - 1, j] &&
                         (!aj[i - 2, j + 2] || !aj[i - 3, j + 3] || !aj[i - 4, j + 4]))
                        added.Add(new Vector3Int(i, j, 0));

                  }
                  //up-diagonals
                  if (i > 0 && j < size.y - 5) {
                     if (!aj[i, j] && aj[i - 1, j + 1] && aj[i, j + 1] && aj[i - 1, j] &&
                         (!aj[i - 1, j + 2] || !aj[i - 1, j + 3] || !aj[i - 1, j + 4] || !aj[i - 1, j + 5]))
                        added.Add(new Vector3Int(i, j, 0));

                  }
                  if (i < size.x - 1 && j < size.y - 5) {
                     if (!aj[i, j] && aj[i + 1, j + 1] && aj[i, j + 1] && aj[i + 1, j] &&
                         (!aj[i + 1, j + 2] || !aj[i + 1, j + 3] || !aj[i + 1, j + 4] || !aj[i + 1, j + 5]))
                        added.Add(new Vector3Int(i, j, 0));

                  }
                  if (i > 1 && j < size.y - 5) {
                     if (!aj[i, j] && aj[i - 1, j + 1] && aj[i, j + 1] && aj[i - 1, j] &&
                         (!aj[i - 2, j + 2] || !aj[i - 2, j + 3] || !aj[i - 2, j + 4] || !aj[i - 2, j + 5]))
                        added.Add(new Vector3Int(i, j, 0));

                  }
                  if (i < size.x - 2 && j < size.y - 5) {
                     if (!aj[i, j] && aj[i + 1, j + 1] && aj[i, j + 1] && aj[i + 1, j] &&
                         (!aj[i + 2, j + 2] || !aj[i + 2, j + 3] || !aj[i + 2, j + 4] || !aj[i + 2, j + 5]))
                        added.Add(new Vector3Int(i, j, 0));

                  }
                  if (i > 2 && j < size.y - 5) {
                     if (!aj[i, j] && aj[i - 1, j + 1] && aj[i, j + 1] && aj[i - 1, j] &&
                         (!aj[i - 2, j + 2] || !aj[i - 3, j + 3] || !aj[i - 3, j + 4] || !aj[i - 3, j + 5]))
                        added.Add(new Vector3Int(i, j, 0));

                  }
                  if (i < size.x - 3 && j < size.y - 5) {
                     if (!aj[i, j] && aj[i + 1, j + 1] && aj[i, j + 1] && aj[i + 1, j] &&
                         (!aj[i + 2, j + 2] || !aj[i + 3, j + 3] || !aj[i + 3, j + 4] || !aj[i + 3, j + 5]))
                        added.Add(new Vector3Int(i, j, 0));

                  }

                  //single tile, bound by 3 sides
                  //bound from bottom
                  if (i < size.x - 1 && i > 0 && j > 0 && j < size.y - 1) {
                     if (!aj[i, j] && aj[i - 1, j] && aj[i + 1, j] && aj[i, j - 1] && (aj[i - 1, j + 1] || aj[i + 1, j + 1]))
                        added.Add(new Vector3Int(i, j, 0));
                  }
                  //bound from bottom left
                  if (i > 0 && i < size.x - 1 && j > 0 && j < size.y - 1) {
                     if (!aj[i, j] && aj[i - 1, j] && aj[i, j + 1] && aj[i, j - 1] && adj[i + 1, j - 1])
                        added.Add(new Vector3Int(i, j, 0));
                  }
                  //bound from right
                  if (i > 0 && i < size.x - 1 && j > 0 && j < size.y - 1) {
                     if (!aj[i, j] && aj[i + 1, j] && aj[i, j + 1] && aj[i, j - 1] && adj[i - 1, j - 1])
                        added.Add(new Vector3Int(i, j, 0));
                  }


                  //width
                  if (i < size.x - 4 && j > 0) {
                     if (!aj[i, j] && aj[i, j - 1] && aj[i + 1, j] && (!aj[i + 2, j] || !aj[i + 3, j] || !aj[i + 4, j]))
                        added.Add(new Vector3Int(i, j, 0));
                  }
                  if (i > 3 && j > 0) {
                     if (!aj[i, j] && aj[i, j - 1] && aj[i - 1, j] && (!aj[i - 2, j] || !aj[i - 3, j] || !aj[i - 4, j]))
                        added.Add(new Vector3Int(i, j, 0));
                  }
               }
            }

            for (int i = lastAddedCount; i < added.Count; i++) {
               adj[added[i].x, added[i].y] = true;
            }
         }

         addedTiles.AddRange(added.Select(p => p + from));

         for (int i = 0; i < size.x; i++) {
            for (int j = 0; j < size.y; j++) {
               Vector3Int index = new Vector3Int(i, j, 0) + from;

               // If the position is outside of the map, we will treat this position as if it contains a tile
               if (index.x < bounds.min.x || index.x > bounds.max.x || index.y < bounds.min.y || index.y > bounds.max.y)
                  adj[i, j] = true;
            }
         }

         //Determine tiles based on the adjacency matrix
         for (int i = sides.left; i < size.x - sides.right; i++) {
            for (int j = sides.bot; j < size.y - sides.top; j++) {
               if (adj[i, j]) {
                  Vector3Int position = from + new Vector3Int(i, j, 0);

                  // If the position is outside the map, continue
                  if (position.x < bounds.min.x || position.x > bounds.max.x || position.y < bounds.min.y || position.y > bounds.max.y)
                     continue;

                  SidesInt sur = surroundingCount(adj, new Vector2Int(i, j), new SidesInt {
                     left = 5,
                     right = 5,
                     top = 5,
                     bot = 5
                  });

                  change.tileChanges.Add(new TileChange(group.pickTile(adj, sur, i, j), position, layer));
               }
            }
         }

         foreach (Vector3Int pos in added) {
            change.add(calculateMountainRearangementChanges(
                group,
                addedTiles,
                layer,
                pos + from - new Vector3Int(sides.left * 3, sides.bot * 3, 0),
                size,
                sides,
                depth + 1));
         }
         return change;
      }

      private static List<Vector3Int> tilesForWallEdgeCases (Layer layer, List<Vector3Int> placed, bool ofBoundsIsFilled = true) {
         HashSet<Vector3Int> result = new HashSet<Vector3Int>();
         foreach (var p in placed)
            result.Add(p);

         BoundsInt bounds = getBoardBoundsInt();

         Func<Vector3Int, bool> has = (p) => {
            return result.Contains(p) || layer.hasTile(p);
         };

         if (ofBoundsIsFilled) {
            has = (p) => {
               return result.Contains(p) || layer.hasTile(p) ||
                  p.x < bounds.min.x || p.x > bounds.max.x || p.y < bounds.min.y || p.y > bounds.max.y;
            };
         }

         Func<Vector3Int, Vector3Int[]> getNeighbours = (p) => {
            return new Vector3Int[] { p + Vector3Int.up, p + Vector3Int.down, p + Vector3Int.left, p + Vector3Int.right,
             p + new Vector3Int(1, 1, 0), p + new Vector3Int(-1, 1, 0), p + new Vector3Int(1, -1, 0), p + new Vector3Int(-1, -1, 0)};
         };

         bool[,] aj = new bool[3, 3];

         Queue<Vector3Int> queue = new Queue<Vector3Int>(result.SelectMany(p => getNeighbours(p)));

         for (int i = 0; i < 100000; i++) {
            if (queue.Count == 0) {
               return result.ToList();
            }

            Vector3Int p = queue.Dequeue();

            //Check if already added
            if (result.Contains(p))
               continue;

            //Check if not empty
            if (has(p))
               continue;

            bool add = false;

            for (int ii = 0; ii < 3; ii++)
               for (int jj = 0; jj < 3; jj++)
                  aj[ii, jj] = has(p + new Vector3Int(ii - 1, jj - 1, 0));

            if (aj[1, 2] && aj[2, 2] && aj[2, 1] && aj[2, 0] && !aj[1, 1] && !aj[1, 0])
               add = true;

            else if (aj[1, 2] && aj[0, 2] && aj[0, 1] && aj[0, 0] && !aj[1, 1] && !aj[1, 0])
               add = true;

            else if (aj[1, 2] && aj[1, 0] && aj[2, 1] && aj[0, 1])
               add = true;

            if (add) {
               result.Add(p);
               foreach (var n in getNeighbours(p))
                  if (!has(n))
                     queue.Enqueue(n);
            }
         }

         Debug.LogError("Exceded maximum iteration count. Potential infinite cycle.");
         return result.ToList();
      }

      private static List<Vector3Int> tilesToRoundOutCorners (Layer layer, List<Vector3Int> placed, bool ofBoundsIsFilled = true) {
         HashSet<Vector3Int> result = new HashSet<Vector3Int>();
         foreach (var p in placed)
            result.Add(p);

         BoundsInt bounds = getBoardBoundsInt();

         Func<Vector3Int, bool> has = (p) => {
            return result.Contains(p) || layer.hasTile(p);
         };

         if (ofBoundsIsFilled) {
            has = (p) => {
               return result.Contains(p) || layer.hasTile(p) ||
                  p.x < bounds.min.x || p.x > bounds.max.x || p.y < bounds.min.y || p.y > bounds.max.y;
            };
         }

         Func<Vector3Int, Vector3Int[]> getNeighbours = (p) => {
            return new Vector3Int[] { p + Vector3Int.up, p + Vector3Int.down, p + Vector3Int.left, p + Vector3Int.right,
             p + new Vector3Int(1, 1, 0), p + new Vector3Int(-1, 1, 0), p + new Vector3Int(1, -1, 0), p + new Vector3Int(-1, -1, 0)};
         };

         bool[,] aj = new bool[3, 3];

         Queue<Vector3Int> queue = new Queue<Vector3Int>(result.SelectMany(p => getNeighbours(p)));

         for (int i = 0; i < 100000; i++) {
            if (queue.Count == 0) {
               return result.ToList();
            }

            Vector3Int p = queue.Dequeue();

            //Check if already added
            if (result.Contains(p))
               continue;

            //Check if not empty
            if (has(p))
               continue;

            bool add = false;

            for (int ii = 0; ii < 3; ii++)
               for (int jj = 0; jj < 3; jj++)
                  aj[ii, jj] = has(p + new Vector3Int(ii - 1, jj - 1, 0));

            if (!aj[0, 1] && !aj[1, 0] &&
             aj[0, 2] && aj[1, 2] && aj[2, 2] && aj[2, 1] && aj[2, 0])
               add = true;

            //bottom right
            else if (!aj[2, 1] && !aj[1, 0] &&
                aj[2, 2] && aj[1, 2] && aj[0, 2] && aj[0, 1] && aj[0, 0])
               add = true;

            //top right
            else if (!aj[2, 1] && !aj[1, 2] &&
                aj[2, 0] && aj[1, 0] && aj[0, 0] && aj[0, 1] && aj[0, 2])
               add = true;

            //top left
            else if (!aj[0, 1] && !aj[0, 1] &&
                aj[0, 0] && aj[1, 0] && aj[2, 0] && aj[2, 1] && aj[2, 2])
               add = true;

            //single tile, bound by 3 sides
            //bound from bottom
            else if (aj[0, 1] && aj[2, 1] && aj[1, 0])
               add = true;
            //bound from bottom left
            else if (aj[0, 1] && aj[1, 2] && aj[1, 0] && aj[2, 0])
               add = true;

            //bound from right
            else if (aj[2, 1] && aj[1, 2] && aj[1, 0] && aj[0, 0])
               add = true;

            //bound from top
            else if (aj[0, 1] && aj[2, 1] && aj[1, 2])
               add = true;

            if (add) {
               result.Add(p);
               foreach (var n in getNeighbours(p))
                  if (!has(n))
                     queue.Enqueue(n);
            }
         }

         Debug.LogError("Exceded maximum iteration count. Potential infinite cycle.");
         return result.ToList();
      }

      /// <summary>
      /// Finds the aditional positions that must be placed to make sure no 'thin' parts are present
      /// </summary>
      /// <param name="layer">The layer which to check</param>
      /// <param name="placed">Aditional tiles that will be placed inside the layer</param>
      /// <returns>The given 'placed' positions, and new positions to remove thin parts </returns>
      private static List<Vector3Int> tilesToRemoveThinParts (Layer layer, List<Vector3Int> placed, int minThiccness, bool ofBoundsIsFilled = true) {
         return tilesToRemoveThinParts(layer, placed, new Vector2Int(minThiccness, minThiccness), ofBoundsIsFilled);
      }

      /// <summary>
      /// Finds the aditional positions that must be placed to make sure no 'thin' parts are present
      /// </summary>
      /// <param name="layer">The layer which to check</param>
      /// <param name="placed">Aditional tiles that will be placed inside the layer</param>
      /// <returns>The given 'placed' positions, and new positions to remove thin parts </returns>
      private static List<Vector3Int> tilesToRemoveThinParts (Layer layer, List<Vector3Int> placed, Vector2Int minThiccness, bool ofBoundsIsFilled = true) {
         Vector2Int minDistance = minThiccness + Vector2Int.one;

         HashSet<Vector3Int> result = new HashSet<Vector3Int>();
         foreach (var p in placed)
            result.Add(p);

         Func<Vector3Int, Vector3Int[]> getNeighbours = (p) => {
            return new Vector3Int[] { p + Vector3Int.up, p + Vector3Int.down, p + Vector3Int.left, p + Vector3Int.right };
         };

         BoundsInt bounds = getBoardBoundsInt();

         Func<Vector3Int, bool> has = (p) => {
            return result.Contains(p) || layer.hasTile(p);
         };

         if (ofBoundsIsFilled) {
            has = (p) => {
               return result.Contains(p) || layer.hasTile(p) ||
                  p.x < bounds.min.x || p.x > bounds.max.x || p.y < bounds.min.y || p.y > bounds.max.y;
            };
         }

         Queue<Vector3Int> queue = new Queue<Vector3Int>(result.SelectMany(p => getNeighbours(p)));

         for (int j = 0; j < 100000; j++) {
            if (queue.Count == 0)
               return result.ToList();

            Vector3Int p = queue.Dequeue();

            //Check if already added
            if (result.Contains(p))
               continue;

            //Check if not empty
            if (has(p))
               continue;

            //Check straight to all 4 sides
            int dst = distanceToEmpty(has, p, Vector2Int.left, minDistance.x);
            if (dst > 1 && dst < minDistance.x) {
               result.Add(p);
               foreach (var n in getNeighbours(p))
                  queue.Enqueue(n);

               continue;
            }

            dst = distanceToEmpty(has, p, Vector2Int.right, minDistance.x);
            if (dst > 1 && dst < minDistance.x) {
               result.Add(p);
               foreach (var n in getNeighbours(p))
                  queue.Enqueue(n);

               continue;
            }

            dst = distanceToEmpty(has, p, Vector2Int.up, minDistance.y);
            if (dst > 1 && dst < minDistance.y) {
               result.Add(p);
               foreach (var n in getNeighbours(p))
                  queue.Enqueue(n);

               continue;
            }

            dst = distanceToEmpty(has, p, Vector2Int.down, minDistance.y);
            if (dst > 1 && dst < minDistance.y) {
               result.Add(p);
               foreach (var n in getNeighbours(p))
                  queue.Enqueue(n);

               continue;
            }

            //Check diagonals
            //top right
            if (has(p + Vector3Int.up) && has(p + Vector3Int.right) && !distanceToEmptyAtLeast(has, p, new Vector3Int(1, 1, 0), minDistance)) {

               result.Add(p);
               foreach (var n in getNeighbours(p))
                  queue.Enqueue(n);

               continue;
            }
            //top left
            if (has(p + Vector3Int.up) && has(p + Vector3Int.left) && !distanceToEmptyAtLeast(has, p, new Vector3Int(-1, 1, 0), minDistance)) {
               result.Add(p);
               foreach (var n in getNeighbours(p))
                  queue.Enqueue(n);

               continue;
            }
            //bot right
            if (has(p + Vector3Int.down) && has(p + Vector3Int.right) && !distanceToEmptyAtLeast(has, p, new Vector3Int(1, -1, 0), minDistance)) {
               result.Add(p);
               foreach (var n in getNeighbours(p))
                  queue.Enqueue(n);

               continue;
            }
            //bot left
            if (has(p + Vector3Int.down) && has(p + Vector3Int.left) && !distanceToEmptyAtLeast(has, p, new Vector3Int(-1, -1, 0), minDistance)) {
               result.Add(p);
               foreach (var n in getNeighbours(p))
                  queue.Enqueue(n);

               continue;
            }
         }

         Debug.LogError("Exceded maximum iteration count. Potential infinite cycle.");
         return result.ToList();
      }


      /// <summary>
      /// Searches for a path to an empty cell, calculating the distance.
      /// </summary>
      /// <param name="hasTile"></param>
      /// <param name="from"></param>
      /// <param name="direction">Direction to seatch</param>
      /// <param name="maxDepth"></param>
      /// <returns>Returns true, if the distance to an empty cell is at least maxDepth</returns>
      private static bool distanceToEmptyAtLeast (Func<Vector3Int, bool> hasTile, Vector3Int from, Vector3Int direction, Vector2Int maxDepth) {
         return
             distanceToEmpty(hasTile, from, Vector3Int.right * direction.x, direction, maxDepth) == maxDepth.x &&
             distanceToEmpty(hasTile, from, Vector3Int.up * direction.y, direction, maxDepth) == maxDepth.y;
      }

      /// <summary>
      /// Checks the distance to an empty cell. 
      /// </summary>
      /// <param name="hasTile">Funtion to check if a tile is present in a given place</param>
      /// <param name="from">From where to look</param>
      /// <param name="direction"></param>
      /// <param name="maxDepth">Maximum length of path to check</param>
      /// <returns>Maximum length of path, but no bigger than maxDepth</returns>
      private static int distanceToEmpty (Func<Vector3Int, bool> hasTile, Vector3Int from, Vector2Int direction, int maxDepth) {
         Vector3Int next = from + (Vector3Int) direction;
         if (!hasTile(next) || maxDepth == 1)
            return 1;
         else
            return 1 + distanceToEmpty(hasTile, next, direction, maxDepth - 1);
      }

      /// <summary>
      /// Checks the distance to an empty cell.
      /// </summary>
      /// <param name="hasTile">Funtion to check if a tile is present in a given place</param>
      /// <param name="from">From where to look</param>
      /// <param name="maxDepth">Maximum length of path to check</param>
      /// <returns></returns>
      private static int distanceToEmpty (Func<Vector3Int, bool> hasTile, Vector3Int from, Vector3Int straightDir, Vector3Int diagonalDir, Vector2Int maxDepth) {
         //Check if maxDepth to the direction of straightDir is 1
         if (Math.Abs(straightDir.x) * maxDepth.x == 1 || Math.Abs(straightDir.y) * maxDepth.y == 1)
            return 1;

         if (maxDepth.x > 1 && maxDepth.y > 1) {
            if (!hasTile(from + diagonalDir))
               return 1;
            return 1 + Mathf.Min(
                distanceToEmpty(hasTile, from + straightDir, straightDir, diagonalDir, maxDepth - new Vector2Int(Math.Abs(straightDir.x), Math.Abs(straightDir.y))),
                distanceToEmpty(hasTile, from + diagonalDir, straightDir, diagonalDir, maxDepth - Vector2Int.one));
         }

         if (!hasTile(from + straightDir))
            return 1;

         return
             1 + distanceToEmpty(hasTile, from + straightDir, straightDir, diagonalDir, maxDepth - new Vector2Int(Math.Abs(straightDir.x), Math.Abs(straightDir.y)));
      }

      /// <summary>
      /// Given a matrix and a starting point, calculates how many cells are 'true' in a row
      /// in all four sides.
      /// </summary>
      /// <param name="matrix"></param>
      /// <param name="from"></param>
      /// <param name="maxDepth">Maximum depth to search</param>
      /// <returns></returns>
      public static SidesInt surroundingCount (bool[,] matrix, Vector2Int from, SidesInt maxDepth) {
         maxDepth = new SidesInt {
            left = Mathf.Min(from.x, maxDepth.left),
            right = Mathf.Min(matrix.GetLength(0) - from.x - 1, maxDepth.right),
            bot = Mathf.Min(from.y, maxDepth.bot),
            top = Mathf.Min(matrix.GetLength(1) - from.y - 1, maxDepth.top)
         };

         SidesInt result = new SidesInt();

         for (int i = 1; i <= maxDepth.left; i++) {
            if (matrix[from.x - i, from.y])
               result.left++;
            else
               break;
         }

         for (int i = 1; i <= maxDepth.right; i++) {
            if (matrix[from.x + i, from.y])
               result.right++;
            else
               break;
         }

         for (int i = 1; i <= maxDepth.top; i++) {
            if (matrix[from.x, from.y + i])
               result.top++;
            else
               break;
         }

         for (int i = 1; i <= maxDepth.bot; i++) {
            if (matrix[from.x, from.y - i])
               result.bot++;
            else
               break;
         }
         return result;
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
