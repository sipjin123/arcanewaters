using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.SceneManagement;

public class SoundEffectToolUI : MonoBehaviour
{
   #region Public Variables

   // Tool that this UI is attached unto
   public SoundEffectTool tool;

   // The first effect that will be used as an instantiation template (already present in the scene)
   public SoundEffectUI firstEffectPreviewObjectTemplate;

   // The Edit Screen that should open when wanting to Edit a SoundEffect
   public GameObject editScreen;

   // The name of the effect that can be edited
   public InputField nameInputField;

   // The Audio Clip Name associated with this Sound Effect
   public Text clipNameText;

   // Button to press when we should play the effect's audio clip
   public Button playClipButton;

   // The Random Volume Toggle
   public Toggle randomVolumeToggle;

   // The Non-Random Volume Container
   public GameObject nonRandomVolumeContainer;

   // The Random Volume Container
   public GameObject randomVolumeContainer;

   // The Volume the Sound Effect should have when playing
   public Slider volumeSlider;

   // The Minimum Volume the Sound Effect should have when playing
   public Slider minVolumeSlider;

   // The Maximum Volume the Sound Effect should have when playing
   public Slider maxVolumeSlider;

   // The displayed Volume amount
   public Text volumeAmountText;

   // The displayed Minimum Volume amount
   public Text minVolumeAmountText;

   // The displayed Maximum Volume amount
   public Text maxVolumeAmountText;

   // The Random Pitch Toggle
   public Toggle randomPitchToggle;

   // The Non-Random Pitch Container
   public GameObject nonRandomPitchContainer;

   // The Random Pitch Container
   public GameObject randomPitchContainer;

   // The Pitch the Sound Effect should have when playing
   public Slider pitchSlider;

   // The Minimum Pitch the Sound Effect should have when playing
   public Slider minPitchSlider;

   // The Maximum Pitch the Sound Effect should have when playing
   public Slider maxPitchSlider;

   // The displayed Pitch amount
   public Text pitchAmountText;

   // The displayed Minimum Pitch amount
   public Text minPitchAmountText;

   // The displayed Maximum Pitch amount
   public Text maxPitchAmountText;

   // The Starting Offset of the AudipClip when  playing
   public Slider offsetSlider;

   // The displayed Starting Offset amount in Seconds
   public Text offsetAmountText;

   // The screen that will let you pick and link Audio Clips to the Sound Effect
   public GameObject pickAudioClipScreen;

   // The clip selection template (already present in the scene)
   public SoundEffectClipUI firstAudioClipPreviewObjectTemplate;

   // Opens the main tool
   public Button mainTool;

   // Refreshes the SoundEffects preview screen
   public Button refreshButton;

   #endregion

   private void Start () {
      mainTool.onClick.AddListener(() => {
         SceneManager.LoadScene(MasterToolScene.masterScene);
      });

      refreshButton.onClick.AddListener(() => {
         tool.loadXML();
      });
   }

   public void populatePreviews (List<SoundEffect> effects) {
      // Clear previous entries
      if (_effectUIs != null) {
         for (int iEntry = _effectUIs.Count - 1; iEntry >= 0; --iEntry) {
            Destroy(_effectUIs[iEntry].gameObject);
         }
      }
      _effectUIs = new List<SoundEffectUI>();

      // Create and assign new entries
      foreach (SoundEffect effect in effects) {
         SoundEffectUI newEffectUIObject = Instantiate(firstEffectPreviewObjectTemplate.gameObject, firstEffectPreviewObjectTemplate.transform.parent).GetComponent<SoundEffectUI>();
         newEffectUIObject.init(effect);
         newEffectUIObject.gameObject.SetActive(true);
         _effectUIs.Add(newEffectUIObject);
      }
   }

