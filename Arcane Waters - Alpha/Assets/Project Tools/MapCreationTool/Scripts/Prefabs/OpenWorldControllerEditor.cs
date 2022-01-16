using UnityEngine;
using MapCreationTool.Serialization;
using UnityEngine.UI;

namespace MapCreationTool {
   public class OpenWorldControllerEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable {
      // The text gui reference
      public Text maxEnemyCountText;

      // Outline reference
      private SpriteOutline outline;

      private void Awake () {
         outline = GetComponentInChildren<SpriteOutline>();
      }

      public void dataFieldChanged (DataField field) {
         if (field.k.CompareTo(DataField.OPEN_WORLD_ENEMY_COUNT) == 0) {
            if (field.tryGetFloatValue(out float maxEnemyCount)) {
               maxEnemyCountText.text = maxEnemyCount.ToString();
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