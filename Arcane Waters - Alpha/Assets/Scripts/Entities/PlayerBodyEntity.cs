using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System.Threading;
using UnityEngine.EventSystems;
using TMPro;

public class PlayerBodyEntity : BodyEntity, IPointerEnterHandler, IPointerExitHandler
{
   #region Public Variables

   // The farming trigger component used for detecting crops
   public FarmingTrigger farmingTrigger;

   // The mining trigger component used for interacting with ores
   public MiningTrigger miningTrigger;

   // Max collision check around the player
   public static int MAX_COLLISION_COUNT = 32;

   // Color indications if the fuel is usable or not
   public Color recoveringColor, defaultColor;

   // Script handling the battle initializer
   public PlayerBattleCollider playerBattleCollider;

   // The animators handling the dash vfx
   public Animator[] dashAnimators;

   // If the player is near an enemy
   public bool isWithinEnemyRadius;

   // The reference to the sprite animators
   public Animator[] animators;

   // The speedup value of the animation
   public static float ANIM_SPEEDUP_VALUE = 1.5f;

   // Strength of the jump
   public float jumpUpMagnitude = 1.2f;

   // Max height of the jump
   public float jumpHeightMax = .15f;

   // Reference to the transform of the sprite holder
   public Transform spritesTransform;

   // Reference to the wind vfx during dash
   public Transform windDashSprite;

   // The offset of the wind vfx
   public const float windDashZOffset = 0.002f;

   // The pivot of the jump colliders
   public Transform jumpCollisionPivot;

   // The jump end collider that indicates if the user can jump over an object
   public CircleCollider2D jumpEndCollider;

   // The collider that determines if the player is infront of an obstacle
   public CircleCollider2D obstacleDetectionCollider;

   // Size of the obstacle collider
   public float obstacleColliderScale = .15f;

   // Size of the jump end collider
   public float jumpEndColliderScale = .15f;

   // If the user is jumping over an obstacle
   public bool isJumpingOver;

   // The interpolation speed value
   public float lerpValue;

   // The speed of the interpolation
   public float lerpSpeedValue = 1.4f;

   // The target world location when jumping over an obstacle
   public Vector2 jumpOverWorldLocation, jumpOverSourceLocation;

   // The dust particle when sprinting
   public ParticleSystem dustTrailParticleObj;

   // How long the jump takes to execute
   public const float JUMP_DURATION = 0.5f;

   // How long the player has to wait after completing a jump, before they can jump again
   public const float JUMP_COOLDOWN = 0.1f;

   // Sprinting animator parameter name
   public const string IS_SPRINTING = "isSprinting";

   // If the user can engage in combat
   public bool canEngageInCombat = true;

   #endregion

   protected override void Awake () {
      base.Awake();
   }

   protected override void Start () {
      base.Start();

      // Disable our collider if we are not the local player
      if (!isLocalPlayer) {
         getMainCollider().isTrigger = true;
      }

      // If we are the local player, assign us the audio listener
      if (isLocalPlayer && AudioListenerManager.self) {
         _audioListener = GetComponent<AudioListener>();
         AudioListenerManager.self.setActiveListener(_audioListener);
      }
   }
   
   private void OnDisable () {
      // If we are the local player, activate the camera's audio listener
      if (isLocalPlayer) {
         if (!_audioListener) {
            _audioListener = GetComponent<AudioListener>();
         }

         AudioListenerManager.self.setActiveListener(Camera.main.GetComponent<AudioListener>());
      }
   }

   protected override void FixedUpdate () {
      // Blocks user input and user movement if an enemy is within player collider radius
      if (!isWithinEnemyRadius) {
         base.FixedUpdate();
      }
   }

   protected override void Update () {
      base.Update();

      // Any time out sprite changes, we need to regenerate our outline
      _outline.recreateOutlineIfVisible();

      updateJumpHeight();

      if (!isLocalPlayer || !Util.isGeneralInputAllowed()) {
         if (getVelocity().magnitude > NETWORK_PLAYER_SPEEDUP_MAGNITUDE) {
            foreach (Animator animator in dashAnimators) {
               animator.SetBool(IS_SPRINTING, true);
            }
         } else {
            foreach (Animator animator in dashAnimators) {
               animator.SetBool(IS_SPRINTING, false);
            }
         }
         sprintRecovery();
         processJumpLogic();

         return;
      }

      if (!isInBattle()) {
         handleShortcutsInput();
      }

      processJumpLogic();
      processActionLogic();
      processSprintLogic();
   }

   public void OnPointerEnter (PointerEventData pointerEventData) {
      if (entityNameGO.GetComponent<TextMeshProUGUI>().text != null) {
         showEntityName();
      }
      if (guildIcon != null) {
         showGuildIcon();
      }
   }

