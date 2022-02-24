using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class FootSound : ClientMonoBehaviour {
   #region Public Variables

   // Sound data for specific footstep scenarios
   public List<AudioGroupData> footStepSounds = new List<AudioGroupData>();

   // Footstep data object, allows access to footstep data type details, sounds, and more
   public AudioGroupData currentFootStepAudioGroup;

   // Our associated Audio Source
   //public AudioSource audioSource;

   // Used to find the Grass footstep object type and return its audio data
   public const string GRASS_FOOTSTEP = "Grass FootStep";

   // Used to find the Stone footstep object type and return its audio data
   public const string STONE_FOOTSTEP = "Stone FootStep";

   // Used to find the Water footstep object type and return its audio data
   public const string WATER_FOOTSTEP = "Water FootStep";

   // Used to find the Wood footstep object type and return its audio data
   public const string WOOD_FOOTSTEP = "Wood FootStep";

   #endregion

   public void Start () {

      // Perform compartmentalized setup for audio clip and its settings
      setupFootStepAudioClip ();

      // If we assigned a clip, then play it
      //if (audioSource.clip != null) {
      //   // We want to treat multiple versions of the same sound effect as one, so strip out any numbers from the name
      //   string clipName = Util.removeNumbers(audioSource.clip.name);

      //   // Make sure enough time has passed
      //   if (!_lastSoundTime.ContainsKey(clipName) || Time.time - _lastSoundTime[clipName] > .25f) {

      //      // Check the footstepAudioGroupData for a possibly randomized pitch
      //      audioSource.pitch = currentFootStepAudioGroup.getPitch();

      //      audioSource.Play();

      //      // Note the time
      //      _lastSoundTime[clipName] = Time.time;
      //   }
      //}

      // Destroy this object after 1 second
      Destroy(this.gameObject, 1f);
   }

   // Takes in a string:soundTypeName, traverses footstepSounds, and returns corresponding footstep audioGroupData
   private AudioGroupData findFootStepData (string soundTypeName) {
      return footStepSounds.Find(footStepData=> footStepData.soundType == soundTypeName);
   }

   private void setupFootStepAudioClip () {
      //// When set, Allows main function access to footstep data, sounds, and more
      //currentFootStepAudioGroup = null;
      
      //// If we have a player, check if they're in water or on some type of ground
      //if (Global.player != null) {
      //   if (Global.player.waterChecker.inWater()) {
      //      currentFootStepAudioGroup = findFootStepData(WATER_FOOTSTEP);
      //   } else if (Global.player.groundChecker.isOnWood) {
      //      currentFootStepAudioGroup = findFootStepData(WOOD_FOOTSTEP);
      //   } else if (Global.player.groundChecker.isOnStone) {
      //      currentFootStepAudioGroup = findFootStepData(STONE_FOOTSTEP);
      //   } else if (Global.player.groundChecker.isOnGrass) {
      //      currentFootStepAudioGroup = findFootStepData(GRASS_FOOTSTEP);
      //   } else if (Global.player.groundChecker.isOnBridge) {
      //      if (!_lastSoundTime.ContainsKey(BRIDGE_KEY) || _lastSoundTime[BRIDGE_KEY] + _minTimeBetweenBridgeSound < Time.time) {
      //         //SoundManager.playClipAtPoint(SoundManager.Type.Bridge_Crunching_Wood, Global.player.transform.position);

      //         if (!_lastSoundTime.ContainsKey(BRIDGE_KEY)) {
      //            _lastSoundTime.Add(BRIDGE_KEY, Time.time);
      //         } else {
      //            _lastSoundTime[BRIDGE_KEY] = Time.time;
      //         }
      //      }
      //   }
      //}

      //if (currentFootStepAudioGroup) {
      //   //audioSource.clip = currentFootStepAudioGroup.sounds.ChooseRandom();
      //}
   }

   #region Private Variables

   // Keeps track of when we last played sounds
   protected static Dictionary<string, float> _lastSoundTime = new Dictionary<string, float>();

   // Bridge sound is a bit longer and more intensive - don't play it all the time
   protected static float _minTimeBetweenBridgeSound = 5.0f;

   // Key for _lastSoundTime dictionary, for bridge sound
   protected static string BRIDGE_KEY = "bridge_crunching_wood";

   #endregion
}