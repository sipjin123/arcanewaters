﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ScreenSettingsManager : MonoBehaviour {

   #region Public Variables

   // The screen resolution width
   public static int Width { get; private set; }

   // The screen resolution height
   public static int Height { get; private set; }

   // The screen fullscreen mode
   public static FullScreenMode FullScreenMode { get; private set; }

   // Boolean that is true if FullScreenMode isn't Windowed
   public static bool IsFullScreen { get { return FullScreenMode == FullScreenMode.ExclusiveFullScreen; } }

   // The minimum screen resolution width
   public static int MIN_WIDTH = 1024;

   // The minimum screen resolution height
   public static int MIN_HEIGHT = 768;

   // The cached full screen resolution
   public static int fullScreenResolutionWidth = 1920;
   public static int fullScreenResolutionHeight = 1080;

   #endregion

   private void Awake () {
      // Cache this resolution at the very start to determine the monitor resolution
      fullScreenResolutionWidth = Screen.currentResolution.width;
      fullScreenResolutionHeight = Screen.currentResolution.height;

      if (PlayerPrefs.HasKey(SCREEN_RESOLUTION_H) && PlayerPrefs.HasKey(SCREEN_RESOLUTION_W)) {
         Height = PlayerPrefs.GetInt(SCREEN_RESOLUTION_H);
         Width = PlayerPrefs.GetInt(SCREEN_RESOLUTION_W);
         FullScreenMode = (FullScreenMode) PlayerPrefs.GetInt(SCREEN_RESOLUTION_FULLSCREEN_MODE);

         Screen.SetResolution(Width, Height, FullScreenMode);
      } else {
         Width = Screen.width;
         Height = Screen.height;
         FullScreenMode = Screen.fullScreenMode;
         saveSettings();
      }

      // If the resolution is too low, change it to the minimum supported
      if (Width < MIN_WIDTH || Height < MIN_HEIGHT) {
         Width = MIN_WIDTH;
         Height = MIN_HEIGHT;
         setResolution(Width, Height);
      }
   }

   public static void setResolution (int width, int height) {
      // If the given resolution is too low, change it to the minimum supported
      if (width < MIN_WIDTH || height < MIN_HEIGHT) {
         width = MIN_WIDTH;
         height = MIN_HEIGHT;
      }

      Width = width;
      Height = height;
      Screen.SetResolution(width, height, FullScreenMode);

      saveSettings();
   }

   public static void setFullscreen (bool fullscreen) {
      FullScreenMode = fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
      Screen.SetResolution(Width, Height, FullScreenMode);
      //Screen.SetResolution(fullscreen ? Screen.width : Width, fullscreen ? Screen.height : Height, FullScreenMode);
      saveSettings();
   }

   public static void setToResolutionFullscreenWindows () {
      Screen.SetResolution(fullScreenResolutionWidth, fullScreenResolutionHeight, FullScreenMode.Windowed);
      D.debug("Setting resolution as: " + FullScreenMode.Windowed + " : " + fullScreenResolutionWidth + " : " + fullScreenResolutionHeight);
      saveSettings();
   }

   public static void setToResolutionFullscreenExclusive () {
      Screen.SetResolution(fullScreenResolutionWidth, fullScreenResolutionHeight, FullScreenMode.ExclusiveFullScreen);
      D.debug("Setting resolution as: " + FullScreenMode.ExclusiveFullScreen + " : " + fullScreenResolutionWidth + " : " + fullScreenResolutionHeight);
      saveSettings();
   }

   private static void saveSettings () {
      PlayerPrefs.SetInt(SCREEN_RESOLUTION_W, Width);
      PlayerPrefs.SetInt(SCREEN_RESOLUTION_H, Height);
      PlayerPrefs.SetInt(SCREEN_RESOLUTION_FULLSCREEN_MODE, (int) FullScreenMode);
   }
   
   #region Private Variables

   // The screen resolution width PlayerPrefs key
   private const string SCREEN_RESOLUTION_W = "ScreenResolutionWidth";

   // The screen resolution height PlayerPrefs key
   private const string SCREEN_RESOLUTION_H = "ScreenResolutionHeight";

   // The screen fullscreen mode PlayerPrefs key
   private const string SCREEN_RESOLUTION_FULLSCREEN_MODE = "ScreenResolutionFullscreenMode";

   #endregion
}
