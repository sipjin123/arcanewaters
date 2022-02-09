using UnityEngine;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Text;
using System.Xml;
using System;

public class SoundEffectManager : GenericGameManager
{
   #region Public Variables

   // The self
   public static SoundEffectManager self;

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
   public const string TURNING_PAGES_ON_BOOKS = "event:/SFX/Game/UI/Turning_Pages_on_Books";
   public const string EQUIP = "event:/SFX/Game/UI/Equip";
   public const string TUTORIAL_STEP = "event:/SFX/Game/UI/Tutorial_Step";
   public const string TUTORIAL_POP_UP = "event:/SFX/Game/UI/Tutorial_Pop_Up";
   public const string LAYOUTS_DESTINATIONS = "event:/SFX/Game/UI/Layouts_Destinations";

   #endregion

   #region LAND BATTLE

   public const string GENERIC_HIT_LAND = "event:/SFX/Game/Land_Battle/Generic_Hit_Land";
   public const string GENERIC_GUN_SHOT = "event:/SFX/Player/Interactions/Diegetic/Weapons/Guns/Generic_Gun_Shot";

   public const string MOVEMENT_WHOOSH = "event:/SFX/Game/Land_Battle/Movement_Whoosh";
   //public const string NPC_STRIKE = "event:/SFX/Game/Land_Battle/NPC_Strike";
   public const string BLOCK_ATTACK = "event:/SFX/Game/Land_Battle/Block_Attack";
   public const string STANCE_CHANGE = "event:/SFX/Game/Land_Battle/Stance_Change_Generic";

   public const string LIZARD_KING_ATTACK = "event:/SFX/Game/Land_Battle/Lizard_King/Swipe_Attack";
   public const string LIZARD_KING_HURT = "event:/SFX/NPC/Enemy/Lizard King/Lizard_Pain_Hit";
   public const string LIZARD_KING_DEATH = "event:/SFX/NPC/Enemy/Lizard King/Lizard_Death";

   public const string GOLEM_FOOT_IMPACT = "event:/SFX/NPC/Boss/Rock Golem/Foot_Impact";
   public const string GOLEM_SCREAM_ATTACK = "event:/SFX/NPC/Boss/Rock Golem/Scream_Attack";
   public const string GOLEM_HURT = "event:/SFX/NPC/Boss/Rock Golem/Golem_Pain";
   public const string GOLEM_DEATH = "event:/SFX/NPC/Boss/Rock Golem/Golem_Death";

   public const string DEATH_POOF = "event:/SFX/Player/Interactions/Diegetic/GenericDeath";

   #endregion

   #region SEA BATTLE

   public const string PLAYER_SHIP_DESTROYED = "event:/SFX/Game/Sea_Battle/Player_Ship_Destroyed";
   public const string ENEMY_SHIP_IMPACT = "event:/SFX/Game/Sea_Battle/Enemy_Ship_Impact";
   public const string ENEMY_SHIP_DESTROYED = "event:/SFX/Game/Sea_Battle/Enemy_Ship_Destroyed";

   public const string HORROR_TENTACLE_HURT = "event:/SFX/NPC/Boss/Tentacle_Horror_Boss/Hurt";
   public const string HORROR_TENTACLE_DEATH = "event:/SFX/NPC/Boss/Tentacle_Horror_Boss/Tentacle Death";
   public const string HORROR_DEATH = "event:/SFX/NPC/Boss/Tentacle_Horror_Boss/Death";
   public const string HORROR_POISON_BOMB = "event:/SFX/Game/Sea_Battle/Horror/Poison_Bomb";
   public const string HORROR_BLOB_DAMAGE = "event:/SFX/NPC/Boss/Tentacle_Horror_Boss/Blob_Pylr_Damage";

   public const string FISHMAN_ATTACK = "event:/SFX/NPC/Enemy/Fishman_Seamonster/Fishman_Throw_Attack";
   public const string FISHMAN_HURT = "event:/SFX/NPC/Enemy/Fishman_Seamonster/Seamonster_Hurt";
   public const string FISHMAN_DEATH = "event:/SFX/NPC/Enemy/Fishman_Seamonster/Fishman_Death";

   #endregion

   #region GAME

