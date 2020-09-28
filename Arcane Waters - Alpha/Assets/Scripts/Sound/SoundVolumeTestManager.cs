using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SoundVolumeTestManager : MonoBehaviour {
   #region Public Variables

   // Type of sound to play
   public List<SoundManager.Type> types;

   #endregion

   public void playSound () {
      if (SoundManager.self == null) {
         D.error("You can use this script only during game");
         return;
      }
      _index = 0;
      _isPlaying = true;
      _audioSource = SoundManager.play2DClip(types[_index]);

      D.log("Currently playing: " + types[_index].ToString());
   }

   private void Update () {
      if (_isPlaying && _audioSource == null) {
         _index++;
         _isPlaying = (types.Count > _index);
         if (_isPlaying) {
            _audioSource = SoundManager.play2DClip(types[_index]);
            D.log("Currently playing: " + types[_index].ToString());
         } else {
            D.log("All sounds played");
         }
      }
   }

   private void OnDisable () {
      if (_audioSource != null && _audioSource.gameObject != null) {
         Destroy(_audioSource.gameObject);
      }
   }

   #region Private Variables

   // AudioSource currently playing
   private AudioSource _audioSource;

   // Index of list to play next
   private int _index = 0;

   // Is currently playing sounds
   private bool _isPlaying = false;

   #endregion
}
