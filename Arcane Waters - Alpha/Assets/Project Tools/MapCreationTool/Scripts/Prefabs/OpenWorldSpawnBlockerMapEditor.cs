using UnityEngine;
using MapCreationTool.Serialization;
using UnityEngine.UI;

namespace MapCreationTool
{
   public class OpenWorldSpawnBlockerMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable {
      // Outline reference
      private SpriteOutline outline;

      // The object that will be adjusted to determine block proximity
      public GameObject objectScaler;

      private void Awake () {
         outline = GetComponentInChildren<SpriteOutline>();
      }

      public void dataFieldChanged (DataField field) {
         if (field.k.CompareTo(DataField.SPAWN_BLOCK_SIZE_X_KEY) == 0) {
            if (field.tryGetFloatValue(out float scale)) {
               transform.localScale = new Vector2(scale, transform.localScale.y);
            }
         }
         if (field.k.CompareTo(DataField.SPAWN_BLOCK_SIZE_Y_KEY) == 0) {
            if (field.tryGetFloatValue(out float scale)) {
               transform.localScale = new Vector2(transform.localScale.x, scale);
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