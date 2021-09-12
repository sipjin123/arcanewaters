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

   // Sound effects
   #region Sound Effects
   public const int HARVESTING_PITCHFORK_HIT = 43;
   public const int HARVESTING_FLYING = 44;

   public const int ORE_DROP = 53;
   public const int ORE_PICKUP = 55;
   public const int NEXT_PREFAB_SELECTION = 58;

   public const int SHORTCUT_SELECTION = 60;
   public const int ABILITY_SELECTION = 61;
   public const int STANCE_SELECTION = 62;
   public const int INVENTORY_DRAG_START = 64;
   public const int INVENTORY_DROP = 65;
   public const int NPC_PANEL_POPUP = 66;
   public const int REFINE_COMPLETE = 69;
   #endregion

   // FMOD event paths
   #region FMOD EVENT PATHS

   #region PARAMS
   public const string AUDIO_SWITCH_PARAM = "Audio_Switch";
   public const string SHIP_CHARGE_RELEASE_PARAM = "Ship_Charge_Release";
   public const string AMBIENCE_SWITCH_PARAM = "Ambience_Switch";
   public const string APPLY_CRIT_PARAM = "Apply_Crit";
   public const string WEATHER_PARAM = "Weather_Effects";
   public const string APPLY_PUP_PARAM = "Apply_Powerup";
   public const string APPLY_MAGIC = "Apply_Magic";
   #endregion

   #region UI
   public const string MENU_OPEN = "event:/SFX/Game/UI/Menu_Open";
   public const string BUTTON_CONFIRM = "event:/SFX/Game/UI/Button_Confirm";
   public const string HOVER_CURSOR_GENERIC = "event:/SFX/Game/UI/Hover_Cursor_Generic";
   public const string HOVER_CURSOR_ITEMS = "event:/SFX/Game/UI/Hover_Cursor_Items";
   public const string MAP_OPEN = "event:/SFX/Game/UI/Map_Open";
   public const string PURCHASE_ITEM = "event:/SFX/Game/UI/Purchase_Item";
   public const string CLICK_TAB = "event:/SFX/Game/UI/Click_Tab";
   public const string ASSIGN_PERK_POINT = "event:/SFX/Game/UI/Assign_Perk_Point";
   public const string UNASSIGN_PERK_POINT = "event:/SFX/Game/UI/Unassign_Perk_Point";
   public const string TIP_FOLDOUT = "event:/SFX/Game/UI/Tip_Foldout";
   public const string MAIL_NOTIFICATION = "event:/SFX/Game/UI/Mail_Notification";
   public const string LOCALE_UNLOCK = "event:/SFX/Game/UI/Locale_Unlock";
   #endregion

   #region LAND BATTLE
   public const string GENERIC_HIT_LAND = "event:/SFX/Game/Land_Battle/Generic_Hit_Land";
   public const string MOVEMENT_WHOOSH = "event:/SFX/Game/Land_Battle/Movement_Whoosh";
   public const string NPC_STRIKE = "event:/SFX/Game/Land_Battle/NPC_Strike";
   public const string BLOCK_ATTACK = "event:/SFX/Game/Land_Battle/Block_Attack";
   #endregion

   #region SEA BATTLE
   public const string PLAYER_SHIP_DESTROYED = "event:/SFX/Game/Sea_Battle/Player_Ship_Destroyed";
   public const string ENEMY_SHIP_IMPACT = "event:/SFX/Game/Sea_Battle/Enemy_Ship_Impact";
   public const string ENEMY_SHIP_DESTROYED = "event:/SFX/Game/Sea_Battle/Enemy_Ship_Destroyed";
   public const string HORROR_DEATH = "event:/SFX/Game/Sea_Battle/Horror/Death";
   public const string HORROR_POISON_BOMB = "event:/SFX/Game/Sea_Battle/Horror/Poison_Bomb";
   #endregion

   #region GAME
   public const string BGM_MASTER = "event:/Music/BGM_Master";
   public const string COLLECT_SILVER = "event:/SFX/Game/Collect_Silver";
   public const string DIALOGUE_TEXT = "event:/SFX/Game/UI/NPC_Dialogue_Text";
   public const string TRANSITION_IN = "event:/SFX/Game/Screen_Transition_In";
   public const string TRANSITION_OUT = "event:/SFX/Game/Screen_Transition_Out";
   public const string PLACE_EDITABLE_OBJECT = "event:/SFX/Game/Place_Editable_Object";
   public const string PICKUP_EDITABLE_OBJECT = "event:/SFX/Game/Pickup_Editable_Object";
   public const string CRAFT_SUCCESS = "event:/SFX/Game/UI/Craft_Success";
   #endregion

   #region AMBIENCE
   public const string AMBIENCE_BED_MASTER = "event:/SFX/Ambience/Beds/Ambience_Bed_Master";
   public const string FISH_SURFACING = "event:/SFX/Ambience/Emitters/Fish_Surfacing";
   public const string LIGHTNING_FLASH = "event:/SFX/Ambience/Beds/Lightning_Flash";
   public const string CALMING_WATERFALL = "event:/SFX/Ambience/Emitters/Calming_Waterfall";
   #endregion

   #region PLAYER INTERACTIONS
   public const string JUMP = "event:/SFX/Player/Interactions/Diegetic/Jump";
   public const string LAND = "event:/SFX/Player/Interactions/Diegetic/Land";
   public const string CRITTER_PET = "event:/SFX/Player/Interactions/Diegetic/Critter_Pet";
   public const string SHIP_CANNON = "event:/SFX/Player/Interactions/Diegetic/Ship_Cannon_Fire";
   public const string CANNONBALL_IMPACT = "event:/SFX/Player/Interactions/Diegetic/Cannonball_Impact";
   public const string MINING_ROCKS = "event:/SFX/Player/Interactions/Diegetic/Mine_Rocks";
   public const string SHIP_LAUNCH_CHARGE = "event:/SFX/Player/Interactions/Non_Diegetic/Ship_Launch_Charge";
   public const string THROW_SEEDS = "event:/SFX/Player/Interactions/Diegetic/Throw_Seeds";
   public const string WATERING_PLANTS = "event:/SFX/Player/Interactions/Diegetic/Watering_Plants";
   public const string FOOTSTEP = "event:/SFX/Player/Interactions/Diegetic/Footstep";
   public const string PICKUP_CROP = "event:/SFX/Player/Interactions/Non_Diegetic/Pickup_Crop";
   public const string DOOR_OPEN = "event:/SFX/Player/Interactions/Diegetic/Door_Open";
   public const string PICKUP_POWERUP = "event:/SFX/Player/Interactions/Non_Diegetic/Pickup_Powerup_Generic";
   public const string COLLECT_LOOT_SEA = "event:/SFX/Player/Interactions/Diegetic/Collect_Loot_Sea";
   public const string COLLECT_LOOT_LAND = "event:/SFX/Player/Interactions/Diegetic/Collect_Loot_Land";
   public const string OPEN_CHEST = "event:/SFX/Player/Interactions/Diegetic/Open_Treasure_Site_Chest";
   public const string WEAPON_SWING = "event:/SFX/Player/Interactions/Diegetic/Weapons/Swings";
   #endregion

   #region NPC
   public const string CRITTER_INFLECTION = "event:/SFX/NPC/Critter/Inflections";
   public const string ANGER_EMOTE = "event:/SFX/NPC/Critter/Anger_Emote";
   public const string QUESTION_EMOTE = "event:/SFX/NPC/Critter/Question_Emote";
   public const string AFFECTION_EMOTE = "event:/SFX/NPC/Critter/Affection_Emote";
   #endregion

   #endregion

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

   public EventInstance getEventInstance (string eventPath) {
      return RuntimeManager.CreateInstance(eventPath);
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

   //public void playFmodWithPath (string path, Transform target) {
   //   if (Util.isBatch()) {
   //      return;
   //   }

   //   RuntimeManager.PlayOneShot(path, target.position);
   //}

   public void playFmodSfx (string path, Transform target = null, Vector3 targetPos = default) {
      if (Util.isBatch()) {
         return;
      }

      EventInstance eventInstance = RuntimeManager.CreateInstance(path);
      if (eventInstance.isValid()) {
         if (target != null) {
            eventInstance.set3DAttributes(RuntimeUtils.To3DAttributes(target));
         } else if (targetPos != default) {
            eventInstance.set3DAttributes(RuntimeUtils.To3DAttributes(targetPos));
         }
         eventInstance.start();
         eventInstance.release();
      }
   }

   public void playFmod2dSfxWithId (int id) {
      if (Util.isBatch()) {
         return;
      }

      playFmodOneShot(id, CameraManager.getCurrentCamera().transform);
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

   public void playCannonballImpact (CannonballSfxType impactType, Vector3 position) {
      EventInstance impactEvent = RuntimeManager.CreateInstance(CANNONBALL_IMPACT);
      impactEvent.setParameterByName(AUDIO_SWITCH_PARAM, ((int) impactType) - 1);
      impactEvent.set3DAttributes(RuntimeUtils.To3DAttributes(position));
      impactEvent.start();
      impactEvent.release();
   }

   public void playAmbienceMusic (bool isSea, bool isInterior, Biome.Type biomeType) {
      if (!_ambienceMusicEvent.isValid()) {
         _ambienceMusicEvent = RuntimeManager.CreateInstance(AMBIENCE_BED_MASTER);
      }

      int param = (int) AmbienceType.None;

      if (isSea) {
         param = (int) AmbienceType.SeaMap;
      } else if (isInterior) {
         param = (int) AmbienceType.Interior;
      } else {
         switch (biomeType) {
            case Biome.Type.Forest:
               param = (int) AmbienceType.Forest;
               break;
            case Biome.Type.Desert:
               param = (int) AmbienceType.Desert;
               break;
            case Biome.Type.Snow:
               param = (int) AmbienceType.Snow;
               break;
            case Biome.Type.Lava:
               param = (int) AmbienceType.Lava;
               break;
            case Biome.Type.Pine:
               param = (int) AmbienceType.Pine;
               break;
            case Biome.Type.Mushroom:
               param = (int) AmbienceType.Shroom;
               break;
         }
      }

      _ambienceMusicEvent.setParameterByName(AMBIENCE_SWITCH_PARAM, param);

      PLAYBACK_STATE amState;
      _ambienceMusicEvent.getPlaybackState(out amState);
      if (amState == PLAYBACK_STATE.STOPPED) {
         _ambienceMusicEvent.start();
      }
   }

   public void playBackgroundMusic (SoundManager.Type musicType) {
      if (!_backgroundMusicEvent.isValid()) {
         _backgroundMusicEvent = RuntimeManager.CreateInstance(BGM_MASTER);
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
         case SoundManager.Type.Battle_Music:
            param = 10;
            break;
      }

      _backgroundMusicEvent.setParameterByName(AMBIENCE_SWITCH_PARAM, param);

      PLAYBACK_STATE bgState;
      _backgroundMusicEvent.getPlaybackState(out bgState);
      if (bgState == PLAYBACK_STATE.STOPPED) {
         _backgroundMusicEvent.start();
      }

      // If the type of music is "None"
      if (param == -1) {
         _backgroundMusicEvent.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
      }

   }

   public void playAnimalCry (string path, Transform target) {
      string[] splits = path.Split('/');
      if (splits.Length > 0) {
         string name = splits[splits.Length - 1];
         int param = -1;

         // Each animal
         if (name.Contains("fox")) {
            param = 0;
         } else if (name.Contains("cow")) {
            param = 1;
         } else if (name.Contains("cat")) {
            param = 2;
         } else if (name.Contains("badger")) {
            param = 3;
         } else if (name.Contains("little_chicken")) {
            param = 5;
         } else if (name.Contains("chicken")) {
            param = 4;
         } else if (name.Contains("monkey")) {
            param = 6;
         } else if (name.Contains("racoon")) {
            param = 7;
         } else if (name.Contains("rat")) {
            param = 8;
         } else if (name.Contains("rooster")) {
            param = 9;
         } else if (name.Contains("scorpion")) {
            param = 10;
         } else if (name.Contains("spider")) {
            param = 11;
         } else if (name.Contains("skunk")) {
            param = 12;
         } else if (name.Contains("snail")) {
            param = 13;
         }

         if (param > -1) {
            EventInstance animalEvent = RuntimeManager.CreateInstance(CRITTER_INFLECTION);
            animalEvent.setParameterByName(AUDIO_SWITCH_PARAM, param);
            animalEvent.set3DAttributes(RuntimeUtils.To3DAttributes(target.position));
            animalEvent.start();
            animalEvent.release();
         }
      }
   }

   public void playSeaBossDeathSfx (SeaMonsterEntity.Type monsterType, Transform target) {
      switch (monsterType) {
         case SeaMonsterEntity.Type.Horror:
            playFmodSfx(HORROR_DEATH, target);
            break;
      }
   }
   //public void playFmod2D (int id) {
   //   if (Util.isBatch()) {
   //      return;
   //   }

   //   playFmodOneShot(id, CameraManager.getCurrentCamera().transform);
   //}

   public void playGuiButtonConfirmSfx () {
      playFmodSfx(BUTTON_CONFIRM);
   }

   public void playGuiMenuOpenSfx () {
      playFmodSfx(MENU_OPEN);
   }

   public void playFmodGuiHover (string path) {
      if (_lastPlayTime.ContainsKey(path) && Time.time - _lastPlayTime[path] < .10f) {
         return;
      }

      // Make note of the time
      _lastPlayTime[path] = Time.time;

      playFmodSfx(path);
   }

   public void playLegacyInteractionOneShot (int equipmentDataId, Transform target) {
      WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(equipmentDataId);
      if (weaponData != null && weaponData.actionSfxDirectory.Length > 1) {
         SoundManager.create3dSoundWithPath(weaponData.actionSfxDirectory, target.position);
      }
   }

   public void playInteractionSfx (Weapon.ActionType weaponAction, Weapon.Class weaponClass, WeaponSfxType sfxType, Transform target) {
      switch (weaponAction) {
         case Weapon.ActionType.PlantCrop:
            playFmodSfx(THROW_SEEDS, target);
            break;
         case Weapon.ActionType.WaterCrop:
            playFmodSfx(WATERING_PLANTS, target);
            break;
         default:
            playWeaponSfx(sfxType, weaponClass, target);
            break;
      }
   }

   public void playWeaponSfx (WeaponSfxType sfxType, Weapon.Class weaponClass, Transform target) {
      if (sfxType != WeaponSfxType.None) {
         EventInstance eventInstance = RuntimeManager.CreateInstance(WEAPON_SWING);
         eventInstance.setParameterByName(AUDIO_SWITCH_PARAM, ((int) sfxType) - 1);
         eventInstance.setParameterByName(APPLY_MAGIC, weaponClass == Weapon.Class.Magic ? 1 : 0);
         eventInstance.set3DAttributes(RuntimeUtils.To3DAttributes(target));
         eventInstance.start();
         eventInstance.release();
      }
   }

   public void playEnemyHitSfx (bool isShip, bool isCrit, CannonballEffector.Type effectorType, Vector3 position) {
      EventInstance eventInstance = RuntimeManager.CreateInstance(ENEMY_SHIP_IMPACT);

      if (!isShip) {
         eventInstance.setParameterByName(AUDIO_SWITCH_PARAM, 1);
      }

      if (isCrit) {
         eventInstance.setParameterByName(APPLY_CRIT_PARAM, 1);
      }

      switch (effectorType) {
         case CannonballEffector.Type.Explosion:
            eventInstance.setParameterByName(APPLY_PUP_PARAM, 1);
            break;
      }

      eventInstance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
      eventInstance.start();
      eventInstance.release();
   }

   //public void playSeaAbilitySfx (float delay, Attack.Type attackType, ProjectileSfxType sfxType, Transform target = null, Vector3 position = default) {
   //   switch (sfxType) {
   //      case ProjectileSfxType.Horror_Poison:
   //         playHorrorPoisonSfx(delay, target, position, attackType == Attack.Type.Poison_Circle);
   //         break;
   //   }
   //}

   // Horror Boss
   public void playHorrorPoisonSfx (float delay, Transform target = null, Vector3 pos = default, bool isCluster = false) {
      StartCoroutine(CO_PlayHorrorPoisonSfx(delay, pos, isCluster));
   }

   private IEnumerator CO_PlayHorrorPoisonSfx (float delay, Vector3 position, bool isCluster) {
      yield return new WaitForSeconds(delay);
      EventInstance eventInstance = RuntimeManager.CreateInstance(HORROR_POISON_BOMB);
      eventInstance.setParameterByName(AUDIO_SWITCH_PARAM, isCluster ? 1 : 0);
      eventInstance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
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

   public void playNotificationPanelSfx () {
      if (CameraManager.defaultCamera.getPixelFadeEffect().isFadingIn) {
         playFmodSfx(TIP_FOLDOUT);
      }
   }

   //public void playFmodWithDelay (int id, float delay, Transform target = null, bool is3D = false) {
   //   StartCoroutine(CO_PlayFmodAfterDelay(id, delay, target, is3D));
   //}

   public void playFmodSfxAfterDelay (string path, float delay, Transform target = null, Vector3 pos = default) {
      StartCoroutine(CO_PlayFmodSfxAfterDelay(path, delay, target, pos));
   }

   private IEnumerator CO_PlayFmodSfxAfterDelay (string path, float delay, Transform target, Vector3 pos) {
      yield return new WaitForSeconds(delay);
      playFmodSfx(path, target, pos);
   }

   //private IEnumerator CO_PlayFmodAfterDelay (int id, float delay, Transform target = null, bool is3D = false) {
   //   yield return new WaitForSeconds(delay);

   //   if (is3D && target != null) {
   //      self.playFmodOneShot(id, target);
   //   } else {
   //      self.playFmod2dSfxWithId(id);
   //   }
   //}

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

   // Event for main background music
   private EventInstance _backgroundMusicEvent;

   // Event for main ambience music
   private EventInstance _ambienceMusicEvent;

   private enum AmbienceType
   {
      None = -1,
      Forest = 0,
      Desert = 1,
      Snow = 2,
      Lava = 3,
      Pine = 4,
      Shroom = 5,
      TreasureSite = 6,
      Farm = 7,
      Interior = 8,
      SeaMap = 9
   }

   #endregion
}

// SFX related enums
public enum WeaponSfxType
{
   None = 0,
   Blunt_Metallic = 1,
   Metallic_Thin = 2,
   Metallic_Heavy = 3,
   Wooden_Thin = 4,
   Wooden_Thick_Heavy = 5,
   Flammables_Swigs_Swishes = 6,
   Clunky_Mechanical = 7
}

public enum SeaAbilitySfxType
{
   None = 0,
   Horror_Poison = 1
}

public enum CannonballSfxType
{
   None = 0,
   Water_Impact = 1
}