   public const string BGM_MASTER = "event:/Music/BGM_Master";
   public const string DIALOGUE_TEXT = "event:/SFX/Game/UI/NPC_Dialogue_Text";
   public const string TRANSITION_IN = "event:/SFX/Game/Screen_Transition_In";
   public const string TRANSITION_OUT = "event:/SFX/Game/Screen_Transition_Out";
   public const string PLACE_EDITABLE_OBJECT = "event:/SFX/Game/Place_Editable_Object";
   public const string PICKUP_EDITABLE_OBJECT = "event:/SFX/Game/Pickup_Editable_Object";
   public const string CRAFT_SUCCESS = "event:/SFX/Game/UI/Craft_Success";
   public const string ON_THE_HOUR_CHIME = "event:/SFX/Ambience/Emitters/On_the_hour_Chime";

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
   public const string JUMP_LAND = "event:/SFX/Player/Interactions/Diegetic/Land";
   public const string CRITTER_PET = "event:/SFX/Player/Interactions/Diegetic/Critter_Pet";
   public const string SNEAKY_GOPHER = "event:/SFX/NPC/Critter/Sneaky_Gopher";
   public const string SHIP_CANNON = "event:/SFX/Player/Interactions/Diegetic/Ship_Cannon_Fire";
   public const string CANNONBALL_IMPACT = "event:/SFX/Player/Interactions/Diegetic/Cannonball_Impact";
   public const string MINING_ROCKS = "event:/SFX/Player/Interactions/Diegetic/Mine_Rocks";
   public const string SHIP_LAUNCH_CHARGE = "event:/SFX/Player/Interactions/Non_Diegetic/Ship_Launch_Charge";
   public const string THROW_SEEDS = "event:/SFX/Player/Interactions/Diegetic/Throw_Seeds";
   public const string WATERING_PLANTS = "event:/SFX/Player/Interactions/Diegetic/Watering_Plants";
   public const string FOOTSTEP = "event:/SFX/Player/Interactions/Diegetic/Footstep";

   public const string DOOR_OPEN = "event:/SFX/Player/Interactions/Diegetic/Door_Open";
   public const string DOOR_CLOSE = "event:/SFX/Player/Interactions/Diegetic/Door_Close";

   public const string DOOR_CLOTH_OPEN = "";
   public const string DOOR_CLOTH_CLOSE = "event:/SFX/Player/Interactions/Diegetic/Door_Cloth_Close";

   public const string PICKUP_POWERUP = "event:/SFX/Player/Interactions/Non_Diegetic/Pickup_Powerup_Generic";
   public const string COLLECT_LOOT_LAND = "event:/SFX/Player/Interactions/Diegetic/Collect_Loot_Land";
   public const string OPEN_CHEST = "event:/SFX/Player/Interactions/Diegetic/Open_Treasure_Site_Chest";
   public const string WEAPON_SWING = "event:/SFX/Player/Interactions/Diegetic/Weapons/Swings";
   public const string TRIUMPH_HARVEST = "event:/SFX/Player/Interactions/Non_Diegetic/Triumph_Harvest";
   public const string LOOT_BAG = "event:/SFX/Player/Interactions/Diegetic/Loot_Bag";
   public const string GAIN_SILVER = "event:/SFX/Player/Interactions/Non_Diegetic/Gain_Silver";
   public const string HARVESTING_HIT = "event:/SFX/Player/Interactions/Diegetic/Harvesting_Hit";
   public const string CROP_PLANT = "event:/SFX/Player/Interactions/Diegetic/Crop_Plant";
   public const string BUYSELL = "event:/SFX/Game/UI/Buy_Sell";

   #endregion

   #region NPC

   public const string CRITTER_INFLECTION = "event:/SFX/NPC/Critter/Inflections";
   public const string ANGER_EMOTE = "event:/SFX/NPC/Critter/Anger_Emote";
   public const string QUESTION_EMOTE = "event:/SFX/NPC/Critter/Question_Emote";
   public const string AFFECTION_EMOTE = "event:/SFX/NPC/Critter/Affection_Emote";
   public const string SKELETON_WALK = "event:/SFX/Ambience/Emitters/Skeleton_Walks";

   #endregion

   #endregion

   #endregion

   protected override void Awake () {
      self = this;
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

   public void playLandBattleHitSfx (Battler source, Battler target, Vector3 position) {
      Enemy.Type sourceType = source.enemyType;
      Enemy.Type targetType = target.enemyType;

      string hitPath = GENERIC_HIT_LAND;
      string hurtPath = "";

      // Hurt sfx
      switch (targetType) {
         case Enemy.Type.Lizard_King:
            hurtPath = LIZARD_KING_HURT;
            break;
         case Enemy.Type.Golem_Boss:
            hurtPath = GOLEM_HURT;
            break;
      }

      // If the source is the Lizard King, then we don't play a hit sfx, since the swipe sfx is enough.
      if (sourceType != Enemy.Type.Lizard_King) {
         playFmodSfx(hitPath, position);
      }

      if (!string.IsNullOrEmpty(hurtPath)) {
         playFmodSfx(hurtPath);
      }
   }

   public void playCannonballImpact (Cannonball impactType, Vector3 position) {
      FMOD.Studio.EventInstance impactEvent = FMODUnity.RuntimeManager.CreateInstance(CANNONBALL_IMPACT);
      impactEvent.setParameterByName(AUDIO_SWITCH_PARAM, ((int) impactType) - 1);
      impactEvent.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(position));
      impactEvent.start();
      impactEvent.release();
   }

