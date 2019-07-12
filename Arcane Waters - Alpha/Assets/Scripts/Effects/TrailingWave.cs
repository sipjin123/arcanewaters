using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class TrailingWave : MonoBehaviour {
   #region Public Variables

   // How long these waves should last
   public static float LIFETIME = 2.5f;

   // Which way the wave should move
   public Vector2 velocity;

   #endregion

   void Start () {
      _creationTime = Time.time;
      _renderer = GetComponent<SpriteRenderer>();

      // Get rid of the wave after enough time passes
      Invoke("destroyThis", LIFETIME);
   }

   private void Update () {
      // Move in the specified direction
      this.transform.Translate(this.velocity * Time.smoothDeltaTime * .1f);

      // Fade out over time
      float timeSoFar = Time.time - _creationTime;
      float percentComplete = timeSoFar / LIFETIME;
      Util.setAlpha(_renderer, (1f - percentComplete) * .5f);
      Util.setScale(_renderer.transform, (1f - percentComplete) * .35f);
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
