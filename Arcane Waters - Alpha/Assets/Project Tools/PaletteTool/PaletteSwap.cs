using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEditor;

public class PaletteSwap : MonoBehaviour {
   #region Public Variables

   // Name of palette to download from database
   public string paletteName = "";

   #endregion

   private void Awake () {
      _material = null;
      try {
         _material = GetComponent<SpriteRenderer>().material;
      } catch {
         _material = GetComponent<Image>().material;
      }

      if (_material.HasProperty(SHADER_TEXTURE_KEY)) {
         generatePaletteSwapTexture();
      } else {
         terminatePaletteSwapping();
      }
   }

   public void generatePaletteSwapTexture () {
      if (paletteName.Length == 0) {
         terminatePaletteSwapping();
         return;
      }

      Texture2D paletteTex = PaletteSwapManager.generateTexture2D(paletteName);
      if (paletteTex == null) {
         Invoke("generatePaletteSwapTexture", 1.0f);
         return;
      }

      if (_material.HasProperty(SHADER_TEXTURE_KEY)) {
         _material.SetTexture(SHADER_TEXTURE_KEY, paletteTex);
      } else {
         D.error("Failed to assign palette to material. Destroying palette swap script");
         terminatePaletteSwapping();
      }
   }

   private void terminatePaletteSwapping () {
      Destroy(this);
   }

   #region Private Variables

   // Shader property name of palette's texture
   private const string SHADER_TEXTURE_KEY = "_Palette";

   // Material which has correct property key
   private Material _material = null;

   #endregion
}
