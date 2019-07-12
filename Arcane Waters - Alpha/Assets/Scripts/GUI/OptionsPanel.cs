using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class OptionsPanel : Panel, IPointerClickHandler {
   #region Public Variables

   // The zone Text
   public Text zoneText;

   // The music slider
   public Slider musicSlider;

   // The effects slider
   public Slider effectsSlider;

   // Self
   public static OptionsPanel self;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;
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

      // Stop any client or server that may have been running
      MyNetworkManager.self.StopHost();

      // Activate the Title Screen camera
      Util.activateVirtualCamera(TitleScreen.self.virtualCamera);

      // Clear out our saved data
      Global.lastUsedAccountName = "";
      Global.lastUserAccountPassword = "";
      Global.currentlySelectedUserId = 0;
   }

   #region Private Variables

   // The time at which we were last shown
   protected float _lastShownTime;

   #endregion
}
