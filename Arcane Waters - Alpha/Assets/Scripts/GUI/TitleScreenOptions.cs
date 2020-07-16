using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using static UnityEngine.UI.Dropdown;

public class TitleScreenOptions : MonoBehaviour {
   #region Public Variables

   // The music slider
   public Slider musicSlider;

   // The effects slider
   public Slider effectsSlider;

   // The GUI scale slider
   public Slider guiScaleSlider;

   // The label of the gui scale in percentage
   public Text guiScaleLabel;

   // The available resolutions dropdown
   public Dropdown resolutionsDropdown;

   // The fullscreen toggle
   public Toggle fullscreenToggle;

   // The reference to the UI Parent Canvas
   public Canvas mainGameCanvas;

   #endregion

   private void Start () {
      initializeResolutionsDropdown();
      initializeFullScreenToggle();

      musicSlider.value = SoundManager.musicVolume;
      effectsSlider.value = SoundManager.effectsVolume;

      guiScaleSlider.value = mainGameCanvas.scaleFactor;
      guiScaleLabel.text = (guiScaleSlider.value * 100).ToString("f1") + " %";

      _lastShownTime = Time.time;
   }

   private void initializeFullScreenToggle () {
      fullscreenToggle.SetIsOnWithoutNotify(ScreenSettingsManager.IsFullScreen);
      fullscreenToggle.onValueChanged.AddListener(setFullScreen);
   }

   private void setFullScreen (bool fullscreen) {
      ScreenSettingsManager.setFullscreen(fullscreen);
   }

   private void setResolution (int resolutionIndex) {
      ScreenSettingsManager.setResolution(_supportedResolutions[resolutionIndex].width, _supportedResolutions[resolutionIndex].height);
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
   }

   #region Private Variables

   // The time at which we were last shown
   protected float _lastShownTime;

   // The list of supported resolutions
   private List<Resolution> _supportedResolutions = new List<Resolution>();

   #endregion
}