   public void populateClips (List<AudioClip> clips) {
      // Clear previous entries
      if (_clipUIs != null) {
         for (int iEntry = _clipUIs.Count - 1; iEntry >= 0; --iEntry) {
            Destroy(_clipUIs[iEntry].gameObject);
         }
      }
      _clipUIs = new List<SoundEffectClipUI>();

      foreach (AudioClip iClip in clips) {
         SoundEffectClipUI newClipUIObject = Instantiate(firstAudioClipPreviewObjectTemplate.gameObject, firstAudioClipPreviewObjectTemplate.transform.parent).GetComponent<SoundEffectClipUI>();
         newClipUIObject.init(iClip);
         newClipUIObject.gameObject.SetActive(true);
         _clipUIs.Add(newClipUIObject);
      }
   }

   public void onEditSoundEffectClicked (SoundEffectUI effectUI) {
      _currentEditingSoundEffectUI = effectUI;

      nameInputField.SetTextWithoutNotify(effectUI.effect.name);
      clipNameText.text = effectUI.effect.clipName;
      if (string.IsNullOrEmpty(_currentEditingSoundEffectUI.effect.clipName)) {
         clipNameText.text = NO_CLIP;
      } else if (!string.IsNullOrEmpty(_currentEditingSoundEffectUI.effect.clipName) && _currentEditingSoundEffectUI.effect.clip == null) {
         clipNameText.text += BROKEN_LINK;
      }

      playClipButton.gameObject.SetActive(effectUI.effect.clip != null);

      minVolumeSlider.SetValueWithoutNotify(effectUI.effect.minVolume);
      maxVolumeSlider.SetValueWithoutNotify(effectUI.effect.maxVolume);
      volumeSlider.SetValueWithoutNotify(Mathf.Lerp(minVolumeSlider.value, maxVolumeSlider.value, 0.5f));
      randomVolumeToggle.SetIsOnWithoutNotify(!Mathf.Approximately(effectUI.effect.minVolume, effectUI.effect.maxVolume));
      nonRandomVolumeContainer.SetActive(!randomVolumeToggle.isOn);
      randomVolumeContainer.SetActive(randomVolumeToggle.isOn);
      if (randomVolumeToggle.isOn) {

         onSoundEffectEditMinVolumeSliderChanged();
         onSoundEffectEditMaxVolumeSliderChanged();
      } else {
         onSoundEffectEditVolumeSliderChanged();
      }

      minPitchSlider.SetValueWithoutNotify(effectUI.effect.minPitch);
      maxPitchSlider.SetValueWithoutNotify(effectUI.effect.maxPitch);
      pitchSlider.SetValueWithoutNotify(Mathf.Lerp(minPitchSlider.value, maxPitchSlider.value, 0.5f));
      randomPitchToggle.SetIsOnWithoutNotify(!Mathf.Approximately(effectUI.effect.minPitch, effectUI.effect.maxPitch));
      nonRandomPitchContainer.SetActive(!randomPitchToggle.isOn);
      randomPitchContainer.SetActive(randomPitchToggle.isOn);
      if (randomPitchToggle.isOn) {

         onSoundEffectEditMinPitchSliderChanged();
         onSoundEffectEditMaxPitchSliderChanged();
      } else {
         onSoundEffectEditPitchSliderChanged();
      }

      offsetSlider.value = effectUI.effect.offset;

      float offsetAmount = offsetSlider.value;
      if (effectUI.effect.clip != null) {
         offsetAmount = offsetAmount * effectUI.effect.clip.length;
      }
      offsetAmountText.text = string.Format("{0:n2}s", offsetAmount);

      editScreen.SetActive(true);
   }

   public void onCreateSoundEffectClicked () {
      tool.createSoundEffect();
   }

   public void onDuplicateSoundEffectClicked (SoundEffectUI effectUI) {
      tool.duplicateSoundEffect(effectUI.effect);
   }

   public void onDeleteSoundEffectClicked (SoundEffectUI effectUI) {
      tool.deleteSoundEffect(effectUI.effect);
   }

   public void onSoundEffectEditCloseClicked () {
      editScreen.SetActive(false);
   }

