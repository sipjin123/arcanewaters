using UnityEngine;
using System;

[Serializable]
public class HatClipMaskGenerationSetting {
   #region Public Variables

   // The texture to process
   public Texture2D texture;

   // Additional clipping masks. Useful to tweak the results from the tool
   public Texture2D extraClipMaskTexture;

   #endregion
}