   public void OnPointerExit (PointerEventData pointerEventData) {
      if ((guildIcon != null) && (!OptionsPanel.allGuildIconsShowing)) {
         GetComponent<PlayerBodyEntity>().hideGuildIcon();
      }
      hideEntityName();
   }

   private void handleShortcutsInput () {
      // We should move these to the InputManager
      if (Input.GetKeyDown(KeyCode.Alpha1)) {
         PanelManager.self.itemShortcutPanel.activateShortcut(1);
      } else if (Input.GetKeyDown(KeyCode.Alpha2)) {
         PanelManager.self.itemShortcutPanel.activateShortcut(2);
      } else if (Input.GetKeyDown(KeyCode.Alpha3)) {
         PanelManager.self.itemShortcutPanel.activateShortcut(3);
      } else if (Input.GetKeyDown(KeyCode.Alpha4)) {
         PanelManager.self.itemShortcutPanel.activateShortcut(4);
      } else if (Input.GetKeyDown(KeyCode.Alpha5)) {
         PanelManager.self.itemShortcutPanel.activateShortcut(5);
      }
   }

   private void updateJumpHeight () {
      float y = spritesTransform.localPosition.y;

      // Updates the current jump height
      if (isJumping()) {
         float timeSinceJumpStart = (float) (NetworkTime.time - _jumpStartTime);
         y = Util.getPointOnParabola(jumpUpMagnitude, JUMP_DURATION, timeSinceJumpStart);
         y = Mathf.Clamp(y, 0.0f, float.MaxValue);

         float newShadowScale = 1.0f - (y / jumpUpMagnitude) / 2.0f;
         shadow.transform.localScale = _shadowInitialScale * newShadowScale;
      }
      else {
         y = 0.0f;
      }

      spritesTransform.localPosition = new Vector3(spritesTransform.localPosition.x, y, spritesTransform.localPosition.z);
      windDashSprite.localPosition = Vector3.up * y;
   }

   private void processJumpLogic () {
      // Blocks movement when unit is jumping over an obstacle
      if (isJumpingOver) {
         // Interpolates from source location to target location
         lerpValue += Time.deltaTime * lerpSpeedValue;
         Vector3 newInterpValue = Vector3.Lerp(jumpOverSourceLocation, jumpOverWorldLocation, lerpValue);
         transform.position = new Vector3(newInterpValue.x, newInterpValue.y, transform.position.z);

         if (lerpValue < .5f) {
            float newHeight = Mathf.Lerp(0, jumpHeightMax, lerpValue * 2);
            spritesTransform.localPosition = new Vector3(0, newHeight, 0);
         } else {
            float newHeight = Mathf.Lerp(jumpHeightMax, 0, lerpValue * 2);
            spritesTransform.localPosition = new Vector3(0, newHeight, 0);
         }

         // Stops the jumping and triggers a smoke effect with sound
         if (lerpValue >= 1) {
            Instantiate(PrefabsManager.self.requestCannonSmokePrefab(Attack.ImpactMagnitude.None), this.sortPoint.transform.position, Quaternion.identity);
            SoundManager.create3dSound("ledge", this.sortPoint.transform.position);
            isJumpingOver = false;
         }
         return;
      }

      if (InputManager.isJumpKeyPressed() && !isJumpCoolingDown() && !this.waterChecker.inWater() && isLocalPlayer) {
         // Adjust the collider pivot
         int currentAngle = 0;
         if (facing == Direction.East || facing == Direction.SouthEast || facing == Direction.NorthEast) {
            currentAngle = -90;
         } else if (facing == Direction.West || facing == Direction.SouthWest || facing == Direction.NorthWest) {
            currentAngle = 90;
         } else if (facing == Direction.South) {
            currentAngle = 180;
         }
         jumpCollisionPivot.localEulerAngles = new Vector3(0, 0, currentAngle);

         // TODO: Look for jumping over obstacle alternatives / confirm if this will be part of the target feature
         /*
         // Gathers all the collider data
         Collider2D[] obstacleCollidedEntries = Physics2D.OverlapCircleAll(obstacleDetectionCollider.transform.position, obstacleColliderScale);
         Collider2D[] jumpEndCollidedEntries = Physics2D.OverlapCircleAll(jumpEndCollider.transform.position, jumpEndColliderScale);
         */

         // Jump straight away locally
         if (isLocalPlayer) {
            _jumpStartTime = NetworkTime.time;
            playJumpAnimation();
         }

         Cmd_NoteJump();
      }
   }

   private void playJumpAnimation () {
      if (facing == Direction.North) {
         requestAnimationPlay(Anim.Type.NC_Jump_North);
      }
      else if (facing == Direction.South) {
         requestAnimationPlay(Anim.Type.NC_Jump_South);
      } else {
         requestAnimationPlay(Anim.Type.NC_Jump_East);
      }
   }

