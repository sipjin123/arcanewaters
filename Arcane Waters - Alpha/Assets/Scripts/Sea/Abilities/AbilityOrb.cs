using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using DG.Tweening;
using UnityEngine.Events;

public class AbilityOrb : MonoBehaviour {
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
   public int targetUserId;

   // The time multiplier for the snapping
   public const float SNAP_SPEED_MULTIPLIER = .5f;

   // Minimum snap distance
   public const float SNAP_DISTANCE_THRESOLD = .5f;

   // If snapping to position
   public bool isSnapping = false;

   // If this orb has snapped to target
   public UnityEvent snappedToTargetEvent = new UnityEvent();

   #endregion

   public void init (Attack.Type attackType, Transform parentTransform, int userTarget, bool snapToTargetInstantly) {
      ParticleSystem.MainModule mainModule = trailParticles.main;
      mainModule.customSimulationSpace = parentTransform;

      // Scale up the orb as it is created
      transform.localScale = Vector3.one * 0.1f;
      transform.DOScale(1.0f, 0.5f).SetEase(Ease.OutElastic);

      StartCoroutine(initializeDelay(attackType, parentTransform, userTarget, snapToTargetInstantly));
   }

   private IEnumerator initializeDelay (Attack.Type attackType, Transform parentTransform, int userTarget, bool snapToTargetInstantly) {
      this.attackType = attackType;
      if (!snapToTargetInstantly) {
         yield return new WaitForSeconds(ShipAbilityData.STATUS_CAST_DELAY);
         isSnapping = true;
         targetUserId = userTarget;
         targetEntity = EntityManager.self.getEntity(userTarget);
      } else {
         targetUserId = userTarget;
         targetEntity = EntityManager.self.getEntity(userTarget);
         transform.position = targetEntity.transform.position;
      }
   }

   private void Update () {
      if (targetEntity != null) {
         transform.position = Vector2.Lerp(transform.position, targetEntity.transform.position, snapTimer);
         snapTimer += Time.deltaTime * SNAP_SPEED_MULTIPLIER;
         if (isSnapping && Vector2.Distance(transform.position, targetEntity.transform.position) < SNAP_DISTANCE_THRESOLD) {
            isSnapping = false;
            snappedToTargetEvent.Invoke();
         }
      }
   }

   #region Private Variables

   #endregion
}
