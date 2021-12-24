using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaHarpoon : SeaProjectile {
   #region Public Variables

   // A reference to the line renderer used to display the harpoon rope
   public LineRenderer harpoonRope;

   // The maximum length for the 
   public float lineMaxLength = 1.0f;

   #endregion

   protected override void Start () {
      base.Start();

      if (!Util.isBatch()) {
         if (_attackType == Attack.Type.Harpoon) {
            harpoonRope.enabled = true;
         }
      }

      if (isServer) {
         _sourceEntity = MyNetworkManager.fetchEntityFromNetId<SeaEntity>(_creatorNetId);
      }
   }

   protected override void Update () {
      base.Update();

      updateHarpoon();
   }

   private void updateHarpoon () {
      if (!(_attackType == Attack.Type.Harpoon) || _sourceEntity == null) {
         return;
      }

      Vector3 ropeStart = _circleCollider.transform.position;
      Vector3 ropeEnd = _sourceEntity.transform.position;
      ropeStart.z = 7.0f;
      ropeEnd.z = 7.0f;

      harpoonRope.SetPosition(0, ropeStart);
      harpoonRope.SetPosition(1, ropeEnd);

      if (_attachedEntity != null) {
         // D.debug("Line length: " + (ropeEnd - ropeStart).magnitude);
      }
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
      RotatedChild rotatedChild = entity.GetComponentInChildren<RotatedChild>();
      if (rotatedChild != null) {
         transform.SetParent(rotatedChild.transform, true);
      } else {
         transform.SetParent(entity.transform, true);
      }
      
      _attachedEntity = entity;
      Rpc_OnAttachToEntity();
   }

   [ClientRpc]
   private void Rpc_OnAttachToEntity () {
      _circleCollider.enabled = false;
      if (trailParticles != null) {
         trailParticles.Stop();
      }

      _circleCollider.transform.localPosition = Vector3.zero;
      shadowRenderer.enabled = false;
   }

   #region Private Variables

   // A reference to the entity that created this projectile
   protected SeaEntity _sourceEntity = null;

   // A reference to the entity that we are attached to, if any
   protected SeaEntity _attachedEntity = null;

   #endregion
}
