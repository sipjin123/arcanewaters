using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SoundEffectManager : MonoBehaviour
{
   #region Public Variables

   // The self
   public static SoundEffectManager self;

   // The AudioSource used to play the SoundEffects
   public AudioSource source;

   #endregion

   private void Awake () {
      self = this;
   }

   private void Start () {
      _projectAudioClips = new List<AudioClip>(Resources.LoadAll<AudioClip>(RESOURCE_FOLDER_PATH));
   }

   public void initializeDataCache () {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<SoundEffect> fetchedSoundEffects = DB_Main.getSoundEffects();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (SoundEffect effect in fetchedSoundEffects) {
               findAndAssignAudioClip(effect);
               _soundEffects.Add(effect.id, effect);

               _hasInitialized = true;
            }
         });
      });
   }

   public void receiveListFromServer (SoundEffect[] effects) {
      if (!_hasInitialized) {
         foreach (SoundEffect effect in effects) {
            findAndAssignAudioClip(effect);
            _soundEffects.Add(effect.id, effect);
         }

         _hasInitialized = true;
      }
   }

   public SoundEffect getSoundEffect (int id) {
      SoundEffect data = null;
      _soundEffects.TryGetValue(id, out data);

      return data;
   }

   public List<SoundEffect> getAllSoundEffects () {
      return new List<SoundEffect>(_soundEffects.Values);
   }

   public bool isValidSoundEffect (int id) {
      return _soundEffects.ContainsKey(id);
   }

   public void playSoundEffect (int id) {
      SoundEffect effect;
      if (_soundEffects.TryGetValue(id, out effect)) {
         source.clip = effect.clip;
         source.volume = effect.calculateValue(SoundEffect.ValueType.VOLUME);
         source.pitch = effect.calculateValue(SoundEffect.ValueType.PITCH);

         if (source.pitch < 0.0f) {
            source.timeSamples = Mathf.Clamp(
               Mathf.FloorToInt(source.clip.samples - 1 - source.clip.samples * effect.offset),
               0,
               source.clip.samples - 1);
         } else {
            source.timeSamples = Mathf.Clamp(
               Mathf.FloorToInt(source.clip.samples * effect.offset),
               0,
               source.clip.samples - 1);
         }

         source.loop = false;
         source.Play();
      } else if (id >= 0) {
         D.log("Could not find SoundEffect with 'id' : '" + id + "'");
      }
   }

   private void findAndAssignAudioClip (SoundEffect effect) {
      AudioClip foundClip = _projectAudioClips.Find(iClip => iClip.name.Equals(effect.clipName));
      if (foundClip != null) {
         effect.clip = foundClip;
      } else {
         Debug.LogWarning("Found a SoundEffect from the DB that didn't have an associated AudioClip in the project.\nPlease fix this in the SoundEffectTool.");
      }
   }

   #region Private Variables

   // Holds the path of the folder containing Sound Effects (with the Resource folder as a base)
   private const string RESOURCE_FOLDER_PATH = "Sound/Effects";

   // The SoundEffects that has been stored on the DB
   private Dictionary<int, SoundEffect> _soundEffects = new Dictionary<int, SoundEffect>();

   // All the Audio Clips that are present in the Project
   private List<AudioClip> _projectAudioClips;

   // If xml data is initialized
   private bool _hasInitialized;

   #endregion
}
