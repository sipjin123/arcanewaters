using UnityEngine;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class DebugObjMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable
   {
      private SpriteOutline outline;

      private void Awake () {
         outline = GetComponentInChildren<SpriteOutline>();
      }

      public void dataFieldChanged (DataField field) {
         if (field.k.CompareTo(DataField.DEBUG_OBJECT_ID) == 0) {
            if (field.tryGetIntValue(out int debugId)) {
               D.debug("Selected debugId: " + debugId);
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