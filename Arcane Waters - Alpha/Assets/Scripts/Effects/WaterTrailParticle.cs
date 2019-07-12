using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class WaterTrailParticle : ClientMonoBehaviour {
   #region Public Variables

   // How long we wait between flipping back and forth
   public float flipInterval = .35f;

   // How fast we fade out
   public float fadeOutSpeed = .5f;

   // Our Simple Animation
   public SimpleAnimation simpleAnim;

   // Our rigid body
   [HideInInspector]
   public Rigidbody2D body;

   #endregion

   protected override void Awake () {
      base.Awake();

      // Note the creation time
      _creationTime = Time.time;

      // Get Components
      this.body = GetComponent<Rigidbody2D>();
      _renderer = GetComponent<SpriteRenderer>();

      
   }

   protected void Start () {
      // Randomize our starting flip
      _renderer.flipX = Random.Range(0f, 1f) > .5f;

      // Routinely flip the sprite back and forth
      if (flipInterval > 0f) {
         InvokeRepeating("flipSprite", 0f, flipInterval);
      }
   }

   protected void Update () {
      // Slowly fade out
      float currentAlpha = _renderer.color.a;
      Util.setAlpha(_renderer, currentAlpha - Time.smoothDeltaTime * fadeOutSpeed);
   }

   protected void flipSprite () {
      _renderer.flipX = !_renderer.flipX;
   }

   #region Private Variables

   // Our Sprite Renderer
   protected SpriteRenderer _renderer;

   // The time at which we were created
   protected float _creationTime;

   #endregion
}
