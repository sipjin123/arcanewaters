using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System;
using static UnityEngine.UI.Dropdown;
using System.Text;
using System.Linq;

public class OptionsPanel : Panel
{

   #region Public Variables

   // The zone Text
   public Text zoneText;

   // The size of the chat
   public Text chatSizeText;

   // The chat size slider
   public Slider chatSizeSlider;

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

   // The button to apply changes to display settings
   public Button applyDisplaySettingsButton;

   // The guild icon toggle
   public Toggle displayGuildIconsToggle;

   // Flashing lights toggle
   public Toggle disableFlashingLightsToggle;

   // Player Name Display
   public Toggle displayPlayersNameToggle;

   // Help tips display
   public Toggle displayHelpTipsToggle;

   // Ignores guild alliance invites
   public Toggle ignoreGuildAllianceInviteToggle;

   // The constantly sprinting toggle
   public Toggle sprintConstantlyToggle;

   // A toggle controlling whether the user will automatically farm
   public Toggle autoFarmToggle;

   // Determines whether mouse cursor should be confined within the game screen
   public Toggle mouseLockToggle;

   // The screen mode toggle
   public Dropdown screenModeDropdown;

   // Bool to track if all players should continuously display their guild icon
   public static bool onlyShowGuildIconsOnMouseover = false;

   // Bool to track if all players should continuously display their name
   public static bool onlyShowPlayerNamesOnMouseover = false;

   // Bool to track if flashing lights should be disabled
   public static bool disableFlashingLights = false;

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

   // A reference to the game object containing the server log accessing buttons
   public GameObject serverLogRow;

   // Spaces out the right-column 'other settings' buttons when the server log row is disabled
   public GameObject logRowSeparator;

   // A list of accepted fullscreen modes (windowed, borderless windowed, fullscreen)
   public List<FullScreenMode> fullScreenModes = new List<FullScreenMode>();

   // The label that displays the total amount of active players
   public Text activePlayersCountLabel;

   // Admin tooltip to display active players
   public ToolTipComponent activePlayersAdminTooltip;
   
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
      chatSizeSlider.value = ChatManager.self.chatFontSize;
      chatSizeText.text = ChatManager.self.chatFontSize.ToString();
      chatSizeSlider.onValueChanged.AddListener(_ => {
         ChatManager.self.updateChatFontSize(_);
         chatSizeText.text = _.ToString();
      });

      // Loads the saved gui scale
      float guiScaleValue = OptionsManager.GUIScale;
      guiScaleLabel.text = Mathf.RoundToInt(guiScaleValue) + " %";
      guiScaleSlider.SetValueWithoutNotify(guiScaleValue / 25f);
      guiScaleSlider.onValueChanged.AddListener(_ => guiScaleSliderChanged());

      // Loads the saved minimap scale
      float minimapScaleValue = OptionsManager.minimapScale;
      minimapScaleLabel.text = Mathf.RoundToInt(minimapScaleValue) + " %";
      minimapScaleSlider.SetValueWithoutNotify(minimapScaleValue / 25f);
      minimapScaleSlider.onValueChanged.AddListener(_ => minimapScaleSliderChanged());

      // If any of the screen resolution settings is changed, we want to enable the button if the new setting is different from the applied one
      vsyncToggle.onValueChanged.AddListener((isOn) => applyDisplaySettingsButton.interactable = isOn != OptionsManager.isVsyncEnabled());
      screenModeDropdown.onValueChanged.AddListener((index) => {
         applyDisplaySettingsButton.interactable = index != getScreenModeIndex(ScreenSettingsManager.fullScreenMode);

         // In borderless fullscreen players can only use the native screen resolution
         bool isBorderlessWindow = index == getScreenModeIndex(FullScreenMode.FullScreenWindow);
         resolutionsDropdown.interactable = !isBorderlessWindow;

         if (isBorderlessWindow) {
            int width = Screen.currentResolution.width;
            int height = Screen.currentResolution.height;
            resolutionsDropdown.SetValueWithoutNotify(getResolutionOptionIndex(width, height, getMaxRefreshRateForResolution(width, height)));
         }
      });

