using System.Collections.Generic;
using UnityEngine;

public class AmbienceManager : ClientMonoBehaviour
{
   #region Public Variables

   // Self
   public static AmbienceManager self;

   // Is the FMOD event ready?
   bool isEventReady = false;

   public enum AmbienceType
   {
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

   protected override void Awake () {
      D.adminLog("AmbienceManager.Awake...", D.ADMIN_LOG_TYPE.Initialization);
      base.Awake();

      self = this;
      D.adminLog("AmbienceManager.Awake: OK", D.ADMIN_LOG_TYPE.Initialization);
   }

   protected void Start () {
      // No need for this in batch mode
      if (Util.isBatch()) {
         this.gameObject.SetActive(false);
      }
   }

   protected void Update () {
      // Check if our area has changed
      if (Global.player != null && Global.player.areaKey != _lastArea) {
         updateAmbienceForArea(Global.player.areaKey);

         // Make note of our current area
         _lastArea = Global.player.areaKey;
      }
   }

   public void setTitleScreenAmbience () {
      updateAmbienceForArea("");
   }

   protected void updateAmbienceForArea (string newAreaKey) {
      // Figure out what type we should be playing
      //List<SoundManager.Type> ambienceTypes = getAmbienceTypeForArea(newAreaKey);

      // Remove any currently playing ambience
      this.gameObject.DestroyChildren();
      LoopedSound[] loopedSounds = this.gameObject.GetComponents<LoopedSound>();
      foreach (LoopedSound s in loopedSounds) {
         Destroy(s);
      }

      Biome.Type biomeType = AreaManager.self.getDefaultBiome(newAreaKey);
      bool isSea = AreaManager.self.isSeaArea(newAreaKey);
      bool isInterior = AreaManager.self.isInteriorArea(newAreaKey);

      // Using one event, we can change the ambience using a parameter
      //if (isSea || biomeType == Biome.Type.Forest || biomeType == Biome.Type.Desert || biomeType == Biome.Type.Snow) {
      if (!isEventReady) {
         SoundEffect effect = SoundEffectManager.self.getSoundEffect(SoundEffectManager.AMBIENCE_BED_MASTER);

         if (effect != null) {
            _ambienceEvent = FMODUnity.RuntimeManager.CreateInstance(effect.fmodId);
            isEventReady = true;
         }
      }

      if (isEventReady) {
         if (isSea) {
            _ambienceEvent.setParameterByName(SoundEffectManager.AMBIENCE_AUDIO_SWITCH_PARAM, (int) AmbienceType.SeaMap);
         } else if (isInterior) {
            _ambienceEvent.setParameterByName(SoundEffectManager.AMBIENCE_AUDIO_SWITCH_PARAM, (int) AmbienceType.Interior);
         } else {
            switch (biomeType) {
               case Biome.Type.Forest:
                  _ambienceEvent.setParameterByName(SoundEffectManager.AMBIENCE_AUDIO_SWITCH_PARAM, (int) AmbienceType.Forest);
                  break;
               case Biome.Type.Desert:
                  _ambienceEvent.setParameterByName(SoundEffectManager.AMBIENCE_AUDIO_SWITCH_PARAM, (int) AmbienceType.Desert);
                  break;
               case Biome.Type.Snow:
                  _ambienceEvent.setParameterByName(SoundEffectManager.AMBIENCE_AUDIO_SWITCH_PARAM, (int) AmbienceType.Snow);
                  break;
               case Biome.Type.Lava:
                  _ambienceEvent.setParameterByName(SoundEffectManager.AMBIENCE_AUDIO_SWITCH_PARAM, (int) AmbienceType.Lava);
                  break;
               case Biome.Type.Pine:
                  _ambienceEvent.setParameterByName(SoundEffectManager.AMBIENCE_AUDIO_SWITCH_PARAM, (int) AmbienceType.Pine);
                  break;
               case Biome.Type.Mushroom:
                  _ambienceEvent.setParameterByName(SoundEffectManager.AMBIENCE_AUDIO_SWITCH_PARAM, (int) AmbienceType.Shroom);
                  break;
            }
         }

         _ambienceEvent.start();
      }
      //} else {
      //   if (isEventReady) {
      //      ambienceEvent.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
      //   }

      //   // Add the new sounds
      //   foreach (SoundManager.Type typeToPlay in ambienceTypes) {
      //      playAmbience(typeToPlay);
      //   }
      //}
   }

   public void setAmbienceWeatherEffect (WeatherEffectType weatherEffect) {
      if (_ambienceEvent.isValid()) {
         int weatherValue = 0;

         switch (weatherEffect) {
            case WeatherEffectType.None:
               weatherValue = 0;
               break;
            case WeatherEffectType.Rain:
               weatherValue = 1;
               break;
         }

         _ambienceEvent.setParameterByName(SoundEffectManager.WEATHER_PARAM, weatherValue);
      }
   }

   protected List<SoundManager.Type> getAmbienceTypeForArea (string areaKey) {
      if (AreaManager.self.getArea(areaKey)?.isSea == true) {
         return new List<SoundManager.Type>() { SoundManager.Type.Ambience_Ocean };
      }

      if (Area.isHouse(areaKey)) {
         return new List<SoundManager.Type>() { SoundManager.Type.Ambience_Outdoor, SoundManager.Type.Ambience_House };
      }

      if (AreaManager.self.getAreaSpecialType(areaKey) == Area.SpecialType.Town) {
         return new List<SoundManager.Type>() { SoundManager.Type.Ambience_Outdoor, SoundManager.Type.Ambience_Town };
      }

      return new List<SoundManager.Type>() { SoundManager.Type.Ambience_Forest_Chirps };
   }

   protected void playAmbience (SoundManager.Type ambienceType) {
      LoopedSound loopedSound = this.gameObject.AddComponent<LoopedSound>();
      loopedSound.soundType = ambienceType;
   }

   #region Private Variables

   // The last area we were in
   protected string _lastArea = "";

   // FMOD event instance
   FMOD.Studio.EventInstance _ambienceEvent;

   #endregion
}
