using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SoundEffectTool : XmlDataToolManager
{
   #region Public Variables

   // Holds the path of the folder containing Sound Effects (with the Resource folder as a base)
   public const string RESOURCE_FOLDER_PATH = "Sound/Effects";

   // The name string addition that will be appended to the SoundEffect Name when duplicated
   public const string DUPLICATE = "(duplicate)";

   // The AudioSource we use to play AudioClips for the SoundEffects
   public AudioSource soundAudioSource;

   // The Scene reference to the Main UI
   public SoundEffectToolUI mainUI;

   #endregion

   protected override void Awake () {
      base.Awake();
      self = this;
   }

   private void Start () {
      Invoke("loadXML", MasterToolScene.loadDelay);

      _projectAudioClips = new List<AudioClip>(Resources.LoadAll<AudioClip>(RESOURCE_FOLDER_PATH));
      mainUI.populateClips(_projectAudioClips);
   }

   public void loadXML () {
      XmlLoadingPanel.self.startLoading();

      _soundEffects = new List<SoundEffect>();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         _soundEffects = DB_Main.getSoundEffects();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (SoundEffect effect in _soundEffects) {
               findAndAssignAudioClip(effect);
            }

            mainUI.populatePreviews(_soundEffects);

            XmlLoadingPanel.self.finishLoading();
         });
      });
   }

   public void updateEffect (SoundEffect effect) {
      XmlLoadingPanel.self.startLoading();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateSoundEffect(effect);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXML();
         });
      });
   }

   public void playEffect (SoundEffect effect) {
      soundAudioSource.clip = effect.clip;
      effect.calibrateSource(soundAudioSource);
      soundAudioSource.loop = false;
      soundAudioSource.Play();
   }

   public void playClip (AudioClip clip) {
      soundAudioSource.clip = clip;
      soundAudioSource.volume = 1.0f;
      soundAudioSource.pitch = 1.0f;

      soundAudioSource.loop = false;
      soundAudioSource.Play();
   }

   public void createSoundEffect () {
      XmlLoadingPanel.self.startLoading();

      SoundEffect newSoundEffect = new SoundEffect();
      newSoundEffect.id = findFirstUniqueID();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateSoundEffect(newSoundEffect);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXML();
         });
      });
   }

   public void duplicateSoundEffect (SoundEffect effect) {
      XmlLoadingPanel.self.startLoading();

      SoundEffect newSoundEffect = new SoundEffect();
      newSoundEffect.id = findFirstUniqueID();

      if (!effect.name.Contains(DUPLICATE)) {
         newSoundEffect.name = effect.name + " " + DUPLICATE;
      } else {
         newSoundEffect.name = effect.name;
      }
      if (newSoundEffect.name.Length > NUM_CHARACTERS_ALLOWED_FOR_NAME) {
         newSoundEffect.name = newSoundEffect.name.Substring(0, NUM_CHARACTERS_ALLOWED_FOR_NAME);
      }

      newSoundEffect.clip = effect.clip;
      newSoundEffect.clipName = effect.clipName;
      newSoundEffect.minVolume = effect.minVolume;
      newSoundEffect.maxVolume = effect.maxVolume;
      newSoundEffect.minPitch = effect.minPitch;
      newSoundEffect.maxPitch = effect.maxPitch;
      newSoundEffect.offset = effect.offset;

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.updateSoundEffect(newSoundEffect);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXML();
         });
      });
   }

   public void deleteSoundEffect (SoundEffect effect) {
      XmlLoadingPanel.self.startLoading();

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.deleteSoundEffect(effect);

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            loadXML();
         });
      });
   }

   private void findAndAssignAudioClip (SoundEffect effect) {
      AudioClip foundClip = _projectAudioClips.Find(iClip => iClip.name.Equals(effect.clipName));
      if (foundClip != null) {
         effect.clip = foundClip;
      } else {
         Debug.LogWarning("Found a Sound Effect from the DB that didn't have an associated Audio Clip in the project.\nPlease fix this.");
      }
   }

   private int findFirstUniqueID () {
      List<SoundEffect> sortedEffects = new List<SoundEffect>(_soundEffects);
      sortedEffects.Sort((effect1, effect2) => effect1.id.CompareTo(effect2.id));

      // If there's no effects yet, return the default 0
      if (sortedEffects.Count == 0) {
         return 0;
      }

      // If there are effects in the list and the first one isn't Id 0, then return 0
      if (sortedEffects.Count > 0 && sortedEffects[0].id > 0) {
         return 0;
      }

      // If there are effects, find the first gap in the Id sequence
      for (int iEffect = 1; iEffect < sortedEffects.Count; ++iEffect) {
         SoundEffect previousEffect = sortedEffects[iEffect - 1];
         SoundEffect currentEffect = sortedEffects[iEffect];

         // If there's an id gap larger than 1, it means we've found an unoccupied id we can use for the new effect
         if (currentEffect.id - previousEffect.id > 1) {
            return previousEffect.id + 1;
         }
      }

      // If there's no gap in the sequence, and the default first id 0 is taken, return a new last Id
      return sortedEffects[sortedEffects.Count - 1].id + 1;
   }

   #region Private Variables

   // How many letters in the SoundEffect Name that the DB will allow
   private const int NUM_CHARACTERS_ALLOWED_FOR_NAME = 45;

   // All the Audio Clips that are present in the Project
   private List<AudioClip> _projectAudioClips;

   // All the SoundEffects that are currently in the DB
   private List<SoundEffect> _soundEffects;

   #endregion
}
