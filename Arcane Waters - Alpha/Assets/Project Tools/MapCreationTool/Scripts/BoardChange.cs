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
      public bool isSelectionChange { get; set; }

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
         BoundsInt boundsInt = DrawBoard.getBoardBoundsInt();
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

      public static IEnumerable<Vector3Int> getRectTilePositions (Vector3Int p1, Vector3Int p2) {
         BoundsInt bounds = DrawBoard.getBoardBoundsInt();

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
      public bool select { get; set; }

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
