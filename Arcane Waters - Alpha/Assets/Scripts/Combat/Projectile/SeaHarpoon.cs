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
         if (_attackType == Attack.Type.Harpoon) {
            harpoonRope.enabled = true;
         }

         _alignDirectionWithVelocity = true;
      }

      if (isServer) {
         _sourceEntity = MyNetworkManager.fetchEntityFromNetId<SeaEntity>(_creatorNetId);
      }
   }

   protected override void Update () {
      base.Update();

      updateHarpoon();
   }

   private void FixedUpdate () {
      if (!isServer) {
         return;
      }

      if (_sourceEntity != null && _attachedEntity != null) {

         Vector2 ropeVector = getRopeEndPos() - getRopeStartPos();
         float ropeLength = ropeVector.magnitude;

         // Don't apply a force if the rope's length isn't greater than the max length
         if (ropeLength <= _lineMaxLength) {
            return;
         }

         float currentRopeStretch = ropeLength - _lineMaxLength;

         // If the line is currently under stress, increment the stress timer
         if (currentRopeStretch >= lineMaxStretch) {
            _lineStressTime += Time.deltaTime;
         } else {
            _lineStressTime = 0.0f;
         }

         // If the line has been under stress for long enough, break it
         if (_lineStressTime > lineStressBreakTime) {
            NetworkServer.Destroy(this.gameObject);
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

         forceOnSource += springForceOnSource;
         forceOnAttached += springForceOnAttached;

         _sourceEntity.getRigidbody().AddForce(forceOnSource);
         _attachedEntity.getRigidbody().AddForce(forceOnAttached);

         float ropeAlpha = 1.0f - Mathf.Clamp01(_lineStressTime / lineStressBreakTime);

         Gradient ropeGradient = harpoonRope.colorGradient;
         ropeGradient.SetKeys(new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.white, 0.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(ropeAlpha, 0.0f), new GradientAlphaKey(ropeAlpha, 0.0f) });
         harpoonRope.colorGradient = ropeGradient;
      }
   }

   private void updateHarpoon () {
      if (!(_attackType == Attack.Type.Harpoon) || _sourceEntity == null) {
         return;
      }

      Vector3 ropeStart = getRopeStartPos();
      Vector3 ropeEnd = getRopeEndPos();
      ropeStart.z = 7.0f;
      ropeEnd.z = 7.0f;

      harpoonRope.SetPosition(0, ropeStart);
      harpoonRope.SetPosition(1, ropeEnd);
   }

   protected override void onHitEnemy (SeaEntity hitEntity, SeaEntity sourceEntity, int finalDamage) {
      base.onHitEnemy(hitEntity, sourceEntity, finalDamage);

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
      Rpc_OnAttachToEntity();

      Vector2 toHitEntity = _attachedEntity.transform.position - _sourceEntity.transform.position;
      _lineMaxLength = toHitEntity.magnitude;
   }

   [ClientRpc]
   private void Rpc_OnAttachToEntity () {
      _circleCollider.enabled = false;
      if (trailParticles != null) {
         trailParticles.Stop();
      }

      _circleCollider.transform.localPosition = Vector3.zero;
      shadowRenderer.enabled = false;

      SpriteRenderer mainRenderer = spriteRenderers[0];

      if (mainRenderer != null) {
         mainRenderer.enabled = false;
      }
   }

   private Vector3 getRopeStartPos () {
      return _sourceEntity.transform.position;
   }

   private Vector3 getRopeEndPos () {
      return ropeAttachPoint.position;
   }

   #region Private Variables

   // A reference to the entity that created this projectile
   protected SeaEntity _sourceEntity = null;

   // A reference to the entity that we are attached to, if any
   protected SeaEntity _attachedEntity = null;

   // The maximum length for the harpoon rope
   private float _lineMaxLength = 1.0f;

   // How long the harpoon line has been under stress for
   private float _lineStressTime = 0.0f;

   #endregion
}
