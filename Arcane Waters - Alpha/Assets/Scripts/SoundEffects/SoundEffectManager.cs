using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

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

   public const int ORE_DROP = 53;
   public const int ORE_PICKUP = 55;
   public const int NEXT_PREFAB_SELECTION = 58;

   public const int SHORTCUT_SELECTION = 60;
   public const int ABILITY_SELECTION = 61;
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
   public const string STANCE_CHANGE = "event:/SFX/Game/Land_Battle/Stance_Change_Generic";
   public const string LIZARD_KING_ATTACK = "event:/SFX/Game/Land_Battle/Lizard_King/Swipe_Attack";
   public const string LIZARD_KING_HURT = "event:/SFX/NPC/Enemy/Lizard King/Lizard_Pain_Hit";
   public const string GOLEM_FOOT_IMPACT = "event:/SFX/NPC/Boss/Rock Golem/Foot_Impact";
   public const string GOLEM_SCREAM_ATTACK = "event:/SFX/NPC/Boss/Rock Golem/Scream_Attack";

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
   //public const string COLLECT_SILVER = "event:/SFX/Game/Collect_Silver";
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
   public const string TITLE_SCREEN_AMBIENCE = "event:/SFX/Ambience/Title_Screen";

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
   //public const string PICKUP_CROP = "event:/SFX/Player/Interactions/Non_Diegetic/Pickup_Crop";
   public const string DOOR_OPEN = "event:/SFX/Player/Interactions/Diegetic/Door_Open";
   public const string PICKUP_POWERUP = "event:/SFX/Player/Interactions/Non_Diegetic/Pickup_Powerup_Generic";
   //public const string COLLECT_LOOT_SEA = "event:/SFX/Player/Interactions/Diegetic/Collect_Loot_Sea";
   public const string COLLECT_LOOT_LAND = "event:/SFX/Player/Interactions/Diegetic/Collect_Loot_Land";
   public const string OPEN_CHEST = "event:/SFX/Player/Interactions/Diegetic/Open_Treasure_Site_Chest";
   public const string WEAPON_SWING = "event:/SFX/Player/Interactions/Diegetic/Weapons/Swings";
   public const string TRIUMPH_HARVEST = "event:/SFX/Player/Interactions/Non_Diegetic/Triumph_Harvest";
   public const string LOOT_BAG = "event:/SFX/Player/Interactions/Diegetic/Loot_Bag";
   public const string GAIN_SILVER = "event:/SFX/Player/Interactions/Non_Diegetic/Gain_Silver";
   public const string HARVESTING_HIT = "event:/SFX/Player/Interactions/Diegetic/Harvesting_Hit";

   #endregion

   #region NPC

   public const string CRITTER_INFLECTION = "event:/SFX/NPC/Critter/Inflections";
   public const string ANGER_EMOTE = "event:/SFX/NPC/Critter/Anger_Emote";
   public const string QUESTION_EMOTE = "event:/SFX/NPC/Critter/Question_Emote";
   public const string AFFECTION_EMOTE = "event:/SFX/NPC/Critter/Affection_Emote";

   #region Enemy

   public const string FISHMAN_ATTACK = "event:/SFX/NPC/Enemy/Fishman_Seamonster/Fishman_Throw_Attack";
   public const string FISHMAN_HURT = "event:/SFX/NPC/Enemy/Fishman_Seamonster/Seamonster_Hurt";

   #endregion

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

   public void playFmodSfx (string path, Vector3 position = default) {
      if (Util.isBatch()) {
         return;
      }

      // If the SFX is 2D
      if (position == default) {
         position = AudioListenerManager.self.getActiveFmodListener().gameObject.transform.position;
      }
      FMODUnity.RuntimeManager.PlayOneShot(path, position);
      //FMOD.Studio.EventInstance eventInstance = FMODUnity.RuntimeManager.CreateInstance(path);
      //if (eventInstance.isValid()) {
      //   eventInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(position));
      //   eventInstance.start();
      //   eventInstance.release();
      //}
   }

   public void playBossAbilitySfx (Enemy.Type enemyType, int abilityId, Vector3 position) {
      switch (enemyType) {
         case Enemy.Type.Lizard_King:
            playFmodSfx(LIZARD_KING_ATTACK, position);
            break;
         case Enemy.Type.Golem_Boss:
            LandAbility ability = (LandAbility) abilityId;
            switch (ability) {
               case LandAbility.BoneBreaker:
                  playFmodSfx(GOLEM_FOOT_IMPACT, position);
                  break;
               case LandAbility.GolemShout:
                  playFmodSfx(GOLEM_SCREAM_ATTACK, position);
                  break;
            }
            break;
      }
   }

   public void playFishSfx (Vector3 position) {
      if (Global.player != null) {
         playFmodSfx(FISH_SURFACING, position: position);
      }
   }

   public void playLandEnemyHitSfx (Enemy.Type enemyType, Vector3 position) {
      switch (enemyType) {
         case Enemy.Type.Lizard_King:
            playFmodSfx(LIZARD_KING_HURT, position);
            break;
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
            FMODUnity.RuntimeManager.PlayOneShot(effect.fmodId, target.position);
         }
      }
   }

   public void playCannonballImpact (Cannonball impactType, Vector3 position) {
      FMOD.Studio.EventInstance impactEvent = FMODUnity.RuntimeManager.CreateInstance(CANNONBALL_IMPACT);
      impactEvent.setParameterByName(AUDIO_SWITCH_PARAM, ((int) impactType) - 1);
      impactEvent.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(position));
      impactEvent.start();
      impactEvent.release();
   }

   public void playAmbienceMusic (bool isSea, bool isInterior, Biome.Type biomeType) {
      if (!_ambienceMusicEvent.isValid()) {
         _ambienceMusicEvent = FMODUnity.RuntimeManager.CreateInstance(AMBIENCE_BED_MASTER);
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
      _ambienceMusicEvent.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE amState);
      if (amState == FMOD.Studio.PLAYBACK_STATE.STOPPED) {
         _ambienceMusicEvent.start();
      }
   }

   public void playBackgroundMusic (SoundManager.Type musicType) {
      if (!_backgroundMusicEvent.isValid()) {
         _backgroundMusicEvent = FMODUnity.RuntimeManager.CreateInstance(BGM_MASTER);
      }
      if (!_titleScreenAmbienceEvent.isValid()) {
         _titleScreenAmbienceEvent = FMODUnity.RuntimeManager.CreateInstance(TITLE_SCREEN_AMBIENCE);
      }
      _titleScreenAmbienceEvent.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE titleAmbienceState);
      _backgroundMusicEvent.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE backgroundMusicState);

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
            // Here we play the ambience event for the Title Screen
            if (titleAmbienceState == FMOD.Studio.PLAYBACK_STATE.STOPPED) {
               _titleScreenAmbienceEvent.start();
            }
            param = 6;
            break;
         case SoundManager.Type.Farm_Music:
            param = 7;
            break;
         case SoundManager.Type.Interior:
            param = 8;
            break;
         case SoundManager.Type.Sea_PvP:
            param = 10;
            break;
         case SoundManager.Type.Sea_Forest:
            param = 11;
            break;
         case SoundManager.Type.Sea_Desert:
            param = 12;
            break;
         case SoundManager.Type.Sea_Lava:
            param = 13;
            break;
         case SoundManager.Type.Sea_Mushroom:
            param = 14;
            break;
         case SoundManager.Type.Sea_Pine:
            param = 15;
            break;
         case SoundManager.Type.Sea_Snow:
            param = 16;
            break;
         case SoundManager.Type.Sea_League:
            param = 17;
            break;
         case SoundManager.Type.Battle_Music:
            param = 19;
            break;
      }

      if (musicType != SoundManager.Type.Intro_Music) {
         _titleScreenAmbienceEvent.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
      }

      _backgroundMusicEvent.setParameterByName(AMBIENCE_SWITCH_PARAM, param);
      if (backgroundMusicState == FMOD.Studio.PLAYBACK_STATE.STOPPED) {
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
            FMOD.Studio.EventInstance animalEvent = FMODUnity.RuntimeManager.CreateInstance(CRITTER_INFLECTION);
            animalEvent.setParameterByName(AUDIO_SWITCH_PARAM, param);
            animalEvent.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(target.position));
            animalEvent.start();
            animalEvent.release();
         }
      }
   }

   public void playSeaBossDeathSfx (SeaMonsterEntity.Type monsterType, Vector3 position) {
      switch (monsterType) {
         case SeaMonsterEntity.Type.Horror:
            playFmodSfx(HORROR_DEATH, position);
            break;
      }
   }

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

   public void playInteractionSfx (Weapon.ActionType weaponAction, Weapon.Class weaponClass, WeaponType sfxType, Vector3 position) {
      switch (weaponAction) {
         case Weapon.ActionType.PlantCrop:
            playFmodSfx(THROW_SEEDS, position);
            break;
         case Weapon.ActionType.WaterCrop:
            playFmodSfx(WATERING_PLANTS, position);
            break;
         default:
            playWeaponSfx(sfxType, weaponClass, position);
            break;
      }
   }

   public void playWeaponSfx (WeaponType sfxType, Weapon.Class weaponClass, Vector3 position) {
      if (sfxType != WeaponType.None) {
         FMOD.Studio.EventInstance eventInstance = FMODUnity.RuntimeManager.CreateInstance(WEAPON_SWING);
         eventInstance.setParameterByName(AUDIO_SWITCH_PARAM, ((int) sfxType) - 1);
         eventInstance.setParameterByName(APPLY_MAGIC, weaponClass == Weapon.Class.Magic ? 1 : 0);
         eventInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(position));
         eventInstance.start();
         eventInstance.release();
      }
   }

   public void playEnemyHitSfx (bool isShip, SeaMonsterEntity.Type seaMonsterType, bool isCrit, CannonballEffector.Type effectorType, Vector3 position) {
      FMOD.Studio.EventInstance impactEvent = FMODUnity.RuntimeManager.CreateInstance(ENEMY_SHIP_IMPACT);
      impactEvent.setParameterByName(AUDIO_SWITCH_PARAM, isShip ? 0 : 1);
      impactEvent.setParameterByName(APPLY_CRIT_PARAM, isCrit ? 1 : 0);

      switch (effectorType) {
         case CannonballEffector.Type.Explosion:
            impactEvent.setParameterByName(APPLY_PUP_PARAM, 1);
            break;
      }

      string hurtPath = string.Empty;

      switch (seaMonsterType) {
         case SeaMonsterEntity.Type.Fishman:
            hurtPath = FISHMAN_HURT;
            break;
      }

      impactEvent.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(position));
      impactEvent.start();
      impactEvent.release();

      if (!string.IsNullOrEmpty(hurtPath)) {
         playFmodSfx(hurtPath, position);
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

   public void playNotificationPanelSfx () {
      if (CameraManager.defaultCamera.getPixelFadeEffect().isFadingIn) {
         playFmodSfx(TIP_FOLDOUT);
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

   // Sea Abilities SFX
   public void playSeaAbilitySfx (SeaMonsterEntity.Type seaMonsterType, Vector3 position) {
      switch (seaMonsterType) {
         case SeaMonsterEntity.Type.Fishman:
            playFmodSfx(FISHMAN_ATTACK, position);
            break;
      }
   }

   // Sea Projectile SFX
   public void playSeaProjectileSfx (ProjectileType projectileType, GameObject projectileGo) {
      string path = string.Empty;
      switch (projectileType) {
         case ProjectileType.Cannonball:
            path = SHIP_CANNON;
            break;
      }

      if (!string.IsNullOrEmpty(path)) {
         FMODUnity.RuntimeManager.PlayOneShotAttached(path, projectileGo);
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

   // The time at which we last player a specified clip
   private static Dictionary<string, float> _lastPlayTime = new Dictionary<string, float>();

   // Event for main background music
   private FMOD.Studio.EventInstance _backgroundMusicEvent;

   // Event for main ambience music
   private FMOD.Studio.EventInstance _ambienceMusicEvent;

   // Event for title screen ambience music
   private FMOD.Studio.EventInstance _titleScreenAmbienceEvent;

   private enum LandAbility
   {
      None = 0,
      BoneBreaker = 11,
      GolemShout = 90
   }

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

   // SFX related enums
   public enum WeaponType
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

   public enum SeaAbility
   {
      None = 0,
      Horror_Poison = 1
   }

   public enum Cannonball
   {
      None = 0,
      Water_Impact = 1
   }

   public enum ProjectileType
   {
      None = 0,
      Cannonball = 1,
      Fishman_Attack = 2
   }

   #endregion
}

