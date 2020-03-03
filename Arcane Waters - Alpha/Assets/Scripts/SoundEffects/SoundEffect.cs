using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

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

   public enum ValueType
   {
      VOLUME,
      PITCH
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

   #region Private Variables

   #endregion
}
