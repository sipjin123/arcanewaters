using UnityEngine;
using MapCreationTool.Serialization;
using System.Linq;

namespace MapCreationTool
{
   public class ShipMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable
   {
      // Components
      private SpriteRenderer ren;
      private SpriteOutline outline;

      // Ship sprite indicating this is not assigned
      public Sprite unassignedShipSprite;

      private void Awake () {
         ren = GetComponentInChildren<SpriteRenderer>();
         outline = GetComponentInChildren<SpriteOutline>();
      }

      public void dataFieldChanged (DataField field) {
         // Do Nothing, ship setup is randomly generated in game per biome
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
