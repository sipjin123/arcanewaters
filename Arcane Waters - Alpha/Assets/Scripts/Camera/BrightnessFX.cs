using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;

public class BrightnessFX : CameraFilterPack_Color_BrightContrastSaturation {
   #region Public Variables

   #endregion

   void Awake () {
      // Start at full brightness
      _currentIntensity = this.maxIntensity;
   }

   public float getCurrentIntensity () {
      return _currentIntensity;
   }

   #region Private Variables

   #endregion
}
