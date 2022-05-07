using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class WhirlpoolMapEditor : MapEditorPrefab, IPrefabDataListener, IHighlightable
   {
      #region Public Variables

      // Scale of this object when displayed in the palette panel
      public const float PALETTE_SCALE = 5f;

      // The starting scale of the model if its not fixed to one
      public float startingScale = 1;

      // A reference to the sprite renderer displaying the whirlpool
      public SpriteRenderer whirlpoolSprite;

      #endregion

      private void Awake () {
         _outline = GetComponentInChildren<SpriteOutline>();
         startingScale = transform.localScale.x;
      }

      public void dataFieldChanged (DataField field) {
         // Adjust the scale of this building when spawned in the drawing board
         if (transform.parent.GetComponent<Palette>() != null) {
            transform.localScale = new Vector3(startingScale / PALETTE_SCALE, startingScale / PALETTE_SCALE, 1);
         } else {
            transform.localScale = new Vector3(startingScale, startingScale, startingScale);
         }

         if (field.k.CompareTo(DataField.WHIRLPOOL_RADIUS_KEY) == 0) {
            try {
               float newRadius = float.Parse(field.v);
               whirlpoolSprite.transform.localScale = Vector3.one * (newRadius / SPRITE_RADIUS);
            } catch {

            }
            
         } else if (field.k.CompareTo(DataField.WHIRLPOOL_CLOCKWISE_KEY) == 0) {
            try {
               bool newValue = bool.Parse(field.v);
               whirlpoolSprite.flipX = !newValue;
            } catch {

            }
         }
      }

      public void setHighlight (bool hovered, bool selected, bool deleting) {
         setOutlineHighlight(_outline, hovered, selected, deleting);
      }

      #region Private Variables

      // The outline
      private SpriteOutline _outline;

      // The radius needed to match the sprite for this whirlpool
      private const float SPRITE_RADIUS = 0.62f;

      #endregion
   }

}
