using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System;
using static UnityEngine.UI.Dropdown;

public class OptionsPanel : Panel, IPointerClickHandler
{

   #region Public Variables

   // The zone Text
   public Text zoneText;

   // The music slider
   public Slider musicSlider;

   // The effects slider
   public Slider effectsSlider;

   // The GUI scale slider
   public Slider guiScaleSlider;

   // The minimap scale slider
   public Slider minimapScaleSlider;

   // The label of the gui scale in percentage
   public Text guiScaleLabel;

   // The label of the minimap scale in percentage
   public Text minimapScaleLabel;

   // The available resolutions dropdown
   public Dropdown resolutionsDropdown;

   // The vsync toggle
   public Toggle vsyncToggle;

   // The screen mode toggle
   public Dropdown screenModeDropdown;

   // Self
   public static OptionsPanel self;

   // The section that only appears when a user is logged in
   public GameObject loggedInOnlySection;

   // Buttons only admins can access
   public GameObject[] adminOnlyButtons;

   // The reference to the UI Parent Canvas
   public Canvas mainGameCanvas;

   // The reference to the UI Minimap Transform
   public RectTransform minimapTransform;

   // Player pref key for gui scale
   public static string PREF_GUI_SCALE = "pref_gui_scale";

   // Player pref key for minimap scale
   public static string PREF_MINIMAP_SCALE = "pref_minimap_scale";

   // Player pref key for screen mode
   public static string PREF_SCREEN_MODE = "pref_screen_mode";

   // Player Pref key for vsync
   public static string VSYNC_COUNT_KEY = "vsync_count";

   // The object containing all the UI of the panel, for initialization purposes only
   public GameObject panelContainer;

   // Size of the windows title bar
   public Vector2Int borderSize;

   // If the initial option settings have been loaded
   public bool hasInitialized = false;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;

      initializeResolutionsDropdown();
      initializeFullScreenSettings();

      musicSlider.value = SoundManager.musicVolume;
      effectsSlider.value = SoundManager.effectsVolume;

      // Loads the saved gui scale
      float guiScaleValue = PlayerPrefs.GetFloat(PREF_GUI_SCALE, 100);
      guiScaleLabel.text = (guiScaleValue).ToString("f1") + " %";
      mainGameCanvas.scaleFactor = guiScaleValue / 100;
      guiScaleSlider.value = guiScaleValue / 100;
      guiScaleSlider.onValueChanged.AddListener(_ => guiScaleSliderChanged());

      // Loads the saved minimap scale
      float minimapScaleValue = PlayerPrefs.GetFloat(PREF_MINIMAP_SCALE, 100);
      minimapScaleLabel.text = (minimapScaleValue).ToString("f1") + " %";
      minimapTransform.localScale = new Vector3(minimapScaleValue / 100, minimapScaleValue / 100, minimapScaleValue / 100);
      minimapScaleSlider.value = minimapScaleValue / 100;
      minimapScaleSlider.onValueChanged.AddListener(_ => minimapScaleSliderChanged());