   public void onSoundEffectEditNameChanged () {
      _currentEditingSoundEffectUI.effect.name = nameInputField.text;
      _currentEditingSoundEffectUI.onSoundEffectUpdated();
   }

   public void onSoundEffectEditRandomVolumeToggled () {
      nonRandomVolumeContainer.SetActive(!randomVolumeToggle.isOn);
      randomVolumeContainer.SetActive(randomVolumeToggle.isOn);

      if (!randomVolumeToggle.isOn) {
         volumeSlider.SetValueWithoutNotify(Mathf.Lerp(minVolumeSlider.value, maxVolumeSlider.value, 0.5f));
         onSoundEffectEditVolumeSliderChanged();
      } else {
         minVolumeSlider.SetValueWithoutNotify(volumeSlider.value);
         maxVolumeSlider.SetValueWithoutNotify(volumeSlider.value);
         onSoundEffectEditMinVolumeSliderChanged();
         onSoundEffectEditMaxVolumeSliderChanged();
      }
   }

   public void onSoundEffectEditVolumeSliderChanged () {
      volumeAmountText.text = string.Format("{0:n2}", volumeSlider.value);
      _currentEditingSoundEffectUI.effect.minVolume = volumeSlider.value;
      _currentEditingSoundEffectUI.effect.maxVolume = volumeSlider.value;
      _currentEditingSoundEffectUI.onSoundEffectUpdated();
   }

   public void onSoundEffectEditMinVolumeSliderChanged () {
      minVolumeAmountText.text = string.Format("{0:n2}", minVolumeSlider.value);
      _currentEditingSoundEffectUI.effect.minVolume = minVolumeSlider.value;

      if (minVolumeSlider.value > _currentEditingSoundEffectUI.effect.maxVolume) {
         maxVolumeSlider.SetValueWithoutNotify(minVolumeSlider.value);
         _currentEditingSoundEffectUI.effect.maxVolume = minVolumeSlider.value;
         maxVolumeAmountText.text = string.Format("{0:n2}", minVolumeSlider.value);
      }

      _currentEditingSoundEffectUI.onSoundEffectUpdated();
   }

   public void onSoundEffectEditMaxVolumeSliderChanged () {
      maxVolumeAmountText.text = string.Format("{0:n2}", maxVolumeSlider.value);
      _currentEditingSoundEffectUI.effect.maxVolume = maxVolumeSlider.value;

      if (maxVolumeSlider.value < _currentEditingSoundEffectUI.effect.minVolume) {
         minVolumeSlider.SetValueWithoutNotify(maxVolumeSlider.value);
         _currentEditingSoundEffectUI.effect.minVolume = maxVolumeSlider.value;
         minVolumeAmountText.text = string.Format("{0:n2}", maxVolumeSlider.value);
      }

      _currentEditingSoundEffectUI.onSoundEffectUpdated();
   }

   public void onSoundEffectEditRandomPitchToggled () {
      nonRandomPitchContainer.SetActive(!randomPitchToggle.isOn);
      randomPitchContainer.SetActive(randomPitchToggle.isOn);

      if (!randomPitchToggle.isOn) {
         pitchSlider.SetValueWithoutNotify(Mathf.Lerp(minPitchSlider.value, maxPitchSlider.value, 0.5f));
         onSoundEffectEditPitchSliderChanged();
      } else {
         minPitchSlider.SetValueWithoutNotify(pitchSlider.value);
         maxPitchSlider.SetValueWithoutNotify(pitchSlider.value);
         onSoundEffectEditMinPitchSliderChanged();
         onSoundEffectEditMaxPitchSliderChanged();
      }
   }

   public void onSoundEffectEditPitchSliderChanged () {
      pitchAmountText.text = string.Format("{0:n2}", pitchSlider.value);
      _currentEditingSoundEffectUI.effect.minPitch = pitchSlider.value;
      _currentEditingSoundEffectUI.effect.maxPitch = pitchSlider.value;
      _currentEditingSoundEffectUI.onSoundEffectUpdated();
   }

