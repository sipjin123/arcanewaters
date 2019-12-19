using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class AudioClipManager : MonoBehaviour {
   #region Public Variables

   [Serializable]
   public struct AudioClipData
   {
      // The name of the Audio
      public string audioName;

      // The relative path to the Audio
      public string audioPath;

      // The relative path to the Audio, without the file extension
      public string audioPathWithoutExtension;

      // The actual audio clip
      public AudioClip audioClip;
   }

   // Self
   public static AudioClipManager self;

   // List of the audio data
   public List<AudioClipData> audioDataList;

   // Holds the default values of audio clip paths
   public string defaultHitAudio;
   public string defaultCastAudio;

   #endregion

   private void Awake () {
      self = this;
   }

   public AudioClipData getAudioClipData (string path) {
      return audioDataList.Find(_=> _.audioPath == path);
   }

   #region Private Variables

   #endregion
}
