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

   // The AudioSource used to play 3D SoundEffects
   public AudioSource source3D;

   // The database id of the jump start
   public const int JUMP_START_ID = 5;

   // The database id of the jump end
   public const int JUMP_END_ID = 6;

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
               if (!_soundEffects.ContainsKey(effect.id)) {
                  findAndAssignAudioClip(effect);
                  _soundEffects.Add(effect.id, effect);
               }
            }
            _hasInitialized = true;
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

   public void playSoundEffect (int id, Transform target) {
      SoundEffect effect;

      if (_soundEffects.TryGetValue(id, out effect)) {
         if (effect.is3D) {
            playSoundEffect3D(effect, target);
         }  else {
            source.clip = effect.clip;
            if (effect.clip == null) {
               D.debug("Missing Sound Effect ID: " + id);
               return;
            }
            effect.calibrateSource(source);

            source.loop = false;
            source.Play();
         }
         
      } else if (id >= 0) {
         D.debug("Could not find SoundEffect with 'id' : '" + id + "'");
      }
   }

   private void playSoundEffect3D (SoundEffect effect, Transform target) {
      // Setup audio player
      AudioSource audioSource = Instantiate(PrefabsManager.self.sound3dPrefab, target.position, Quaternion.identity);
      audioSource.transform.SetParent(target, true);

      // Makes sure audio source z-position matches camera, for 3D sound
      Vector3 audioSourcePos = audioSource.transform.position;
      audioSourcePos.z = Camera.main.transform.position.z;
      audioSource.transform.position = audioSourcePos;

      audioSource.clip = effect.clip;
      audioSource.Play();

      // Destroy object after clip finishes playing
      Destroy(audioSource.gameObject, audioSource.clip.length);
   }

   private void findAndAssignAudioClip (SoundEffect effect) {
      AudioClip foundClip = _projectAudioClips.Find(iClip => iClip.name.Equals(effect.clipName));
      if (foundClip != null) {
         effect.clip = foundClip;
      } else if (!string.IsNullOrEmpty(effect.clipName)) {
         D.debug("SoundEffect '" + effect.name + "' has an invalid AudioClip link: '" + effect.clipName + "'");
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
