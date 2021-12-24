using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class DirectionalCombatCollider : MonoBehaviour {
   #region Public Variables

   // The entity that we are a collider for
   public NetEntity assignedEntity;

   // What settings we want for the collider, for each direction that the entity is facing, indexed by Direction
   public List<ColliderData> directionalColliderData;

   [System.Serializable]
   public struct ColliderData {
      public Vector2 offset;
      public Vector2 size;
   }

   #endregion

   private void Awake () {
      _collider = GetComponent<BoxCollider2D>();
   }

   private void Update () {
      if (assignedEntity == null) {
         return;
      }

      if (assignedEntity.facing != _lastFacing) {
         _lastFacing = assignedEntity.facing;

         ColliderData colliderData = directionalColliderData[(int) _lastFacing - 1];
         _collider.offset = colliderData.offset;
         _collider.size = colliderData.size;
      }
   }

   #region Private Variables

   // The collider we are adjusting the size of
   private BoxCollider2D _collider;

   // The last direction that our tracked entity was facing
   private Direction _lastFacing = Direction.West;

   #endregion
}