   [Command]
   private void Cmd_NoteJump () {
      Rpc_NoteJump();
   }

   [ClientRpc]
   private void Rpc_NoteJump () {
      if (!isLocalPlayer) {
         _jumpStartTime = NetworkTime.time;
         playJumpAnimation();
      }
   }

   [TargetRpc]
   public void Target_EndPlayerCombat () {
      if (isLocalPlayer) {
         canEngageInCombat = true;
      }
   }

   private void sprintRecovery () {
      if (!canSprint()) {
         // Only notify other clients once if disabling
         if (isSpeedingUp) {
            isSpeedingUp = false;
         }

         setDustParticles(false);
      }
   }

   private void processSprintLogic () {
      // Sets animators speed to default
      foreach (Animator animator in dashAnimators) {
         animator.speed = 1;
      }
      foreach (Animator animator in animators) {
         animator.speed = 1;
      }

      // Speed sprint boost feature
      if (canSprint()) {
         isSpeedingUp = true;
         if (!waterChecker.inWater()) {
            setDustParticles(true);
            foreach (Animator dashAnimator in dashAnimators) {
               dashAnimator.SetBool(IS_SPRINTING, true);
            }
         } else {
            isSpeedingUp = false;
            foreach (Animator dashAnimator in dashAnimators) {
               dashAnimator.SetBool(IS_SPRINTING, false);
            }
         }
      } else {
         foreach (Animator dashAnimator in dashAnimators) {
            dashAnimator.SetBool(IS_SPRINTING, false);
         }
         sprintRecovery();
      }
   }

   private bool canSprint () {
      return ((Global.sprintConstantly || Input.GetKey(KeyCode.LeftShift)) && !isWithinEnemyRadius && getVelocity().magnitude > MOVING_MAGNITUDE);
   }

