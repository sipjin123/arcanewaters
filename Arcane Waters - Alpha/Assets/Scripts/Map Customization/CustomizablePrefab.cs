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
         return unappliedChanges.isLocalPositionSet();
      }

      public void revertUnappliedChanges () {
         transform.localPosition = customizedState.localPosition;

         unappliedChanges.clearAll();
      }

      public void revertToMapEditor () {
         transform.localPosition = mapEditorState.localPosition;

         customizedState = mapEditorState;

         unappliedChanges.clearAll();
      }

      public void submitUnappliedChanges () {
         if (unappliedChanges.isLocalPositionSet()) {
            customizedState.localPosition = unappliedChanges.localPosition;
         }

         revertUnappliedChanges();
      }

      #region Private Variables

      // Main Sprite Renderer of the prefab
      private SpriteRenderer _ren;

      #endregion
   }
}
