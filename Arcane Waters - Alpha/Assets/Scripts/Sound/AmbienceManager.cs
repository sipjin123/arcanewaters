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
      if (Application.isBatchMode) {
         this.gameObject.SetActive(false);
      }
   }

   protected void Update () {
      // Check if our area has changed
      if (Global.player != null && Global.player.areaType != _lastArea) {
         updateAmbienceForArea(Global.player.areaType);

         // Make note of our current area
         _lastArea = Global.player.areaType;
      }
   }

   protected void updateAmbienceForArea (Area.Type newAreaType) {
      // Figure out what type we should be playing
      List<SoundManager.Type> ambienceTypes = getAmbienceTypeForArea(newAreaType);

      // Remove any currently playing ambience
      this.gameObject.DestroyChildren();

      // Add the new sounds
      foreach (SoundManager.Type typeToPlay in ambienceTypes) {
         playAmbience(typeToPlay);
      }
   }

   protected List<SoundManager.Type> getAmbienceTypeForArea (Area.Type areaType) {
      if (Area.isSea(areaType)) {
         return new List<SoundManager.Type>() { SoundManager.Type.Ambience_Ocean };
      }

      if (Area.isHouse(areaType)) {
         return new List<SoundManager.Type>() { SoundManager.Type.Ambience_Outdoor, SoundManager.Type.Ambience_House };
      }

      if (Area.isTown(areaType)) {
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
   protected Area.Type _lastArea = Area.Type.None;

   #endregion
}
