using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AmbienceManager : ClientMonoBehaviour {
   #region Public Variables

   // Self
   public static AmbienceManager self;

   #endregion

   protected override void Awake () {
      base.Awake();

      self = this;
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

   protected void updateAmbienceForArea (string newAreaKey) {
      // Figure out what type we should be playing
      List<SoundManager.Type> ambienceTypes = getAmbienceTypeForArea(newAreaKey);

      // Remove any currently playing ambience
      this.gameObject.DestroyChildren();

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

      if (Area.isTown(areaKey)) {
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

   #endregion
}
