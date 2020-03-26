using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System;
using static UnityEngine.UI.Dropdown;

public class OptionsPanel : Panel, IPointerClickHandler {

   #region Public Variables

   // The zone Text
   public Text zoneText;

   // The music slider
   public Slider musicSlider;

   // The effects slider
   public Slider effectsSlider;

   // The available resolutions dropdown
   public Dropdown resolutionsDropdown;

   // The fullscreen toggle
   public Toggle fullscreenToggle;

   // Log Out Button
   public Button logoutButton;

   // Self
   public static OptionsPanel self;

   // Buttons only admins can access
   public GameObject[] adminOnlyButtons;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;
   }

   public override void Start () {
      base.Start();

      initializeResolutionsDropdown();
      initializeFullScreenToggle();

      musicSlider.value = SoundManager.musicVolume;
      effectsSlider.value = SoundManager.effectsVolume;
   }

   private void initializeFullScreenToggle () {
      fullscreenToggle.SetIsOnWithoutNotify(ScreenSettingsManager.IsFullScreen);
      fullscreenToggle.onValueChanged.AddListener(setFullScreen);
   }

   private void setFullScreen (bool fullscreen) {
      ScreenSettingsManager.setFullscreen(fullscreen);
   }

   private void setResolution (int resolutionIndex) {
      ScreenSettingsManager.setResolution(Screen.resolutions[resolutionIndex].width, Screen.resolutions[resolutionIndex].height);
   }

   public override void show () {
      base.show();

      logoutButton.gameObject.SetActive(NetworkServer.active || NetworkClient.active);
   }

   private void initializeResolutionsDropdown () {
      Resolution[] supportedResolutions = Screen.resolutions;
      List<OptionData> options = new List<OptionData>();
      int currentResolution = -1;

      for (int i = 0; i < supportedResolutions.Length; i++) {
         Resolution res = supportedResolutions[i];
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

   public void logOut () {
      // Close this panel
      PanelManager.self.popPanel();

      // Hide the voyage group invite panel, if opened
      VoyageManager.self.refuseVoyageInvitation();

      // Stop any client or server that may have been running
      MyNetworkManager.self.StopHost();

      // Activate the Title Screen camera
      Util.activateVirtualCamera(TitleScreen.self.virtualCamera);

      // Clear out our saved data
      Global.lastUsedAccountName = "";
      Global.lastUserAccountPassword = "";
      Global.currentlySelectedUserId = 0;
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

   #region Private Variables

   // The time at which we were last shown
   protected float _lastShownTime;

   #endregion
}