      resolutionsDropdown.onValueChanged.AddListener((index) => applyDisplaySettingsButton.interactable = index != getActiveResolutionOptionIndex());

      // Loads vsync
      int vsyncCount = OptionsManager.vsyncCount;
      vsyncToggle.SetIsOnWithoutNotify(vsyncCount != 0);
      QualitySettings.vSyncCount = vsyncCount;
      ignoreGuildAllianceInviteToggle.isOn = Global.ignoreGuildAllianceInvites;
      ignoreGuildAllianceInviteToggle.onValueChanged.AddListener(_ => {
         Global.ignoreGuildAllianceInvites = _;
         PlayerPrefs.SetInt(OptionsManager.PREF_GUILD_ALLIANCE_INVITE, _ == true ? 1 : 0);
      });

      // Set the single player toggle event
      singlePlayerToggle.onValueChanged.AddListener(_ => {
         Global.isSinglePlayer = _;
      });

      // Set the guild icons toggle event
      displayGuildIconsToggle.onValueChanged.AddListener(value => {
         onlyShowGuildIconsOnMouseover = !value;
         showAllGuildIcons(value);
      });

      // Set the flashing lights toggle event
      disableFlashingLightsToggle.onValueChanged.AddListener(value => {
         disableFlashingLights = value;
      });

      // Set the player name toggle event
      displayPlayersNameToggle.onValueChanged.AddListener(value => {
         onlyShowPlayerNamesOnMouseover = !value;
         showPlayersName(value);
      });

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
      mouseLockToggle.transform.parent.gameObject.SetActive(false);
#else
      // Set the player name toggle event
      mouseLockToggle.isOn = PlayerPrefs.GetInt(OptionsManager.PREF_LOCK_CURSOR) == 1 ? true : false;
      ScreenSettingsManager.self.refreshMouseLockState();
      mouseLockToggle.onValueChanged.AddListener(value => {
         PlayerPrefs.SetInt(OptionsManager.PREF_LOCK_CURSOR, value ? 1 : 0);
      });
#endif

      // Initialize the help tips toggle
      displayHelpTipsToggle.SetIsOnWithoutNotify(!NotificationManager.self.areAllNotificationsDisabled());
      displayHelpTipsToggle.onValueChanged.AddListener(value => {
         setHelpTipsDisplay();
      });

      sprintConstantlyToggle.isOn = PlayerPrefs.GetInt(OptionsManager.PREF_SPRINT_CONSTANTLY) == 1 ? true : false;
      sprintConstantlyToggle.onValueChanged.AddListener(value => {
         PlayerPrefs.SetInt(OptionsManager.PREF_SPRINT_CONSTANTLY, value ? 1 : 0);
         Global.sprintConstantly = value;
      });

      bool autoFarm = PlayerPrefs.GetInt(OptionsManager.PREF_AUTO_FARM, 0) == 1 ? true : false;
      autoFarmToggle.isOn = autoFarm;
      Global.autoFarm = autoFarm;
      autoFarmToggle.onValueChanged.AddListener(value => {
         PlayerPrefs.SetInt(OptionsManager.PREF_AUTO_FARM, value ? 1 : 0);
         Global.autoFarm = value;
      });

      // Build string and show version number
      versionGameObject.SetActive(true);
      versionNumberText.text = Util.getFormattedGameVersion();

      applyDisplaySettingsButton.onClick.RemoveAllListeners();
      applyDisplaySettingsButton.onClick.AddListener(() => applyDisplaySettings());

      refreshDisplaySettingsControls();

      requestPlayersCount();

