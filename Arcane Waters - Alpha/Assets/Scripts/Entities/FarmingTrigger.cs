using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class FarmingTrigger : MonoBehaviour
{
   #region Public Variables

   // Reference to the player body
   public BodyEntity bodyEntity;

   // The reference to the colliders
   public Collider2D coneCollider, arcCollider, chopCollider;

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

   // Cached crop spots highlighted
   public List<CropSpot> cachedCropSpots = new List<CropSpot>();

   // Tree plant indicator
   [Space(5)]
   public Transform treePlantIndicator = null;
   public SpriteRenderer treePlantIndicatorRenderer = null;

   // Types of colors plant tree indicator can have
   public Color treeIndicatorValidColor = Color.white;
   public Color treeIndicatorInvalidColor = Color.white;

   #endregion

   private void Start () {
      InvokeRepeating(nameof(checkForCropInteractions), 1, .5f);
      coneCollider.gameObject.SetActive(true);
   }

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

   private void Update () {
      if (Util.isBatch()) {
         return;
      }

      if (NetworkClient.active && Global.player != null && Global.player.userId == bodyEntity.userId) {
         if (AreaManager.self.tryGetArea(bodyEntity.areaKey, out Area area)) {
            Vector2 pos = area.transform.InverseTransformPoint(getTreePlantPosition());
            bool canPlantTree = PlantableTreeManager.self.canPlayerPlant(bodyEntity, bodyEntity.areaKey, pos, out PlantableTreeDefinition definition);

            if (treePlantIndicator.gameObject.activeSelf != canPlantTree) {
               treePlantIndicator.gameObject.SetActive(canPlantTree);
            }

            if (canPlantTree) {
               treePlantIndicator.transform.position = getTreePlantPosition();

               // Make sure indicator doesn't flip along with the player
               treePlantIndicator.transform.localScale = new Vector3(
                  (Global.player.getRenderers()[0].flipX ? -1 : 1) * Mathf.Abs(treePlantIndicator.transform.localScale.x),
                  treePlantIndicator.transform.localScale.y,
                  treePlantIndicator.transform.localScale.z);


               treePlantIndicatorRenderer.sprite = getGrownTreeSprite(definition);
               treePlantIndicatorRenderer.color = canPlantTree ? treeIndicatorValidColor : treeIndicatorInvalidColor;
            }
         }
      }
   }

   private Sprite getGrownTreeSprite (PlantableTreeDefinition definition) {
      Sprite[] sprites = ImageManager.getSprites(definition.growthStageSprites);
      if (definition.leavesStump) {
         return sprites[sprites.Length - 2];
      } else {
         return sprites[sprites.Length - 1];
      }
   }

   private void checkForCropInteractions () {
      // Skip for batch mode
      if (Util.isBatch()) return;

      if (Global.player == null) {
         return;
      }
      if (Global.player.userId != bodyEntity.userId) {
         return;
      }

      // Check guild map conditions
      bool isInGuildMap = CustomMapManager.isGuildSpecificAreaKey(Global.player.areaKey);
      if (isInGuildMap) {
         // If we're in a guild map, make sure we belong to that guild
         int guildId = CustomMapManager.getGuildId(Global.player.areaKey);
         if (Global.player.guildId != guildId) {
            return;
         }
      }

      // Check user-specific map conditions
      bool isInUserSpecificMap = CustomMapManager.isUserSpecificAreaKey(Global.player.areaKey);
      if (isInUserSpecificMap) {
         // Check if this map belongs to the user
         if (CustomMapManager.getUserId(Global.player.areaKey) != Global.player.userId) {
            return;
         }
      }

      // If we're not in a guild map, or a user-specific map, don't show indicators
      if (!isInGuildMap && !isInUserSpecificMap) {
         return;
      }

      coneCollider.gameObject.SetActive(false);
      arcCollider.gameObject.SetActive(false);

      Direction newDirection = ((PlayerBodyEntity) bodyEntity).forceLookAt(Camera.main.ScreenToWorldPoint(MouseUtils.mousePosition));
      updateTriggerDirection(newDirection);

      // Unmark crop spots that already has crops
      if (cachedCropSpots.Count > 0) {
         List<CropSpot> cropsToClear = new List<CropSpot>();
         foreach (CropSpot cachedCrop in cachedCropSpots) {
            if (cachedCrop.crop != null) {
               cropsToClear.Add(cachedCrop);
               cachedCrop.indicatorObj.SetActive(false);
            }
         }

         foreach (CropSpot cropToClear in cropsToClear) {
            cachedCropSpots.Remove(cropToClear);
         }
      }

      WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(bodyEntity.weaponManager.equipmentDataId);
      if (weaponData != null) {
         if (weaponData.actionType != Weapon.ActionType.PlantCrop) {
            foreach (CropSpot cachedCrop in cachedCropSpots) {
               cachedCrop.indicatorObj.SetActive(false);
            }
            cachedCropSpots.Clear();
            return;
         }

         Collider2D currentCollider = coneCollider;

         // Using pitch fork uses a different collider since it requires a closer range contact
         if (weaponData.actionType == Weapon.ActionType.HarvestCrop) {
            currentCollider = arcCollider;
         }
         currentCollider.gameObject.SetActive(true);

         RaycastHit2D[] rayHits = new RaycastHit2D[10];
         int hitNum = currentCollider.Cast(new Vector2(0, 0), rayHits);

         List<CropSpot> collidedCropSpots = new List<CropSpot>();
         foreach (RaycastHit2D hit in rayHits) {
            if (hit.collider != null) {
               CropSpot cropSpot = hit.collider.GetComponent<CropSpot>();
               if (cropSpot != null && cropSpot.crop == null) {
                  collidedCropSpots.Add(cropSpot);

                  // Register crop as cached crop
                  if (cachedCropSpots.Find(_ => _.cropNumber == cropSpot.cropNumber) == null) {
                     cachedCropSpots.Add(cropSpot);
                     cropSpot.indicatorObj.SetActive(true);
                  }
               }
            }
         }

         // Search for crops that are to be discarded due to no longer colliding with player
         List<CropSpot> toDiscardCrop = new List<CropSpot>();
         foreach (CropSpot cachedCrop in cachedCropSpots) {
            if (collidedCropSpots.Find(_ => _.cropNumber == cachedCrop.cropNumber) != null) {
               // Retain Obj
            } else {
               // Remove crop that did not collide with this
               toDiscardCrop.Add(cachedCrop);
            }
         }

         // Discard the cached crops
         for (int i = 0; i < toDiscardCrop.Count; i++) {
            CropSpot cropToRemove = cachedCropSpots.Find(_ => _.cropNumber == toDiscardCrop[i].cropNumber);
            if (cropToRemove != null) {
               cropToRemove.indicatorObj.SetActive(false);
               cachedCropSpots.Remove(cropToRemove);
            }
         }
      }
   }

   private void clearCache () {

   }

   private IEnumerator CO_ProcessInteraction () {
      _isFarming = true;

      // If player is planting trees, don't try to do anything else 
      PlantableTreeManager.self.playerTriesPlanting(bodyEntity, getTreePlantPosition());

      if (tryGetTreesInRange(out List<PlantableTree> trees)) {
         foreach (PlantableTree tree in trees) {
            PlantableTreeManager.self.playerSwungAtTree(bodyEntity, tree);
         }
      }

      if (tryGetTreesInChopRange(out trees)) {
         foreach (PlantableTree tree in trees) {
            PlantableTreeManager.self.playerSwungAtTreeAxeRange(bodyEntity, tree);
         }
      }

      yield return new WaitForSeconds(.1f);

      // Play weapon SFX upon triggering animation
      WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(bodyEntity.weaponManager.equipmentDataId);
      if (weaponData != null) {
         //if (weaponData != null && weaponData.actionSfxDirectory.Length > 1) {
         //   SoundManager.create3dSoundWithPath(weaponData.actionSfxDirectory, transform.position);
         //}
         // Legacy support for previous implementation
         //SoundEffectManager.self.playLegacyInteractionOneShot(bodyEntity.weaponManager.equipmentDataId, transform);

         Weapon.ActionType currentActionType = weaponData.actionType;

         Collider2D currentCollider = coneCollider;
         updateTriggerDirection(bodyEntity.facing);

         // Using pitch fork uses a different collider since it requires a closer range contact
         if (currentActionType == Weapon.ActionType.HarvestCrop) {
            currentCollider = arcCollider;
         }
         currentCollider.gameObject.SetActive(true);

         // Interact with crops overlapping the cone collider
         //bool anyCropHarvested = false;
         RaycastHit2D[] rayHits = new RaycastHit2D[10];
         int hitNum = currentCollider.Cast(new Vector2(0, 0), rayHits);
         foreach (RaycastHit2D hit in rayHits) {
            if (hit.collider != null && hit.collider.GetComponent<CropSpot>() != null) {
               CropSpot cropSpot = hit.collider.GetComponent<CropSpot>();
               cropSpot.tryToInteractWithCropOnClient();

               // Try to harvest crop when colliding with crops using a pitchfork
               if (currentActionType == Weapon.ActionType.HarvestCrop && cropSpot.crop != null) {
                  if (cropSpot.crop.isMaxLevel() && !cropSpot.crop.hasBeenHarvested()) {
                     //anyCropHarvested = true;

                     cropSpot.tryHarvestCropOnClient();
                  }
               }
            }

            //if (currentActionType == Weapon.ActionType.HarvestCrop) {
            //   if (!anyCropHarvested) {
            //      SoundManager.play2DClip(SoundManager.Type.Harvesting_Pitchfork_Miss);
            //   }
            //}
         }
      }

      _isFarming = false;
   }

   public bool tryGetTreesInChopRange (out List<PlantableTree> trees) {
      trees = new List<PlantableTree>();

      bool wasActive = chopCollider.gameObject.activeSelf;
      chopCollider.gameObject.SetActive(true);

      int hitNum = chopCollider.Cast(new Vector2(0, 0), _treeCheckBuffer);

      chopCollider.gameObject.SetActive(wasActive);

      for (int i = 0; i < hitNum; i++) {
         PlantableTree t = _treeCheckBuffer[i].collider.GetComponentInParent<PlantableTree>();
         if (t != null) {
            trees.Add(t);
         }
      }

      return trees.Count > 0;
   }

   public bool tryGetTreesInRange (out List<PlantableTree> trees) {
      trees = new List<PlantableTree>();

      bool wasActive = arcCollider.gameObject.activeSelf;
      arcCollider.gameObject.SetActive(true);

      int hitNum = arcCollider.Cast(new Vector2(0, 0), _treeCheckBuffer);

      arcCollider.gameObject.SetActive(wasActive);

      for (int i = 0; i < hitNum; i++) {
         PlantableTree t = _treeCheckBuffer[i].collider.GetComponentInParent<PlantableTree>();
         if (t != null) {
            trees.Add(t);
         }
      }

      return trees.Count > 0;
   }

   public Vector2 getTreePlantPosition () {
      Vector2 mPos = Camera.main.ScreenToWorldPoint(MouseUtils.mousePosition);
      Vector2 ourPos = transform.position;
      if ((mPos - ourPos).sqrMagnitude > 1f * 1f) {
         mPos = ourPos + (mPos - ourPos).normalized * 1f;
      }

      return mPos;
   }

   public void playFarmingParticles (Weapon.ActionType actionType) {
      // Handle the effect to spawn
      foreach (Transform spawnPoint in effectSpawnList) {
         switch (actionType) {
            case Weapon.ActionType.PlantCrop:
            case Weapon.ActionType.PlantTree:
               ExplosionManager.createFarmingParticle(actionType, spawnPoint.position, fadeSpeed, 4);
               break;
            case Weapon.ActionType.WaterCrop:
               ExplosionManager.createFarmingParticle(actionType, spawnPoint.position, fadeSpeed, 2, false, 30, 60);
               break;
         }
      }
   }

   public void updateTriggerDirection (Direction direction) {
      // Check if the user is stationary (the interact animation will have been triggered in PlayerBodyEntity)
      AnimatorClipInfo[] m_AnimatorClipInfo = animator.GetCurrentAnimatorClipInfo(0);
      float currentAngle = 0;
      if (m_AnimatorClipInfo[0].clip.name.ToLower().Contains("interact")) {
         // Handle the angle of the cone shaped collider
         cropSpawnEffectDirection = Direction.North;
         if (direction == Direction.East || direction == Direction.SouthEast || direction == Direction.NorthEast) {
            currentAngle = -90;
            cropSpawnEffectDirection = Direction.East;
         } else if (direction == Direction.West || direction == Direction.SouthWest || direction == Direction.NorthWest) {
            currentAngle = 90;
            cropSpawnEffectDirection = Direction.West;
         } else if (direction == Direction.South) {
            currentAngle = 180;
            cropSpawnEffectDirection = Direction.South;
         }

         transform.localEulerAngles = new Vector3(0, 0, currentAngle);
      } else {
         // When moving around, the farming direction is in front of the character
         cropSpawnEffectDirection = direction;
         switch (direction) {
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

   // Buffer used to check for close trees
   private RaycastHit2D[] _treeCheckBuffer = new RaycastHit2D[10];

   #endregion
}