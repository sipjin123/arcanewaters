using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class DirectionArrow : ClientMonoBehaviour {
   #region Public Variables

   // The object that contains this arrow
   public GameObject container;

   // The target angle we want to be at
   public float targetAngle;

   // The speed at which we reach our target
   public float turnSpeed = 1f;

   #endregion

   private void Start () {
      _startingOffset = this.transform.localPosition.y;

      // Lookup components
      _entity = GetComponentInParent<PlayerShipEntity>();
      _renderer = GetComponent<SpriteRenderer>();
   }

   private void Update () {
      float speedPercent = 1.0f;

      // Note when we last moved
      if (_entity.getVelocity().magnitude > .01) {
         _lastMoveTime = Time.time;
      }

      // Move the arrows further away as the speed increases
      Util.setLocalY(this.transform, _startingOffset * (1f + speedPercent));

      // Adjust the color of our arrow based on the speed
      // _renderer.color = Color.Lerp(Color.yellow, Color.green, speedPercent);

      // Adjust the alpha transparency of our arrow based on the speed
      float timeSinceMovement = Time.time - _lastMoveTime;
      Util.setAlpha(_renderer, 1f - timeSinceMovement);

      // Rotate to the new angle
      this.container.transform.rotation = Quaternion.RotateTowards(this.container.transform.rotation, Quaternion.Euler(0f, 0f, targetAngle), turnSpeed * Time.deltaTime);
   }

   #region Private Variables

   // The associated entity
   protected PlayerShipEntity _entity;

   // Our Sprite Renderer
   protected SpriteRenderer _renderer;

   // The starting offset for this arrow
   protected float _startingOffset;

   // The time at which the entity last moved
   protected float _lastMoveTime;

   #endregion
}
