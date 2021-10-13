using UnityEngine;
using System;

[Serializable]
public class HatClipMaskGenerationSetting {
   #region Public Variables

   // Should this setting be processed
   public bool isEnabled = true;

   // The texture to process
   public Texture2D texture;

   // Additional clipping masks. Useful to tweak the results from the tool
   public Texture2D extraClipMaskTexture;

   // Third clipping masks layer
   public Texture2D paintedClipMaskTexture;

   #endregion
}
