using UnityEngine;

namespace MapCustomization
{
   public class CustomizablePrefab : ClientMonoBehaviour
   {
      #region Public Variables

      // Color of alpha of the renderer, when the prefab is set to 'semi-transparent'
      public const float SEMI_TRANSPARENT_ALPHA = 0.5f;

      // Bounds in which the mouse cursor has to be to interact with the prefab
      public Collider2D interactionCollider;

      // Size of prefab in tiles
      public Vector2Int size = Vector2Int.one;

      // State of the prefab that is set in map editor
      public PrefabState mapEditorState;

      // State of the prefab after server-confirmed customizations are applied
      public PrefabState customizedState;

      // Changes of state during user-end customization
      public PrefabState unappliedChanges;

      #endregion

      private void OnEnable () {
         _ren = GetComponent<SpriteRenderer>();
      }

      public void setSemiTransparent (bool semiTransparent) {
         _ren.color = new Color(_ren.color.r, _ren.color.g, _ren.color.b, semiTransparent ? SEMI_TRANSPARENT_ALPHA : 1f);
      }

      public bool anyUnappliedState () {
         return unappliedChanges.isLocalPositionSet() || unappliedChanges.created;
      }

      public void revertUnappliedChanges () {
         transform.localPosition = customizedState.localPosition;
         if (!mapEditorState.created && !customizedState.created) {
            MapCustomizationManager.removeTracked(this);
            Destroy(gameObject);
         } else {
            unappliedChanges.clearAll();
         }
      }

      public void revertToMapEditor () {
         if (!mapEditorState.created) {
            D.error("Cannot revert changes to map editor, because prefab was not created in map editor");
            return;
         }

         transform.localPosition = mapEditorState.localPosition;

         customizedState = mapEditorState;

         unappliedChanges.clearAll();
      }

      public void submitUnappliedChanges () {
         if (unappliedChanges.isLocalPositionSet()) {
            customizedState.localPosition = unappliedChanges.localPosition;
            customizedState.created = customizedState.created || unappliedChanges.created;
         }

         revertUnappliedChanges();
      }

      #region Private Variables

      // Main Sprite Renderer of the prefab
      private SpriteRenderer _ren;

      #endregion
   }
}