      // Loads vsync
      vsyncToggle.onValueChanged.AddListener(setVSync);
      int vsyncCount = PlayerPrefs.GetInt(VSYNC_COUNT_KEY, 0);
      vsyncToggle.SetIsOnWithoutNotify(vsyncCount != 0);
      QualitySettings.vSyncCount = vsyncCount;
   }

   public void setVSync (bool vsync) {
      int vsyncCount = vsync ? 1 : 0;
      PlayerPrefs.SetInt(VSYNC_COUNT_KEY, vsyncCount);
      QualitySettings.vSyncCount = vsyncCount;

      if (vsyncToggle.isOn != vsync) {
         vsyncToggle.SetIsOnWithoutNotify(vsync);
      }
   }

   private void initializeFullScreenSettings () {
      processCursorState();
      List<OptionData> screenModeOptions = new List<OptionData>();

      // Initialize override options
      List<FullScreenMode> customScreenMode = new List<FullScreenMode>();
      customScreenMode.Add(FullScreenMode.ExclusiveFullScreen);
      customScreenMode.Add(FullScreenMode.FullScreenWindow);
      customScreenMode.Add(FullScreenMode.Windowed);

      // Initialize display options
      screenModeOptions.Add(new OptionData { text = "Fullscreen" });
      screenModeOptions.Add(new OptionData { text = "Borderless" });
      screenModeOptions.Add(new OptionData { text = "Windowed" });

      screenModeDropdown.options = screenModeOptions;
      screenModeDropdown.onValueChanged.AddListener(_ => {
         switch (customScreenMode[_]) {
            case FullScreenMode.ExclusiveFullScreen:
               StartCoroutine(CO_ProcessScreenAdjustments(true, FullScreenMode.ExclusiveFullScreen));
               break;
            case FullScreenMode.FullScreenWindow:
               StartCoroutine(CO_ProcessScreenAdjustments(false, FullScreenMode.FullScreenWindow));
               break;
            case FullScreenMode.Windowed:
               StartCoroutine(CO_ProcessScreenAdjustments(false, FullScreenMode.Windowed));
               break;
         }
         PlayerPrefs.SetInt(PREF_SCREEN_MODE, _);
         processCursorState();
      });
      int loadedScreenModeValue = PlayerPrefs.GetInt(PREF_SCREEN_MODE, customScreenMode.FindIndex(_=>_ == FullScreenMode.Windowed));
      screenModeDropdown.value = loadedScreenModeValue;
      screenModeDropdown.onValueChanged.Invoke(loadedScreenModeValue);
   }

   private IEnumerator CO_ProcessScreenAdjustments (bool isFullScreen, FullScreenMode mode) {
      if (mode == FullScreenMode.ExclusiveFullScreen) {
         ScreenSettingsManager.setFullscreen(true);
      } else {
         ScreenSettingsManager.setFullscreen(false);
      }

      yield return new WaitForSeconds(.5f);

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

      if (!hasInitialized) {
         endInitialSetup();
      }
   }

   private void endInitialSetup () {
      hasInitialized = true;
      panelContainer.SetActive(true);
      gameObject.SetActive(false);
   }

   private void processCursorState () {
      if (ScreenSettingsManager.IsFullScreen) {
         Cursor.lockState = CursorLockMode.Confined;
      } else {
         Cursor.lockState = CursorLockMode.None;
      }
   }

   private void setResolution (int resolutionIndex) {
      ScreenSettingsManager.setResolution(_supportedResolutions[resolutionIndex].width, _supportedResolutions[resolutionIndex].height);
   }

   public override void show () {
      base.show();

      // Hide some options when called from the title screen
      loggedInOnlySection.SetActive(NetworkServer.active || NetworkClient.active);
   }

   private void initializeResolutionsDropdown () {
      Resolution[] allResolutions = Screen.resolutions;

      // Remove unsupported and duplicate resolutions (duplicate with different refresh rates)
      _supportedResolutions = new List<Resolution>();
      for (int i = 0; i < allResolutions.Length; i++) {
         if (allResolutions[i].width >= ScreenSettingsManager.MIN_WIDTH &&
            allResolutions[i].height >= ScreenSettingsManager.MIN_HEIGHT &&
            !_supportedResolutions.Exists(r => r.width == allResolutions[i].width &&
               r.height == allResolutions[i].height)) {
            _supportedResolutions.Add(allResolutions[i]);
         }
      }

      List<OptionData> options = new List<OptionData>();
      int currentResolution = -1;

      for (int i = 0; i < _supportedResolutions.Count; i++) {
         Resolution res = _supportedResolutions[i];
         OptionData o = new OptionData($"{res.width} x {res.height}");
         options.Add(o);

         if (res.width == ScreenSettingsManager.Width && res.height == ScreenSettingsManager.Height) {
            currentResolution = i;
         }
      }

      resolutionsDropdown.options = options;

      if (currentResolution >= 0) {
         resolutionsDropdown.SetValueWithoutNotify(currentResolution);
      }

      resolutionsDropdown.onValueChanged.AddListener(setResolution);
   }

   public void receiveDataFromServer (int instanceNumber, int totalInstances) {
      // Note that we just started being shown
      _lastShownTime = Time.time;

      // Update the Zone info
      zoneText.text = "Zone " + instanceNumber + " of " + totalInstances;

      // Set our music and volume sliders
      musicSlider.value = SoundManager.musicVolume / 1f;
      effectsSlider.value = SoundManager.effectsVolume / 1f;
   }

   public void OnPointerClick (PointerEventData eventData) {
      // If the black background outside is clicked, hide the panel
      if (eventData.rawPointerPress == this.gameObject) {
         PanelManager.self.popPanel();
      }
   }

   public void musicSliderChanged () {
      // If the panel just switched on, ignore the change
      if (Time.time - _lastShownTime < .1f) {
         return;
      }

      SoundManager.musicVolume = musicSlider.value;
   }

   public void effectsSliderChanged () {
      // If the panel just switched on, ignore the change
      if (Time.time - _lastShownTime < .1f) {
         return;
      }

      SoundManager.effectsVolume = effectsSlider.value;
   }

   public void guiScaleSliderChanged () {
      mainGameCanvas.scaleFactor = guiScaleSlider.value;
      guiScaleLabel.text = (guiScaleSlider.value * 100).ToString("f1") + " %";
      PlayerPrefs.SetFloat(PREF_GUI_SCALE, guiScaleSlider.value * 100);
   }

   public void minimapScaleSliderChanged () {
      minimapTransform.localScale = new Vector3(minimapScaleSlider.value, minimapScaleSlider.value, minimapScaleSlider.value);
      minimapScaleLabel.text = (minimapScaleSlider.value * 100).ToString("f1") + " %";
      PlayerPrefs.SetFloat(PREF_MINIMAP_SCALE, minimapScaleSlider.value * 100);
   }

   public void onLogOutButtonPress () {
      if (Global.player == null) {
         return;
      }

      // Hide the voyage group invite panel, if opened
      VoyageManager.self.refuseVoyageInvitation();

      // Check if the user is at sea
      if (Global.player is ShipEntity) {
         // Initialize the countdown screen
         PanelManager.self.countdownScreen.cancelButton.onClick.RemoveAllListeners();
         PanelManager.self.countdownScreen.onCountdownEndEvent.RemoveAllListeners();
         PanelManager.self.countdownScreen.cancelButton.onClick.AddListener(() => PanelManager.self.countdownScreen.hide());
         PanelManager.self.countdownScreen.onCountdownEndEvent.AddListener(() => logOut());
         PanelManager.self.countdownScreen.customText.text = "Logging out in";

         // Start the countdown
         PanelManager.self.countdownScreen.seconds = DisconnectionManager.SECONDS_UNTIL_PLAYERS_DESTROYED;
         PanelManager.self.countdownScreen.show();

         // Close this panel
         PanelManager.self.popPanel();
      } else {
         logOut();
      }
   }

   public void onGoHomeButtonPress () {
      if (Global.player == null) {
         return;
      }

      Global.player.Cmd_GoHome();

      // Close this panel
      if (isShowing()) {
         PanelManager.self.popPanel();
      }
   }

   public void onTutorialButtonPress () {
      TutorialManager3.self.panel.openPanel();
   }

   public void onKeybindingsButtonPress () {
      PanelManager.self.pushIfNotShowing(Type.Keybindings);
   }

   public void logOut () {
      if (Global.player == null) {
         return;
      }

      // Hide the voyage group invite panel, if opened
      VoyageManager.self.refuseVoyageInvitation();

      // Tell the server that the player logged out safely
      Global.player.rpc.Cmd_OnPlayerLogOutSafely();

      // Return to the title screen
      Util.stopHostAndReturnToTitleScreen();

      // Close this panel
      if (isShowing()) {
         PanelManager.self.popPanel();
      }
   }

   public void enableAdminButtons (bool isEnabled) {
      foreach (GameObject row in adminOnlyButtons) {
         row.SetActive(isEnabled);
      }
   }

   public void requestTeamCombat () {
      PanelManager.self.popPanel();
      TeamCombatPanel panel = (TeamCombatPanel) PanelManager.self.get(Panel.Type.Team_Combat);

      if (!panel.isShowing()) {
         panel.fetchSQLData();
      } else {
         PanelManager.self.pushPanel(Panel.Type.Team_Combat);
      }
   }

   public void setBorderedWindow () {
      if (BorderlessWindow.framed)
         return;

      BorderlessWindow.setBorder();
      BorderlessWindow.moveWindowPos(Vector2Int.zero, Screen.width + borderSize.x, Screen.height + borderSize.y); // Compensating the border offset.
   }

   public void setBorderlessWindow () {
      if (!BorderlessWindow.framed)
         return;

#if UNITY_EDITOR
      BorderlessWindow.setBorder();
#elif !UNITY_EDITOR
      BorderlessWindow.setBorderless();
#endif

      BorderlessWindow.moveWindowPos(Vector2Int.zero, Screen.width - borderSize.x, Screen.height - borderSize.y);
   }

   #region Private Variables

   // The time at which we were last shown
   protected float _lastShownTime;

   // The list of supported resolutions
   private List<Resolution> _supportedResolutions = new List<Resolution>();

   #endregion
}
