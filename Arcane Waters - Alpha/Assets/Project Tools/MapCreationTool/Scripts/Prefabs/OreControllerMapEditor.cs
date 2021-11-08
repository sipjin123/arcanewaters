using UnityEngine;
using MapCreationTool.Serialization;
using UnityEngine.UI;

namespace MapCreationTool {
   public class OreControllerMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable {
      // The text gui reference
      public Text oreCountText;
      public Text oreTimerText;

      private SpriteOutline outline;

      private void Awake () {
         outline = GetComponentInChildren<SpriteOutline>();
      }

      public void dataFieldChanged (DataField field) {
         if (field.k.CompareTo(DataField.ORE_RESPAWN_TIME_DATA_KEY) == 0) {
            if (field.tryGetFloatValue(out float oreTimer)) {
               oreTimerText.text = oreTimer.ToString();
            }
         }
         if (field.k.CompareTo(DataField.ORE_TOTAL_ACTIVE_DATA_KEY) == 0) {
            if (field.tryGetIntValue(out int oreCount)) {
               oreCountText.text = oreCount.ToString();
            }
         }
      }

      public override void createdForPreview () {
         setDefaultSprite();
      }

      public override void createdInPalette () {
         setDefaultSprite();
      }

      public void setDefaultSprite () {
      }

      public void setHighlight (bool hovered, bool selected, bool deleting) {
         setOutlineHighlight(outline, hovered, selected, deleting);
      }
   }
}