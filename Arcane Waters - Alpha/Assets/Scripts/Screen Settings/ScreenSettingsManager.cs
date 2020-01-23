using UnityEngine;
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

   #endregion

   private void Awake () {

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
   }

   public static void setResolution (int width, int height) {
      Width = width;
      Height = height;
      Screen.SetResolution(width, height, FullScreenMode);

      saveSettings();
   }

   public static void setFullscreen (bool fullscreen) {
      FullScreenMode = fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
      Screen.SetResolution(Width, Height, FullScreenMode);

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
