using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class FarmingTrigger : MonoBehaviour {
   #region Public Variables

   // Reference to the player body
   public BodyEntity bodyEntity;

   // The reference to the cone collision
   public Collider2D myCollider;

   // The spawn positions for the sprite effects
   public Transform[] effectSpawnList;

   // Reference to the animator component of the player body
   public Animator animator;

   // The main reference to the coroutine
   public IEnumerator processingCoroutine;

   #endregion

   public void interactFarming () {
      // Force stop existing coroutine
      if (processingCoroutine != null) {
         StopCoroutine(processingCoroutine);
      }

      // Make sure the player is not moving
      if (bodyEntity.getVelocity().magnitude < .07f) {
         processingCoroutine = CO_ProcessInteraction();
         StartCoroutine(processingCoroutine);
      }
   }

   private IEnumerator CO_ProcessInteraction () {
      yield return new WaitForSeconds(.1f);

      AnimatorClipInfo[] m_AnimatorClipInfo = animator.GetCurrentAnimatorClipInfo(0);
      if (m_AnimatorClipInfo[0].clip.name.ToLower().Contains("interact")) {
         // Handle the angle of the cone shaped collider
         float currentAngle = 0;
         if (bodyEntity.facing == Direction.East || bodyEntity.facing == Direction.SouthEast || bodyEntity.facing == Direction.NorthEast) {
            currentAngle = -90;
         } else if (bodyEntity.facing == Direction.West || bodyEntity.facing == Direction.SouthWest || bodyEntity.facing == Direction.NorthWest) {
            currentAngle = 90;
         } else if (bodyEntity.facing == Direction.South) {
            currentAngle = 180;
         }
         transform.localEulerAngles = new Vector3(0, 0, currentAngle);

         // Handle the effect to spawn
         foreach (Transform spawnPoint in effectSpawnList) {
            switch (bodyEntity.weaponManager.actionType) {
               case Weapon.ActionType.PlantCrop:
                  EffectManager.self.create(Effect.Type.Crop_Dirt_Small, spawnPoint.position);
                  break;
               case Weapon.ActionType.WaterCrop:
                  EffectManager.self.create(Effect.Type.Crop_Water, spawnPoint.position);
                  break;
               case Weapon.ActionType.HarvestCrop:
                  EffectManager.self.create(Effect.Type.Crop_Harvest, spawnPoint.position);
                  break;
            }
         }

         // Interact with crops overlapping the cone collider
         RaycastHit2D[] rayHits = new RaycastHit2D[10];
         int hitNum = myCollider.Cast(new Vector2(0, 0), rayHits);
         foreach (RaycastHit2D hit in rayHits) {
            if (hit.collider != null && hit.collider.GetComponent<CropSpot>() != null) {
               hit.collider.GetComponent<CropSpot>().tryToInteractWithCropOnClient();
            }
         }
      }
   }

   #region Private Variables

   #endregion
}