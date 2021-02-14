using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System;
using static UnityEngine.UI.Dropdown;
using System.Text;

public class OptionsPanel : Panel
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

   // The guild icon toggle
   public Toggle displayGuildIconsToggle;

   // Player Name Display
   public Toggle displayPlayersNameToggle;

   // The constantly sprinting toggle
   public Toggle sprintConstantlyToggle;

   // The screen mode toggle
   public Dropdown screenModeDropdown;

   // Bool to track if all players should continuously display their guild icon
   public static bool allGuildIconsShowing;

   // Bool to track if all players should continuously display their name
   public static bool allPlayersNameShowing;

   // Self
   public static OptionsPanel self;

   // Version number gameObject
   public GameObject versionGameObject;

   // Version Number text field
   public Text versionNumberText;

   // The objects that only appears when a user is logged in
   public GameObject[] loggedInObjects;

   // The objects that only appears when a user is NOT logged in
   public GameObject[] notLoggedInObjects;

   // Buttons only admins can access
   public GameObject[] adminOnlyButtons;

   // If the initial option settings have been loaded
   public bool hasInitialized = false;

   // The single player toggle
   public Toggle singlePlayerToggle;

   // A list of accepted fullscreen modes (windowed, borderless windowed, fullscreen)
   public List<FullScreenMode> fullScreenModes = new List<FullScreenMode>();

   #endregion

   public override void Awake () {
      base.Awake();
      
      self = this;
   }

   public override void Start () {
      initializeResolutionsDropdown();
      initializeFullScreenSettings();

      musicSlider.value = SoundManager.musicVolume;
      effectsSlider.value = SoundManager.effectsVolume;

      // Loads the saved gui scale
      float guiScaleValue = OptionsManager.GUIScale;
      guiScaleLabel.text = (guiScaleValue).ToString("f1") + " %";
      guiScaleSlider.SetValueWithoutNotify(guiScaleValue / 100.0f);
      guiScaleSlider.onValueChanged.AddListener(_ => guiScaleSliderChanged());
      
      // Loads the saved minimap scale
      float minimapScaleValue = OptionsManager.minimapScale;
      minimapScaleLabel.text = (minimapScaleValue).ToString("f1") + " %";      
      minimapScaleSlider.SetValueWithoutNotify(minimapScaleValue / 100.0f);
      minimapScaleSlider.onValueChanged.AddListener(_ => minimapScaleSliderChanged());

      // Loads vsync
      vsyncToggle.onValueChanged.AddListener(setVSync);
      int vsyncCount = OptionsManager.vsyncCount;
      vsyncToggle.SetIsOnWithoutNotify(vsyncCount != 0);
      QualitySettings.vSyncCount = vsyncCount;

      // Set the single player toggle event
      singlePlayerToggle.onValueChanged.AddListener(_ => {
         Global.isSinglePlayer = _;
      });

      // Set the guild icons toggle event
      displayGuildIconsToggle.onValueChanged.AddListener(value => {
         allGuildIconsShowing = value;
         showAllGuildIcons(value);
      });

      // Set the player name toggle event
      displayPlayersNameToggle.onValueChanged.AddListener(value => {
         allPlayersNameShowing = value;
         showPlayersName(value);
      });

      sprintConstantlyToggle.isOn = PlayerPrefs.GetInt(OptionsManager.PREF_SPRINT_CONSTANTLY) == 1 ? true : false;
      sprintConstantlyToggle.onValueChanged.AddListener(value => {
         PlayerPrefs.SetInt(OptionsManager.PREF_SPRINT_CONSTANTLY, value ? 1 : 0);
         Global.sprintConstantly = value;
      });

      // Build string and show version number
      versionGameObject.SetActive(true);
      versionNumberText.text = Util.getFormattedGameVersion();
   }

   public void setVSync (bool vsync) {
      OptionsManager.setVsync(vsync);

      if (vsyncToggle.isOn != vsync) {
         vsyncToggle.SetIsOnWithoutNotify(vsync);
      }
   }

   public void showAllGuildIcons (bool showGuildIcons) {
      if (showGuildIcons) {
         // Display the guild icons of all the players
         foreach (NetEntity entity in EntityManager.self.getAllEntities()) {
            if ((entity.guildId > 0) && (entity is PlayerBodyEntity)) {
               entity.showGuildIcon();
            }
            allGuildIconsShowing = true;
         }
      } else {
         // Do not display the guild icons of all the players
         foreach (NetEntity entity in EntityManager.self.getAllEntities()) {
            if (entity is PlayerBodyEntity) {
               entity.hideGuildIcon();
            }
            allGuildIconsShowing = false;
         }
      }
   }

   public void showPlayersName (bool displayPlayersName) {
      if (displayPlayersName) {
         foreach (NetEntity entity in EntityManager.self.getAllEntities()) {
            if (entity is PlayerBodyEntity) {
               entity.showEntityName();
            }
            allPlayersNameShowing = true;
         }
      } else {
         foreach (NetEntity entity in EntityManager.self.getAllEntities()) {
            if (entity is PlayerBodyEntity) {
               entity.hideEntityName();
            }
            allPlayersNameShowing = false;
         }
      }
   }

   private void initializeFullScreenSettings () {
      List<OptionData> screenModeOptions = new List<OptionData>();

      // Initialize override options
      initializeFullScreenModesList();

      // Initialize display options
      screenModeOptions.Add(new OptionData { text = "Fullscreen" });
      screenModeOptions.Add(new OptionData { text = "Borderless" });
      screenModeOptions.Add(new OptionData { text = "Windowed" });

      screenModeDropdown.options = screenModeOptions;
      screenModeDropdown.onValueChanged.AddListener(index => {
         ScreenSettingsManager.setFullscreenMode(fullScreenModes[index]);
      });

      int loadedScreenModeValue = getScreenModeIndex(ScreenSettingsManager.fullScreenMode);
      screenModeDropdown.SetValueWithoutNotify(loadedScreenModeValue);
   }

   public int getScreenModeIndex (FullScreenMode mode) {
      if (fullScreenModes == null || fullScreenModes.Count < 1) {
         initializeFullScreenModesList();
      }

      return fullScreenModes.IndexOf(mode);
   }

   private void initializeFullScreenModesList () {
      fullScreenModes = new List<FullScreenMode>();
      fullScreenModes.Add(FullScreenMode.ExclusiveFullScreen);
      fullScreenModes.Add(FullScreenMode.FullScreenWindow);
      fullScreenModes.Add(FullScreenMode.Windowed);
   }

   private void setResolution (int resolutionIndex) {
      ScreenSettingsManager.setResolution(_supportedResolutions[resolutionIndex].width, _supportedResolutions[resolutionIndex].height);      
   }

   public override void show () {
      base.show();

      // Show/hide some options when the user is logged in and when he is not
      if (NetworkServer.active || NetworkClient.active) {
         foreach (GameObject go in loggedInObjects) {
            go.SetActive(true);
         }
         foreach (GameObject go in notLoggedInObjects) {
            go.SetActive(false);
         }
      } else {
         foreach (GameObject go in loggedInObjects) {
            go.SetActive(false);
         }
         foreach (GameObject go in notLoggedInObjects) {
            go.SetActive(true);
         }
      }
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

         if (res.width == ScreenSettingsManager.width && res.height == ScreenSettingsManager.height) {
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
      guiScaleLabel.text = (guiScaleSlider.value * 100.0f).ToString("f1") + " %";
      OptionsManager.setGUIScale(guiScaleSlider.value * 100.0f);
   }

   public void minimapScaleSliderChanged () {      
      minimapScaleLabel.text = (minimapScaleSlider.value * 100.0f).ToString("f1") + " %";
      OptionsManager.setMinimapScale(minimapScaleSlider.value * 100.0f);
   }

   public void onOpenLogFilePressed () {
      D.openLogFile();
   }

   public void onCopyLogPressed () {
      D.copyLogToClipboard();
   }

   public void onLogOutButtonPress () {
      if (Global.player == null) {
         // If we are at the character screen, lets go back to title
         if (CharacterScreen.self.isShowing()) {
            // Return to the title screen
            Util.stopHostAndReturnToTitleScreen();

            // Close this panel
            if (isShowing()) {
               PanelManager.self.unlinkPanel();
            }
         }

         return;
      }

      // Hide the voyage group invite panel, if opened
      VoyageGroupManager.self.refuseGroupInvitation();

      // Stop weather simulation
      WeatherManager.self.setWeatherSimulation(WeatherEffectType.None);

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
         PanelManager.self.unlinkPanel();
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
         PanelManager.self.unlinkPanel();
      }
   }

   public void onTutorialButtonPress () {
      TutorialManager3.self.panel.openPanel();
   }

   public void onKeybindingsButtonPress () {
      PanelManager.self.linkIfNotShowing(Type.Keybindings);
   }

   public void onExitButtonPress () {
      PanelManager.self.showConfirmationPanel("Are you sure you want to exit the game?",
         () => {
            Application.Quit();
         });
   }

   public void logOut () {
      if (Global.player == null) {
         return;
      }

      // Hide the voyage group invite panel, if opened
      VoyageGroupManager.self.refuseGroupInvitation();

      // Tell the server that the player logged out safely
      Global.player.rpc.Cmd_OnPlayerLogOutSafely();

      // Return to the title screen
      Util.stopHostAndReturnToTitleScreen();

      // Close this panel
      if (isShowing()) {
         PanelManager.self.unlinkPanel();
      }
   }

   public void enableAdminButtons (bool isEnabled) {
      foreach (GameObject row in adminOnlyButtons) {
         row.SetActive(isEnabled);
      }
   }

   #region Private Variables

   // The time at which we were last shown
   protected float _lastShownTime;

   // The list of supported resolutions
   private List<Resolution> _supportedResolutions = new List<Resolution>();

   #endregion
}
