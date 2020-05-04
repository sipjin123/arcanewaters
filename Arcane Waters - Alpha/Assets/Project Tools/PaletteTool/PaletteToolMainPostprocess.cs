using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PaletteToolMainPostprocess : MonoBehaviour {
   #region Public Variables

   // Fullscreen postprocess shader limiting image to palette colors only
   public Shader shader;

   // Texture of palette that limits colors on the screen
   public Texture2D paletteTexture;

   #endregion

   private void Awake () {
      _material = new Material(shader);
      if (_material.HasProperty(SHADER_TEXTURE_KEY)) {
         _material.SetTexture(SHADER_TEXTURE_KEY, paletteTexture);
      }
   }

   void OnRenderImage (RenderTexture sourceTexture, RenderTexture destTexture) {
      if (_material != null) {
         Graphics.Blit(sourceTexture, destTexture, _material);
      } else {
         Graphics.Blit(sourceTexture, destTexture);
      }
   }

   #region Private Variables

   // Instantiated material that will be used as postprocess
   private Material _material;

   // Shader property name of palette's texture
   private const string SHADER_TEXTURE_KEY = "_Palette";

   #endregion
}
