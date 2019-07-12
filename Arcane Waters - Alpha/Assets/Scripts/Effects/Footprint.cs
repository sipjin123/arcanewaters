using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Footprint : MonoBehaviour {
   #region Public Variables

   // Our associated Renderer
   public SpriteRenderer spriteRenderer;

   #endregion

   void Start () {
      _creationTime = Time.time;
   }

   private void Update () {
      // If we haven't been around long enough, don't do anything
      if (Time.time - _creationTime < 5f) {
         return;
      }

      // Slowly fade out
      float currentAlpha = spriteRenderer.color.a;
      Util.setAlpha(spriteRenderer, currentAlpha - Time.smoothDeltaTime * .3f);

      // Destroy once we reach 0 opacity
      if (spriteRenderer.color.a <= 0f) {
         Destroy(this.gameObject);
      }
   }

   #region Private Variables

   // The time at which we were createde
   protected float _creationTime = float.MinValue;

   #endregion
}