      activePlayersAdminTooltip.message = "";
   }

   public void showAllGuildIcons (bool showGuildIcons) {
      if (showGuildIcons) {
         // Display the guild icons of all the players
         foreach (NetEntity entity in EntityManager.self.getAllEntities()) {
            if ((entity.guildId > 0) && (entity is PlayerBodyEntity)) {
               entity.updateGuildIconSprites();
               entity.showGuildIcon();
            }
         }
      } else {
         // Do not display the guild icons of all the players
         foreach (NetEntity entity in EntityManager.self.getAllEntities()) {
            if (entity is PlayerBodyEntity) {
               entity.hideGuildIcon();
            }
         }
      }
   }

   public void showPlayersName (bool displayPlayersName) {
      if (displayPlayersName) {
         foreach (NetEntity entity in EntityManager.self.getAllEntities()) {
            if (entity is PlayerBodyEntity) {
               entity.showEntityName();
            }
         }
      } else {
         foreach (NetEntity entity in EntityManager.self.getAllEntities()) {
            if (entity is PlayerBodyEntity) {
               entity.hideEntityName();
            }
         }
      }
   }

   public void setHelpTipsDisplay () {
      NotificationManager.self.toggleNotifications(displayHelpTipsToggle.isOn);
   }

   public void setVSync (bool vsync) {
      OptionsManager.setVsync(vsync);

      if (vsyncToggle.isOn != vsync) {
         vsyncToggle.SetIsOnWithoutNotify(vsync);
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

   private void initializeResolutionsList () {
      Resolution[] allResolutions = Screen.resolutions;

      // Remove unsupported and duplicate resolutions (duplicate with different refresh rates)
      _supportedResolutions = new List<Resolution>();
      for (int i = 0; i < allResolutions.Length; i++) {
         Resolution res = allResolutions[i];

         if (res.width >= res.height &&
            res.width >= ScreenSettingsManager.MIN_WIDTH &&
            res.height >= ScreenSettingsManager.MIN_HEIGHT &&
            !_supportedResolutions.Exists(r => r.width == res.width &&
               r.height == res.height && r.refreshRate == res.refreshRate)) {
            _supportedResolutions.Add(res);
         }
      }
   }

   private void initializeResolutionsDropdown () {
      initializeResolutionsList();

      List<OptionData> options = new List<OptionData>();
      int currentResolution = -1;

      for (int i = 0; i < _supportedResolutions.Count; i++) {
         Resolution res = _supportedResolutions[i];
         OptionData o = new OptionData($"{res.width} x {res.height} ({res.refreshRate})");
         options.Add(o);

         if (res.width == ScreenSettingsManager.width && res.height == ScreenSettingsManager.height) {
            currentResolution = i;
         }
      }

      resolutionsDropdown.options = options;

      if (currentResolution >= 0) {
         resolutionsDropdown.SetValueWithoutNotify(currentResolution);
      }
   }

   private int getActiveResolutionOptionIndex () {
      return getResolutionOptionIndex(ScreenSettingsManager.width, ScreenSettingsManager.height, ScreenSettingsManager.refreshRate);
   }

   private int getResolutionOptionIndex (int width, int height, int refreshRate) {
      return _supportedResolutions.FindIndex(x => x.width == width && x.height == height && x.refreshRate == refreshRate);
   }

   private void setResolution (int resolutionIndex) {
      ScreenSettingsManager.setResolution(_supportedResolutions[resolutionIndex].width, _supportedResolutions[resolutionIndex].height, _supportedResolutions[resolutionIndex].refreshRate);
   }

   private int getMaxRefreshRateForResolution (int width, int height) {
      if (_supportedResolutions == null || _supportedResolutions.Count < 1) {
         initializeResolutionsList();
      }

      if (_supportedResolutions.Any(x => x.width == width && x.height == height)) {
         return _supportedResolutions.Where(x => x.width == width && x.height == height).Max(r => r.refreshRate);
      } else {
         D.warning($"Could not find max refresh rate for resolution {width} x {height}. Returning default.");
         return 59;
      }
   }

   private void refreshDisplaySettingsControls () {
      int vsyncCount = OptionsManager.vsyncCount;
      vsyncToggle.SetIsOnWithoutNotify(vsyncCount != 0);

      int loadedScreenModeValue = getScreenModeIndex(ScreenSettingsManager.fullScreenMode);
      screenModeDropdown.SetValueWithoutNotify(loadedScreenModeValue);

      int activeResolutionIndex = -1;
      if (ScreenSettingsManager.fullScreenMode == FullScreenMode.FullScreenWindow) {
         int width = Screen.currentResolution.width;
         int height = Screen.currentResolution.height;

         activeResolutionIndex = getResolutionOptionIndex(width, height, getMaxRefreshRateForResolution(width, height));
         resolutionsDropdown.interactable = false;
      } else {
         activeResolutionIndex = getActiveResolutionOptionIndex();
         resolutionsDropdown.interactable = true;
      }

      if (activeResolutionIndex >= 0) {
         resolutionsDropdown.SetValueWithoutNotify(activeResolutionIndex);
      }

      // The controls now match the applied settings, we can disable the "Apply" button until something changes again
      applyDisplaySettingsButton.interactable = false;
   }

   private void applyDisplaySettings () {
      setResolution(resolutionsDropdown.value);
      setVSync(vsyncToggle.isOn);
      ScreenSettingsManager.setFullscreenMode(fullScreenModes[screenModeDropdown.value]);

      refreshDisplaySettingsControls();
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

      bool isAdmin = Global.isLoggedInAsAdmin();
      serverLogRow.SetActive(isAdmin);
      logRowSeparator.SetActive(!isAdmin);
      if (isAdmin) {
         Global.player.admin.Cmd_GetServerLogString();
      }

      refreshDisplaySettingsControls();

      requestPlayersCount();

      ScreenSettingsManager.self.refreshMouseLockState();
      
      activePlayersAdminTooltip.gameObject.SetActive(isAdmin);
   }

   public override void hide () {
      base.hide();
      ScreenSettingsManager.self.refreshMouseLockState();
   }

   private void requestPlayersCount () {
      if (Global.player != null) {
         Global.player.rpc.Cmd_RequestPlayersCount(Global.player.isAdmin());
      }
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

      SoundManager.self.musicVCA.setVolume(SoundManager.musicVolume);
   }

   public void effectsSliderChanged () {
      // If the panel just switched on, ignore the change
      if (Time.time - _lastShownTime < .1f) {
         return;
      }

      SoundManager.effectsVolume = effectsSlider.value;

      SoundManager.self.sfxVCA.setVolume(SoundManager.effectsVolume);
   }

   public void guiScaleSliderChanged () {
      guiScaleLabel.text = Mathf.RoundToInt(guiScaleSlider.value * 25) + " %";
   }

   public void applyGuiScaleChanges () {
      OptionsManager.setGUIScale(Mathf.RoundToInt(guiScaleSlider.value * 25));
   }

   public void minimapScaleSliderChanged () {
      int scale = Mathf.RoundToInt(minimapScaleSlider.value * 25);
      minimapScaleLabel.text = scale + " %";
      OptionsManager.setMinimapScale(scale);
   }

   public void onOpenLogFilePressed () {
      D.openLogFile();
   }

   public void onCopyLogPressed () {
      D.copyLogToClipboard();
   }

   public void onOpenServerLogFilePressed () {
      D.openServerLogFile();
   }

   public void onCopyServerLogFilePressed () {
      D.copyServerLogToClipboard();
   }

   public void onLogOutButtonPress () {
      if (Global.player == null) {
         // If we are at the character screen, lets go back to title
         if (CharacterScreen.self.isShowing()) {
            // Return to the title screen
            Util.stopHostAndReturnToTitleScreen();

            if (CharacterCreationPanel.self.isShowing()) {
               CharacterCreationPanel.self.cancelCreating();
            }

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

   public void onGifButtonPress () {
      PanelManager.self.linkIfNotShowing(Type.GIFReplaySettings);
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

      // Close this panel
      if (isShowing()) {
         PanelManager.self.unlinkPanel();
      }

      LoadingUtil.executeAfterFade(() => {
         // Return to the character selection screen
         Util.stopHostAndReturnToCharacterSelectionScreen();
      });
   }

   public void enableAdminButtons (bool isEnabled) {
      foreach (GameObject row in adminOnlyButtons) {
         row.SetActive(isEnabled);
      }
   }

   public void onPlayersCountReceived (int playersCount, string [] playersNames) {
      activePlayersCountLabel.text = "Active Players: " + playersCount.ToString();
      activePlayersAdminTooltip.message = string.Join("\n", playersNames);
   }

   #region Private Variables

   // The time at which we were last shown
   protected float _lastShownTime;

   // The list of supported resolutions
   private List<Resolution> _supportedResolutions = new List<Resolution>();

   #endregion
}
