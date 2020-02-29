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

   // The volume of the sound
   [Range(0.0f, 1.0f)]
   public float volume = 1.0f;

   // The speed of the sound
   [Range(-3.0f, 3.0f)]
   public float pitch = 1.0f;

   #endregion

   #region Private Variables

   #endregion
}
