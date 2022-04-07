using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaHarpoon : SeaProjectile {
   #region Public Variables

   // A reference to the line renderer used to display the harpoon rope
   public LineRenderer harpoonRope;

   // How much the line can stretch before being 'under stress'
   public float lineMaxStretch = 0.2f;

   // The line will break after being under stress for this amount of time
   public float lineStressBreakTime = 0.5f;

   // A reference the to transform that shows where the rope will attach to this projectile
   public Transform ropeAttachPoint;

   #endregion

   protected override void Start () {
      base.Start();

      if (!Util.isBatch()) {
         harpoonRope.enabled = true;

         _alignDirectionWithVelocity = true;
      }

      SeaEntity sourceEntity = SeaManager.self.getEntity(_creatorNetId);
      if (sourceEntity == null) {
         D.error("Harpoon couldn't find a reference to its source entity, netId: " + _creatorNetId);
      } else {
         _sourceEntity = sourceEntity as PlayerShipEntity;
         if (_sourceEntity == null) {
            D.error("Harpoon source entity isn't a player ship.");
         }
      }

      if (isClient && _attachedEntityNetId != 0) {
         onAttachToEntityClient(_attachedEntityNetId);
      }
   }

   protected override void Update () {
      base.Update();

      // Destroy the harpoon if either entity dies
      if (isServer) {
         if ((_sourceEntity && _sourceEntity.isDead()) || (_attachedEntity && _attachedEntity.isDead()) || (isAttachedToEntity && (_sourceEntity == null || _attachedEntity == null))) {
            NetworkServer.Destroy(gameObject);
            return;
         }
      }

      updateHarpoon();
   }

   private void FixedUpdate () {
      if (!isServer) {
         return;
      }

      if (_sourceEntity != null && _attachedEntity != null) {

         Vector2 ropeVector = getRopeEndPos() - getRopeStartPos();
         float ropeLength = ropeVector.magnitude;
         float ropeLengthReduction = 0.0f;

         // Reduce the rope max length, if the source entity is reeling in
         if (_sourceEntity.isReelingIn) {
            float timeSpentReeling = (float) NetworkTime.time - _sourceEntity.reelInStartTime;
            float reelSpeedMultiplier = Mathf.Clamp01(timeSpentReeling / REEL_IN_SPEED_RAMP_UP_TIME);
            ropeLengthReduction = Time.deltaTime * REEL_IN_SPEED * reelSpeedMultiplier;

            _lineMaxLength = Mathf.Clamp(_lineMaxLength - ropeLengthReduction, ROPE_MIN_LENGTH, float.MaxValue);
         }

         // Don't apply a force if the rope's length isn't greater than the max length
         if (ropeLength <= _lineMaxLength) {
            return;
         }

         Vector2 directionToAttachedEntity = ropeVector.normalized;
         Vector2 directionToSourceEntity = -directionToAttachedEntity;

         // Apply forces to both entities, based on the movement of the other entity
         float forceOnSourceMagnitude = Mathf.Clamp(Vector2.Dot(_attachedEntity.movementForce, directionToAttachedEntity), 0.0f, float.MaxValue);
         float forceOnAttachedMagnitude = Mathf.Clamp(Vector2.Dot(_sourceEntity.movementForce, directionToSourceEntity), 0.0f, float.MaxValue);
         Vector2 forceOnSource = forceOnSourceMagnitude * directionToAttachedEntity * Time.deltaTime;
         Vector2 forceOnAttached = forceOnAttachedMagnitude * directionToSourceEntity * Time.deltaTime;

         // Apply a spring force, even if both entities aren't moving
         float springConstant = 800.0f;
         Vector2 springForceOnSource = directionToAttachedEntity * springConstant * Time.deltaTime;
         Vector2 springForceOnAttached = directionToSourceEntity * springConstant * Time.deltaTime;

         if (_sourceEntity.isReelingIn) {
            springForceOnSource = Vector2.zero;
         }

         forceOnSource += springForceOnSource;
         forceOnAttached += springForceOnAttached;

         _sourceEntity.getRigidbody().AddForce(forceOnSource);
         _attachedEntity.getRigidbody().AddForce(forceOnAttached);
      }
   }

   private void updateHarpoon () {
      if (_sourceEntity == null) {
         return;
      }

      Vector3 ropeStart = getRopeStartPos();
      Vector3 ropeEnd = getRopeEndPos();
      ropeStart.z = 7.0f;
      ropeEnd.z = 7.0f;

      harpoonRope.SetPosition(0, ropeStart);
      harpoonRope.SetPosition(1, ropeEnd);
      
      // Only check rope tension once we are attached to something
      if (_attachedEntity == null) {
         return;
      }

      // Update rope alpha based on stretch amount
      Vector2 ropeVector = ropeEnd - ropeStart;
      float ropeLength = ropeVector.magnitude;

      float deltaLineMaxLength = _lineMaxLength - _previousLineMaxLength;

      float reelingStretchModifier = -deltaLineMaxLength * 35.0f;
      float currentRopeStretch = ropeLength - _lineMaxLength + reelingStretchModifier;
      _previousLineMaxLength = _lineMaxLength;

      if (isServer) {
         // If the line is currently under stress, increment the stress timer
         if (currentRopeStretch >= lineMaxStretch) {
            _lineStressTime += Time.deltaTime;
         } else {
            _lineStressTime -= Time.deltaTime;
         }

         _lineStressTime = Mathf.Clamp(_lineStressTime, 0.0f, lineStressBreakTime * 2.0f);

         // If the line has been under stress for long enough, break it
         if (_lineStressTime >= lineStressBreakTime) {
            NetworkServer.Destroy(this.gameObject);
            return;
         }
      }

      if (_lineStressTime > 0.0f) {
         float stressAmount = Mathf.Clamp01(_lineStressTime / lineStressBreakTime);
         float STRESS_MULTIPLIER_CONSTANT = 0.25f;

         // Rope flash speed is proportional to stress amount
         float stressMultiplier = STRESS_MULTIPLIER_CONSTANT * stressAmount;
         float flashAmount = (Mathf.Sin(Time.time * stressMultiplier) + 1.0f) / 2.0f;

         harpoonRope.material.SetFloat("_FlashAmount", flashAmount);
      } else {
         harpoonRope.material.SetFloat("_FlashAmount", 0.0f);
      }
   }

   protected override void onHitEnemy (SeaEntity hitEntity, SeaEntity sourceEntity, int finalDamage) {
      base.onHitEnemy(hitEntity, sourceEntity, finalDamage);

      // If either entity is already attached to the other by a harpoon, don't attach them.
      if (hitEntity.attachedByHarpoonNetIds.Contains(sourceEntity.netId) || sourceEntity.attachedByHarpoonNetIds.Contains(hitEntity.netId)) {
         return;
      }

      attachToEntity(hitEntity);
      _cancelDestruction = true;
      _rigidbody.velocity = Vector2.zero;
      projectileVelocity = Vector2.zero;
      _rigidbody.isKinematic = true;
   }

   private void attachToEntity (SeaEntity entity) {
      // Always attach to the center of the entity
      transform.SetParent(entity.transform, true);
      transform.localPosition = Vector3.zero;

      _attachedEntity = entity;
      _attachedEntityNetId = entity.netId;
      _sourceEntityNetId = _sourceEntity.netId;
      Rpc_OnAttachToEntity(entity.netId);
      _circleCollider.enabled = false;

      _sourceEntity.attachedByHarpoonNetIds.Add(_attachedEntityNetId);
      _attachedEntity.attachedByHarpoonNetIds.Add(_sourceEntityNetId);
   }

   [ClientRpc]
   private void Rpc_OnAttachToEntity (uint entityNetId) {
      onAttachToEntityClient(entityNetId);
   }

   private void onAttachToEntityClient (uint entityNetId) {
      _circleCollider.enabled = false;
      if (trailParticles != null) {
         trailParticles.Stop();
      }

      _circleCollider.transform.localPosition = Vector3.zero;
      shadowRenderer.enabled = false;

      _attachedEntity = SeaManager.self.getEntity(entityNetId);
      transform.SetParent(_attachedEntity.transform, true);
      transform.localPosition = Vector3.zero;

      _rigidbody.velocity = Vector2.zero;
      _rigidbody.isKinematic = true;

      foreach (SpriteRenderer renderer in spriteRenderers) {
         renderer.enabled = false;
      }
   }

   private void OnDisable () {
      if (isServer || Util.isHost()) {
         if (_sourceEntity != null) {
            _sourceEntity.attachedByHarpoonNetIds.Remove(_attachedEntityNetId);
         }
         
         if (_attachedEntity != null) {
            _attachedEntity.attachedByHarpoonNetIds.Remove(_sourceEntityNetId);
         }
      }
   }

   private Vector3 getRopeStartPos () {
      return _sourceEntity.transform.position;
   }

   private Vector3 getRopeEndPos () {
      if (_attachedEntity == null) {
         return ropeAttachPoint.position;
      } else {
         return _attachedEntity.transform.position;
      }
      
   }

   protected override void OnDestroy () {
      base.OnDestroy();

      // Don't need to handle any of these effects in Batch Mode
      if (Util.isBatch() || ClientManager.isApplicationQuitting) {
         return;
      }

      bool hitLand = Util.hasLandTile(transform.position) || hitSeaStructureIsland();

      playHitSound(hitLand, _hitEnemy);

      // Plays SFX and VFX for land collision
      if (hitLand || _hitEnemy) {
         Instantiate(PrefabsManager.self.requestCannonSmokePrefab(_impactMagnitude), transform.position, Quaternion.identity);
      } else {
         Instantiate(PrefabsManager.self.requestCannonSplashPrefab(_impactMagnitude), transform.position, Quaternion.identity);
      }
   }

   #region Private Variables

   // A reference to the entity that created this projectile
   protected PlayerShipEntity _sourceEntity = null;

   // A reference to the entity that we are attached to, if any
   protected SeaEntity _attachedEntity = null;

   // The netid of the entity we are attached to
   [SyncVar]
   protected uint _attachedEntityNetId = 0;

   // The netid of the source entity
   protected uint _sourceEntityNetId = 0;

   // The maximum length for the harpoon rope
   [SyncVar]
   private float _lineMaxLength = 3.0f;

   // How long the harpoon line has been under stress for
   [SyncVar]
   private float _lineStressTime = 0.0f;

   // The length of the rope last frame
   private float _previousLineMaxLength = 3.0f;

   // How fast the line will be reeled in
   private const float REEL_IN_SPEED = 0.25f;

   // How long the player needs to be reeling for, before the reeling reaches 'max speed'
   private const float REEL_IN_SPEED_RAMP_UP_TIME = 1.0f;

   // The minimum amount of length for the harpoon rope, when reeling in, it will stop reeling once it reaches this length
   private const float ROPE_MIN_LENGTH = 0.5f;

   // Returns true when the harpoon is connected to an entity
   private bool isAttachedToEntity => _rigidbody.isKinematic;

   #endregion
}
