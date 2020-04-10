using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Tilemaps;

public class MapChunk : MonoBehaviour
{
   #region Public Variables

   #endregion

   public void initialize (RectInt bounds) {
      _bounds = bounds;

      TilemapCollider2D[] colliders = GetComponentsInChildren<TilemapCollider2D>();
      if (colliders != null) {
         _colliders = new List<TilemapCollider2D>(colliders);
      }

      Tilemap[] tilemaps = GetComponentsInChildren<Tilemap>();
      if (tilemaps != null) {
         _tilemaps = new List<Tilemap>(tilemaps);
      }
   }

   public bool contains (Vector3Int cellPos) {
      return _bounds.Contains(new Vector2Int(cellPos.x, cellPos.y));
   }

   public List<TilemapCollider2D> getTilemapColliders () {
      return _colliders;
   }

   public List<Tilemap> getTilemaps () {
      return _tilemaps;
   }

   #region Private Variables

   // The list of tilemap colliders
   protected List<TilemapCollider2D> _colliders = new List<TilemapCollider2D>();

   // The list of tilemaps
   protected List<Tilemap> _tilemaps = new List<Tilemap>();

   // The bounds of the chunk, in cell units
   protected RectInt _bounds;

   #endregion
}
