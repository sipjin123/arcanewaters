using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Xml.Serialization;
using System.Text;
using System.Xml;
using FMODUnity;
using FMOD.Studio;

public class SoundEffectManager : GenericGameManager
{
   #region Public Variables

   // The self
   public static SoundEffectManager self;

   // The AudioSource used to play the SoundEffects
   public AudioSource source;

   // The AudioSource used to play 3D SoundEffects
   public AudioSource source3D;

   // The database id of the jump start
   public const int JUMP_START_ID = 3;

   // The database id of the jump end
   public const int JUMP_END_ID = 6;

   // The database id of the sound effect
   public const int HARVESTING_PITCHFORK_HIT = 43;

   // The database id of the sound effect
   public const int HARVESTING_FLYING = 44;

   // In game action sound effects
   public const int WATERING_PLANTS = 51;
   public const int ORE_MINE = 52;
   public const int ORE_DROP = 53;
   public const int SHIPBOOST_ID = 54;
   public const int ORE_PICKUP = 55;
   public const int PICKUP_EDIT_OBJ = 56;
   public const int DROP_EDIT_OBJ = 57;
   public const int NEXTPREFAB_SELECTION = 58;

   public const int PICKUP_POWERUP = 59;
   public const int SHORTCUT_SELECTION = 60;
   public const int ABILITY_SELECTION = 61;
   public const int STANCE_SELECTION = 62;

   public const int INVENTORY_HOVER = 63;
   public const int INVENTORY_DRAG_START = 64;
   public const int INVENTORY_DROP = 65;

   public const int NPC_PANEL_POPUP = 66;
   public const int ENTER_DOOR = 67;
   public const int CRAFT_COMPLETE = 68;
   public const int REFINE_COMPLETE = 69;
   public const int MAIL_NOTIF = 70;

   public const int OPEN_SEA_BAG = 71;
   public const int OPEN_LAND_BAG = 72;
   public const int OPEN_CHEST = 73;

   public const int OCEAN_PAD = 74;

   public const int BATTLE_INTRO = 81;
   public const int BATTLE_OUTRO = 82;

   public const int SHIP_CANNON = 85;
   public const int FISH_JUMP = 86;
   public const int FOOTSTEP = 88;
   public const int THROW_SEEDS = 89;

   public const int CALMING_WATERFALL = 90;
   public const int ROCK_MINE = 91;
   public const int SHIP_LAUNCH_CHARGE = 92;
   public const int PICKUP_CROP = 93;

   public const int ENEMY_SHIP_IMPACT = 94;
   public const int PLAYER_SHIP_IMPACT = 95;

   public const int LIGHTNING_FLASH = 96;
   public const int AMBIENCE_BED_MASTER = 97;

   public const int BLOCK_ATTACK = 98;

   public const int MAP_OPEN = 99;
   public const int LOCALE_UNLOCK = 100;
   public const int CLICK_TAB = 101;
   //public const int MENU_OPEN = 102;
   public const int BUTTON_CONFIRM = 103;

   public const int ENEMY_SHIP_DESTROYED = 108;
   public const int CANNONBALL_IMPACT = 109;
   public const int PURCHASE_ITEM = 110;
   public const int ASSIGN_PERK_POINT = 111;
   public const int UNASSIGN_PERK_POINT = 112;
   public const int TIP_FOLDOUT = 113;

   public const string MENU_OPEN = "event:/SFX/Game/UI/Menu_Open";
   public const string BUTTON_CONFIRM_PATH = "event:/SFX/Game/UI/Button_Confirm";
   public const string HOVER_CURSOR_GENERIC = "event:/SFX/Game/UI/Hover_Cursor_Generic";
   public const string HOVER_CURSOR_ITEMS = "event:/SFX/Game/UI/Hover_Cursor_Items";

   public const string PLAYER_SHIP_DESTROYED = "event:/SFX/Game/Sea_Battle/Player_Ship_Destroyed";

   public const string GENERIC_HIT_LAND = "event:/SFX/Game/Land_Battle/Generic_Hit_Land";

   public const string COLLECT_SILVER = "event:/SFX/Game/Collect_Silver";

