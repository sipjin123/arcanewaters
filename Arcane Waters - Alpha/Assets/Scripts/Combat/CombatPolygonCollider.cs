using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

[RequireComponent(typeof(PolygonCollider2D))]
public class CombatPolygonCollider : MonoBehaviour {
   #region Public Variables

   // The sprite renderer that will determine the shape of the collider
   public SpriteRenderer spriteRenderer;

   #endregion

   private void Awake () {      
      if (spriteRenderer == null) {
         D.error($"CombatPolygonCollider in game object {gameObject.name} hasn't been assigned a SpriteRenderer. The shape of the collider won't be updated.");
         enabled = false;
         return;
      }

      _collider = GetComponent<PolygonCollider2D>();
   }

   private void Update () {
      if (_lastSprite != spriteRenderer.sprite) {
         regenerateColliderShape();
      }
   }

   private void regenerateColliderShape () {
      List<Vector2> spriteShape = new List<Vector2>();
      int count = spriteRenderer.sprite.GetPhysicsShape(0, spriteShape);
      
      if (count > 0) {
         _collider.SetPath(0, spriteShape);
      }

      _lastSprite = spriteRenderer.sprite;
   }

   #region Private Variables

   // The polygon collider
   private PolygonCollider2D _collider;

   // The sprite that's used for setting the current shape of the collider
   private Sprite _lastSprite;

   #endregion
}
