using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class FarmingTrigger : MonoBehaviour {
   #region Public Variables

   // Reference to the player body
   public BodyEntity bodyEntity;

   // The reference to the colliders
   public Collider2D coneCollider, arcCollider;

   // The spawn positions for the sprite effects
   public Transform[] effectSpawnList;

   // Reference to the animator component of the player body
   public Animator animator;

   // The main reference to the coroutine
   public IEnumerator processingCoroutine;

   // The particle sprite fading speed
   public static float fadeSpeed = 1.5f;

   // The direction where the crop spawn effect will be moving towards
   public Direction cropSpawnEffectDirection;

   #endregion

   public void interactFarming () {
      // Force stop existing coroutine
      if (processingCoroutine != null) {
         StopCoroutine(processingCoroutine);
      }

      // Make sure the player is not moving
      if (bodyEntity.getVelocity().magnitude < .07f) {
         coneCollider.gameObject.SetActive(false);
         arcCollider.gameObject.SetActive(false);

         processingCoroutine = CO_ProcessInteraction();
         StartCoroutine(processingCoroutine);
      }
   }

   private IEnumerator CO_ProcessInteraction () {
      yield return new WaitForSeconds(.1f);

      Weapon.ActionType currentActionType = bodyEntity.weaponManager.actionType;
      AnimatorClipInfo[] m_AnimatorClipInfo = animator.GetCurrentAnimatorClipInfo(0);
      Collider2D currentCollider = coneCollider;
      if (m_AnimatorClipInfo[0].clip.name.ToLower().Contains("interact")) {
         // Handle the angle of the cone shaped collider
         float currentAngle = 0;
         cropSpawnEffectDirection = Direction.North;
         if (bodyEntity.facing == Direction.East || bodyEntity.facing == Direction.SouthEast || bodyEntity.facing == Direction.NorthEast) {
            currentAngle = -90; 
            cropSpawnEffectDirection = Direction.East;
         } else if (bodyEntity.facing == Direction.West || bodyEntity.facing == Direction.SouthWest || bodyEntity.facing == Direction.NorthWest) {
            currentAngle = 90;
            cropSpawnEffectDirection = Direction.West;
         } else if (bodyEntity.facing == Direction.South) {
            currentAngle = 180;
            cropSpawnEffectDirection = Direction.South;
         }
         transform.localEulerAngles = new Vector3(0, 0, currentAngle);

         // Handle the effect to spawn
         foreach (Transform spawnPoint in effectSpawnList) {
            switch (currentActionType) {
               case Weapon.ActionType.PlantCrop:
                  ExplosionManager.createFarmingParticle(currentActionType, spawnPoint.position, fadeSpeed, 4);
                  break;
               case Weapon.ActionType.WaterCrop:
                  ExplosionManager.createFarmingParticle(currentActionType, spawnPoint.position, fadeSpeed, 2, false, 30, 60);
                  break;
            }
         }

         // Using pitch fork uses a different collider since it requires a closer range contact
         if (currentActionType == Weapon.ActionType.HarvestCrop) {
            currentCollider = arcCollider;
         }
         currentCollider.gameObject.SetActive(true);

         // Interact with crops overlapping the cone collider
         RaycastHit2D[] rayHits = new RaycastHit2D[10];
         int hitNum = currentCollider.Cast(new Vector2(0, 0), rayHits);
         foreach (RaycastHit2D hit in rayHits) {
            if (hit.collider != null && hit.collider.GetComponent<CropSpot>() != null) {
               CropSpot cropSpot = hit.collider.GetComponent<CropSpot>();
               cropSpot.tryToInteractWithCropOnClient();

               // Create dirt particle when colliding with crop spots with crops using a pitchfork
               if (currentActionType == Weapon.ActionType.HarvestCrop && cropSpot.crop != null) {
                  if (cropSpot.crop.isMaxLevel() && cropSpot.crop.gameObject.activeSelf) {
                     ExplosionManager.createFarmingParticle(currentActionType, hit.collider.transform.position, fadeSpeed, 4, false);
                     cropSpot.crop.gameObject.SetActive(false);

                     GameObject cropBounce = Instantiate(PrefabsManager.self.cropBouncePrefab);
                     cropBounce.GetComponent<CropHarvest>().cropSpot = cropSpot;
                     cropBounce.GetComponent<CropHarvest>().setSprite(cropSpot.crop.cropType);
                     cropBounce.transform.position = hit.collider.transform.position;

                     if (cropSpawnEffectDirection == Direction.East) {
                        cropBounce.transform.localScale = new Vector3(-1, 1, 1);
                     }
                     cropBounce.GetComponent<Animator>().speed = Random.Range(.8f, 1.2f);
                  }
               }
            }
         }
      }
   }

   #region Private Variables

   #endregion
}