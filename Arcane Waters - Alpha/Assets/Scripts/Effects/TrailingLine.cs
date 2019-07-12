using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TrailingLine : MonoBehaviour {
   #region Public Variables

   // How long this trailing line should last
   public static float LIFETIME = 1f;

   #endregion

   void Start () {
      _creationTime = Time.time;
      _renderer = GetComponent<SpriteRenderer>();

      // Get rid of the wave after enough time passes
      Invoke("destroyThis", LIFETIME);
   }

   private void Update () {
      float timeSoFar = Time.time - _creationTime;

      // Stay invisible for a bit
      if (timeSoFar < .1f) {
         Util.setAlpha(_renderer, 0f);
         return;
      }

      // Fade out over time
      float percentComplete = timeSoFar / LIFETIME;
      Util.setAlpha(_renderer, (1f - percentComplete) * .75f);
      // Util.setScale(_renderer.transform, (1f - percentComplete) * .25f);
      Util.setScale(_renderer.transform, .3f);
   }

   protected void destroyThis () {
      Destroy(this.gameObject);
   }

   #region Private Variables

   // Our sprite renderer
   protected SpriteRenderer _renderer;

   // The time at which we were created
   protected float _creationTime;

   #endregion
}
