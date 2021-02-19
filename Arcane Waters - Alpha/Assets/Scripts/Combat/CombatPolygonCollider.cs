using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

[RequireComponent(typeof(PolygonCollider2D))]
public class CombatPolygonCollider : GenericCombatCollider {
   #region Public Variables

   #endregion

   protected override void Awake () {
      base.Awake();

      _collider = GetComponent<PolygonCollider2D>();
   }

   protected override void updateCollider () {
      if (_collider == null) {
         D.logOnce($"Combat Collider for gameObject {gameObject} is null.");

         return;
      }

      int count = spriteRenderer.sprite.GetPhysicsShape(0, _spriteShapePoints);
      
      if (count > 0) {
         _collider.SetPath(0, _spriteShapePoints);
      }
   }

   #region Private Variables

   // The polygon collider
   private PolygonCollider2D _collider;

   // A list containing the points of the sprite physics shape
   protected List<Vector2> _spriteShapePoints = new List<Vector2>();

   #endregion
}
