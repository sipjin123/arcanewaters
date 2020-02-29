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

   // The Volume the Sound Effect should have when playing
   public Slider volumeSlider;

   // The displayed Volume amount
   public Text volumeAmountText;

   // The Pitch the Sound Effect should have when playing
   public Slider pitchSlider;

   // The displayed Pitch amount
   public Text pitchAmountText;

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

      volumeSlider.minValue = 0.0f;
      volumeSlider.maxValue = 1.0f;
      volumeSlider.value = effectUI.effect.volume;
      volumeAmountText.text = string.Format("{0:n2}", volumeSlider.value);

      pitchSlider.minValue = -3.0f;
      pitchSlider.maxValue = 3.0f;
      pitchSlider.value = effectUI.effect.pitch;
      pitchAmountText.text = string.Format("{0:n2}", pitchSlider.value);

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

   public void onSoundEffectEditVolumeSliderChanged () {
      volumeAmountText.text = string.Format("{0:n2}", volumeSlider.value);
      _currentEditingSoundEffectUI.effect.volume = volumeSlider.value;
      _currentEditingSoundEffectUI.onSoundEffectUpdated();
   }

   public void onSoundEffectEditPitchSliderChanged () {
      pitchAmountText.text = string.Format("{0:n2}", pitchSlider.value);
      _currentEditingSoundEffectUI.effect.pitch = pitchSlider.value;
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
