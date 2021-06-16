using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System.Threading;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.InputSystem;

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

   // A reference to the transform containing the wind particle effects
   public Transform windEffectsParent;

   // Reference to the canvas containing the player's health and name
   public Transform nameAndHealthUI;

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

   // How long the jump takes to execute
   public const float JUMP_DURATION = 0.5f;

   // How long the player has to wait after completing a jump, before they can jump again
   public const float JUMP_COOLDOWN = 0.1f;

   // Sprinting animator parameter name
   public const string IS_SPRINTING = "isSprinting";

   // If players can one shot land enemies on combat
   public bool oneShotEnemies = false;

   // A reference to the spider web trigger we are inside, if any
   [HideInInspector]
   public SpiderWebTrigger collidingSpiderWebTrigger = null;

   // A reference to the sprint dust particle system
   public ParticleSystem sprintDustParticles;

   // A reference to the sprint wind particle systems for going vertically or horizontally
   public ParticleSystem sprintWindParticlesHorizontal, sprintWindParticlesVertical;

   // The cached battle stance player pref key
   public const string CACHED_STANCE_PREF = "CACHE_STANCE";

   // The width of the outline around the local player's name
   [Range(0.0f, 1.0f)]
   public float playerNameOutlineWidth = 0.35f;

   // The default color of a player's name
   public Color playerNameColor = Color.white;

   // The default color of the outline around a player's name
   public Color playerNameOutlineColor = Color.black;

   // The color of the local player's name
   public Color localPlayerNameColor = Color.white;

   // The color of the outline around the local player's name
   public Color localPlayerNameOutlineColor = Color.blue;

   // A transform that will follow the player as they jump, as will child objects of it
   public Transform followJumpHeight;

   // Reference to the Player's Level Tag
   public PlayerLevelTag levelTag;

   #endregion

   protected override void Awake () {
      base.Awake();

      _sprintDustParticlesMain = sprintDustParticles.main;
      _sprintDustParticlesEmission = sprintDustParticles.emission;
      _sprintWindParticlesEmissionHorizontal = sprintWindParticlesHorizontal.emission;
      _sprintWindParticlesEmissionVertical = sprintWindParticlesVertical.emission;
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
         AudioListenerManager.self.setActiveFmodListener(null);
      }

      // Retrieve the current sprites for the guild icon
      updateGuildIconSprites();

      // On start, check if we need to hide the names and guild icons
      if (OptionsPanel.onlyShowGuildIconsOnMouseover) {
         this.hideGuildIcon();
      }

      // On start, hide entity name for land players unless the toggle is set in Options Panel
      if (OptionsPanel.onlyShowPlayerNamesOnMouseover) {
         this.hideEntityName();
      }

      if (isLocalPlayer && !Util.isBatch()) {
         // Gamepad action trigger
         InputManager.self.inputMaster.Player.Interact.performed += func => triggerInteractAction(false);

         // Gamepad jump trigger
         InputManager.self.inputMaster.Player.Jump.performed += func => {
            if (InputManager.isActionInputEnabled()) {
               triggerJumpAction();
            }
         };

         PanelManager.self.hidePowerupPanel();
      }

      // Show the local player's level tag
      showLevelTag(isLocalPlayer);
   }

   public void showLevelTag (bool show) {
      if (levelTag != null) {
         if (show) {
            int level = LevelUtil.levelForXp(this.XP);
            levelTag.setLevel(level.ToString());
         }
         levelTag.toggle(show);
      }
   }

   public override PlayerBodyEntity getPlayerBodyEntity () {
      return this;
   }

   private void OnDisable () {
      // If we are the local player, activate the camera's audio listener
      if (isLocalPlayer && !ClientManager.isApplicationQuitting) {
         if (!_audioListener) {
            _audioListener = GetComponent<AudioListener>();
         }

         if (CameraManager.defaultCamera != null && CameraManager.defaultCamera.getAudioListener() != null) {
            AudioListenerManager.self.setActiveListener(CameraManager.defaultCamera.getAudioListener());
            AudioListenerManager.self.setActiveFmodListener(CameraManager.defaultCamera.getFmodListener());
         } else {
            D.error("Couldn't switch audio listener back to main camera");
         }
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

      updateJumpHeight();
      updateSprintParticles();

      if (!isLocalPlayer || !Util.isGeneralInputAllowed()) {
         webBounceUpdate();
         processJumpLogic();

         return;
      }

      checkAudioListener();

      if (!isInBattle()) {
         handleShortcutsInput();
      }

      webBounceUpdate();
      processJumpLogic();
      processActionLogic();

      // Display land players' name when ALT key is pressed.
      if (KeyUtils.GetKey(Key.LeftAlt) || KeyUtils.GetKey(Key.RightAlt)) {
         if (OptionsPanel.onlyShowPlayerNamesOnMouseover) {
            showEntityName();
         }
         if (OptionsPanel.onlyShowGuildIconsOnMouseover) {
            showGuildIcon();
         }
      }
      if (KeyUtils.GetKeyUp(Key.LeftAlt) || KeyUtils.GetKeyUp(Key.RightAlt)) {
         if (OptionsPanel.onlyShowPlayerNamesOnMouseover) {
            hideEntityName();
         }
         if (OptionsPanel.onlyShowGuildIconsOnMouseover) {
            hideGuildIcon();
         }
      }
   }

   public void OnPointerEnter (PointerEventData pointerEventData) {
      showEntityName();

      if (guildIcon != null) {
         updateGuildIconSprites();
         showGuildIcon();
      }

      showLevelTag(true);
   }

   public void OnPointerExit (PointerEventData pointerEventData) {
      if ((guildIcon != null) && OptionsPanel.onlyShowGuildIconsOnMouseover) {
         GetComponent<PlayerBodyEntity>().hideGuildIcon();
      }
      if ((entityName != null) && (OptionsPanel.onlyShowPlayerNamesOnMouseover)) {
         hideEntityName();
      }

      showLevelTag(isLocalPlayer);
   }

   private void handleShortcutsInput () {
      // We should move these to the InputManager
      if (KeyUtils.GetKeyDown(Key.Digit1)) {
         PanelManager.self.itemShortcutPanel.activateShortcut(1);
      } else if (KeyUtils.GetKeyDown(Key.Digit2)) {
         PanelManager.self.itemShortcutPanel.activateShortcut(2);
      } else if (KeyUtils.GetKeyDown(Key.Digit3)) {
         PanelManager.self.itemShortcutPanel.activateShortcut(3);
      } else if (KeyUtils.GetKeyDown(Key.Digit4)) {
         PanelManager.self.itemShortcutPanel.activateShortcut(4);
      } else if (KeyUtils.GetKeyDown(Key.Digit5)) {
         PanelManager.self.itemShortcutPanel.activateShortcut(5);
      }
   }

   public void setDropShadowScale (float newScale) {
      shadow.transform.localScale = _shadowInitialScale * newScale;
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
      } else if (!isBouncingOnWeb()) {
         y = 0.0f;
      }

      setSpritesHeight(y);
      Util.setLocalY(windEffectsParent, y);
      Util.setLocalY(nameAndHealthUI, y);
      Util.setLocalY(followJumpHeight, y);
   }

   private void updateSprintParticles () {
      bool isSprinting;

      if (isLocalPlayer) {
         isSprinting = (canSprint() && !_waterChecker.inWater());
      } else {
         isSprinting = isSpeedingUp;
      }

      if (isSprinting) {
         // Disable dust particles if we are jumping
         if (isJumping()) {
            _sprintWindParticlesEmissionVertical.rateOverDistance = 0.0f;
            _sprintWindParticlesEmissionHorizontal.rateOverDistance = 0.0f;
         } else {
            // Alter dust particles based on the direction we are facing
            float particleXScale = 1.0f;
            float particleStartRotation = 0.0f;

            if (facing == Direction.East || facing == Direction.South) {
               particleXScale = -1.0f;
            }

            if (facing == Direction.North || facing == Direction.South) {
               particleStartRotation = 90.0f;
            }

            _sprintDustParticlesMain.startRotation = particleStartRotation;
            sprintDustParticles.transform.localScale = new Vector3(particleXScale, 1.0f, 1.0f);
            _sprintDustParticlesEmission.rateOverDistance = SPRINT_DUST_PARTICLES_RATE_OVER_DISTANCE;
         }

         // Activate the vertical or horizontal wind effects based on which way we are facing
         bool facingVertical = (facing == Direction.North || facing == Direction.South);

         _sprintWindParticlesEmissionVertical.rateOverDistance = facingVertical ? SPRINT_WIND_PARTICLES_RATE_OVER_DISTANCE : 0.0f;
         _sprintWindParticlesEmissionHorizontal.rateOverDistance = facingVertical ? 0.0f : SPRINT_WIND_PARTICLES_RATE_OVER_DISTANCE;

      } else {
         _sprintDustParticlesEmission.rateOverDistance = 0.0f;
         _sprintWindParticlesEmissionHorizontal.rateOverDistance = 0.0f;
         _sprintWindParticlesEmissionVertical.rateOverDistance = 0.0f;
      }

      if (isLocalPlayer) {
         if (isSpeedingUp != isSprinting) {
            Cmd_SetSpeedingUp(isSprinting);
         }
         isSpeedingUp = isSprinting;
      }
   }

   [Command]
   private void Cmd_SetSpeedingUp (bool isSpeedingUp) {
      Rpc_SetSpeedingUp(isSpeedingUp);
   }

   [ClientRpc]
   private void Rpc_SetSpeedingUp (bool isSpeedingUp) {
      if (!isLocalPlayer) {
         this.isSpeedingUp = isSpeedingUp;
      }
   }

   public void setSpritesHeight (float newHeight) {
      spritesTransform.localPosition = new Vector3(spritesTransform.localPosition.x, newHeight, spritesTransform.localPosition.z);
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
            Instantiate(PrefabsManager.self.poofPrefab, this.sortPoint.transform.position, Quaternion.identity);
            SoundManager.create3dSound("ledge", this.sortPoint.transform.position);
            isJumpingOver = false;
         }
         return;
      }

      // Can't jump while interacting
      if (InputManager.isJumpKeyPressed() && !interactingAnimation) {
         triggerJumpAction();
      }
   }

   private void triggerJumpAction () {
      if (!isJumpCoolingDown() && isLocalPlayer && !isBouncingOnWeb() && !_isClimbing) {
         // If we are in a spider web  trigger, and facing the right way, jump onto the spider web
         if (collidingSpiderWebTrigger != null && collidingSpiderWebTrigger.isFacingWeb(facing)) {
            collidingSpiderWebTrigger.onPlayerJumped(this);
            return;
         }

         if (this.waterChecker.inWater()) {
            return;
         }

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
      } else if (facing == Direction.South) {
         requestAnimationPlay(Anim.Type.NC_Jump_South);
      } else {
         requestAnimationPlay(Anim.Type.NC_Jump_East);
      }
   }

   [Command]
   public void Cmd_NoteJump () {
      Rpc_NoteJump();
   }

   [ClientRpc]
   private void Rpc_NoteJump () {
      if (!isLocalPlayer) {
         _jumpStartTime = NetworkTime.time;
         playJumpAnimation();
      }
   }

   private bool canSprint () {
      return ((Global.sprintConstantly || InputManager.isSpeedUpKeyDown()) && !isWithinEnemyRadius && getVelocity().magnitude > SPRINTING_MAGNITUDE);
   }

   private void processActionLogic () {
      if (MapCustomization.MapCustomizationManager.isCustomizing) {
         return;
      }

      if (PriorityOverProcessActionLogic.isAnyHovered()) {
         return;
      }

      if (InputManager.isLeftClickKeyPressed() && !PanelManager.self.hasPanelInLinkedList() && !PanelManager.self.isFullScreenSeparatePanelShowing()) {
         NetEntity body = getClickedBody();
         if (body != null && body is PlayerBodyEntity) {
            PanelManager.self.contextMenuPanel.showDefaultMenuForUser(body.userId, body.entityName);
         }
      }

      if (InputManager.isRightClickKeyPressed()) {
         triggerInteractAction();
      }

      // Try to open chest through code (instead of UI) in case if UI is blocking raycasts casted to the chest Canvas
      if (InputManager.isLeftClickKeyPressed()) {
         tryToOpenChest();
      }
   }

   private void triggerInteractAction (bool faceMouseDirection = true) {
      // First check under the mouse
      bool foundInteractable = tryInteractUnderMouse();

      // If no interactables under the mouse, check in a radius
      if (!foundInteractable) {
         foundInteractable = tryInteractNearby();
      }

      // Don't allow the player to enter the interact animation if they're jumping
      if (!foundInteractable && !isJumping()) {
         tryInteractAnimation(faceMouseDirection);
      }
   }

   private bool tryInteractUnderMouse () {
      Collider2D[] hits = Physics2D.OverlapPointAll(Util.getMousePos());
      int collisionCount = 0;

      foreach (Collider2D hit in hits) {
         if (collisionCount > MAX_COLLISION_COUNT) {
            break;
         }
         collisionCount++;

         Collider2D[] colliders = hit.GetComponents<Collider2D>();
         bool hitNonTrigger = false;
         foreach (Collider2D collider in colliders) {
            // If a non-trigger collider contains the mouse
            float distToMouse = ((Vector2) (collider.transform.position - Util.getMousePos())).magnitude;
            if (!collider.isTrigger && distToMouse < collider.bounds.size.x) {
               hitNonTrigger = true;
            }
         }

         if (!hitNonTrigger) {
            continue;
         }

         // If we clicked on a chest, interact with it
         TreasureChest chest = hit.GetComponent<TreasureChest>();
         if (chest && !chest.hasBeenOpened() && chest.instanceId == instanceId) {
            chest.sendOpenRequest();
            forceLookAt(chest.transform.position);
            return true;
         }

         // If we clicked on an NPC, interact with them
         NPC npc = hit.GetComponent<NPC>();
         if (npc) {
            npc.clientClickedMe();
            forceLookAt(npc.sortPoint.transform.position);
            return true;
         }

         // If we clicked on some ore, interact with it
         OreNode oreNode = hit.GetComponent<OreNode>();
         if (oreNode) {
            tryInteractAnimation();
            return true;
         }

         // If we clicked on a crop, interact with it
         CropSpot cropSpot = hit.GetComponent<CropSpot>();
         if (cropSpot) {
            tryInteractAnimation();
            return true;
         }
      }

      return false;
   }

   private bool tryInteractNearby () {
      bool foundInteractable = false;
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

            TreasureChest chest = hit.GetComponent<TreasureChest>();
            if (chest && !chest.hasBeenOpened() && chest.instanceId == instanceId) {
               treasuresNearby.Add(hit.GetComponent<TreasureChest>());
            }
         }

         if (treasuresNearby.Count > 0 || npcsNearby.Count > 0) {
            // Prevent the player from playing attack animation when interacting NPC's / Enemies / Loot Bags
            foundInteractable = true;
         }

         // Loot the nearest lootbag/treasure chest
         interactNearestLoot(treasuresNearby);

         // If there are no loots nearby, interact with nearest npc
         if (treasuresNearby.Count < 1) {
            interactNearestNpc(npcsNearby);
         }
      }

      return foundInteractable;
   }

   private void tryInteractAnimation (bool faceMouseDirection = true) {
      Direction newDirection = forceLookAt(Camera.main.ScreenToWorldPoint(MouseUtils.mousePosition));

      if (newDirection == Direction.East || newDirection == Direction.SouthEast || newDirection == Direction.NorthEast
         || newDirection == Direction.West || newDirection == Direction.SouthWest || newDirection == Direction.NorthWest) {
         rpc.Cmd_InteractAnimation(Anim.Type.Interact_East, newDirection);
      } else if (newDirection == Direction.North) {
         rpc.Cmd_InteractAnimation(Anim.Type.Interact_North, newDirection);
      } else if (newDirection == Direction.South) {
         rpc.Cmd_InteractAnimation(Anim.Type.Interact_South, newDirection);
      }
   }

   public void playInteractParticles () {
      Weapon.ActionType currentActionType = weaponManager.actionType;
      farmingTrigger.updateTriggerDirection();
      farmingTrigger.playFarmingParticles(currentActionType);
   }

   public void playFastInteractAnimation (Vector3 lookTarget, bool playLocalInstantly) {
      Direction newDirection = forceLookAt(lookTarget);

      Anim.Type animToPlay;

      if (newDirection == Direction.North) {
         animToPlay = Anim.Type.Fast_Interact_North;
      } else if (newDirection == Direction.South) {
         animToPlay = Anim.Type.Fast_Interact_South;
      } else {
         animToPlay = Anim.Type.Fast_Interact_East;
      }

      rpc.Cmd_FastInteractAnimation(animToPlay, newDirection, playLocalInstantly);

      if (playLocalInstantly) {
         requestAnimationPlay(animToPlay);
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

   private void jumpOver (Collider2D[] obstacleCollidedEntries, Collider2D[] jumpEndCollidedEntries) {
      if (obstacleCollidedEntries.Length > 2 && jumpEndCollidedEntries.Length < 3) {
         Cmd_JumpOver(transform.position, jumpEndCollider.transform.position, (int) facing);
      }
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
               armorManager.updateArmorSyncVars(0, 0, "", 0);
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
                  armorManager.updateArmorSyncVars(armor.itemTypeId, armor.id, armor.paletteNames, armor.durability);
               });
            }
         });
      }
   }

   public override void setAreaParent (Area area, bool worldPositionStays) {
      this.transform.SetParent(area.userParent, worldPositionStays);
   }

   public bool isJumpCoolingDown () {
      float timeSinceLastJump = (float) (NetworkTime.time - _jumpStartTime);
      return (timeSinceLastJump < (JUMP_DURATION + JUMP_COOLDOWN));
   }

   public bool isJumping () {
      float timeSinceLastJump = (float) (NetworkTime.time - _jumpStartTime);
      return (timeSinceLastJump < JUMP_DURATION);
   }

   protected override void webBounceUpdate () {
      if (!isBouncingOnWeb()) {
         return;
      }

      float timeSinceBounce = (float) NetworkTime.time - _webBounceStartTime;

      // If we're falling down, reverse t
      if (!_isGoingUpWeb) {
         timeSinceBounce = getWebBounceDuration() - timeSinceBounce;
      }

      // Update sprite height
      float tSprites = _activeWeb.getSpriteHeightCurve(_isDoingHalfBounce).Evaluate(timeSinceBounce);
      setSpritesHeight(tSprites);

      // Update drop shadow scale
      float tShadow = _activeWeb.getShadowCurve(_isDoingHalfBounce).Evaluate(timeSinceBounce);
      setDropShadowScale(tShadow);
   }

   private void checkAudioListener () {
      // If the player is in battle
      if (CameraManager.battleCamera.getCamera().isActiveAndEnabled) {
         // If the listener hasn't been switched, switch it
         if (CameraManager.battleCamera.getAudioListener() != AudioListenerManager.self.getActiveListener()) {
            AudioListenerManager.self.setActiveListener(CameraManager.battleCamera.getAudioListener());
            AudioListenerManager.self.setActiveFmodListener(CameraManager.battleCamera.getFmodListener());
         }
         // If the player isn't in battle
      } else if (_audioListener != null) {
         // Make sure the game is using the player's audio listener
         if (_audioListener != AudioListenerManager.self.getActiveListener()) {
            AudioListenerManager.self.setActiveListener(_audioListener);
            AudioListenerManager.self.setActiveFmodListener(null);
         }
      } else if (CameraManager.defaultCamera != null && CameraManager.defaultCamera.getAudioListener() != null) {
         AudioListenerManager.self.setActiveListener(CameraManager.defaultCamera.getAudioListener());
         AudioListenerManager.self.setActiveFmodListener(CameraManager.defaultCamera.getFmodListener());
      } else {
         D.error("Couldn't find an audio listener to assign");
      }
   }

   public override void recolorNameText () {
      nameText.fontMaterial = new Material(nameText.fontSharedMaterial);

      nameTextOutline.enabled = true;
      nameTextOutline.fontMaterial = new Material(nameTextOutline.fontSharedMaterial);
      nameTextOutline.fontMaterial.SetFloat("_OutlineWidth", playerNameOutlineWidth);
      nameTextOutline.text = this.entityName;

      if (isLocalPlayer) {
         nameText.fontMaterial.SetColor("_FaceColor", localPlayerNameColor);
         nameTextOutline.fontMaterial.SetColor("_FaceColor", localPlayerNameColor);
         nameTextOutline.fontMaterial.SetColor("_OutlineColor", localPlayerNameOutlineColor);
      } else {
         nameText.fontMaterial.SetColor("_FaceColor", playerNameColor);
         nameTextOutline.fontMaterial.SetColor("_FaceColor", playerNameColor);
         nameTextOutline.fontMaterial.SetColor("_OutlineColor", playerNameOutlineColor);
      }
   }

   protected override void autoMove () {
      if (Global.player == null || !isLocalPlayer || isInBattle()) {
         return;
      }

      if (!Util.isGeneralInputAllowed()) {
         // Try to close any opened panel
         PanelManager.self.onEscapeKeyPressed();

         return;
      }

      // Choose an action
      int action = Random.Range(0, 7);

      switch (action) {
         case 0:
            InputManager.self.simulateDirectionPress(Direction.East, Random.Range(0.5f, AUTO_MOVE_ACTION_DURATION));
            break;
         case 1:
            InputManager.self.simulateDirectionPress(Direction.West, Random.Range(0.5f, AUTO_MOVE_ACTION_DURATION));
            break;
         case 2:
            InputManager.self.simulateDirectionPress(Direction.North, Random.Range(0.5f, AUTO_MOVE_ACTION_DURATION));
            break;
         case 3:
            InputManager.self.simulateDirectionPress(Direction.South, Random.Range(0.5f, AUTO_MOVE_ACTION_DURATION));
            break;
         case 4:
            triggerJumpAction();
            break;
         case 5:
            PanelManager.self.itemShortcutPanel.activateShortcut(Random.Range(1, 6));
            break;
         case 6:
            triggerInteractAction();
            break;
         default:
            break;
      }
   }

   #region Private Variables

   // The player the mouseover occurs on
   private PlayerBodyEntity _playerBody;
   private PlayerBodyEntity _previousPlayerBody;

   // The time at which this entity started its jump
   private double _jumpStartTime = 0.0f;

   // A reference to the audiolistener attached to the player
   private AudioListener _audioListener;

   // The 'startSizeMultiplier' of the dust trail particle
   private float _dustStartSizeMultiplier;

   // A reference to the main module of the sprint dust particle system
   private ParticleSystem.MainModule _sprintDustParticlesMain;

   // A reference to the emission module of the dust particle system
   private ParticleSystem.EmissionModule _sprintDustParticlesEmission;

   // References to the emission modules of the sprint wind particle systems
   private ParticleSystem.EmissionModule _sprintWindParticlesEmissionVertical, _sprintWindParticlesEmissionHorizontal;

   // The rate over distance variable for the sprint dust particles
   private const float SPRINT_DUST_PARTICLES_RATE_OVER_DISTANCE = 1.0f;

   // The rate over distance variable for the sprint wind particles
   private const float SPRINT_WIND_PARTICLES_RATE_OVER_DISTANCE = 2.0f;

   #endregion
}
