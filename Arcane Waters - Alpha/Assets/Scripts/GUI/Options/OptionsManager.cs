using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class OptionsManager : MonoBehaviour {
   #region Public Variables

   // Player pref key for gui scale
   public const string PREF_GUI_SCALE = "pref_gui_scale";

   // Player pref key for minimap scale
   public const string PREF_MINIMAP_SCALE = "pref_minimap_scale";

   // Player Pref key for vsync
   public const string VSYNC_COUNT_KEY = "vsync_count";

   // The reference to the UI Parent Canvas
   public Canvas mainGameCanvas;

   // The reference to the UI Minimap Transform
   public RectTransform minimapTransform;

   // The GUI scale
   public static float GUIScale { get; private set; }

   // The minimap scale
   public static float minimapScale { get; private set; }

   // The vsync count
   public static int vsyncCount { get; private set; }

   // Self
   public static OptionsManager self;

   #endregion

   private void Awake () {
      self = this;

      GUIScale = PlayerPrefs.GetFloat(PREF_GUI_SCALE, 100);
      minimapScale = PlayerPrefs.GetFloat(PREF_MINIMAP_SCALE, 100);
      vsyncCount = PlayerPrefs.GetInt(VSYNC_COUNT_KEY, 0);      
   }

   private void Start () {
      CameraManager.self.resolutionChanged += applyGUIScale;

      applyCurrentSettings();
   }

   private void applyCurrentSettings () {
      applyGUIScale();
      applyMinimapScale();
      applyVsyncCount();
   }

   private void applyVsyncCount () {
      QualitySettings.vSyncCount = vsyncCount;
   }

   private void applyGUIScale () {
      mainGameCanvas.scaleFactor = (GUIScale / 100.0f) * getConstantUIScalingFactor();
   }

   private void applyMinimapScale () {
      float scale = minimapScale / 100.0f;
      minimapTransform.localScale = Vector3.one * scale;
   }

   public static void setVsync (bool isVsyncEnabled) {
      vsyncCount = isVsyncEnabled ? 1 : 0;
      PlayerPrefs.SetInt(VSYNC_COUNT_KEY, vsyncCount);
      self.applyVsyncCount();
   }

   public static void setGUIScale (float scale) {
      PlayerPrefs.SetFloat(PREF_GUI_SCALE, scale);
      GUIScale = scale;
      self.applyGUIScale();
   }

   public static void setMinimapScale (float scale) {
      PlayerPrefs.SetFloat(PREF_MINIMAP_SCALE, scale);
      minimapScale = scale;
      self.applyMinimapScale();
   }

   public static float getConstantUIScalingFactor () {
      if (Screen.width >= ScreenSettingsManager.largeScreenWidth && Screen.height >= ScreenSettingsManager.largeScreenHeight) {
         return 2f;
      }

      return 1f;
   }

   #region Private Variables

   #endregion
}
