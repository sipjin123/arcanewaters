using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class LoopedSound : MonoBehaviour {
   #region Public Variables

   // The type of looped sound we want to create
   public SoundManager.Type soundType;

   #endregion

   void Start () {
      // Don't play sounds on the server
      if (!Util.isBatchServer()) {
         // Play when we're created
         startPlaying();
      }
   }

   public void startPlaying () {
      // Tell the Sound Manager to create an audio source for us
      _source = SoundManager.createLoopedAudio(soundType, this.transform);
   }

   public AudioSource getSource () {
      return _source;
   }

   #region Private Variables

   // Our Audio Source
   protected AudioSource _source;

   #endregion
}