   public const string AUDIO_SWITCH_PARAM = "Audio_Switch";
   public const string SHIP_CHARGE_RELEASE_PARAM = "Ship_Charge_Release";
   public const string AMBIENCE_SWITCH_PARAM = "Ambience_Switch";
   public const string APPLY_CRIT_PARAM = "Apply_Crit";
   public const string WEATHER_PARAM = "Weather_Effects";

   public const string BG_MUSIC = "event:/Music/BGM_Master";

   public EventInstance bgMusicEvent;

   public enum CannonballImpactType
   {
      Water = 0
   }

   #endregion

   protected override void Awake () {
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
      SoundEffect data;
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
         } else {
            source.clip = effect.clip;
            if (effect.clip == null) {
               D.debug("Missing Sound Effect ID: " + id);
               return;
            }
            effect.calibrateSource(source);
            source.volume = effect.minVolume;
            source.loop = false;
            source.Play();
         }

      } else if (id >= 0) {
         D.debug("Could not find SoundEffect with 'id' : '" + id + "'");
      }
   }

   public void playFmodWithPath (string path, Transform target) {
      if (Util.isBatch()) {
         return;
      }

      RuntimeManager.PlayOneShot(path, target.position);
   }

   public void playFmodOneShot (int id, Transform target) {
      if (Util.isBatch()) {
         return;
      }

      SoundEffect effect = getSoundEffect(id);

      if (effect != null) {
         if (effect.fmodId.Length > 0) {
            RuntimeManager.PlayOneShot(effect.fmodId, target.position);
         }
      }
   }

   public void playCannonballImpact (CannonballImpactType impactType, Vector3 position) {
      SoundEffect effect = getSoundEffect(CANNONBALL_IMPACT);

      if (effect != null) {
         EventInstance impactEvent = RuntimeManager.CreateInstance(effect.fmodId);
         impactEvent.setParameterByName(AUDIO_SWITCH_PARAM, (int) impactType);
         impactEvent.set3DAttributes(RuntimeUtils.To3DAttributes(position));
         impactEvent.start();
         impactEvent.release();
      }
   }
   public void playBgMusic (SoundManager.Type musicType) {
      if (Util.isBatch()) {
         return;
      }

      if (!bgMusicEvent.isValid()) {
         bgMusicEvent = RuntimeManager.CreateInstance(BG_MUSIC);
      }

      int param = -1;

      switch (musicType) {
         case SoundManager.Type.Town_Forest:
            param = 0;
            break;
         case SoundManager.Type.Town_Desert:
            param = 1;
            break;
         case SoundManager.Type.Town_Snow:
            param = 2;
            break;
         case SoundManager.Type.Town_Lava:
            param = 3;
            break;
         case SoundManager.Type.Town_Pine:
            param = 4;
            break;
         case SoundManager.Type.Town_Mushroom:
            param = 5;
            break;
         case SoundManager.Type.Intro_Music:
            param = 6;
            break;
         case SoundManager.Type.Farm_Music:
            param = 7;
            break;
         case SoundManager.Type.Interior:
            param = 8;
            break;
         case SoundManager.Type.Sea_Forest:
         case SoundManager.Type.Sea_Desert:
         case SoundManager.Type.Sea_Pine:
         case SoundManager.Type.Sea_Snow:
         case SoundManager.Type.Sea_Lava:
         case SoundManager.Type.Sea_Mushroom:
            param = 9;
            break;
      }

      PLAYBACK_STATE playbackState;
      bgMusicEvent.getPlaybackState(out playbackState);
      if (playbackState != PLAYBACK_STATE.PLAYING) {
         bgMusicEvent.start();
      }

      if (param == -1) {
         bgMusicEvent.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
      }

      bgMusicEvent.setParameterByName(AMBIENCE_SWITCH_PARAM, param);
   }

   public void playFmod2D (int id) {
      if (Util.isBatch()) {
         return;
      }

      playFmodOneShot(id, CameraManager.getCurrentCamera().transform);
   }

   public void playGuiButtonConfirmSfx () {
      playFmod2DWithPath(BUTTON_CONFIRM_PATH);
   }

   public void playGuiMenuOpenSfx () {
      playFmod2DWithPath(MENU_OPEN);
   }

   public void playFmodGuiHover (string path) {
      if (_lastPlayTime.ContainsKey(path) && Time.time - _lastPlayTime[path] < .10f) {
         return;
      }

      // Make note of the time
      _lastPlayTime[path] = Time.time;

      self.playFmod2DWithPath(path);
   }

   public void playLegacyInteractionOneShot (int equipmentDataId, Transform target) {
      WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(equipmentDataId);
      if (weaponData != null && weaponData.actionSfxDirectory.Length > 1) {
         SoundManager.create3dSoundWithPath(weaponData.actionSfxDirectory, target.position);
      }
   }

   public void playInteractionOneShot (Weapon.ActionType weaponAction, Transform target) {
      switch (weaponAction) {
         case Weapon.ActionType.PlantCrop:
            self.playFmodOneShot(THROW_SEEDS, target);
            break;
         case Weapon.ActionType.WaterCrop:
            self.playFmodOneShot(WATERING_PLANTS, target);
            break;
      }
   }

   public void playEnemyHitOneShot (bool isShip, Transform target) {
      SoundEffect effect = getSoundEffect(ENEMY_SHIP_IMPACT);

      if (effect != null) {
         EventInstance eventInstance = RuntimeManager.CreateInstance(effect.fmodId);

         if (isShip) {
            eventInstance.setParameterByName(AUDIO_SWITCH_PARAM, 0);
         } else {
            eventInstance.setParameterByName(AUDIO_SWITCH_PARAM, 1);
         }

         eventInstance.set3DAttributes(RuntimeUtils.To3DAttributes(target));
         eventInstance.start();
         eventInstance.release();
      }
   }

   public void playFmod2DWithPath (string path) {
      if (path.Length > 0) {
         RuntimeManager.PlayOneShot(path, CameraManager.getCurrentCamera().transform.position);
      }
   }

   private void playSoundEffect3D (SoundEffect effect, Transform target) {
      // Setup audio player
      AudioSource audioSource = Instantiate(PrefabsManager.self.sound3dPrefab, target.position, Quaternion.identity);
      audioSource.transform.SetParent(target, true);
      audioSource.clip = effect.clip;
      audioSource.volume = effect.minVolume;
      audioSource.Play();

      // Destroy object after clip finishes playing
      Destroy(audioSource.gameObject, audioSource.clip.length);
   }

   public void playFmodWithDelay (int id, float delay, Transform target = null, bool is3D = false) {
      StartCoroutine(CO_PlayFmodAfterDelay(id, delay, target, is3D));
   }

   private IEnumerator CO_PlayFmodAfterDelay (int id, float delay, Transform target = null, bool is3D = false) {
      yield return new WaitForSeconds(delay);

      if (is3D && target != null) {
         self.playFmodOneShot(id, target);
      } else {
         self.playFmod2D(id);
      }
   }

   public IEnumerator CO_DestroyAfterEnd (StudioEventEmitter emitter) {
      while (emitter != null && emitter.IsPlaying()) {
         yield return 0;
      }
      if (emitter != null) {
         Destroy(emitter.gameObject);
      }
   }

   private void findAndAssignAudioClip (SoundEffect effect) {
      AudioClip foundClip = _projectAudioClips.Find(iClip => iClip.name.Equals(effect.clipName));
      if (foundClip != null) {
         effect.clip = foundClip;
      } else if (!string.IsNullOrEmpty(effect.clipName)) {
         D.debug("SoundEffect '" + effect.name + "' has an invalid AudioClip link: '" + effect.clipName + "'");
      }
   }

   public string getSoundEffectsStringData (List<SoundEffect> soundEffectsRawData) {
      string content = "";
      foreach (SoundEffect sfx in soundEffectsRawData) {
         XmlSerializer ser = new XmlSerializer(sfx.GetType());
         var sb = new StringBuilder();
         using (var writer = XmlWriter.Create(sb)) {
            ser.Serialize(writer, sfx);
         }
         string xmlValue = sb.ToString();

         content += sfx.id + "[space]" + xmlValue + "[next]\n";
      }
      return content;
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

   // The time at which we last player a specified clip
   private static Dictionary<string, float> _lastPlayTime = new Dictionary<string, float>();

   #endregion
}
