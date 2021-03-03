using UnityEngine;
using System.Collections;

public class ScreenSettingsManager : MonoBehaviour {

   #region Public Variables

   // The screen resolution width
   public static int width { get; private set; }

   // The screen resolution height
   public static int height { get; private set; }

   // The screen refresh rate
   public static int refreshRate { get; private set; }

   // The screen fullscreen mode
   public static FullScreenMode fullScreenMode { get; private set; }

   // Boolean that is true if FullScreenMode isn't Windowed
   public static bool IsFullScreen { get { return fullScreenMode == FullScreenMode.ExclusiveFullScreen; } }

   // The minimum screen resolution width
   public const int MIN_WIDTH = 1024;

   // The minimum screen resolution height
   public const int MIN_HEIGHT = 768;

   // The cached full screen resolution
   public static int fullScreenResolutionWidth = 1920;
   public static int fullScreenResolutionHeight = 1080;

   // The screen size that we consider 'large' and alter scaling for
   public static int largeScreenWidth = 2048;
   public static int largeScreenHeight = 1536;

   // The time to wait before we finish applying screen mode settings
   public const float SCREEN_TRANSITION_TIME = 0.05f;

   // Size of the windows title bar
   public static Vector2Int BORDER_SIZE { get; } = new Vector2Int(8, 40);

   // Self
   public static ScreenSettingsManager self;

   #endregion

   private void Awake () {
      self = this;

      // Cache this resolution at the very start to determine the monitor resolution
      fullScreenResolutionWidth = Screen.currentResolution.width;
      fullScreenResolutionHeight = Screen.currentResolution.height;

      refreshRate = PlayerPrefs.GetInt(REFRESH_RATE, Screen.currentResolution.refreshRate);

      if (PlayerPrefs.HasKey(SCREEN_RESOLUTION_H) && PlayerPrefs.HasKey(SCREEN_RESOLUTION_W)) {
         height = PlayerPrefs.GetInt(SCREEN_RESOLUTION_H);
         width = PlayerPrefs.GetInt(SCREEN_RESOLUTION_W);
         fullScreenMode = (FullScreenMode)PlayerPrefs.GetInt(FULL_SCREEN_MODE);
         setResolution(width, height, refreshRate);
      } else {
         width = Screen.width;
         height = Screen.height;
         fullScreenMode = Screen.fullScreenMode;         
         saveSettings();
      }

      // If the resolution is too low, change it to the minimum supported
      if (width < MIN_WIDTH || height < MIN_HEIGHT) {
         width = MIN_WIDTH;
         height = MIN_HEIGHT;
         setResolution(width, height, refreshRate);
      }
   }

   public static void setFullscreenMode (FullScreenMode screenMode) {
      if (screenMode == fullScreenMode) { 
         return; 
      }

      fullScreenMode = screenMode;

      // If the player selected the borderless windowed mode, width and height should match the native screen resolution
      if (screenMode == FullScreenMode.FullScreenWindow) {
         width = Screen.currentResolution.width;
         height = Screen.currentResolution.height;
      }

      self.StartCoroutine(self.CO_ProcessScreenAdjustments(screenMode));

      Debug.Log($"Fullscreen mode changed to {screenMode}");

      processCursorState();
   }

   private static void processCursorState () {
#if !UNITY_EDITOR
      if (IsFullScreen) {
         Cursor.lockState = CursorLockMode.Confined;
      } else {
         Cursor.lockState = CursorLockMode.None;
      }
#endif
   }

   public static void setResolution (int width, int height, int refreshRate) {
      if (fullScreenMode == FullScreenMode.FullScreenWindow) {
         setFullscreenMode(fullScreenMode);
         return;
      }

      // If the given resolution is too low, change it to the minimum supported
      if (width < MIN_WIDTH || height < MIN_HEIGHT) {
         width = MIN_WIDTH;
         height = MIN_HEIGHT;
      }

      ScreenSettingsManager.width = width;
      ScreenSettingsManager.height = height;
      ScreenSettingsManager.refreshRate = refreshRate;

      Screen.SetResolution(width, height, fullScreenMode, refreshRate);

      saveSettings();
   }

   public static void setToResolutionFullscreenWindowed () {
      setFullscreenMode(FullScreenMode.FullScreenWindow);
      saveSettings();
   }

   public static void setToResolutionFullscreenExclusive () {
      setFullscreenMode(FullScreenMode.ExclusiveFullScreen);
      saveSettings();
   }

   private static void saveSettings () {
      PlayerPrefs.SetInt(SCREEN_RESOLUTION_W, width);
      PlayerPrefs.SetInt(SCREEN_RESOLUTION_H, height);
      PlayerPrefs.SetInt(FULL_SCREEN_MODE, (int) fullScreenMode);
      PlayerPrefs.SetInt(REFRESH_RATE, refreshRate);
   }

   private void OnApplicationQuit () {
      saveSettings();
   }

   private IEnumerator CO_ProcessScreenAdjustments (FullScreenMode mode) {
      if (mode == FullScreenMode.FullScreenWindow) {
         setBorderlessWindow();
      } else {
         setBorderedWindow();
      }

      // Only setup full screen after resizing the window
      if (mode == FullScreenMode.ExclusiveFullScreen) {
         yield return new WaitForSeconds(SCREEN_TRANSITION_TIME);
      }

      Screen.SetResolution(width, height, fullScreenMode, refreshRate);
   }

   public void setBorderedWindow () {
      if (BorderlessWindow.framed)
         return;

#if !UNITY_EDITOR
      BorderlessWindow.setBorder();
      BorderlessWindow.moveWindowPos(Vector2Int.zero, Screen.width + BORDER_SIZE.x, Screen.height + BORDER_SIZE.y); // Compensating the border offset.
#endif
   }

   public void setBorderlessWindow () {
      if (!BorderlessWindow.framed)
         return;

#if !UNITY_EDITOR
      BorderlessWindow.setBorderless();
      BorderlessWindow.moveWindowPos(Vector2Int.zero, Screen.width - BORDER_SIZE.x, Screen.height - BORDER_SIZE.y);      
#endif
   }

   #region Private Variables

   // The screen resolution width PlayerPrefs key
   private const string SCREEN_RESOLUTION_W = "ScreenResolutionWidth";

   // The screen resolution height PlayerPrefs key
   private const string SCREEN_RESOLUTION_H = "ScreenResolutionHeight";

   // The full screen mode PlayerPrefs key
   private const string FULL_SCREEN_MODE = "FullScreenMode";

   // The refresh rate PlayerPrefs key
   private const string REFRESH_RATE = "RefreshRate";

   #endregion
}
