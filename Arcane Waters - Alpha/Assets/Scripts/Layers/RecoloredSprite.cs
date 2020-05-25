using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class RecoloredSprite : MonoBehaviour {
   #region Public Variables

   #endregion

   public virtual void Awake () {
      _image = GetComponent<Image>();
      _spriteRenderer = GetComponent<SpriteRenderer>();

      // Look up the Sprite based on the type of component we use
      if (_image) {
         _sprite = _image.sprite;
      } else if (_spriteRenderer) {
         _sprite = _spriteRenderer.sprite;
      }
   }

   public void recolor (string paletteName1, string paletteName2) {
      if (paletteName1 == "") {
         recolor(paletteName2);
         return;
      }
      if (paletteName2 == "") {
         recolor(paletteName1);
         return;
      }
      // Two palettes should be probably used only for armor (or similar sprites which have "two parts")
      _palette1 = paletteName1;
      _palette2 = paletteName2;

      checkMaterialAvailability();
      getMaterial().SetTexture("_Palette", PaletteSwapManager.generateTexture2D(paletteName1));
      getMaterial().SetTexture("_Palette2", PaletteSwapManager.generateTexture2D(paletteName2));
   }

   public void recolor (string paletteName) {
      _palette1 = paletteName;
      checkMaterialAvailability();
      getMaterial().SetTexture("_Palette", PaletteSwapManager.generateTexture2D(paletteName));
   }

   public void setNewMaterial (Material oldMaterial) {
      Material newMaterial = new Material(oldMaterial);

      if (_spriteRenderer == null && _image == null) {
         _image = GetComponent<Image>();
         _spriteRenderer = GetComponent<SpriteRenderer>();
      }

      if (_spriteRenderer != null) {
         _spriteRenderer.material = newMaterial;
      } else if (_image != null) {
         _image.material = newMaterial;
      }
   }

   public Material getMaterial () {
      if (_spriteRenderer == null && _image == null) {
         _image = GetComponent<Image>();
         _spriteRenderer = GetComponent<SpriteRenderer>();
      }

      if (_spriteRenderer != null) {
         return _spriteRenderer.material;
      } else if (_image != null) {
         return _image.material;
      }

      return null;
   }

   public string getPalette1 () {
      return _palette1;
   }

   public string getPalette2 () {
      return _palette2;
   }

   private void checkMaterialAvailability () {
      if (getMaterial() == null) {
         // We get a different material for GUI Images
         Material material = (_image != null) ?
            MaterialManager.self.getGUIMaterial() : MaterialManager.self.get();

         // Assign either GUI or standard material for palette swap
         setNewMaterial(material);
      }
   }

   #region Private Variables

   // Our Sprite
   protected Sprite _sprite;

   // Our colors
   protected string _palette1 = "";
   protected string _palette2 = "";

   // Our Sprite Renderer or Image, whichever we have
   protected SpriteRenderer _spriteRenderer;
   protected Image _image;

   #endregion
}
