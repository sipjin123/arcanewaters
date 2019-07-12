using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class WakeParticle : MonoBehaviour {
   #region Public Variables

   // How long it should take this particle to fade out
   public static float FADE_TIME = 1.0f;

   // Our scale modifier for increasing the particles over time
   public static float SCALE_MODIFIER = .4f;

   // Our Sprite
   public SpriteRenderer spriteRenderer;

   #endregion

   private void Start () {
      _creationTime = Time.time;
   }

   void Update () {
      // Check how long we've been alive
      float timeAlive = Time.time - _creationTime;

      // Fade out over time
      float newAlpha = 1f - (timeAlive / FADE_TIME);
      Util.setAlpha(spriteRenderer, newAlpha * .75f);

      // Increase size over time
      this.transform.localScale += (Time.smoothDeltaTime * this.transform.localScale * SCALE_MODIFIER);

      // Destroy once we've faded completely out
      if (newAlpha <= 0f) {
         Destroy(this.gameObject);
      }
   }

   #region Private Variables

   // The time at which we were created
   protected float _creationTime;

   #endregion
}
