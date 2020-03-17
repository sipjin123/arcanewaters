using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Vines : MonoBehaviour {
   #region Public Variables

   #endregion

   private void Awake () {
      _collider = GetComponent<BoxCollider2D>();
      SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
      Bounds bounds = _collider.bounds;

      foreach (SpriteRenderer renderer in renderers) {
         bounds.Encapsulate(renderer.bounds);
      }

      _collider.size = bounds.size;
      _collider.offset = bounds.center;
   }

   private void OnTriggerEnter2D (Collider2D other) {
      BodyEntity player = other.transform.GetComponent<BodyEntity>();

      if (player == null) {
         return;
      }

      player.isClimbing = true;
   }

   private void OnTriggerExit2D (Collider2D other) {
      BodyEntity player = other.transform.GetComponent<BodyEntity>();

      if (player == null) {
         return;
      }

      player.isClimbing = false;
   }

   #region Private Variables

   // The trigger collider
   private BoxCollider2D _collider;

   #endregion
}
