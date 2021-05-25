using UnityEngine;
using System;
using Random = UnityEngine.Random;

[Serializable]
public class SoundEffect
{
   #region Public Variables

   // The Database ID of the stored effect
   public int id = 0;

   // The name of the effect
   public string name = "";

   // The name of the audio clip
   public string clipName = "";

   // The asset reference to the audio clip
   public AudioClip clip = null;

   // The Minimum Volume of the sound
   [Range(0.0f, 1.0f)]
   public float minVolume = 1.0f;

   // The Maximum Volume of the sound
   [Range(0.0f, 1.0f)]
   public float maxVolume = 1.0f;

   // The Minimum Speed of the sound
   [Range(-3.0f, 3.0f)]
   public float minPitch = 1.0f;

   // The Maximum Speed of the sound
   [Range(-3.0f, 3.0f)]
   public float maxPitch = 1.0f;

   // The starting point of the AudioClip
   [Range(0.0f, 1.0f)]
   public float offset = 0.0f;

   // Whether to play this sound in 3D
   public bool is3D = false;

   // The fmod id used for fmod implementation
   public string fmodId = "";

   public enum ValueType
   {
      VOLUME = 1,
      PITCH = 2,
   }

   #endregion

   public float calculateValue (ValueType type) {
      switch (type) {
         case ValueType.VOLUME:
            return Random.Range(minVolume, maxVolume);
         case ValueType.PITCH:
            return Random.Range(minPitch, maxPitch);
         default:
            return 1.0f;
      }
   }

   public void calibrateSource (AudioSource toCalibrate) {
      toCalibrate.volume = calculateValue(SoundEffect.ValueType.VOLUME);
      toCalibrate.pitch = calculateValue(SoundEffect.ValueType.PITCH);

      if (toCalibrate.pitch < 0.0f) {
         toCalibrate.timeSamples = Mathf.Clamp(
            Mathf.FloorToInt(toCalibrate.clip.samples - 1 - toCalibrate.clip.samples * offset),
            0,
            toCalibrate.clip.samples - 1);
      } else {
         toCalibrate.timeSamples = Mathf.Clamp(
            Mathf.FloorToInt(toCalibrate.clip.samples * offset),
            0,
            toCalibrate.clip.samples - 1);
      }
   }

   #region Private Variables

   #endregion
}
