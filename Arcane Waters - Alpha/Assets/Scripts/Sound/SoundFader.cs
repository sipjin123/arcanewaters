using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SoundFader : ClientMonoBehaviour {
   #region Public Variables

   #endregion

   void Start () {
      _audioSource = GetComponent<AudioSource>();
   }

   void Update () {
      if (Global.player == null) {
         _audioSource.volume = 0f;
         return;
      }

      float distance = Vector2.Distance(Global.player.transform.position, this.transform.position);
      _audioSource.volume = Mathf.Clamp(1.5f - distance, 0f, 1f);
   }

   #region Private Variables

   // Our associated sound
   protected AudioSource _audioSource;

   #endregion
}