   private void checkBackgroundMusicEvent () {
      if (!_backgroundMusicEvent.isValid()) {
         _backgroundMusicEvent = FMODUnity.RuntimeManager.CreateInstance(BGM_MASTER);
      }
   }

   private void playBackgroundMusicEvent () {
      _backgroundMusicEvent.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE backgroundMusicState);
      if (backgroundMusicState == FMOD.Studio.PLAYBACK_STATE.STOPPED) {
         _backgroundMusicEvent.start();
      }
   }

   public void playBackgroundMusic (SoundManager.Type musicType) {
      checkBackgroundMusicEvent();

      if (!_titleScreenAmbienceEvent.isValid()) {
         _titleScreenAmbienceEvent = FMODUnity.RuntimeManager.CreateInstance(TITLE_SCREEN_AMBIENCE);
      }

      _titleScreenAmbienceEvent.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE titleAmbienceState);

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

      playBackgroundMusicEvent();

      // If the type of music is "None"
      if (param == -1) {
         _backgroundMusicEvent.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
      }
   }

   public void playTriumphSfx () {
      checkBackgroundMusicEvent();

      _backgroundMusicEvent.setParameterByName(AMBIENCE_SWITCH_PARAM, 20);

      playBackgroundMusicEvent();
   }

   private void checkAmbienceEvent () {
      if (!_ambienceMusicEvent.isValid()) {
         _ambienceMusicEvent = FMODUnity.RuntimeManager.CreateInstance(AMBIENCE_BED_MASTER);
      }
   }

   private void playAmbienceEvent () {
      _ambienceMusicEvent.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE playbackState);
      if (playbackState == FMOD.Studio.PLAYBACK_STATE.STOPPED) {
         _ambienceMusicEvent.start();
      }
   }

   public void playAmbienceMusic (SoundManager.Type musicType) {
      checkAmbienceEvent();

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
         case SoundManager.Type.Farm_Music:
            param = 7;
            break;
         case SoundManager.Type.Interior:
            param = 8;
            break;
         case SoundManager.Type.Sea_Forest:
         case SoundManager.Type.Sea_Desert:
         case SoundManager.Type.Sea_Snow:
         case SoundManager.Type.Sea_Lava:
         case SoundManager.Type.Sea_Pine:
         case SoundManager.Type.Sea_Mushroom:
         case SoundManager.Type.Sea_PvP:
         case SoundManager.Type.Sea_League:
            param = 9;
            break;
      }

      _ambienceMusicEvent.setParameterByName(AMBIENCE_SWITCH_PARAM, param);

      if (param == -1) {
         _ambienceMusicEvent.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
      }

      playAmbienceEvent();
   }

   public void setAmbienceWeather (WeatherEffectType weatherEffect) {
      checkAmbienceEvent();

      int param = 0;

      switch (weatherEffect) {
         case WeatherEffectType.Rain:
            param = 1;
            break;
      }

      _ambienceMusicEvent.setParameterByName(WEATHER_PARAM, param);

      playAmbienceEvent();
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

   public void playLandEnemyDeathSfx (Enemy.Type enemyType, Vector3 position) {
      string path = "";

      switch (enemyType) {
         case Enemy.Type.Lizard_King:
            path = LIZARD_KING_DEATH;
            break;
         case Enemy.Type.Golem_Boss:
            path = GOLEM_DEATH;
            break;
      }

      if (!string.IsNullOrEmpty(path)) {
         playFmodSfx(path, position);
      }
   }

   public void playSeaEnemyDeathSfx (SeaMonsterEntity.Type monsterType, Vector3 position) {
      string path = "";

      switch (monsterType) {
         case SeaMonsterEntity.Type.Horror:
            path = HORROR_DEATH;
            break;
         case SeaMonsterEntity.Type.Horror_Tentacle:
            path = HORROR_TENTACLE_DEATH;
            break;
         case SeaMonsterEntity.Type.Fishman:
            path = FISHMAN_DEATH;
            break;
      }

      if (!string.IsNullOrEmpty(path)) {
         playFmodSfx(path, position);
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
      //if (sfxType != WeaponType.None) {
         FMOD.Studio.EventInstance weaponEvent = FMODUnity.RuntimeManager.CreateInstance(WEAPON_SWING);

         //eventInstance.setParameterByName(AUDIO_SWITCH_PARAM, ((int) sfxType) - 1);
         weaponEvent.setParameterByName(AUDIO_SWITCH_PARAM, 3); // Using the same parameter, for now.

         weaponEvent.setParameterByName(APPLY_MAGIC, weaponClass == Weapon.Class.Magic ? 1 : 0);
         weaponEvent.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(position));
         weaponEvent.start();
         weaponEvent.release();
      //}
   }

   public void playSeaEnemyHurtSfx (bool isShip, SeaMonsterEntity.Type seaMonsterType, bool isCrit, CannonballEffector.Type effectorType, Vector3 position) {
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
         case SeaMonsterEntity.Type.Horror_Tentacle:
            hurtPath = HORROR_TENTACLE_HURT;
            break;
      }

      impactEvent.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(position));
      impactEvent.start();
      impactEvent.release();

      if (!string.IsNullOrEmpty(hurtPath)) {
         playFmodSfx(hurtPath, position);
      }
   }

   public void playNotificationPanelSfx () {
      if (CameraManager.defaultCamera.getPixelFadeEffect().isFadingIn) {
         playFmodSfx(TIP_FOLDOUT);
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
   public void playSeaAbilitySfx (SeaAbilityType seaAbilityType, Vector3 position) {
      switch (seaAbilityType) {
         case SeaAbilityType.Sail_Shredder:
         case SeaAbilityType.Davy_Jones:
            playShipCannonSfx(seaAbilityType, position: position);
            break;
         case SeaAbilityType.Fishman_Attack:
            playFmodSfx(FISHMAN_ATTACK, position);
            break;
      }
   }

   // Ship Cannon Ball SFX
   public void playShipCannonSfx (SeaAbilityType seaAbilityType = SeaAbilityType.None, ProjectileType projectileType = ProjectileType.None, Vector3 position = default, Transform projectileTransform = null, Rigidbody2D projectileBody = null) {
      FMOD.Studio.EventInstance shipCannonEvent = FMODUnity.RuntimeManager.CreateInstance(SHIP_CANNON);

      int audioParam = 0;

      switch (projectileType) {
         case ProjectileType.Cannonball_Ice:
            audioParam = 1;
            break;
         case ProjectileType.Cannonball_Fire:
            audioParam = 2;
            break;
      }

      switch (seaAbilityType) {
         case SeaAbilityType.Sail_Shredder:
            audioParam = 3;
            break;
         case SeaAbilityType.Davy_Jones:
            audioParam = 4;
            break;
      }

      //Debug.Log(audioParam);

      shipCannonEvent.setParameterByName(AUDIO_SWITCH_PARAM, audioParam);

      if (projectileTransform != null && projectileBody != null) {
         FMODUnity.RuntimeManager.AttachInstanceToGameObject(shipCannonEvent, projectileTransform, projectileBody);
      } else if (position != default) {
         shipCannonEvent.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(position));
      }

      shipCannonEvent.start();
      shipCannonEvent.release();
   }

   // Sea Projectile SFX
   public void playSeaProjectileSfx (SeaAbilityType seaAbilityType, ProjectileType projectileType, Transform projectileTransform, Rigidbody2D projectileBody) {
      switch (projectileType) {
         case ProjectileType.Cannonball:
         case ProjectileType.Cannonball_Ice:
         case ProjectileType.Cannonball_Fire:
            if (seaAbilityType != SeaAbilityType.Sail_Shredder && seaAbilityType != SeaAbilityType.Davy_Jones) {
               playShipCannonSfx(seaAbilityType, projectileType, projectileTransform: projectileTransform, projectileBody: projectileBody);
            }
            break;
      }
   }

   // Play attached SFX
   public void playAttached (SoundManager.Type soundType, GameObject target) {
      string path = "";

      switch (soundType) {
         case SoundManager.Type.Skeleton_Walk:
            path = SKELETON_WALK;
            break;
      }

      if (!string.IsNullOrEmpty(path)) {
         FMODUnity.RuntimeManager.PlayOneShotAttached(path, target);
      }
   }

   public void playFootstepSfx (Vector3 playerPosition) {
      Area area = AreaManager.self.getArea(Global.player.areaKey);
      Biome.Type biomeType = AreaManager.self.getDefaultBiome(Global.player.areaKey);

      // Footsteps in the farm area
      if (AreaManager.self.tryGetCustomMapManager(Global.player.areaKey, out CustomMapManager customMapManager)) {
         if (customMapManager is CustomMapManager) {
            biomeType = Biome.Type.Forest;
         }
      }

      TileAttributes.Type[] attributesBuffer = new TileAttributes.Type[16];

      int count = area.getTileAttributes(playerPosition, attributesBuffer);
      int audioParam = 7; // For generic footsteps, default

      //Debug.Log(string.Join(", ", attributesBuffer));

      if (count > 0 && !area.isInterior) {
         TileAttributes.Type attribute = attributesBuffer[count - 1];

         switch (attribute) {
            case TileAttributes.Type.Generic:
               switch (biomeType) {
                  case Biome.Type.Forest:
                  case Biome.Type.Mushroom:
                  case Biome.Type.Pine:
                     audioParam = 0; // Grass
                     break;
                  case Biome.Type.Desert:
                  case Biome.Type.Snow:
                     audioParam = 6; // Sand / Snow
                     break;
                  case Biome.Type.Lava:
                     audioParam = 1; // Stone
                     break;
               }
               break;
            case TileAttributes.Type.Stone:
               audioParam = 1;
               break;
            case TileAttributes.Type.Vine:
               audioParam = 5;
               break;
            case TileAttributes.Type.WaterPartial:
            case TileAttributes.Type.WaterFull:
               audioParam = 4;
               break;
            case TileAttributes.Type.Wood:
               audioParam = 3;
               break;
            case TileAttributes.Type.Wooden_Bridge:
               audioParam = 2;
               break;
         }
      }

      if (!_footstepsLastSound.ContainsKey(audioParam) || Time.time - _footstepsLastSound[audioParam] > .25f) {
         FMOD.Studio.EventInstance footstepEvent = FMODUnity.RuntimeManager.CreateInstance(FOOTSTEP);

         footstepEvent.setParameterByName(AUDIO_SWITCH_PARAM, audioParam);
         footstepEvent.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(playerPosition));
         footstepEvent.start();
         footstepEvent.release();

         _footstepsLastSound[audioParam] = Time.time;
      }
   }

   public void playAttachedSfx (string path, GameObject target) {
      FMODUnity.RuntimeManager.PlayOneShotAttached(path, target);
   }

   public void playJumpLandSfx (Vector3 playerPosition) {
      Area area = AreaManager.self.getArea(Global.player.areaKey);
      Biome.Type biomeType = AreaManager.self.getDefaultBiome(Global.player.areaKey);

      TileAttributes.Type[] attributesBuffer = new TileAttributes.Type[16];

      int count = area.getTileAttributes(playerPosition, attributesBuffer);
      int audioParam = 0; // Grass is default

      if (count > 0 && !area.isInterior) {
         TileAttributes.Type attribute = attributesBuffer[count - 1];

         switch (attribute) {
            case TileAttributes.Type.Generic:
               switch (biomeType) {
                  case Biome.Type.Desert:
                  case Biome.Type.Snow:
                     audioParam = 1; // Dirt / Sand / Snow
                     break;
               }
               break;
            case TileAttributes.Type.WaterPartial:
            case TileAttributes.Type.WaterFull:
               audioParam = 2;
               break;
         }
      }

      FMOD.Studio.EventInstance landEvent = FMODUnity.RuntimeManager.CreateInstance(JUMP_LAND);

      landEvent.setParameterByName(AUDIO_SWITCH_PARAM, audioParam);
      landEvent.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(playerPosition));
      landEvent.start();
      landEvent.release();
   }

   #region Private Variables

   // The time at which we last player a specified clip
   private static Dictionary<string, float> _lastPlayTime = new Dictionary<string, float>();

   // Event for main background music
   private FMOD.Studio.EventInstance _backgroundMusicEvent;

   // Event for main ambience music
   private FMOD.Studio.EventInstance _ambienceMusicEvent;

   // Event for title screen ambience music
   private FMOD.Studio.EventInstance _titleScreenAmbienceEvent;

   // Footsteps dictionary
   private Dictionary<int, float> _footstepsLastSound = new Dictionary<int, float>();

   private enum LandAbility
   {
      None = 0,
      BoneBreaker = 11,
      GolemShout = 90
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

   public enum SeaAbilityType
   {
      None = 0,
      Horror_Poison = 1,
      Sail_Shredder = 2,
      Davy_Jones = 3,
      Fishman_Attack = 4
   }

   public enum Cannonball
   {
      None = 0,
      Water_Impact = 1
   }

   public enum ProjectileType
   {
      None = 0,
      Cannonball = 1, Cannonball_Ice = 2, Cannonball_Fire = 3,
      Fishman_Attack = 6
   }

   #endregion
}

