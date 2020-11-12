using UnityEngine;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class RandomEnemySpawnerMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable
   {
      private SpriteOutline outline;

      private void Awake () {
         outline = GetComponentInChildren<SpriteOutline>();
      }

      public void dataFieldChanged (DataField field) {
         // Do not need any data
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