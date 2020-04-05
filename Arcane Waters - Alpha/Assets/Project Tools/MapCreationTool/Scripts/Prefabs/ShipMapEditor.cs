using UnityEngine;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class ShipMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable
   {
      private SpriteRenderer ren;
      private SpriteOutline outline;

      private void Awake () {
         ren = GetComponentInChildren<SpriteRenderer>();
         outline = GetComponentInChildren<SpriteOutline>();
      }

      public void dataFieldChanged (DataField field) {
         if (field.k.CompareTo(DataField.SHIP_DATA_KEY) == 0) {
            Texture2D texture = ShipManager.instance.getShipTexture(field.intValue);

            if (texture != null) {
               ren.sprite = ImageManager.getSprites(texture)[0];
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
         Texture2D texture = ShipManager.instance.getFirstShipTexture();

         if (texture != null) {
            ren.sprite = ImageManager.getSprites(texture)[0];
         }
      }

      public void setHighlight (bool hovered, bool selected, bool deleting) {
         setOutlineHighlight(outline, hovered, selected, deleting);
      }
   }
}
