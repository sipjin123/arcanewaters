using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

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

   // Reference to the Level Up Effect
   public LevelUpEffect levelUpEffect;

   // Whether the collision with effectors is currently enabled for this player
   public bool stairEffectorCollisionEnabled = true;

   [Header("Emoting")]

   [SyncVar]
   // The type of the emote
   public EmoteManager.EmoteTypes emoteType = EmoteManager.EmoteTypes.None;

   [Header("Sitting")]

   [SyncVar]
   // Is the player sitting down?
   public bool isSitting = false;

   [SyncVar]
   // Stores the position where the player stood before sitting down
   public Vector3 positionBeforeSitting = Vector3.zero;

   [SyncVar]
   // The position of the chair the player is sitting on
   public Vector3 chairPosition = Vector3.zero;

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
         InputManager.self.inputMaster.Land.Action.performed += OnActionPerformed;

         // Gamepad jump trigger
         InputManager.self.inputMaster.Land.Jump.performed += OnJumpPerformed;

         PanelManager.self.showPowerupPanel();
         Cmd_CheckAmbientSfx();
      }

      // Check if an npc is above the player
      if (isLocalPlayer) {
         InvokeRepeating(nameof(npcCheck), 0.0f, 0.5f);
      }

      // Show the local player's level tag
      showLevelTag(isLocalPlayer);

      if (isServer) {
         // When we enter a new scene, update powerups on the client
         rpc.Target_UpdateLandPowerups(connectionToClient, LandPowerupManager.self.getPowerupsForUser(userId));
      }

      if (!Util.isBatch()) {
         InputManager.self.inputMaster.Land.Enable();
         InputManager.self.inputMaster.Sea.Disable();
      }
   }

   private void OnActionPerformed (InputAction.CallbackContext ctx) {
      triggerInteractAction(false);
   }

   private void OnJumpPerformed (InputAction.CallbackContext ctx) {
      if (InputManager.isActionInputEnabled()) {
         triggerJumpAction();
      }
   }

   protected override void OnDestroy () {
      base.OnDestroy();

      if (!Util.isBatch()) {
         InputManager.self.inputMaster.Land.Action.performed -= OnActionPerformed;
         InputManager.self.inputMaster.Land.Jump.performed -= OnJumpPerformed;
      }

      if (isLocalPlayer && GenericActionPromptScreen.self != null) {
         GenericActionPromptScreen.self.hide();
      }
   }

   public void npcCheck () {
      // Skip displaying guild icon based on npc distance, when guild icons are disabled in options
      if (OptionsPanel.onlyShowGuildIconsOnMouseover) {
         return;
      }

      // If an npc is above the player, hide the guild icon
      bool isNpcNear = false;
      Collider2D[] colliders = Physics2D.OverlapCircleAll(new Vector2(transform.position.x, transform.position.y + 0.5f), 0.25f);
      foreach (Collider2D collider in colliders) {
         if (collider.transform.GetComponent<NPC>()) {
            isNpcNear = true;
            this.hideGuildIcon();
         }
      }

      if (!isNpcNear) {
         this.showGuildIcon();
      }
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
            AudioListenerManager.self.setActiveListener(CameraManager.defaultCamera.getAudioListener(), CameraManager.defaultCamera.getFmodListener());
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

      processSitting();
      processEmoting();

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
      processActionLogic();
      processJumpLogic();

      // Display land players' name when ALT key is pressed.
      if (InputManager.self.inputMaster.Hud.ShowPlayerName.IsPressed()) {
         if (OptionsPanel.onlyShowPlayerNamesOnMouseover) {
            showEntityName();
         }
         if (OptionsPanel.onlyShowGuildIconsOnMouseover) {
            showGuildIcon();
         }
      }
   }

   private void processSitting () {
      if (isSitting != _prevIsSitting) {
         if (isSitting) {
            getMainCollider().isTrigger = true;
            toggleWeaponVisibility(show: false);
            applySittingEmoteMask(show: true);
            setSpritesDepth(-0.005f);

            if (facing == Direction.North) {
               setSpritesDepth(+0.005f);
               playCompositeAnimation(CompositeAnimationManager.self.KneelingN);
            } else if (facing == Direction.South) {
               playCompositeAnimation(CompositeAnimationManager.self.KneelingS);
            } else if (facing == Direction.West) {
               transform.position = chairPosition + new Vector3(-0.08f, 0, 0);
               playCompositeAnimation(CompositeAnimationManager.self.KneelingWE);
            } else if (facing == Direction.East) {
               transform.position = chairPosition + new Vector3(+0.08f, 0, 0);
               playCompositeAnimation(CompositeAnimationManager.self.KneelingWE);
            }

            if (isLocalPlayer) {
               InputManager.toggleInput(enable: false);
               GenericActionPromptScreen promptScreen = PanelManager.self.genericPromptScreen;

               if (promptScreen != null) {
                  promptScreen.text = "Get Up";
                  promptScreen.button.onClick.RemoveAllListeners();
                  promptScreen.button.onClick.AddListener(() => {
                     Cmd_ExitChair();
                  });
                  promptScreen.show();
               }
            }
         } else {
            transform.position = positionBeforeSitting;
            applySittingEmoteMask(show: false);
            toggleWeaponVisibility(show: true);
            getMainCollider().isTrigger = false;
            setSpritesDepth(0.0f);
            stopCompositeAnimation();

            if (isLocalPlayer) {
               InputManager.toggleInput(enable: true);
               GenericActionPromptScreen.self.hide();
            }
         }

         _prevIsSitting = isSitting;
      }
   }

   private void processEmoting () {
      if (emoteType != _prevEmoteType) {
         if (emoteType != EmoteManager.EmoteTypes.None) {
            shouldAlignRenderersToFacingDirection = false;
            toggleWeaponVisibility(show: false);

            // Greeting
            if (emoteType == EmoteManager.EmoteTypes.Greet) {
               if (facing == Direction.North) {
                  playCompositeAnimation(CompositeAnimationManager.self.WavingN);
               } else if (facing == Direction.South) {
                  playCompositeAnimation(CompositeAnimationManager.self.WavingS);
               } else if (facing == Direction.West || facing == Direction.East) {
                  playCompositeAnimation(CompositeAnimationManager.self.WavingWE);
               }
            }

            // Dancing
            if (emoteType == EmoteManager.EmoteTypes.Dance) {
               playCompositeAnimation(CompositeAnimationManager.self.Dancing);
            }

            // Kneeling
            if (emoteType == EmoteManager.EmoteTypes.Kneel) {
               if (facing == Direction.North) {
                  playCompositeAnimation(CompositeAnimationManager.self.KneelingN);
               } else if (facing == Direction.South) {
                  playCompositeAnimation(CompositeAnimationManager.self.KneelingS);
               } else if (facing == Direction.West || facing == Direction.East) {
                  playCompositeAnimation(CompositeAnimationManager.self.KneelingWE);
               }
            }

            if (isLocalPlayer) {
               InputManager.toggleInput(enable: false);
            }
         } else {
            shouldAlignRenderersToFacingDirection = true;
            toggleWeaponVisibility(show: true);
            stopCompositeAnimation();

            if (isLocalPlayer) {
               InputManager.toggleInput(enable: true);
            }
         }

         _prevEmoteType = emoteType;
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
      if (InputManager.self.inputMaster.Hud.Shortcut1.WasPerformedThisFrame()) {
         PanelManager.self.itemShortcutPanel.activateShortcut(1);
      } else if (InputManager.self.inputMaster.Hud.Shortcut2.WasPerformedThisFrame()) {
         PanelManager.self.itemShortcutPanel.activateShortcut(2);
      } else if (InputManager.self.inputMaster.Hud.Shortcut3.WasPerformedThisFrame()) {
         PanelManager.self.itemShortcutPanel.activateShortcut(3);
      } else if (InputManager.self.inputMaster.Hud.Shortcut4.WasPerformedThisFrame()) {
         PanelManager.self.itemShortcutPanel.activateShortcut(4);
      } else if (InputManager.self.inputMaster.Hud.Shortcut5.WasPerformedThisFrame()) {
         PanelManager.self.itemShortcutPanel.activateShortcut(5);
      }

      if (InputManager.self.inputMaster.Hud.NextShortcut.WasPerformedThisFrame()) {
         PanelManager.self.itemShortcutPanel.nextShortcut();
      } else if (InputManager.self.inputMaster.Hud.PrevShortcut.WasPerformedThisFrame()) {
         PanelManager.self.itemShortcutPanel.prevShortcut();
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

      // If the player has finished jumping, re-enable the stair effectors
      if (!isJumping() && !stairEffectorCollisionEnabled) {
         setStairEffectorCollisions(true);
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

   [Command]
   private void Cmd_CheckAmbientSfx () {
      StartCoroutine(CO_ProcessAmbientSfx());
   }

   private IEnumerator CO_ProcessAmbientSfx () {
      while (AreaManager.self.getArea(areaKey) == null) {
         yield return 0;
      }
      yield return new WaitForSeconds(1);

      string newFmodKey = "";
      if (AreaManager.self.getArea(areaKey).isInterior) {
         foreach (NPCData npcData in NPCManager.self.getNPCDataInArea(areaKey)) {
            NPC npcRef = NPCManager.self.getNPC(npcData.npcId);
            if (npcData != null) {
               switch (npcRef.shopPanelType) {
                  case Panel.Type.Shipyard:
                     newFmodKey = "Shipyard Shop Fmod Path";
                     break;
                  case Panel.Type.Adventure:
                     newFmodKey = "Weapon Shop Fmod Path";
                     break;
                  case Panel.Type.Merchant:
                     newFmodKey = "Merchant Fmod Path";
                     break;
               }
            }
         }
      }
      Target_ReceiveAmbientSfx(newFmodKey);
   }

   [TargetRpc]
   public void Target_ReceiveAmbientSfx (string fmodIdString) {
      // TODO: Play Fmod Ambience here for interior areas
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

   public bool isJumpGrounded () {
      return spritesTransform.localPosition.y < .05f;
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

      if (!Util.isBatch()) {
         if (InputManager.self.inputMaster.Land.Jump.WasPerformedThisFrame()) {
            triggerJumpAction();
         }
      }
   }

   private void triggerJumpAction () {
      // Can't jump while interacting
      if (interactingAnimation) {
         return;
      }

      if (!isJumpCoolingDown() && isLocalPlayer && !isBouncingOnWeb() && !_isClimbing) {
         // If we are in a spider web  trigger, and facing the right way, jump onto the spider web
         if (collidingSpiderWebTrigger != null) {
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
            setStairEffectorCollisions(false);
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
      setStairEffectorCollisions(false);
   }

   [ClientRpc]
   private void Rpc_NoteJump () {
      if (!isLocalPlayer) {
         _jumpStartTime = NetworkTime.time;
         playJumpAnimation();
         setStairEffectorCollisions(false);
      }
   }

   private bool canSprint () {
      if (Global.sprintConstantly) {
         return true;
      }

      return (InputManager.self.inputMaster.Land.Sprint.IsPressed() && !isWithinEnemyRadius && getVelocity().magnitude > SPRINTING_MAGNITUDE);
   }

   private void processActionLogic () {
      if (MapCustomization.MapCustomizationManager.isCustomizing) {
         return;
      }

      if (PriorityOverProcessActionLogic.isAnyHovered()) {
         return;
      }

      if (
         InputManager.self.inputMaster.General.Interact.WasPerformedThisFrame() &&
         !PanelManager.self.hasPanelInLinkedList() &&
         !PanelManager.self.isFullScreenSeparatePanelShowing()
      ) {
         NetEntity body = getClickedBody();
         if (body != null && body is PlayerBodyEntity && !Global.player.isInBattle()) {
            PanelManager.self.contextMenuPanel.showDefaultMenuForUser(body.userId, body.entityName);
         }
      }

      if (InputManager.self.inputMaster.Land.Action.WasPerformedThisFrame()) {
         triggerInteractAction();
      }

      // Try to open chest through code (instead of UI) in case if UI is blocking raycasts casted to the chest Canvas
      if (InputManager.self.inputMaster.General.Interact.WasPerformedThisFrame()) {
         tryToOpenChest();
      }
   }

   private void triggerInteractAction (bool faceMouseDirection = true) {
      if (isJumping() || !isJumpGrounded()) {
         return;
      }

      // First check under the mouse
      bool foundInteractable = tryInteractUnderMouse();

      // If no interactables under the mouse, check in a radius
      if (!foundInteractable) {
         foundInteractable = tryInteractNearby();
      }

      // Don't allow the player to enter the interact animation if they're jumping
      if (!foundInteractable && !isJumping() && !interactingAnimation) {
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
      if (isInBattle()) {
         D.debug("Cannot Interact During Battle!");
         return;
      }
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
      return (timeSinceLastJump < JUMP_DURATION || (animators.Length > 0 && animators[0] != null && animators[0].GetBool("jump")));
   }

   protected override void webBounceUpdate () {
      if (!isBouncingOnWeb()) {
         return;
      }

      float timeSinceBounce = (float) NetworkTime.time - _webBounceStartTime;

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
            AudioListenerManager.self.setActiveListener(CameraManager.battleCamera.getAudioListener(), CameraManager.battleCamera.getFmodListener());
         }
         // If the player isn't in battle
      } else if (_audioListener != null) {
         // Make sure the game is using the player's audio listener
         if (_audioListener != AudioListenerManager.self.getActiveListener()) {
            AudioListenerManager.self.setActiveListener(_audioListener);
         }
      } else if (CameraManager.defaultCamera != null && CameraManager.defaultCamera.getAudioListener() != null) {
         AudioListenerManager.self.setActiveListener(CameraManager.defaultCamera.getAudioListener(), CameraManager.defaultCamera.getFmodListener());
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

   public override void showLevelUpEffect (Jobs.Type jobType) {
      base.showLevelUpEffect(jobType);

      if (levelUpEffect != null) {
         levelUpEffect.play(jobType);
      }
   }

   private void setStairEffectorCollisions (bool collisionsEnabled) {
      if (stairEffectorCollisionEnabled == collisionsEnabled) {
         return;
      }

      GenericEffector.setEffectorCollisions(getMainCollider(), collisionsEnabled, GenericEffector.Type.Stair);
      stairEffectorCollisionEnabled = collisionsEnabled;
   }

   public void toggleWeaponVisibility (bool show) {
      foreach (WeaponLayer layer in weaponManager.weaponsLayers) {
         if (layer != null) {
            layer.getRenderer().enabled = show;
         }
      }
   }

   public void applySittingEmoteMask (bool show) {
      if (_armorLayer != null) {
         _armorLayer.toggleClipmask(show);
      }

      if (_bodyLayer != null) {
         _bodyLayer.toggleClipmask(show);
      }
   }

   public void setSpritesDepth (float offset) {
      spritesTransform.SetLocalZ(offset);
   }

   #region Composite Animations

   public void playCompositeAnimation (CompositeAnimation animation) {
      if (animation == null) {
         return;
      }

      // Pause all the normal animations
      SimpleAnimation[] simpleAnims = GetComponentsInChildren<SimpleAnimation>();
      foreach (SimpleAnimation anim in simpleAnims) {
         anim.isPaused = true;
      }

      // Pause all the normal animations
      Animator[] animators = GetComponentsInChildren<Animator>();
      foreach (Animator animator in animators) {
         animator.enabled = false;
      }

      CompositeAnimationPlayer[] compositeAnims = GetComponentsInChildren<CompositeAnimationPlayer>();
      foreach (CompositeAnimationPlayer anim in compositeAnims) {
         anim.play(animation);
      }
   }

   public void stopCompositeAnimation () {
      // Pause all the normal animations
      SimpleAnimation[] simpleAnims = GetComponentsInChildren<SimpleAnimation>();
      foreach (SimpleAnimation anim in simpleAnims) {
         anim.isPaused = false;
      }

      // Pause all the normal animations
      Animator[] animators = GetComponentsInChildren<Animator>();
      foreach (Animator animator in animators) {
         animator.enabled = true;
      }

      CompositeAnimationPlayer[] compositeAnims = GetComponentsInChildren<CompositeAnimationPlayer>();
      foreach (CompositeAnimationPlayer anim in compositeAnims) {
         anim.stop();
      }
   }

   #endregion

   #region Sitting

   [Command]
   public void Cmd_EnterChair (Vector3 chairPosition, Direction direction) {
      if (!isSitting) {
         isSitting = true;
         facing = direction;
         positionBeforeSitting = transform.position;
         this.chairPosition = chairPosition;
      }
   }

   [Command]
   public void Cmd_ExitChair () {
      if (isSitting) {
         isSitting = false;
      }
   }

   #endregion

   #region Emoting

   public bool isEmoting () {
      return emoteType != EmoteManager.EmoteTypes.None;
   }

   [Command]
   public void Cmd_PlayEmote (EmoteManager.EmoteTypes emoteType, Direction direction) {
      if (this.emoteType != emoteType && this.emoteType == EmoteManager.EmoteTypes.None) {
         facing = direction;
         this.emoteType = emoteType;
      }
   }

   [Command]
   public void Cmd_StopEmote () {
      if (emoteType != EmoteManager.EmoteTypes.None) {
         emoteType = EmoteManager.EmoteTypes.None;
      }
   }

   #endregion

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

   // The previous sitting state
   private bool _prevIsSitting = false;

   // The previous emoting state
   private EmoteManager.EmoteTypes _prevEmoteType = EmoteManager.EmoteTypes.None;

   #endregion
}
