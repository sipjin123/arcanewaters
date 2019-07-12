using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class Looped3dSound : ClientMonoBehaviour {
   #region Public Variables

   // The type of looped sound we want to create
   public SoundManager.Type soundType;

   #endregion

   void Start () {
      // Create an audio source for us
      AudioSource audioSourcePrefab = Resources.Load<AudioSource>("Prefabs/LoopedSoundEffect");
      _source = Instantiate(audioSourcePrefab);
      _source.transform.SetParent(SoundManager.self.transform);
      _source.transform.position = this.transform.position;
      _source.clip = Resources.Load<AudioClip>("Sound/Effects/" + soundType);

      // Set the parent
      _source.transform.SetParent(this.transform);

      // Start it up
      _source.loop = true;
      _source.Play();
   }

   public AudioSource getSource () {
      return _source;
   }

   private void Update () {
      // Annoying, but we need to keep the sound at the same Z offset as the currently active camera
      if (Global.player != null && _source != null) {
         float distance = Vector2.Distance(Global.player.transform.position, this.transform.position);

         if (distance < 3f) {
            Util.setZ(_source.transform, Camera.main.transform.position.z);
         }
      }
   }

   #region Private Variables

   // Our Audio Source
   protected AudioSource _source;

   #endregion
}
