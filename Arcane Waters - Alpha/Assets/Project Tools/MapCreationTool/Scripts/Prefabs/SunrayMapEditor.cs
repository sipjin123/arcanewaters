using UnityEngine;

namespace MapCreationTool
{
   public class SunrayMapEditor : MapEditorPrefab
   {
      #region Public Variables

      // Main sprite renderer which renders the ray sprite
      public SpriteRenderer rayRenderer;

      #endregion

      public override void createdInPalette () {
         rayRenderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
      }

      public override void createdForPreview () {
         rayRenderer.maskInteraction = SpriteMaskInteraction.None;
      }

      public override void placedInEditor () {
         rayRenderer.maskInteraction = SpriteMaskInteraction.None;
      }
   }
}
