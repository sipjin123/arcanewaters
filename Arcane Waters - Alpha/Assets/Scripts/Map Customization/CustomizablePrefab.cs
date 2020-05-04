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

      // Changes that the user is currently making and were not yet submitted to the server
      public PrefabChanges unappliedChanges;

      // Last confirmed placement position of the prefab
      public Vector2 anchorLocalPosition;

      #endregion

      private void OnEnable () {
         _ren = GetComponent<SpriteRenderer>();
      }

      public void setSemiTransparent (bool semiTransparent) {
         _ren.color = new Color(_ren.color.r, _ren.color.g, _ren.color.b, semiTransparent ? SEMI_TRANSPARENT_ALPHA : 1f);
      }

      public void clearChanges () {
         unappliedChanges.localPosition = null;
      }

      public void revertChanges () {
         transform.localPosition = anchorLocalPosition;

         unappliedChanges.localPosition = null;
      }

      public void submitChanges () {
         if (unappliedChanges.localPosition != null) {
            anchorLocalPosition = unappliedChanges.localPosition.Value;
         }

         clearChanges();
      }

      #region Private Variables

      // Main Sprite Renderer of the prefab
      private SpriteRenderer _ren;

      #endregion
   }
}
