﻿using UnityEngine;
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

   public void recolor (string paletteNames, int slot = 0, bool logData = false) {
      if (paletteNames == null) {
         return;
      }

      string[] paletteNamesArray = Item.parseItmPalette(paletteNames);
      List<string> palettesToUse = new List<string>();
      for (int i = 0; i < paletteNamesArray.Length; i++) {
         paletteNamesArray[i] = paletteNamesArray[i].Trim();
         if (paletteNamesArray[i] != "") {
            if (logData) {
               D.editorLog("-> Palette Name Added: {" + paletteNamesArray[i] + "}", Color.yellow);
            }
            palettesToUse.Add(paletteNamesArray[i]);
         }
      }

      if (palettesToUse.Count > 4) {
         D.warning("Currently more than 4 palettes are not supported! Only 4 first palettes will be used");
      }

      if (palettesToUse.Count != 0) {
         // Fill list to four palettes, to make sure that old one gets reset, once this function got called with smaller number of palettes
         while (palettesToUse.Count < 4) {
            palettesToUse.Add("");
         }
      }

      checkMaterialAvailability();
      _palettes = Item.parseItmPalette(palettesToUse.ToArray());

      getMaterial().SetTexture(slot == 0 ? "_Palette" : "_Palette2", PaletteSwapManager.getPaletteTexture(palettesToUse.ToArray()));
   }

   public void setNewMaterial (Material oldMaterial) {
      Material newMaterial = new Material(oldMaterial);

      SpriteRenderer renderer = getSpriteRenderer();

      if (renderer != null) {
         renderer.material = newMaterial;
         return;
      }

      Image image = getImage();

      if (image != null) {
         image.material = newMaterial;
      }
   }

   public Material getMaterial () {
      SpriteRenderer renderer = getSpriteRenderer();
      
      if (renderer != null) {
         return renderer.material;
      }

      Image image = getImage();

      if (image != null) {
         return image.material;
      }

      return null;
   }

   private SpriteRenderer getSpriteRenderer () {
      if (_spriteRenderer == null) {
         TryGetComponent<SpriteRenderer>(out _spriteRenderer);
      }

      return _spriteRenderer;
   }

   private Image getImage () {
      if (_image == null) {
         TryGetComponent<Image>(out _image);
      }

      return _image;
   }

   public string getPalettes () {
      return _palettes;
   }

   public void checkMaterialAvailability () {
      Material oldMaterial = getMaterial();
      if (oldMaterial == null || !oldMaterial.HasProperty("_Palette") || !oldMaterial.HasProperty("_Palette2")) {
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
   protected string _palettes = "";

   // Our Sprite Renderer or Image, whichever we have
   protected SpriteRenderer _spriteRenderer;
   protected Image _image;

   #endregion
}
