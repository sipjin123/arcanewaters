using System.Collections.Generic;
using UnityEngine;

namespace MapCreationTool
{
   public class Selection
   {
      public HashSet<Vector3Int> tiles { get; private set; }
      public HashSet<PlacedPrefab> prefabs { get; private set; }

      public Selection () {
         tiles = new HashSet<Vector3Int>();
         prefabs = new HashSet<PlacedPrefab>();
      }

      public void add (Vector3Int tile) {
         if (!tiles.Contains(tile)) {
            tiles.Add(tile);
         }
      }

      public void add (IEnumerable<Vector3Int> tiles) {
         foreach (Vector3Int tile in tiles) {
            if (!this.tiles.Contains(tile)) {
               this.tiles.Add(tile);
            }
         }
      }

      public void add (PlacedPrefab prefab) {
         if (!prefabs.Contains(prefab)) {
            prefabs.Add(prefab);
         }
      }

      public void add (IEnumerable<PlacedPrefab> prefabs) {
         foreach (PlacedPrefab prefab in prefabs) {
            if (!this.prefabs.Contains(prefab)) {
               this.prefabs.Add(prefab);
            }
         }
      }

      public bool contains (PlacedPrefab prefab) {
         return prefabs.Contains(prefab);
      }

      public bool contains (Vector3Int tile) {
         return tiles.Contains(tile);
      }

      public void remove (Vector3Int tile) {
         if (tiles.Contains(tile)) {
            tiles.Remove(tile);
         }
      }

      public void remove (IEnumerable<Vector3Int> tiles) {
         foreach (Vector3Int tile in tiles) {
            if (this.tiles.Contains(tile)) {
               this.tiles.Remove(tile);
            }
         }
      }

      public void remove (PlacedPrefab prefab) {
         if (prefabs.Contains(prefab)) {
            prefabs.Remove(prefab);
         }
      }

      public void remove (IEnumerable<PlacedPrefab> prefabs) {
         foreach (PlacedPrefab prefab in prefabs) {
            if (this.prefabs.Contains(prefab)) {
               this.prefabs.Remove(prefab);
            }
         }
      }

      public void clear () {
         tiles.Clear();
         prefabs.Clear();
      }

      public bool empty
      {
         get { return tiles.Count == 0 && prefabs.Count == 0; }
      }
   }
}