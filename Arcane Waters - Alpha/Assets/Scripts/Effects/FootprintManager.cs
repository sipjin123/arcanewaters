using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class FootprintManager : ClientMonoBehaviour
{
   #region Public Variables

   // The prefab we use for creating footprints
   public Footprint footprintPrefab;

   // The prefab we use for creating footprint sounds
   public FootSound footSoundPrefab;

   #endregion

   void Start () {
      _player = GetComponent<BodyEntity>();
   }

   private void Update () {
      // If we're not moving or we're jumping, don't do anything.
      if (_player.getRigidbody().velocity.magnitude < MIN_VELOCITY || _player.getPlayerBodyEntity().isJumping()) {
         return;
      }

      // If not enough time has passed, don't do anything
      if (Time.time - _lastFootprintTime < FOOTPRINT_INTERVAL) {
         return;
      }

      // If we're too close to the last footprint, don't do anything
      if (Vector2.Distance(this.transform.position, _lastPos) < MIN_DISTANCE) {
         return;
      }

      // Treat this as the foot hitting the ground
      processFootstep();

      // Note the time and position
      _lastFootprintTime = Time.time;
      _lastPos = this.transform.position;

      // Toggle between left and right foot
      _isLeftFoot = !_isLeftFoot;
   }

   protected void processFootstep () {
      // Figure out what sound to play
      if (_player.isLocalPlayer) {
         SoundEffectManager.self.playFootstepSfx(_player.transform.position);
      }

      // If the player wasn't recently in water, don't do anything else
      if (_player.waterChecker.inWater() || !_player.waterChecker.recentlyInWater() || _player.isClimbing()) {
         return;
      }

      // Figure out where to create the footprint.
      Vector3 spawnPos = _player.sortPoint.transform.position;
      spawnPos -= (Vector3) _player.getRigidbody().velocity / 10f;
      spawnPos.z = 100.45f;

      // Create the footprint
      Footprint footprint = Instantiate(this.footprintPrefab, spawnPos, Quaternion.identity);
      footprint.transform.SetParent(EffectManager.self.transform, true);

      // Adjust the starting opacity based on how long it's been since we were in the water
      float startingOpacity = .50f;
      startingOpacity -= _player.waterChecker.getTimeSinceWater() / 5f;
      Util.setAlpha(footprint.spriteRenderer, startingOpacity);

      // Rotate the sprite based on our movement direction
      Vector2 direction = _player.getRigidbody().velocity.normalized;
      float angle = Util.AngleBetween(Vector2.up, direction);
      footprint.transform.Rotate(0f, 0f, angle);

      // Flip the sprite for the opposite foot
      footprint.spriteRenderer.flipX = _isLeftFoot;
   }

   #region Private Variables

   // The minimum velocity we have to move to create a footprint
   protected static float MIN_VELOCITY = .25f;

   // The amount of time between consecutive footprints
   protected static float FOOTPRINT_INTERVAL = .15f;

   // The minimum distance between footprints
   protected static float MIN_DISTANCE = .08f;

   // Our associated player
   protected BodyEntity _player;

   // The time at which we last created a footprint
   protected float _lastFootprintTime = float.MinValue;

   // The position at which we last created a footprint
   protected Vector2 _lastPos;

   // Whether we should create the left foot or right foot
   protected bool _isLeftFoot = false;

   #endregion
}
