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
      if (!GetComponent<SpriteRenderer>().material.HasProperty(SHADER_MATERIAL_ID_KEY)) {
         Material[] materials = new Material[GetComponent<SpriteRenderer>().materials.Length + 1];
         GetComponent<SpriteRenderer>().materials.CopyTo(materials, 0);
         materials[GetComponent<SpriteRenderer>().materials.Length - 1] = Material.Instantiate(PaletteSwapManager.self.paletteSwapMaterial);

         GetComponent<SpriteRenderer>().materials = materials;
      }
      GetComponent<SpriteRenderer>().materials[GetComponent<SpriteRenderer>().materials.Length - 1].SetInt(SHADER_MATERIAL_ID_KEY, PaletteSwapManager.generatePaletteId(paletteName));
   }

   public void generatePaletteSwapTexture () {
      GetComponent<SpriteRenderer>().material.SetTexture(SHADER_TEXTURE_KEY, PaletteSwapManager.generateTexture2D(paletteName));
   }

   #region Private Variables

   // Shader property name of palette's texture
   private const string SHADER_TEXTURE_KEY = "_Palette";

   // Shader property name of palette's material id
   private const string SHADER_MATERIAL_ID_KEY = "_PaletteId";

   #endregion
}
