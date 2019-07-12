using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class FootSound : ClientMonoBehaviour {
   #region Public Variables

   // Sounds for when we're on grass
   public List<AudioClip> grassSounds = new List<AudioClip>();

   // Sounds for when we're on stone
   public List<AudioClip> stoneSounds = new List<AudioClip>();

   // Sounds for when we're on wood
   public List<AudioClip> woodSounds = new List<AudioClip>();

   // Sounds for when we're in water
   public List<AudioClip> waterSounds = new List<AudioClip>();

   // Our associated Audio Source
   public AudioSource audioSource;

   #endregion

   public void Start () {
      // If we have a player, check if they're in water or on some type of ground
      if (Global.player != null) {
         if (Global.player.waterChecker.inWater()) {
            audioSource.clip = waterSounds.ChooseRandom();
         } else if (Global.player.groundChecker.isOnWood) {
            audioSource.clip = woodSounds.ChooseRandom();
         } else if (Global.player.groundChecker.isOnStone) {
            audioSource.clip = stoneSounds.ChooseRandom();
         } else if (Global.player.groundChecker.isOnGrass) {
            audioSource.clip = grassSounds.ChooseRandom();
         }
      }

      // If we assigned a clip, then play it
      if (audioSource.clip != null) {
         // We want to treat multiple versions of the same sound effect as one, so strip out any numbers from the name
         string clipName = Util.removeNumbers(audioSource.clip.name);

         // Make sure enough time has passed
         if (!_lastSoundTime.ContainsKey(clipName) || Time.time - _lastSoundTime[clipName] > .25f) {
            audioSource.Play();

            // Note the time
            _lastSoundTime[clipName] = Time.time;
         }
      }

      // Destroy this object after 1 second
      Destroy(this.gameObject, 1f);
   }

   #region Private Variables

   // Keeps track of when we last played sounds
   protected static Dictionary<string, float> _lastSoundTime = new Dictionary<string, float>();

   #endregion
}
