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

   // The particle sprite fading speed
   public static float fadeSpeed = 1.5f;

   // The direction where the crop spawn effect will be moving towards
   public Direction cropSpawnEffectDirection;

   // Animator Key for direction
   public const string FACING_KEY = "facing";

   #endregion

   public void interactFarming () {
      // Check if a farming action is already being performed
      if (_isFarming) {
         return;
      }

      coneCollider.gameObject.SetActive(false);
      arcCollider.gameObject.SetActive(false);

      StopAllCoroutines();
      StartCoroutine(CO_ProcessInteraction());
   }

   private IEnumerator CO_ProcessInteraction () {
      _isFarming = true;

      yield return new WaitForSeconds(.1f);

      // Play weapon SFX upon triggering animation
      WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(bodyEntity.weaponManager.equipmentDataId);
      if (weaponData != null && (weaponData.actionSfxDirectory.Length > 1 || weaponData.soundId > 0)) {
         // If the weapon has a soundId attached, it's used for FMOD
         if(weaponData.soundId > 0) {
            SoundEffectManager.self.playFmodSoundEffect(weaponData.soundId, transform);
         } else {
            SoundManager.create3dSoundWithPath(weaponData.actionSfxDirectory, transform.position);
         }
      }

      Weapon.ActionType currentActionType = bodyEntity.weaponManager.actionType;
      Collider2D currentCollider = coneCollider;

      updateTriggerDirection();

      playFarmingParticles(currentActionType);

      // Using pitch fork uses a different collider since it requires a closer range contact
      if (currentActionType == Weapon.ActionType.HarvestCrop) {
         currentCollider = arcCollider;
      }
      currentCollider.gameObject.SetActive(true);

      // Interact with crops overlapping the cone collider
      bool anyCropHarvested = false;
      RaycastHit2D[] rayHits = new RaycastHit2D[10];
      int hitNum = currentCollider.Cast(new Vector2(0, 0), rayHits);
      foreach (RaycastHit2D hit in rayHits) {
         if (hit.collider != null && hit.collider.GetComponent<CropSpot>() != null) {
            CropSpot cropSpot = hit.collider.GetComponent<CropSpot>();
            cropSpot.tryToInteractWithCropOnClient();

            // Create dirt particle when colliding with crop spots with crops using a pitchfork
            if (currentActionType == Weapon.ActionType.HarvestCrop && cropSpot.crop != null) {
               if (cropSpot.crop.isMaxLevel() && !cropSpot.crop.hasBeenHarvested()) {
                  anyCropHarvested = true;

                  cropSpot.harvestCrop();
               }
            }
         }

         if (currentActionType == Weapon.ActionType.HarvestCrop) {
            if (!anyCropHarvested) {
               SoundManager.play2DClip(SoundManager.Type.Harvesting_Pitchfork_Miss);
            }
         }
      }

      _isFarming = false;
   }

   public void playFarmingParticles (Weapon.ActionType actionType) {
      // Handle the effect to spawn
      foreach (Transform spawnPoint in effectSpawnList) {
         switch (actionType) {
            case Weapon.ActionType.PlantCrop:
               ExplosionManager.createFarmingParticle(actionType, spawnPoint.position, fadeSpeed, 4);
               break;
            case Weapon.ActionType.WaterCrop:
               ExplosionManager.createFarmingParticle(actionType, spawnPoint.position, fadeSpeed, 2, false, 30, 60);
               break;
         }
      }
   }

   public void updateTriggerDirection () {
      // Check if the user is stationary (the interact animation will have been triggered in PlayerBodyEntity)
      AnimatorClipInfo[] m_AnimatorClipInfo = animator.GetCurrentAnimatorClipInfo(0);
      float currentAngle = 0;
      if (m_AnimatorClipInfo[0].clip.name.ToLower().Contains("interact")) {
         // Handle the angle of the cone shaped collider
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
      } else {
         // When moving around, the farming direction is in front of the character
         cropSpawnEffectDirection = bodyEntity.facing;
         switch (bodyEntity.facing) {
            case Direction.NorthEast:
            case Direction.SouthEast:
            case Direction.East:
               currentAngle = -90;
               break;
            case Direction.NorthWest:
            case Direction.SouthWest:
            case Direction.West:
               currentAngle = 90;
               break;
            case Direction.South:
               currentAngle = 180;
               break;
            case Direction.North:
            default:
               currentAngle = 0;
               break;
         }

         transform.localEulerAngles = new Vector3(0, 0, currentAngle);
      }
   }

   #region Private Variables

   // Gets set to true when a farming action is underway
   private bool _isFarming = false;

   #endregion
}