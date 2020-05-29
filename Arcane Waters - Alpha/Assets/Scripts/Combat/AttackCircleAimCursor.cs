using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AttackCircleAimCursor : MonoBehaviour
{
   #region Public Variables

   // The renderer of the cursor
   public SpriteRenderer cursorRenderer;

   // The renderer of the cursor aura
   public SpriteRenderer auraRenderer;

   #endregion

   private void Awake () {
      _entitiesUnderCursor = new HashSet<NetEntity>();
      _collider = GetComponent<Collider2D>();
      _animator = GetComponent<Animator>();
   }

   public void OnTriggerEnter2D (Collider2D other) {
      // Check if the other object is a Net Entity
      NetEntity hitEntity = other.GetComponent<NetEntity>();

      if (hitEntity != null && Global.player != null && Global.player.instanceId == hitEntity.instanceId) {
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
      gameObject.SetActive(true);
      _collider.enabled = true;
   }

   public void deactivate () {
      _collider.enabled = false;
      _entitiesUnderCursor.Clear();
      gameObject.SetActive(false);
   }

   public void update(Color c, float speed) {
      // Set the color
      cursorRenderer.color = c;
      auraRenderer.color = new Color(c.r, c.g, c.b, 1f);      

      // Set the animation speed
      _animator.SetFloat("speed", speed);

      // Get the current area
      Area area = AreaManager.self.getArea(Global.player.areaKey);

      // Set the z position of the aura, above the sea and below the land
      if (area != null) {
         Util.setZ(auraRenderer.transform, area.waterZ - 0.01f);
      }
   }

   public Color getColor () {
      return cursorRenderer.color;
   }

   public bool isActive () {
      return gameObject.activeSelf;
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

   // The animator component
   private Animator _animator;

   #endregion
}
