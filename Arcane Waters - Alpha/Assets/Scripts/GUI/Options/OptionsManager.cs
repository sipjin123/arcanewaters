﻿using UnityEngine;
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

   // Player Pref key for constant sprint
   public const string PREF_SPRINT_CONSTANTLY = "SPRINT_CONSTANTLY";

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

      GUIScale = PlayerPrefs.GetInt(PREF_GUI_SCALE, DEFAULT_GUI_SCALE);
      minimapScale = PlayerPrefs.GetInt(PREF_MINIMAP_SCALE, DEFAULT_MINIMAP_SCALE);
      vsyncCount = PlayerPrefs.GetInt(VSYNC_COUNT_KEY, 0);

      GUIScale = Util.getInRangeOrDefault(GUIScale, MIN_GUI_SCALE, MAX_GUI_SCALE, DEFAULT_GUI_SCALE);
      minimapScale = Util.getInRangeOrDefault(minimapScale, MIN_MINIMAP_SCALE, MAX_MINIMAP_SCALE, DEFAULT_MINIMAP_SCALE);
      PlayerPrefs.SetInt(PREF_GUI_SCALE, Mathf.RoundToInt(GUIScale));
      PlayerPrefs.SetInt(PREF_MINIMAP_SCALE, Mathf.RoundToInt(minimapScale));

      Global.sprintConstantly = PlayerPrefs.GetInt(OptionsManager.PREF_SPRINT_CONSTANTLY, 0) == 1 ? true : false;
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

   public static bool isVsyncEnabled () {
      return vsyncCount != 0;
   }

   public static void setVsync (bool isVsyncEnabled) {
      vsyncCount = isVsyncEnabled ? 1 : 0;
      PlayerPrefs.SetInt(VSYNC_COUNT_KEY, vsyncCount);
      self.applyVsyncCount();
   }

   public static void setGUIScale (int scale) {
      scale = Mathf.RoundToInt(Util.getInRangeOrDefault(scale, MIN_GUI_SCALE, MAX_GUI_SCALE, DEFAULT_GUI_SCALE));

      PlayerPrefs.SetInt(PREF_GUI_SCALE, scale);
      GUIScale = scale;
      self.applyGUIScale();
   }

   public static void setMinimapScale (int scale) {
      scale = Mathf.RoundToInt(Util.getInRangeOrDefault(scale, MIN_MINIMAP_SCALE, MAX_MINIMAP_SCALE, DEFAULT_MINIMAP_SCALE));

      PlayerPrefs.SetInt(PREF_MINIMAP_SCALE, scale);
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

   // The minimum valid GUI scale
   private const int MIN_GUI_SCALE = 50;

   // The default GUI scale
   private const int DEFAULT_GUI_SCALE = 100;

   // The maximum valid GUI scale
   private const int MAX_GUI_SCALE = 200;

   // The minimum valid minimap scale
   private const int MIN_MINIMAP_SCALE = 50;

   // The default minimap scale
   private const int DEFAULT_MINIMAP_SCALE = 100;

   // The maximum valid minimap scale
   private const int MAX_MINIMAP_SCALE = 200;

   #endregion
}