   public void onSoundEffectEditMinPitchSliderChanged () {
      minPitchAmountText.text = string.Format("{0:n2}", minPitchSlider.value);
      _currentEditingSoundEffectUI.effect.minPitch = minPitchSlider.value;

      if (minPitchSlider.value > _currentEditingSoundEffectUI.effect.maxPitch) {
         maxPitchSlider.SetValueWithoutNotify(minPitchSlider.value);
         _currentEditingSoundEffectUI.effect.maxPitch = minPitchSlider.value;
         maxPitchAmountText.text = string.Format("{0:n2}", minPitchSlider.value);
      }

      _currentEditingSoundEffectUI.onSoundEffectUpdated();
   }

   public void onSoundEffectEditMaxPitchSliderChanged () {
      maxPitchAmountText.text = string.Format("{0:n2}", maxPitchSlider.value);
      _currentEditingSoundEffectUI.effect.maxPitch = maxPitchSlider.value;

      if (maxPitchSlider.value < _currentEditingSoundEffectUI.effect.minPitch) {
         minPitchSlider.SetValueWithoutNotify(maxPitchSlider.value);
         _currentEditingSoundEffectUI.effect.minPitch = maxPitchSlider.value;
         minPitchAmountText.text = string.Format("{0:n2}", maxPitchSlider.value);
      }

      _currentEditingSoundEffectUI.onSoundEffectUpdated();
   }

   public void onSoundEffectEditOffsetSliderChanged () {

      float offsetAmount = offsetSlider.value;
      if (_currentEditingSoundEffectUI.effect.clip != null) {
         offsetAmount = offsetAmount * _currentEditingSoundEffectUI.effect.clip.length;
      }
      offsetAmountText.text = string.Format("{0:n2}s", offsetAmount);
      _currentEditingSoundEffectUI.effect.offset = offsetSlider.value;
      _currentEditingSoundEffectUI.onSoundEffectUpdated();
   }

   public void onSoundEffectEditSaveAndSubmitClicked () {
      tool.updateEffect(_currentEditingSoundEffectUI.effect);

      editScreen.SetActive(false);
   }

   public void onSoundEffectEditPlayClipClicked () {
      tool.playEffect(_currentEditingSoundEffectUI.effect);
   }

   public void onSoundEffectEditPickClipButtonClicked () {
      pickAudioClipScreen.SetActive(true);
   }

   public void onPickClipPlayClicked (SoundEffectClipUI clipUI) {
      tool.playClip(clipUI.clip);
   }

   public void onPickClipAssignClicked (SoundEffectClipUI clipUI) {
      clipNameText.text = clipUI.clip.name;
      _currentEditingSoundEffectUI.effect.clipName = clipUI.clip.name;
      _currentEditingSoundEffectUI.effect.clip = clipUI.clip;
      _currentEditingSoundEffectUI.onSoundEffectUpdated();

      float offsetAmount = offsetSlider.value;
      if (_currentEditingSoundEffectUI.effect.clip != null) {
         offsetAmount = offsetAmount * _currentEditingSoundEffectUI.effect.clip.length;
      }
      offsetAmountText.text = string.Format("{0:n2}s", offsetAmount);

      playClipButton.gameObject.SetActive(true);

      pickAudioClipScreen.SetActive(false);
   }

   public void onPickClipExitClicked () {
      pickAudioClipScreen.SetActive(false);
   }

   #region Private Variables

   // If there's no clip name at all, display this on the AudioClip Button
   private const string NO_CLIP = "<color=red>Please click me to assign AudioClip</color>";

   // If there's a clip name but the AudioClip doesn't exist anymore, display this on the AudioClip Button
   private const string BROKEN_LINK = " <color=red>(Please reassign)</color>";

   // The SoundEffectUI that is currently being updated
   private SoundEffectUI _currentEditingSoundEffectUI;

   // The instantiated previews of the effects
   private List<SoundEffectUI> _effectUIs;

   // The instantiated previews of the AudioClips
   private List<SoundEffectClipUI> _clipUIs;

   #endregion
}
