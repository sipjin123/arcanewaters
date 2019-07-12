using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ExplosionParticle : ClientMonoBehaviour {
   #region Public Variables

   // How fast we should fade out
   public float fadeSpeed = 1f;

   // Our rigidbody
   public Rigidbody2D body;

   #endregion

   void Start () {
      // Look up components
      _renderer = GetComponent<SpriteRenderer>();
   }

   void Update () {
      // Destroy the particle once we've become fully transparent
      if (_renderer.color.a <= 0f) {
         Destroy(this.gameObject);
         return;
      }
      
      // Slowly fade out
      Util.setAlpha(_renderer, _renderer.color.a - (Time.deltaTime * fadeSpeed));
   }

   #region Private Variables

   // Our sprite
   protected SpriteRenderer _renderer;

   #endregion
}
