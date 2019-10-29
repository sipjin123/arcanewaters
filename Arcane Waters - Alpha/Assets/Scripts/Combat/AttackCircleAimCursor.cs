using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AttackCircleAimCursor : MonoBehaviour
{
   #region Public Variables

   #endregion

   private void Awake () {
      _entitiesUnderCursor = new HashSet<NetEntity>();
      _collider = GetComponent<Collider2D>();
   }

   public void OnTriggerEnter2D (Collider2D other) {
      // Check if the other object is a Net Entity
      NetEntity hitEntity = other.GetComponent<NetEntity>();

      if (hitEntity != null) {
         // Add the entity to the entities under the cursor
         _entitiesUnderCursor.Add(hitEntity);
      }
   }

   public void OnTriggerExit2D (Collider2D other) {
      // Check if the other object is a Net Entity
      NetEntity hitEntity = other.GetComponent<NetEntity>();

      if (hitEntity != null) {
         // Remove the entity from the entities under the cursor
         _entitiesUnderCursor.Remove(hitEntity);
      }
   }

   public void activate () {
      _collider.enabled = true;
   }

   public void deactivate () {
      _collider.enabled = false;
      _entitiesUnderCursor.Clear();
   }

   public bool isCursorHoveringOver(NetEntity entity) {
      if (_entitiesUnderCursor.Contains(entity)) {
         return true;
      } else {
         return false;
      }
   }

   #region Private Variables

   // The entities currently under this cursor
   private HashSet<NetEntity> _entitiesUnderCursor;

   // The collider2D component
   private Collider2D _collider;

   #endregion
}
