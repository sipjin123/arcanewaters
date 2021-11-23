using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;
using UnityEngine.Events;

public class BuffOrb : ClientMonoBehaviour {
   #region Public Variables

   // A value controlling how far around the SeaEntity this powerup orb is currently rotated
   public float rotationValue;

   // A reference to the particle system that displays the trail for this orb
   public ParticleSystem trailParticles;

   // What type of powerup this orb represents
   public Attack.Type attackType;

   // The target entity
   public NetEntity targetEntity;

   // The snap timer towards target
   public float snapTimer = 0;

   // The orb that rotates around a target
   public Transform localOrbRotator;

   // The target user id
   public uint targetNetId;

   // The source of this orb
   public uint ownerNetId;

   // The time multiplier for the snapping
   public const float SNAP_SPEED_MULTIPLIER = .5f;

   // Minimum snap distance
   public const float SNAP_DISTANCE_THRESOLD = .15f;

   // If snapping to position
   public bool isSnapping = false;

   // If the orb is attached to the target
   public bool attachedToTarget = false;

   #endregion

   public void init (uint ownerNetId, Attack.Type attackType, Transform parentTransform, uint targetNetId, bool snapToTargetInstantly, bool isNew) {
      this.ownerNetId = ownerNetId;
      
      if (isNew) {
         ParticleSystem.MainModule mainModule = trailParticles.main;
         mainModule.customSimulationSpace = parentTransform;

         // Scale up the orb as it is created
         transform.localScale = Vector3.one * 0.1f;
         transform.DOScale(1.0f, 0.5f).SetEase(Ease.OutElastic);

         StartCoroutine(initializeDelay(attackType, parentTransform, targetNetId, snapToTargetInstantly));
      }
   }

   private IEnumerator initializeDelay (Attack.Type attackType, Transform parentTransform, uint targetNetId, bool snapToTargetInstantly) {
      this.attackType = attackType;
      this.targetNetId = targetNetId;
      if (snapToTargetInstantly) {
         targetEntity = SeaManager.self.getEntity(targetNetId);
         attachToTarget();
      } else {
         yield return new WaitForSeconds(ShipAbilityData.STATUS_CAST_DELAY);
         isSnapping = true;
         targetEntity = SeaManager.self.getEntity(targetNetId);
      }
   }

   private void Update () {
      // Fly toward target that will receive the buff
      if (targetEntity != null && isSnapping) {
         transform.position = Vector2.Lerp(transform.position, targetEntity.transform.position, snapTimer);
         snapTimer += Time.deltaTime * SNAP_SPEED_MULTIPLIER;
         if (Vector2.Distance(transform.position, targetEntity.transform.position) < SNAP_DISTANCE_THRESOLD) {
            attachToTarget();
         }
      }
   }

   private void attachToTarget () {
      isSnapping = false;
      transform.position = targetEntity.transform.position;
      attachedToTarget = true;
      if (Global.player == null || ownerNetId != targetNetId) {
         gameObject.SetActive(false);
      }
   }

   public void setParticleVisibility (bool isVisible) {
      trailParticles.gameObject.SetActive(isVisible);
   }

   #region Private Variables

   #endregion
}
