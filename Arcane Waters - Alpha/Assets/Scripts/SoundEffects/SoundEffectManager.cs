using UnityEngine;
using System.Collections.Generic;
using System.Xml.Serialization;
using System.Text;
using System.Xml;
using System;
using System.Linq;

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
   public const string AMB_SW_PARAM = "Ambience_Switch";
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
   public const string EQUIP_SPECIAL = "event:/SFX/Game/UI/Equip_Ring_Neck_Trinket";

   public const string TUTORIAL_STEP = "event:/SFX/Game/UI/Tutorial_Step";
   public const string TUTORIAL_POP_UP = "event:/SFX/Game/UI/Tutorial_Pop_Up";
   public const string LAYOUTS_DESTINATIONS = "event:/SFX/Game/UI/Layouts_Destinations";
   public const string FRIEND_REQUEST = "event:/SFX/Game/UI/Send_Recieve_Friend_Request";
   public const string PLACING_WAYPOINT = "event:/SFX/Game/UI/Placing_Waypoint";

   #endregion

   #region LAND BATTLE

   public const string GENERIC_HIT_LAND = "event:/SFX/Game/Land_Battle/Generic_Hit_Land";
   public const string GENERIC_GUN_SHOT = "event:/SFX/Player/Interactions/Diegetic/Weapons/Guns/Generic_Gun_Shot";

   public const string TOAST_RUM = "event:/SFX/Player/Interactions/Diegetic/Weapons/Rum/Toast_Rum";
   public const string THROW_RUM = "event:/SFX/Player/Interactions/Diegetic/Weapons/Rum/Throw_Rum";
   public const string ATTACK_RUM = "event:/SFX/Player/Interactions/Diegetic/Weapons/Rum/Attack_Rum";

   public const string MOVEMENT_WHOOSH = "event:/SFX/Game/Land_Battle/Movement_Whoosh";
   public const string BLOCK_ATTACK = "event:/SFX/Game/Land_Battle/Block_Attack";
   public const string STANCE_CHANGE = "event:/SFX/Game/Land_Battle/Stance_Change_Generic";

   public const string SNAKE_HURT = "event:/SFX/NPC/Enemy/Snake_FIghters/SF_Hurt";

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

   public const string SEA_MINE = "event:/SFX/Player/Interactions/Diegetic/Sea_Mine";

   public const string PLAYER_SHIP_DESTROYED = "event:/SFX/Game/Sea_Battle/Player_Ship_Destroyed";
   public const string ENEMY_SHIP_IMPACT = "event:/SFX/Game/Sea_Battle/Enemy_Ship_Impact";
   public const string ENEMY_SHIP_DESTROYED = "event:/SFX/Game/Sea_Battle/Enemy_Ship_Destroyed";

   public const string HORROR_TENTACLE_HURT = "event:/SFX/NPC/Boss/Tentacle_Horror_Boss/Hurt";
   public const string HORROR_TENTACLE_DEATH = "event:/SFX/NPC/Boss/Tentacle_Horror_Boss/Tentacle Death";
   public const string HORROR_DEATH = "event:/SFX/NPC/Boss/Tentacle_Horror_Boss/Death";
   public const string HORROR_POISON_BOMB = "event:/SFX/NPC/Boss/Tentacle_Horror_Boss/Poison_Bomb";
   public const string HORROR_BLOB_DAMAGE = "event:/SFX/NPC/Boss/Tentacle_Horror_Boss/Blob_Pylr_Damage";

   public const string FISHMAN_ATTACK = "event:/SFX/NPC/Enemy/Fishman_Seamonster/Fishman_Throw_Attack";
   public const string FISHMAN_HURT = "event:/SFX/NPC/Enemy/Fishman_Seamonster/Seamonster_Hurt";
   public const string FISHMAN_DEATH = "event:/SFX/NPC/Enemy/Fishman_Seamonster/Fishman_Death";

   public const string REEFMAN_ATTACK = "event:/SFX/NPC/Enemy/Giant_Reefman/Giant_Reefman_Throw";
   public const string REEFMAN_HURT = "event:/SFX/NPC/Enemy/Giant_Reefman/Giant_Reefman_Pain";
   public const string REEFMAN_DEATH = "event:/SFX/NPC/Enemy/Giant_Reefman/Giant_Reefman_Death";

   public const string HARPOON = "event:/SFX/Player/Interactions/Diegetic/Harpoon";

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
   public const string FOOTSTEP = "event:/SFX/Player/Interactions/Diegetic/Footstep";

   public const string DOOR_OPEN = "event:/SFX/Player/Interactions/Diegetic/Door_Open";
   public const string DOOR_CLOSE = "event:/SFX/Player/Interactions/Diegetic/Door_Close";
   public const string DOOR_CLOTH_OPEN = "event:/SFX/Player/Interactions/Diegetic/Door_Cloth_Open";
   public const string DOOR_CLOTH_CLOSE = "event:/SFX/Player/Interactions/Diegetic/Door_Cloth_Close";

   public const string PICKUP_POWERUP = "event:/SFX/Player/Interactions/Non_Diegetic/Pickup_Powerup_Generic";
   public const string COLLECT_LOOT_LAND = "event:/SFX/Player/Interactions/Diegetic/Collect_Loot_Land";
   public const string OPEN_CHEST = "event:/SFX/Player/Interactions/Diegetic/Open_Treasure_Site_Chest";
   public const string WEAPON_SWING = "event:/SFX/Player/Interactions/Diegetic/Weapons/Swings";
   public const string TRIUMPH_HARVEST = "event:/SFX/Player/Interactions/Non_Diegetic/Triumph_Harvest";
   public const string LOOT_BAG = "event:/SFX/Player/Interactions/Diegetic/Loot_Bag";
   public const string GAIN_SILVER = "event:/SFX/Player/Interactions/Non_Diegetic/Gain_Silver";

   public const string HARVEST_HIT = "event:/SFX/Player/Interactions/Diegetic/Harvest_Hit";
   public const string CROP_PLANT = "event:/SFX/Player/Interactions/Diegetic/Crop_Plant";
   public const string TREE_CUTTER = "event:/SFX/Player/Interactions/Diegetic/TreeCutter";
   public const string THROW_SEEDS = "event:/SFX/Player/Interactions/Diegetic/Throw_Seeds";
   public const string WATERING_PLANTS = "event:/SFX/Player/Interactions/Diegetic/Watering_Plants";

   public const string SHIP_SAILING = "event:/SFX/Player/Interactions/Diegetic/Ship/Ship_Sailing";
   public const string WEB_JUMP = "event:/SFX/Player/Interactions/Diegetic/Web_Jumps";

   public const string INTERACTABLE_BOX = "event:/SFX/Player/Interactions/Diegetic/Wooden_Box_SEQ";
   public const string FIRE_SPLASH = "event:/SFX/Player/Interactions/Diegetic/FireSplash";
   public const string DISCOVERY_RUINS = "event:/SFX/Player/Interactions/Diegetic/Discovery_Ruins";
   public const string BERSERKER_CALL = "event:/SFX/Player/Interactions/Diegetic/Berserker_Call";
   public const string RUFFIAN_REPAIRS = "event:/SFX/Player/Interactions/Diegetic/Ruffian_Repairs";

   public const string WATERFALL_SECRET = "event:/SFX/Player/Interactions/Diegetic/WF_Secret";
   public const string CURTAIN = "event:/SFX/Player/Interactions/Diegetic/Curtain";

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
      if (Util.isBatch() || string.IsNullOrEmpty(path)) {
         return;
      }

      // If the SFX is 2D
      if (position == default) {
         position = AudioListenerManager.self.getActiveFmodListener().gameObject.transform.position;
      }

      FMODUnity.RuntimeManager.PlayOneShot(path, position);
   }

   public void playEquipSfx (bool isSpecial = false) {
      string path = isSpecial ? EQUIP_SPECIAL : EQUIP;
      playFmodSfx(path);
   }

   public void playLandProjectileSfx (Weapon.Class weaponClass, Vector3 position) {
      string path = string.Empty;

      switch (weaponClass) {
         case Weapon.Class.Ranged:
            path = GENERIC_GUN_SHOT;
            break;
         case Weapon.Class.Rum:
            path = THROW_RUM;
            break;
      }

      playFmodSfx(path, position);
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

   public void playLandBattleHitSfx (Enemy.Type sourceType, Enemy.Type targetType, AttackAbilityData ability, Vector3 position) {
      string hitPath = GENERIC_HIT_LAND;
      string hurtPath = string.Empty;

      // Hit sfx
      switch (ability.classRequirement) {
         case Weapon.Class.Rum:
            hitPath = ATTACK_RUM;
            break;
      }

      // Hurt sfx
      switch (targetType) {
         case Enemy.Type.Snake_Assassin:
         case Enemy.Type.Snake_Base:
         case Enemy.Type.Snake_Healer:
         case Enemy.Type.Snake_Ranged:
         case Enemy.Type.Snake_Tank:
            hurtPath = SNAKE_HURT;
            break;
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

      playFmodSfx(hurtPath, position);
   }

   public void playOneShotWithParam (string path, string parameterName, int parameterValue, Vector3 position = default) {
      FMOD.Studio.EventInstance eventInstance = createEventInstance(path);
      if (position != default) {
         eventInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(position));
      }
      eventInstance.setParameterByName(parameterName, parameterValue);
      eventInstance.start();
      eventInstance.release();
   }

   public void playProjectileTerrainHitSound (bool hitLand, bool hitEnemy, ProjectileType projectileType, Transform projectileTransform, Rigidbody2D projectileBody) {
      if (hitLand || hitEnemy) {
         return;
      }

      FMOD.Studio.EventInstance hitInstance;

      string eventPath = CANNONBALL_IMPACT;
      string parameterName = AUDIO_SWITCH_PARAM;
      int parameterValue = 0;

      switch (projectileType) {
         case ProjectileType.Sea_Mine:
            eventPath = SEA_MINE;
            parameterName = AMB_SW_PARAM;
            parameterValue = 1;
            break;
         case ProjectileType.Cannonball_Fire:
            eventPath = FIRE_SPLASH;
            break;
      }

      hitInstance = createEventInstance(eventPath);
      hitInstance.setParameterByName(parameterName, parameterValue);

      if (projectileType == ProjectileType.Sea_Mine) {
         FMODUnity.RuntimeManager.AttachInstanceToGameObject(hitInstance, projectileTransform, projectileBody);
      } else {
         hitInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(projectileTransform.position));
      }

      hitInstance.start();
      hitInstance.release();
   }

   // Background Music & Ambience
   public void playBgMusic (BgType bgType, AmbType ambType) {
      #region Events validation

      if (!_ambEvent.isValid()) {
         _ambEvent = createEventInstance(AMBIENCE_BED_MASTER);
      }

      if (!_tsEvent.isValid()) {
         _tsEvent = createEventInstance(TITLE_SCREEN_AMBIENCE);
      }

      if (!_bgEvent.isValid()) {
         _bgEvent = createEventInstance(BGM_MASTER);
      }

      #endregion

      #region Ambience & Title Screen

      if (_currentAmbience != ambType) {
         if (ambType == AmbType.Previous) {
            ambType = _previousAmbience;
         }

         _previousAmbience = _currentAmbience;
         _currentAmbience = ambType;

         if (ambType != AmbType.None && ambType != AmbType.Title_Screen) {
            _ambEvent.setParameterByName(AMB_SW_PARAM, (int) ambType);

            _ambEvent.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE ambState);
            if (ambState == FMOD.Studio.PLAYBACK_STATE.STOPPED) {
               _ambEvent.start();
            }
         } else {
            _ambEvent.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
         }

         _tsEvent.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE tsState);
         if (bgType == BgType.Intro && tsState == FMOD.Studio.PLAYBACK_STATE.STOPPED) {
            _tsEvent.start();
         } else if (tsState == FMOD.Studio.PLAYBACK_STATE.PLAYING) {
            _tsEvent.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
         }
      }

      #endregion

      #region Background Music

      if (_currentMusic != bgType) {
         if (bgType == BgType.Previous) {
            bgType = _previousMusic;
         }

         _previousMusic = _currentMusic;
         _currentMusic = bgType;

         if (bgType != BgType.None) {
            _bgEvent.setParameterByName(AMB_SW_PARAM, (int) bgType);

            _bgEvent.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE bgState);
            if (bgState == FMOD.Studio.PLAYBACK_STATE.STOPPED || _previousMusic == BgType.Land_Battle) {
               _bgEvent.start();
            }
         } else {
            _bgEvent.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
         }
      }

      #endregion

      //D.debug("Should Play biome BGM: " + _currentMusic + ", with Ambience: " + _currentAmbience);
   }

   public AmbType getAreaBasedAmbience (string areaKey, Biome.Type biomeType) {
      if (string.Equals(areaKey, _cementeryAreaKey, StringComparison.InvariantCultureIgnoreCase)) {
         return AmbType.Forest_Cementery;
      }

      if (VoyageManager.isTreasureSiteArea(areaKey)) {
         return AmbType.Treasure_Site;
      }

      if (AreaManager.self.tryGetCustomMapManager(areaKey, out CustomMapManager customMapManager)) {
         if (customMapManager is CustomFarmManager || CustomMapManager.isPrivateCustomArea(areaKey)) {
            return AmbType.Farm;
         }
      }

      if (AreaManager.self.isInteriorArea(areaKey)) {
         return AmbType.Interior;
      } else if (AreaManager.self.isSeaArea(areaKey)) {
         return AmbType.Sea;
      } else {
         return getBiomeBasedAmbience(biomeType);
      }
   }

   private AmbType getBiomeBasedAmbience (Biome.Type biomeType) {
      switch (biomeType) {
         case Biome.Type.Forest:
            return AmbType.Forest;
         case Biome.Type.Desert:
            return AmbType.Desert;
         case Biome.Type.Pine:
            return AmbType.Pine;
         case Biome.Type.Snow:
            return AmbType.Snow;
         case Biome.Type.Lava:
            return AmbType.Lava;
         case Biome.Type.Mushroom:
            return AmbType.Mushroom;
         default:
            return AmbType.None;
      }
   }

   public BgType getAreaBasedBgMusic (string areaKey, Biome.Type biomeType) {
      // Farm or Guild Map
      if (AreaManager.self.tryGetCustomMapManager(areaKey, out CustomMapManager customMapManager)) {
         if (customMapManager is CustomFarmManager || CustomMapManager.isPrivateCustomArea(areaKey)) {
            return BgType.Farm;
         }
      }

      bool isSea = AreaManager.self.isSeaArea(areaKey);

      if (isSea) {
         if (VoyageManager.isPvpArenaArea(areaKey) && !WorldMapManager.isWorldMapArea(areaKey)) {
            return BgType.Sea_PvP;
         } else if (VoyageManager.isLeagueArea(areaKey)) {
            return BgType.Sea_League;
         } else if (VoyageManager.isLeagueSeaBossArea(areaKey)) {
            return BgType.Sea_Lava; // Temp
         }
      }

      if (AreaManager.self.isInteriorArea(areaKey)) {
         return BgType.Interior;
      } else {
         return getBiomeBasedBgMusic(isSea, biomeType);
      }
   }

   private BgType getBiomeBasedBgMusic (bool isSea, Biome.Type biomeType) {
      switch (biomeType) {
         case Biome.Type.Forest:
            return isSea ? BgType.Sea_Forest : BgType.Forest;
         case Biome.Type.Desert:
            return isSea ? BgType.Sea_Desert : BgType.Desert;
         case Biome.Type.Pine:
            return isSea ? BgType.Sea_Pine : BgType.Pine;
         case Biome.Type.Snow:
            return isSea ? BgType.Sea_Snow : BgType.Snow;
         case Biome.Type.Lava:
            return isSea ? BgType.Sea_Lava : BgType.Lava;
         case Biome.Type.Mushroom:
            return isSea ? BgType.Sea_Mushroom : BgType.Mushroom;
         default:
            return BgType.None;
      }
   }

   public void playTriumphSfx () {
      if (_bgEvent.isValid()) {
         _bgEvent.setParameterByName(AMB_SW_PARAM, 20);
      }
   }

   public void setAmbienceWeather (WeatherEffectType weatherEffect) {
      if (_ambEvent.isValid()) {
         int param = 0;
         switch (weatherEffect) {
            case WeatherEffectType.Rain:
               param = 1;
               break;
         }
         _ambEvent.setParameterByName(WEATHER_PARAM, param);
      }
   }

   public void playShipSailingSfx (ShipSailingType shipSailingType, Transform shipTransform, Rigidbody2D shipBody) {
      if (!_shipSailingEvent.isValid()) {
         _shipSailingEvent = createEventInstance(SHIP_SAILING);
      }

      FMODUnity.RuntimeManager.AttachInstanceToGameObject(_shipSailingEvent, shipTransform, shipBody);

      _shipSailingEvent.setParameterByName(AUDIO_SWITCH_PARAM, (int) shipSailingType);

      _shipSailingEvent.getPlaybackState(out FMOD.Studio.PLAYBACK_STATE state);

      if (state == FMOD.Studio.PLAYBACK_STATE.STOPPED) {
         _shipSailingEvent.start();
      }
   }

   public void triggerSeaAbilitySfx (uint netId, SeaAbilityType seaAbilityType, bool isPlay) {
      if (isPlay) {
         if (seaAbilityType == SeaAbilityType.Ruffian_Repairs) {
            playRuffianRepairs(netId);
         }
      } else {
         if (seaAbilityType == SeaAbilityType.Ruffian_Repairs) {
            stopRuffianRepairs(netId);
         }
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
            FMOD.Studio.EventInstance animalEvent = createEventInstance(CRITTER_INFLECTION);
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

      playFmodSfx(path, position);
   }

   public void playSeaEnemyDeathSfx (SeaMonsterEntity.Type monsterType, Vector3 position) {
      string path = "";

      switch (monsterType) {
         case SeaMonsterEntity.Type.Horror:
            path = HORROR_DEATH;
            break;
         case SeaMonsterEntity.Type.Horror_Tentacle:
         case SeaMonsterEntity.Type.Tentacle:
            path = HORROR_TENTACLE_DEATH;
            break;
         case SeaMonsterEntity.Type.Fishman:
         case SeaMonsterEntity.Type.Worm:
            path = FISHMAN_DEATH;
            break;
         case SeaMonsterEntity.Type.Reef_Giant:
            path = REEFMAN_DEATH;
            break;
      }

      playFmodSfx(path, position);
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
         case Weapon.ActionType.PlantTree:
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
      FMOD.Studio.EventInstance weaponEvent = createEventInstance(WEAPON_SWING);

      //eventInstance.setParameterByName(AUDIO_SWITCH_PARAM, ((int) sfxType) - 1);
      weaponEvent.setParameterByName(AUDIO_SWITCH_PARAM, 3); // Using the same parameter, for now.

      weaponEvent.setParameterByName(APPLY_MAGIC, weaponClass == Weapon.Class.Magic ? 1 : 0);
      weaponEvent.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(position));
      weaponEvent.start();
      weaponEvent.release();
      //}
   }

   public void playSeaEnemyHitSfx (bool isShip, SeaMonsterEntity.Type seaMonsterType, bool isCrit, CannonballEffector.Type effectorType, GameObject source) {
      FMOD.Studio.EventInstance hitEvent = createEventInstance(ENEMY_SHIP_IMPACT);

      hitEvent.setParameterByName(AUDIO_SWITCH_PARAM, isShip ? 0 : 1);
      hitEvent.setParameterByName(APPLY_CRIT_PARAM, isCrit ? 0 : 1);

      switch (effectorType) {
         case CannonballEffector.Type.Explosion:
            hitEvent.setParameterByName(APPLY_PUP_PARAM, 1);
            break;
      }

      string path = string.Empty;

      switch (seaMonsterType) {
         case SeaMonsterEntity.Type.Fishman:
         case SeaMonsterEntity.Type.Worm:
            path = FISHMAN_HURT;
            break;
         case SeaMonsterEntity.Type.Horror_Tentacle:
         case SeaMonsterEntity.Type.Tentacle:
            path = HORROR_TENTACLE_HURT;
            break;
         case SeaMonsterEntity.Type.Reef_Giant:
            path = REEFMAN_HURT;
            break;
      }

      // Hit Event
      hitEvent.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(source.transform.position));
      hitEvent.start();
      hitEvent.release();

      // Hurt Event
      playAttachedWithPath(path, source);
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
   public void playSeaAbilitySfx (SeaAbilityType seaAbilityType, uint netId = 0, Vector3 targetPosition = default) {
      switch (seaAbilityType) {
         case SeaAbilityType.Sail_Shredder:
         case SeaAbilityType.Davy_Jones:
            playShipCannonSfx(seaAbilityType, position: targetPosition);
            break;
         case SeaAbilityType.Fishman_Attack:
            playFmodSfx(FISHMAN_ATTACK, targetPosition);
            break;
         case SeaAbilityType.Reef_Giant_Attack:
            playFmodSfx(REEFMAN_ATTACK, targetPosition);
            break;
         case SeaAbilityType.Berzerkers_Call:
            NetEntity sourceEntity = EntityManager.self.getEntityByNetId(netId);
            if (sourceEntity != null && sourceEntity is SeaEntity) {
               if (seaAbilityType == SeaAbilityType.Berzerkers_Call) {
                  playAttachedSfx(BERSERKER_CALL, sourceEntity.transform, sourceEntity.getRigidbody());
               }
            }
            break;
      }
   }

   // Horror Poison Bomb.
   public void playHorrorPoisonSfx (HorrorAttackType attackType, Vector3 position) {
      string path = string.Empty;
      FMOD.Studio.EventInstance eventInstance = createEventInstance(HORROR_POISON_BOMB);

      int param = attackType == HorrorAttackType.Cluster ? 1 : 0;

      eventInstance.setParameterByName(AUDIO_SWITCH_PARAM, param);
      eventInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(position));
      eventInstance.start();
      eventInstance.release();
   }

   // Ship Cannon Ball SFX
   public void playShipCannonSfx (SeaAbilityType seaAbilityType = SeaAbilityType.None, ProjectileType projectileType = ProjectileType.None, Vector3 position = default, Transform projectileTransform = null, Rigidbody2D projectileBody = null) {
      FMOD.Studio.EventInstance shipCannonEvent = createEventInstance(SHIP_CANNON);

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
            switch (seaAbilityType) {
               case SeaAbilityType.Davy_Jones:
               case SeaAbilityType.None:
                  playShipCannonSfx(projectileType: projectileType, projectileTransform: projectileTransform, projectileBody: projectileBody);
                  break;
            }
            break;
         case ProjectileType.Sea_Mine:
            playAttachedSfx(SEA_MINE, projectileTransform, projectileBody);
            break;
         case ProjectileType.Tentacle:
            if (seaAbilityType != SeaAbilityType.Horror_Poison_Cirle) {
               playHorrorPoisonSfx(HorrorAttackType.Single, projectileTransform.position);
            }
            break;
      }
   }

   // Play attached SFX
   public void playAttachedWithPath (string path, GameObject target) {
      if (!string.IsNullOrEmpty(path)) {
         FMODUnity.RuntimeManager.PlayOneShotAttached(path, target);
      }
   }

   public void playAttachedWithType (SoundManager.Type soundType, GameObject target) {
      string path = "";

      switch (soundType) {
         case SoundManager.Type.Skeleton_Walk:
            path = SKELETON_WALK;
            break;
      }

      playAttachedWithPath(path, target);
   }

   public void playAttachedSfx (string path, Transform targetTransform, Rigidbody2D targetBody) {
      FMOD.Studio.EventInstance soundEvent = createEventInstance(path);
      FMODUnity.RuntimeManager.AttachInstanceToGameObject(soundEvent, targetTransform, targetBody);
      soundEvent.start();
      soundEvent.release();
   }

   public void playFootstepSfx (Vector3 playerPosition, string areaKey) {
      Area area = AreaManager.self.getArea(areaKey);
      Biome.Type biomeType = AreaManager.self.getDefaultBiome(areaKey);

      // Footsteps in the farm area
      if (AreaManager.self.tryGetCustomMapManager(areaKey, out CustomMapManager customMapManager)) {
         if (customMapManager is CustomMapManager) {
            biomeType = Biome.Type.Forest;
         }
      }

      TileAttributes.Type[] buffer = new TileAttributes.Type[16];

      int count = area.getTileAttributes(playerPosition, buffer);
      int audioParam = 7; // For generic / interior footsteps, default

      if (count > 0) {
         if (!AreaManager.self.isInteriorArea(areaKey)) {
            TileAttributes.Type attribute = buffer[count - 1];

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
               case TileAttributes.Type.Dirt:
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
      }

      if (!_footstepsLastSound.ContainsKey(audioParam) || Time.time - _footstepsLastSound[audioParam] > .25f) {
         FMOD.Studio.EventInstance footstepEvent = createEventInstance(FOOTSTEP);

         footstepEvent.setParameterByName(AUDIO_SWITCH_PARAM, audioParam);
         footstepEvent.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(playerPosition));
         footstepEvent.start();
         footstepEvent.release();

         _footstepsLastSound[audioParam] = Time.time;
      }
   }

   public void playJumpLandSfx (Vector3 playerPosition, string areaKey) {
      if (!Util.isBatch()) {
         Area area = AreaManager.self.getArea(areaKey);
         if (area != null) {
            Biome.Type biomeType = AreaManager.self.getDefaultBiome(areaKey);

            TileAttributes.Type[] attributesBuffer = new TileAttributes.Type[16];

            int count = area.getTileAttributes(playerPosition, attributesBuffer);
            int audioParam = 0; // Grass is default

            if (count > 0) {
               if (!AreaManager.self.isInteriorArea(areaKey)) {

                  TileAttributes.Type attribute = attributesBuffer[count - 1];

                  switch (attribute) {
                     case TileAttributes.Type.Generic:
                     case TileAttributes.Type.Dirt:
                        if (attribute == TileAttributes.Type.Dirt) {
                           audioParam = 1;
                        } else {
                           switch (biomeType) {
                              case Biome.Type.Desert:
                              case Biome.Type.Snow:
                                 audioParam = 1; // Sand / Snow
                                 break;
                           }
                        }
                        break;
                     case TileAttributes.Type.WaterPartial:
                     case TileAttributes.Type.WaterFull:
                        audioParam = 2;
                        break;
                  }
               } else {
                  audioParam = 3; // Interior / Carpet
               }
            }

            FMOD.Studio.EventInstance landEvent = createEventInstance(JUMP_LAND);

            landEvent.setParameterByName(AUDIO_SWITCH_PARAM, audioParam);
            landEvent.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(playerPosition));
            landEvent.start();
            landEvent.release();
         }
      }
   }

   public void playDoorSfx (DoorAction action, Biome.Type biomeType, Vector3 position) {
      string path = string.Empty;

      switch (action) {
         case DoorAction.Open:
            path = biomeType == Biome.Type.Desert ? DOOR_CLOTH_OPEN : DOOR_OPEN;
            break;
         case DoorAction.Close:
            path = biomeType == Biome.Type.Desert ? DOOR_CLOTH_CLOSE : DOOR_CLOSE;
            break;
      }

      playFmodSfx(path, position);
   }

   #region Ruffian Repairs

   private void playRuffianRepairs (uint netId) {
      NetEntity entity = EntityManager.self.getEntityByNetId(netId);
      if (entity != null && !_ruffianRepairsEvents.ContainsKey(netId)) {
         FMOD.Studio.EventInstance eventInstance = createEventInstance(RUFFIAN_REPAIRS);
         FMODUnity.RuntimeManager.AttachInstanceToGameObject(eventInstance, entity.transform, entity.getRigidbody());
         eventInstance.start();
         _ruffianRepairsEvents.Add(netId, eventInstance);
      }
   }

   private void stopRuffianRepairs (uint netId) {
      if (_ruffianRepairsEvents.ContainsKey(netId)) {
         if (_ruffianRepairsEvents.TryGetValue(netId, out FMOD.Studio.EventInstance eventInstance)) {
            eventInstance.setParameterByName(AUDIO_SWITCH_PARAM, 1);
            eventInstance.release();
         }
         _ruffianRepairsEvents.Remove(netId);
      }
   }

   #endregion

   public void stopSfxForEntity (uint netId) {
      stopRuffianRepairs(netId);
   }

   public void playTreeChop (Vector3 position, bool isLastHit) {
      if (!Util.isBatch()) {
         FMOD.Studio.EventInstance eventInstance = createEventInstance(TREE_CUTTER);
         eventInstance.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(position));
         eventInstance.setParameterByName(AMB_SW_PARAM, isLastHit ? 1 : 0);
         eventInstance.start();
         eventInstance.release();
      }
   }

   public void playBuySellSfx () {
      playFmodSfx(PURCHASE_ITEM);
   }

   public FMOD.Studio.EventInstance createEventInstance (string path) {
      return FMODUnity.RuntimeManager.CreateInstance(path);
   }

   #region Private Variables

   // The time at which we last player a specified clip
   private Dictionary<string, float> _lastPlayTime = new Dictionary<string, float>();

   // Event for main background music
   private FMOD.Studio.EventInstance _bgEvent;

   // Event for main ambience music
   private FMOD.Studio.EventInstance _ambEvent;

   // Event for title screen ambience music
   private FMOD.Studio.EventInstance _tsEvent;

   // Footsteps dictionary
   private Dictionary<int, float> _footstepsLastSound = new Dictionary<int, float>();

   // Ship Sailing event
   private FMOD.Studio.EventInstance _shipSailingEvent;

   // Last music we played
   private BgType _previousMusic = BgType.None;

   // Current music we're playing
   private BgType _currentMusic = BgType.None;

   // Last ambience we played
   private AmbType _previousAmbience = AmbType.None;

   // Current ambience we're playing
   private AmbType _currentAmbience = AmbType.None;

   // Cementery area key
   private const string _cementeryAreaKey = "Tutorial Town Cemetery v2";

   // Ruffian Repairs events
   private Dictionary<uint, FMOD.Studio.EventInstance> _ruffianRepairsEvents = new Dictionary<uint, FMOD.Studio.EventInstance>();

   public enum BgType
   {
      None = -2,
      Previous = -1,
      Forest = 0,
      Desert = 1,
      Snow = 2,
      Lava = 3,
      Pine = 4,
      Mushroom = 5,
      Intro = 6,
      Farm = 7,
      Interior = 8,
      Sea_PvP = 10,
      Sea_Forest = 11,
      Sea_Desert = 12,
      Sea_Lava = 13,
      Sea_Mushroom = 14,
      Sea_Pine = 15,
      Sea_Snow = 16,
      Sea_League = 17,
      Land_Battle = 19
   }

   public enum AmbType
   {
      None = -3,
      Title_Screen = -2,
      Previous = -1,
      Forest = 0,
      Desert = 1,
      Snow = 2,
      Lava = 3,
      Pine = 4,
      Mushroom = 5,
      Treasure_Site = 6,
      Farm = 7,
      Interior = 8,
      Sea = 9,
      Forest_Cementery = 10
   }

   public enum DoorAction
   {
      None = 0,
      Open = 1,
      Close = 2
   }

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
      Horror_Poison_Cirle = 1,
      Sail_Shredder = 2,
      Davy_Jones = 3,
      Fishman_Attack = 4,
      Reef_Giant_Attack = 5,
      Ruffian_Repairs = 6,
      Berzerkers_Call = 7,
      Harpoon = 8
   }

   public enum HorrorAttackType
   {
      None = 0,
      Single = 1,
      Cluster = 2
   }

   public enum ProjectileType
   {
      None = 0,
      Cannonball = 1,
      Cannonball_Ice = 2,
      Cannonball_Fire = 3,
      Sea_Mine = 4,
      Fishman_Attack = 6,
      Tentacle = 7,
      Harpoon = 8
   }

   public enum ShipSailingType
   {
      Movement = 0,
      Stopped = 1
   }

   public enum RuffianRepairsType
   {
      Loop = 0,
      Stop = 1
   }

   public enum HarpoonEvent
   {
      Stop = -1,
      Fire = 0,
      Hit = 1,
      Drag = 2,
      Reel = 3,
      Snap = 4
   }

   #endregion
}

