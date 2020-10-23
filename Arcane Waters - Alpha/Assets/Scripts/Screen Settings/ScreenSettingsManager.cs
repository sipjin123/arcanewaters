using UnityEngine;
using System.Collections;

public class ScreenSettingsManager : MonoBehaviour {

   #region Public Variables

   // The screen resolution width
   public static int width { get; private set; }

   // The screen resolution height
   public static int height { get; private set; }

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
   public const float SCREEN_TRANSITION_TIME = 0.25f;

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

      if (PlayerPrefs.HasKey(SCREEN_RESOLUTION_H) && PlayerPrefs.HasKey(SCREEN_RESOLUTION_W)) {
         height = PlayerPrefs.GetInt(SCREEN_RESOLUTION_H);
         width = PlayerPrefs.GetInt(SCREEN_RESOLUTION_W);
         fullScreenMode = (FullScreenMode)PlayerPrefs.GetInt(FULL_SCREEN_MODE);
         Screen.SetResolution(width, height, fullScreenMode);
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
         setResolution(width, height);
      }
   }

   public static void setFullscreenMode (FullScreenMode screenMode) {
      fullScreenMode = screenMode;
      Screen.SetResolution(width, height, fullScreenMode);
      self.StartCoroutine(self.CO_ProcessScreenAdjustments(screenMode));
      self.StartCoroutine(self.CO_RefreshBorders());

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


   public static void setResolution (int width, int height) {
      // If the given resolution is too low, change it to the minimum supported
      if (width < MIN_WIDTH || height < MIN_HEIGHT) {
         width = MIN_WIDTH;
         height = MIN_HEIGHT;
      }

      ScreenSettingsManager.width = width;
      ScreenSettingsManager.height = height;
      Screen.SetResolution(width, height, fullScreenMode);

      saveSettings();
   }

   private static void setFullscreen (bool fullscreen) {
      fullScreenMode = fullscreen ? FullScreenMode.ExclusiveFullScreen : FullScreenMode.Windowed;
      Screen.SetResolution(width, height, fullScreenMode);
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
   }

   private IEnumerator CO_RefreshBorders () {
      if (fullScreenMode == FullScreenMode.FullScreenWindow) {
         setBorderedWindow();

         yield return new WaitForSeconds(.15f);

         setBorderlessWindow();
      }
   }

   private IEnumerator CO_ProcessScreenAdjustments (FullScreenMode mode) {
      // Make sure the full screen flag is set to false
      if (mode != FullScreenMode.ExclusiveFullScreen) {
         setFullscreen(false);
      }

      yield return new WaitForSeconds(SCREEN_TRANSITION_TIME);

#if !UNITY_EDITOR
      switch (mode) {
         case FullScreenMode.Windowed:
            setBorderedWindow();
            break;
         case FullScreenMode.FullScreenWindow:
            setBorderlessWindow();
            break;
         case FullScreenMode.ExclusiveFullScreen:
            setBorderedWindow();
            break;
      }
#endif

      // Only setup full screen after resizing the window
      if (mode == FullScreenMode.ExclusiveFullScreen) {
         yield return new WaitForSeconds(SCREEN_TRANSITION_TIME);
         setFullscreen(true);
      }
   }

   public void setBorderedWindow () {
      if (BorderlessWindow.framed)
         return;

      BorderlessWindow.setBorder();
      BorderlessWindow.moveWindowPos(Vector2Int.zero, Screen.width + BORDER_SIZE.x, Screen.height + BORDER_SIZE.y); // Compensating the border offset.
   }

   public void setBorderlessWindow () {
      if (!BorderlessWindow.framed)
         return;

#if UNITY_EDITOR
      BorderlessWindow.setBorder();
#elif !UNITY_EDITOR
      BorderlessWindow.setBorderless();
#endif

      BorderlessWindow.moveWindowPos(Vector2Int.zero, Screen.width - BORDER_SIZE.x, Screen.height - BORDER_SIZE.y);
   }

   #region Private Variables

   // The screen resolution width PlayerPrefs key
   private const string SCREEN_RESOLUTION_W = "ScreenResolutionWidth";

   // The screen resolution height PlayerPrefs key
   private const string SCREEN_RESOLUTION_H = "ScreenResolutionHeight";

   // The full screen mode PlayerPrefs key
   private const string FULL_SCREEN_MODE = "FullScreenMode";

   #endregion
}
