using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.Networking;

public class BrightnessManager : MonoBehaviour {
   #region Public Variables

   // The darkness value for rain
   public static float RAIN_DARKNESS = .60f;

   // The brightness Camera effect
   public BrightnessFX mainBrightnessFX;
   public BrightnessFX battleBrightnessFX;

   // Self
   public static BrightnessManager self;

   #endregion

   void Awake () {
      self = this;
   }

   void Update () {
      // Keep adjusting the brightness level based on the moving intensity setting
      BrightnessFX.ChangeBrightness = mainBrightnessFX.getCurrentIntensity();
   }

   public void setNewTargetIntensity (float newTargetIntensity, float transitionTime) {
      mainBrightnessFX.setTargetIntensity(newTargetIntensity, transitionTime);
      battleBrightnessFX.setTargetIntensity(newTargetIntensity, transitionTime);
   }

   #region Private Variables

   #endregion
}
