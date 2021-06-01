using System.Collections.Generic;
using FMODUnity;
using UnityEngine;

public class AmbienceManager : ClientMonoBehaviour
{
   #region Public Variables

   // Self
   public static AmbienceManager self;

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
      List<SoundManager.Type> ambienceTypes = getAmbienceTypeForArea(newAreaKey);

      // Remove any currently playing ambience
      this.gameObject.DestroyChildren();
      LoopedSound[] loopedSounds = this.gameObject.GetComponents<LoopedSound>();
      foreach (LoopedSound s in loopedSounds) {
         Destroy(s);
      }

      // Add the new sounds
      foreach (SoundManager.Type typeToPlay in ambienceTypes) {
         playAmbience(typeToPlay);
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
      if (ambienceType != SoundManager.Type.Ambience_Ocean) {
         LoopedSound loopedSound = this.gameObject.AddComponent<LoopedSound>();
         loopedSound.soundType = ambienceType;
      } else {
         GameObject eventGo = new GameObject();
         eventGo.name = "Ambience Emitter";
         eventGo.transform.SetParent(this.transform);

         StudioEventEmitter eventEmitter = eventGo.AddComponent<StudioEventEmitter>();
         eventEmitter.Event = SoundEffectManager.self.getSoundEffect(SoundEffectManager.OCEAN_PAD)?.fmodId;
         eventEmitter.PlayEvent = EmitterGameEvent.ObjectStart;
         eventEmitter.StopEvent = EmitterGameEvent.ObjectDestroy;
         eventEmitter.AllowFadeout = true;
         eventEmitter.Play();
      }
   }

   #region Private Variables

   // The last area we were in
   protected string _lastArea = "";

   #endregion
}
