using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

[CreateAssetMenu(fileName = "HatClipMaskGenerationSettings", menuName = "Generate Clip Masks for Hats")]
public class HatClipMaskGenerationSettings : ScriptableObject {
   #region Public Variables

   // The visible horizon height (percentage)
   [Range(0.0f, 1.0f)]
   [Tooltip("The additional horizontal clip line height. Everything above the line is clipped. 0 is the bottom of the sprite. 1 is the top.")]
   public float clipLineHeight;

   // The list of textures to be processed
   public Texture2D[] textures;

   #endregion

   #region Private Variables

   #endregion
}
