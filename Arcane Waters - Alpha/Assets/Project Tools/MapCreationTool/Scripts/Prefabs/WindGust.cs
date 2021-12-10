using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class WindGust : MapEditorPrefab, IHighlightable, IPrefabDataListener
   {
      #region Public Variables

      // Visual we display while gust is in the palette
      public GameObject paletteVisual = null;

      // Visual effect we show in the game
      public WindGustVFX windGustVFX = null;

      // Sprite we use to highlight selection
      public SpriteRenderer spriteHighlight = null;

      // Collider we use for selecting the wind gust
      public BoxCollider2D selectionCollider = null;

      #endregion

      public void dataFieldChanged (DataField field) {
         // If we are inside the palette, lets not apply any of the data fields visually
         if (paletteVisual.activeSelf) {
            return;
         }

         try {
            if (field.k.CompareTo(DataField.WIND_GUST_SIZE_X_KEY) == 0) {
               if (field.tryGetFloatValue(out float w)) {
                  float width = Mathf.Clamp(w, 1, 1000);
                  setSize(new Vector2(width, _size.y));
               }
            } else if (field.k.CompareTo(DataField.WIND_GUST_SIZE_Y_KEY) == 0) {
               if (field.tryGetFloatValue(out float h)) {
                  float height = Mathf.Clamp(h, 1, 1000);
                  setSize(new Vector2(_size.x, height));
               }
            } else if (field.k.CompareTo(DataField.WIND_GUST_ROTATION_KEY) == 0) {
               if (field.tryGetFloatValue(out float rotation)) {
                  transform.rotation = Quaternion.Euler(0, 0, rotation);
               }
            } else if (field.k.CompareTo(DataField.WIND_GUST_STRENGTH_KEY) == 0) {
               if (field.tryGetFloatValue(out float strength)) {
                  windGustVFX.setStrength(Mathf.Clamp(strength, 0.1f, 1f));
               }
            }
         } catch { }
      }

      private void setSize (Vector2 size) {
         _size = size;

         selectionCollider.size = size;
         spriteHighlight.size = size;
         windGustVFX.setSize(size);
      }

      public override void createdInPalette () {
         paletteVisual.SetActive(true);
         windGustVFX.gameObject.SetActive(false);
         setSize(new Vector2(3f, 3f));
      }

      public override void createdForPreview () {
         paletteVisual.SetActive(false);
         windGustVFX.gameObject.SetActive(true);
      }

      public override void placedInEditor () {
         paletteVisual.SetActive(false);
         windGustVFX.gameObject.SetActive(true);
      }

      public override void setHovered (bool hovered) {
         base.setHovered(hovered);
      }

      public override void setSelected (bool selected) {
         base.setSelected(selected);
      }

      public void setHighlight (bool hovered, bool selected, bool deleting) {
         setSpriteHighlight(spriteHighlight, hovered, selected, deleting);
      }

      #region Private Variables

      // Size of the wind gust
      private Vector2 _size = new Vector2(1f, 1f);

      #endregion
   }

}
