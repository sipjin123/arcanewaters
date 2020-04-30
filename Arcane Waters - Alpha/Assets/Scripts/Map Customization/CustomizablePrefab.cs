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
      public Changes unappliedChanges;

      #endregion

      private void OnEnable () {
         _ren = GetComponent<SpriteRenderer>();
      }

      public void setSemiTransparent (bool semiTransparent) {
         _ren.color = new Color(_ren.color.r, _ren.color.g, _ren.color.b, semiTransparent ? SEMI_TRANSPARENT_ALPHA : 1f);
      }

      public void clearChanges () {
         unappliedChanges.translation = Vector2.zero;
      }

      public void revertChanges () {

      }

      public void submitChanges () {
         clearChanges();
      }

      #region Private Variables

      // Main Sprite Renderer of the prefab
      private SpriteRenderer _ren;

      #endregion

      public struct Changes
      {
         public Vector2 translation;
      }
   }
}
