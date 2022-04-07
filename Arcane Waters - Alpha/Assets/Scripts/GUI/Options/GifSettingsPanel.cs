using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class GifSettingsPanel : Panel
{
   #region Public Variables

   #endregion

   public override void Start () {
      base.Start();

      // Initialize controls
      _gifEnabledToggle.SetIsOnWithoutNotify(GIFReplayManager.self.getIsRecording());
      _gifEnabledToggle.onValueChanged.AddListener(isRecordingChanged);

      _resolutionSlider.SetValueWithoutNotify(GIFReplayManager.self.getDownscaleFactor());
      _resolutionSlider.onValueChanged.AddListener(resolutionChanged);

      _fpsSlider.SetValueWithoutNotify(GIFReplayManager.self.getFPS());
      _fpsSlider.onValueChanged.AddListener(fpsChanged);

      _durationSlider.SetValueWithoutNotify(GIFReplayManager.self.getDuration());
      _durationSlider.onValueChanged.AddListener(durationChanged);

      // Lets update all slider visual values (text that is shown to the user)
      updateSliderVisualValues();
   }

   private void isRecordingChanged (bool val) {
      GIFReplayManager.self.setIsRecording(_gifEnabledToggle.isOn, true);
      updateSliderVisualValues();
   }

   private void resolutionChanged (float val) {
      GIFReplayManager.self.setDownscaleFactor(Mathf.RoundToInt(_resolutionSlider.value), true);
      updateSliderVisualValues();
   }

   private void fpsChanged (float val) {
      GIFReplayManager.self.setFPS(Mathf.RoundToInt(_fpsSlider.value), true);
      updateSliderVisualValues();
   }

   private void durationChanged (float val) {
      GIFReplayManager.self.setDuration(Mathf.RoundToInt(_durationSlider.value), true);
      updateSliderVisualValues();
   }

   private void updateSliderVisualValues () {
      float res = _resolutionSlider.value <= 0 ? 1f : _resolutionSlider.value;
      _resolutionSliderText.text = Mathf.RoundToInt(100f / res) + "%";

      _fpsSliderText.text = Mathf.RoundToInt(_fpsSlider.value).ToString();
      _durationSliderText.text = Mathf.RoundToInt(_durationSlider.value) + " s";
   }

   public override void Update () {
      base.Update();

      _memoryEstimationText.text = "Estimated GIF size at current resolution:" +
         System.Environment.NewLine + GIFReplayManager.self.getMemoryEstimation();
   }

   #region Private Variables

   // Various UI components we are handling
   [SerializeField] private Toggle _gifEnabledToggle = null;
   [SerializeField] private Slider _resolutionSlider = null;
   [SerializeField] private Slider _fpsSlider = null;
   [SerializeField] private Slider _durationSlider = null;
   [SerializeField] private Text _resolutionSliderText = null;
   [SerializeField] private Text _fpsSliderText = null;
   [SerializeField] private Text _durationSliderText = null;
   [SerializeField] private Text _memoryEstimationText = null;

   #endregion
}
