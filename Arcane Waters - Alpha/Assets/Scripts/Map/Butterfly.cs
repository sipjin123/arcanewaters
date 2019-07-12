using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Butterfly : MonoBehaviour {
   #region Public Variables

   // Define the different movement states for butterflies
   public enum MoveState { Rising, Landing }

   // The current State that this butterfly is in
   public MoveState moveState = MoveState.Rising;

   // The highest off the ground we'll be allowed to fly
   public float maxY = .2f;

   // When set to a non-zero number, the butterfly will return to its starting position
   public float loopLength = 0f;

   // Our Shadow
   public SpriteRenderer shadow;

   // Our Butterfly
   public SpriteRenderer sprite;

   #endregion

   void Start () {
      _animator = GetComponentInChildren<Animator>();
      _body = GetComponent<Rigidbody2D>();
      _renderer = GetComponent<SpriteRenderer>();
      // _map = GetComponentInParent<LandMap>();

      // Randomly decide if we should face left or right
      sprite.transform.localScale = new Vector3(
          Random.Range(0f, 1f) > .5f ? 1f : -1f,
          1f,
          1f
      );

      // Look up how far we can move when choosing a move target
      _rangeX = GetComponent<BoxCollider2D>().size.x / 2f;
      _rangeY = GetComponent<BoxCollider2D>().size.y / 2f;

      // Assign a random height for this butterfly to hover off the ground
      maxY = Random.Range(.05f, .2f);
      Util.setLocalY(sprite.transform, maxY);

      // Keep track of where we started out at
      _startPos = transform.position;

      // Pick a new move target every second
      InvokeRepeating("pickMoveTarget", 0f, 1f);
      InvokeRepeating("maybeLand", 0f, 3f);

      // Make the butterfly return to the starting position every 6 seconds
      // loopLength = 6f;

      // Start our animation timer
      reset();
   }

   public void Update () {
      // Check the weather
      /*if (_map != null && _map.instance != null) {
         WeatherFX.Type weatherType = _map.instance.weatherType;

         // Fade out in bad weather
         _targetAlpha = weatherType.IsBadWeather() ? 0f : 1f;
      }*/

      // Check what our current alpha value is
      float currentAlpha = _renderer.color.a;

      // If we're already at the target alpha, then we're done
      if (currentAlpha == _targetAlpha) {
         return;
      }

      // Constantly shift towards our target alpha
      if (currentAlpha < _targetAlpha) {
         currentAlpha += (Time.deltaTime / 3f);
      } else if (currentAlpha > _targetAlpha) {
         currentAlpha -= (Time.deltaTime / 3f);
      }

      // Clamp and apply the new alpha value
      currentAlpha = Mathf.Clamp(currentAlpha, 0f, 1f);
      Util.setAlpha(_renderer, currentAlpha);
      Util.setAlpha(shadow, currentAlpha);
   }

   public void FixedUpdate () {
      if (Application.isBatchMode) {
         return;
      }

      // If we've landed, stop moving and stop animating
      if (moveState == MoveState.Landing && sprite.transform.localPosition.y <= 0f) {
         _animator.SetBool("isLanded", true);
      } else {
         _animator.SetBool("isLanded", false);

         // Keep moving towards our target if we're not there yet
         adjustPosition();
      }

      // Adjust our vertical height based on if we're rising or landing
      adjustHeight();
   }

   public void reset () {
      transform.position = _startPos;
      Util.setLocalY(sprite.transform, maxY);
      moveState = MoveState.Rising;
      _startTime = Time.time;
   }

   protected void adjustPosition () {
      // Keep moving towards our target if we're not there yet
      if (Vector2.Distance(transform.position, _moveTarget) > .01f) {
         // Get the direction from our position to the target
         Vector2 dir = (_moveTarget - (Vector2) transform.position).normalized;

         // Multiply the direction by our speed setting
         dir *= _speed;

         // Add the appropriate velocity vector to move us toward the waypoint
         _body.AddForce(dir);
      }
   }

   protected void adjustHeight () {
      // Look up the current Y position of the child transform
      Transform child = sprite.transform;
      float currentY = child.localPosition.y;
      float newY = currentY;

      // Either move us up or down, based on our current Move State
      if (moveState == MoveState.Rising && currentY < maxY) {
         newY = child.localPosition.y + (Time.deltaTime * .25f);
      } else if (moveState == MoveState.Landing && currentY > 0f) {
         newY = child.localPosition.y - (Time.deltaTime * .25f);
      }

      // Clamp
      newY = Util.clamp<float>(newY, 0f, maxY);
      Util.setLocalY(child, newY);
   }

   protected void pickMoveTarget () {
      // If we're disabled, don't do anything
      if (!sprite.enabled) {
         return;
      }

      // Pick a random position within our range to move to
      _moveTarget = _startPos + new Vector2(
          Random.Range(-_rangeX, _rangeX),
          Random.Range(-_rangeY, _rangeY)
      );

      // If we're close to our loop length, return to the start
      if (loopLength != 0f && loopLength - Mathf.Abs(Time.time - _startTime) <= 1f) {
         _moveTarget = _startPos;
         moveState = MoveState.Rising;
      }

      // Pick a random speed to move at
      _speed = Random.Range(2f, 4f);
   }

   protected void maybeLand () {
      // Every few seconds, we have a 50% chance of landing
      moveState = (Random.Range(0f, 1f) <= .5f) ?
          MoveState.Landing : MoveState.Rising;
   }

   #region Private Variables

   // Stores a reference to the Animator component
   protected Animator _animator;

   // Our renderer
   protected SpriteRenderer _renderer;

   // The Map this Butterfly is in
   // protected LandMap _map;

   // Stores a reference to the Rigidbody component
   protected Rigidbody2D _body;

   // The place at which we were initially placed
   protected Vector2 _startPos;

   // The position we're currently moving towards
   protected Vector2 _moveTarget;

   // The speed at which we're currently moving
   protected float _speed;

   // The range at which we're allowed to move from our start position
   protected float _rangeX;

   // The range at which we're allowed to move from our start position
   protected float _rangeY;

   // The time at which we started animating
   protected float _startTime;

   // Our target alpha
   protected float _targetAlpha = 1f;

   // How fast we animated the butterfly
   protected static float ANIM_FPS_SCALE = .45f;

   #endregion
}