   private void processActionLogic () {
      if (MapCustomization.MapCustomizationManager.isCustomizing) {
         return;
      }

      if (InputManager.isLeftClickKeyPressed() && !PanelManager.self.hasPanelInLinkedList()) {
         NetEntity body = getClickedBody();
         if (body != null && body is PlayerBodyEntity) {
            PanelManager.self.contextMenuPanel.showDefaultMenuForUser(body.userId, body.entityName);
         }
      }

      if (InputManager.isRightClickKeyPressed()) {
         bool isNearInteractables = false;
         float overlapRadius = .35f;
         Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, overlapRadius);
         List<NPC> npcsNearby = new List<NPC>();
         List<TreasureChest> treasuresNearby = new List<TreasureChest>();

         int currentCount = 0;
         if (hits.Length > 0) {
            foreach (Collider2D hit in hits) {
               if (currentCount > MAX_COLLISION_COUNT) {
                  break;
               }
               currentCount++;

               if (hit.GetComponent<NPC>() != null) {
                  npcsNearby.Add(hit.GetComponent<NPC>());
               }

               if (hit.GetComponent<TreasureChest>() != null) {
                  treasuresNearby.Add(hit.GetComponent<TreasureChest>());
               }
            }

            if (treasuresNearby.Count > 0 || npcsNearby.Count > 0) {
               // Prevent the player from playing attack animation when interacting NPC's / Enemies / Loot Bags
               isNearInteractables = true;
            }

            // Loot the nearest lootbag/treasure chest
            interactNearestLoot(treasuresNearby);

            // If there are no loots nearby, interact with nearest npc
            if (treasuresNearby.Count < 1) {
               interactNearestNpc(npcsNearby);
            }
         }

         if (!isNearInteractables && !isMoving()) {
            Direction newDirection = forceLookAt(Camera.main.ScreenToWorldPoint(Input.mousePosition));

            if (newDirection == Direction.East || newDirection == Direction.SouthEast || newDirection == Direction.NorthEast
               || newDirection == Direction.West || newDirection == Direction.SouthWest || newDirection == Direction.NorthWest) {
               rpc.Cmd_InteractAnimation(Anim.Type.Interact_East, newDirection);
            } else if (newDirection == Direction.North) {
               rpc.Cmd_InteractAnimation(Anim.Type.Interact_North, newDirection);
            } else if (newDirection == Direction.South) {
               rpc.Cmd_InteractAnimation(Anim.Type.Interact_South, newDirection);
            }
         }
      }
   }

   private Direction forceLookAt (Vector2 targetPosition) {
      // Get horizontal axis difference
      float xAxisDifference = targetPosition.x - transform.position.x;
      xAxisDifference = Mathf.Abs(xAxisDifference);

      // Get vertical axis difference
      float yAxisDifference = targetPosition.y - transform.position.y;
      yAxisDifference = Mathf.Abs(yAxisDifference);

      Direction newDirection = facing;

      // Force player to look at direction
      if (xAxisDifference > yAxisDifference) {
         if (targetPosition.x > transform.position.x && facing != Direction.East) {
            newDirection = Direction.East;
         } else if (targetPosition.x < transform.position.x && facing != Direction.West) {
            newDirection = Direction.West;
         }
      } else {
         if (targetPosition.y < transform.position.y && facing != Direction.South) {
            newDirection = Direction.South;
         } else if (targetPosition.y > transform.position.y && facing != Direction.North) {
            newDirection = Direction.North;
         }
      }

      return newDirection;
   }

   private void setDustParticles (bool isActive) {
      dustTrailParticleObj.enableEmission = isActive;
   }

   private void jumpOver (Collider2D[] obstacleCollidedEntries, Collider2D[] jumpEndCollidedEntries) {
      if (obstacleCollidedEntries.Length > 2 && jumpEndCollidedEntries.Length < 3) {
         Cmd_JumpOver(transform.position, jumpEndCollider.transform.position, (int) facing);
      }
   }

   public override void resetCombatInit () {
      base.resetCombatInit();
      Rpc_ResetMoveDisable();
   }

   [ClientRpc]
   public void Rpc_ResetMoveDisable () {
      isWithinEnemyRadius = false;
      playerBattleCollider.combatInitCollider.enabled = true;
   }

   [Command]
   void Cmd_JumpOver (Vector3 sourceLocation, Vector3 worldLocation, int direction) {
      Rpc_JumpOver(sourceLocation, worldLocation, direction);
   }

   [ClientRpc]
   public void Rpc_JumpOver (Vector3 sourceLocation, Vector3 worldLocation, int direction) {
      lerpValue = 0;
      jumpOverSourceLocation = new Vector2(sourceLocation.x, sourceLocation.y);
      jumpOverWorldLocation = new Vector2(worldLocation.x, worldLocation.y);
      isJumpingOver = true;
      requestAnimationPlay((Anim.Type) direction);
   }

   private void interactNearestLoot (List<TreasureChest> chestList) {
      TreasureChest targetChest = null;

      float nearestTargetDistance = 10;
      foreach (TreasureChest chest in chestList) {
         float newTargetDistance = Vector2.Distance(transform.position, chest.transform.position);
         if (newTargetDistance < nearestTargetDistance && !chest.hasBeenOpened()) {
            nearestTargetDistance = newTargetDistance;
            targetChest = chest;
         }
      }

      if (targetChest != null) {
         targetChest.sendOpenRequest();
         forceLookAt(targetChest.transform.position);
      } else {
         chestList.Clear();
      }
   }

   private void interactNearestNpc (List<NPC> npcList) {
      NPC targetNpc = null;

      float nearestTargetDistance = 10;
      foreach (NPC npc in npcList) {
         float newTargetDistance = Vector2.Distance(transform.position, npc.transform.position);
         if (newTargetDistance < nearestTargetDistance) {
            nearestTargetDistance = newTargetDistance;
            targetNpc = npc;
         }
      }

      if (targetNpc != null) {
         targetNpc.clientClickedMe();
         forceLookAt(targetNpc.sortPoint.transform.position);
      } else {
         npcList.Clear();
      }
   }

   [Command]
   public void Cmd_ToggleClothes () {
      // If they currently have clothes on, then take them off
      if (armorManager.hasArmor()) {
         // DB thread to remove the current armor
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DB_Main.setArmorId(this.userId, 0);

            // Back to Unity
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               armorManager.updateArmorSyncVars(0, 0, "");
            });
         });
      } else {
         // Look up our most recent armor in the database
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<Armor> armorList = DB_Main.getArmorForUser(this.userId);
            if (armorList.Count > 0) {
               Armor armor = armorList.Last();
               DB_Main.setArmorId(this.userId, armor.id);

               // Back to Unity
               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  armorManager.updateArmorSyncVars(armor.itemTypeId, armor.id, armor.paletteNames);
               });
            }
         });
      }
   }

   public override void setAreaParent (Area area, bool worldPositionStays) {
      this.transform.SetParent(area.userParent, worldPositionStays);
   }

   public bool isJumpCoolingDown () {
      float timeSinceLastJump = (float)(NetworkTime.time - _jumpStartTime);
      return (timeSinceLastJump < (JUMP_DURATION + JUMP_COOLDOWN));
   }

   public bool isJumping () {
      float timeSinceLastJump = (float) (NetworkTime.time - _jumpStartTime);
      return (timeSinceLastJump < JUMP_DURATION);
   }

   #region Private Variables

   // The player the mouseover occurs on
   private PlayerBodyEntity _playerBody;
   private PlayerBodyEntity _previousPlayerBody;

   // The time at which this entity started its jump
   private double _jumpStartTime = 0.0f;

   // A reference to the audiolistener attached to the player
   private AudioListener _audioListener;

   #endregion
}
