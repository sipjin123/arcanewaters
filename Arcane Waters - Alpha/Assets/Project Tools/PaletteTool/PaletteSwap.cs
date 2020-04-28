using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PaletteSwap : MonoBehaviour {
   #region Public Variables

   // Name of palette to download from database
   public string paletteName;

   #endregion

   private void Awake () {
      if (!GetComponent<SpriteRenderer>().material.HasProperty(SHADER_TEXTURE_KEY)) {
         GetComponent<SpriteRenderer>().material = Material.Instantiate(PaletteSwapManager.self.paletteSwapMaterial);
      }
      GetComponent<SpriteRenderer>().material.SetTexture(SHADER_TEXTURE_KEY, PaletteSwapManager.generateTexture2D(paletteName));
   }

   public void generatePaletteSwapTexture () {
      GetComponent<SpriteRenderer>().material.SetTexture(SHADER_TEXTURE_KEY, PaletteSwapManager.generateTexture2D(paletteName));
   }

   #region Private Variables

   // Shader property name of palette's texture
   private const string SHADER_TEXTURE_KEY = "_Palette";

   #endregion
}
