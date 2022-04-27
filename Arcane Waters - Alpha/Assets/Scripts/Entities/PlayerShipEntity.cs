﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using UnityEngine.InputSystem;
using Assets.Scripts.Map;
using TMPro.Examples;
using MapCreationTool.Serialization;

public class PlayerShipEntity : ShipEntity
{
   #region Public Variables

   // The ID of this ship in the database
   [SyncVar]
   public int shipId;

   // The ship xml id of this ship, info found in player ship web tool
   [SyncVar]
   public int shipXmlId;

   // Gets set to true when the next shot is scheduled
   public bool isNextShotDefined = false;

   // The coordinates of the next shot
   public Vector2 nextShotTarget = new Vector2(0, 0);

   // Ability Reference
   public SyncList<int> shipAbilities = new SyncList<int>();

   // The equipped weapon characteristics
   [SyncVar]
   public int weaponType = 0;
   [SyncVar]
   public string weaponColors;
   [SyncVar]
   public int weaponCount = 1;

   // The equipped armor characteristics
   [SyncVar]
   public int armorType = 0;
   [SyncVar]
   public string armorColors;

   // The equipped hat characteristics
   [SyncVar]
   public int hatType = 0;
   [SyncVar]
   public string hatColors;

   // The gears equippped that may update the stats of the user
   [SyncVar]
   public int ringType = 0, necklaceType = 0, trinketType = 0;

   // The effect that indicates this ship is speeding up
   public SpriteRenderer[] speedUpEffectHolders;
   public Canvas speedupGUI;
   public Image speedUpBar;

   // Color indications if the fuel is usable or not
   public Color recoveringColor, defaultColor;

   // Speedup variables
   public float boostCooldown = 3.0f;
   public float boostForce = 10.0f;

   // Gets set to true when the player ship is hidden and cannot be damaged or controlled
   [SyncVar]
   public bool isDisabled = true;

   // Gets set to true as soon as the player has performed an action after spawning
   [SyncVar]
   public bool hasPerformedFirstActionAfterSpawn = false;

   // The object icon indicating the shoop boost cooling down
   public GameObject shipBoostCooldownObj;

   // The icon indicating the shoop boost cooling down
   public Image shipBoostCooldownIcon;

   // List of icon sprites of the cooldown icon
   public Sprite[] cooldownIconSprites;

   // The anim reference indicating ship boost meter is full
   public Animator maxBoostEffectAnimation;

   // References to sprite swaps fo the player's boost effect sprites
   public SpriteSwap shipBoostSpriteSwapFront, shipBoostSpriteSwapBack;

   // A reference to the player's lifeboat object
   public GameObject lifeboat;

   // GameObject containing the guildicon
   public GameObject guildIconGO;

   // Canvas Group for ship display information
   public CanvasGroup shipInformationDisplay;

   // A reference to the transform containing the boost fill circle
   public Transform boostFillCircleParent;

   // A reference to the boost cooldown bar outline image
   public Image boostCooldownBarOutline;

   // A reference to the rect transform for the boost bar's parent
   public RectTransform boostBarParent;

   // A reference to the player dash effect behavior
   public SeaPlayerDashEffect seaDashEffect;

   // Portrait game object
   public GameObject playerPortrait;

   // A reference to the bars script that displays the health of this ship
   public ShipBarsPlayer shipBars;

   // If the dash button is pressed using gamepad
   public bool gamePadDashPressed = false;

   // References to animators for the player's boost circle
   public Animator boostCircleOutlineAnimator, boostCircleFillAnimator;

   // Other objects can add callbacks to this event, to be notified when this player damages another player
   public System.Action<PlayerShipEntity, PvpTeamType> onDamagedPlayer;

   // Defines the possible attack types for the player's right-click attack
   public enum CannonTargetingType { None = 0, Normal = 1, Cone = 2, Circle = 3, AbilityProjectile = 4, }

   // Determines what type of attack will trigger when right-clicking
   public CannonTargetingType cannonAttackType = CannonTargetingType.Normal;

   // What the max range of the cannon barrage attack is, compared to the player's normal attack
   public const float CANNON_BARRAGE_RANGE_MULTIPLIER = 0.9f;

   // The PvpCaptureTarget that this player is holding, if any
   [HideInInspector]
   public PvpCaptureTarget heldPvpCaptureTarget = null;

   // A reference to the coin trail effect for this player ship
   public GameObject coinTrailEffect;

   // Whether this player ship is currently holding a pvp capture target
   [SyncVar]
   public bool holdingPvpCaptureTarget = false;

   // The index of the selected ship ability
   public int selectedShipAbilityIndex = 0;

   // The different flags the ship can display
   public enum Flag
   {
      None = 0,
      White = 1,
      Group = 2,
      Pvp = 3,
   }

   // The xp rewarded to attackers when being killed, in pvp
   public const int REWARDED_XP = 10;

   // The minimum value of mouse scroll to switch ship abilities
   public const int BASE_SCROLL_VALUE = 120;

   // Reference to the level up effect
   public LevelUpEffect levelUpEffect;

   // Reference to the Warp Waiting Effect
   public IndeterminateProgressBar warpInProgressEffect;

   // Whether the player has the 'reeling in' key down.
   [HideInInspector]
   public bool isReelingIn = false;

   // When the player last started reeling in
   [HideInInspector]
   public float reelInStartTime = 0.0f;

   #endregion

   protected override bool isBot () { return false; }

   public override PlayerShipEntity getPlayerShipEntity () {
      return this;
   }

   protected override void Start () {
      base.Start();

      // Player ships spawn hidden and invulnerable, until the client finishes loading the area
      if (isDisabled) {
         StartCoroutine(CO_TemporarilyDisableShip());
      }

      // Only show the guild icon if one exits
      if (this.guildId > 0) {
         updateGuildIconSprites();
         showGuildIcon();
      } else {
         hideGuildIcon();
      }

      if (isLocalPlayer) {
         // Get a reference to our audio listener
         _fmodListener = GetComponent<FMODUnity.StudioListener>();

         _targetSelector = GetComponentInChildren<PlayerTargetSelector>();

         // Position the boost bar based on ship size
         Vector2 boostBarParentPos = boostBarParent.anchoredPosition;
         switch (shipSize) {
            case ShipSize.Medium:
               boostBarParentPos.y = -27.0f;
               break;
            case ShipSize.Large:
               boostBarParentPos.y = -40.0f;
               break;
            default:
            case ShipSize.Small:
               boostBarParentPos.y = -25.0f;
               break;
         }

         boostBarParent.anchoredPosition = boostBarParentPos;

         PanelManager.self.showPowerupPanel();

         InputManager.self.inputMaster.Sea.Dash.performed += OnSeaDashPerformed;
         InputManager.self.inputMaster.Sea.Dash.canceled += OnSeaDashCanceled;
         VoyageTriggerPopup.self.disableAllPanels();

         // Creating the FMOD event Instance
         _boostState = FMODUnity.RuntimeManager.CreateInstance(SoundEffectManager.SHIP_LAUNCH_CHARGE);
         _boostState.set3DAttributes(FMODUnity.RuntimeUtils.To3DAttributes(this.transform));
      } else if (isServer) {
         _movementInputDirection = Vector2.zero;
      } else {
         // Disable our collider if we are not either the local player or the server
         getMainCollider().isTrigger = true;
      }

      // Create targeting objects
      GameObject targetCirclePrefab = Resources.Load<GameObject>("Prefabs/Targeting/TargetCircle");
      GameObject targetConePrefab = Resources.Load<GameObject>("Prefabs/Targeting/TargetConeDots");
      GameObject cannonTargeterPrefab = Resources.Load<GameObject>("Prefabs/Targeting/CannonTargeter");

      _targetCircle = Instantiate(targetCirclePrefab, transform.parent).GetComponent<TargetCircle>();
      _targetCone = Instantiate(targetConePrefab, transform.parent).GetComponent<TargetCone>();
      _cannonTargeter = Instantiate(cannonTargeterPrefab, transform.parent).GetComponent<CannonTargeter>();
      _healEffect = Instantiate(PrefabsManager.self.healPrefab, abilityEffectHolder).GetComponent<Animator>();

      _targetCircle.gameObject.SetActive(false);
      _targetCone.gameObject.SetActive(false);
      _cannonTargeter.gameObject.SetActive(false);

      // The target circle has a range slightly lower than the player's fully charged shot, since the range limits the center of the circle, and the edge of the circle can reach further
      _targetCircle.maxRange = getCannonballDistance(1.0f) * CANNON_BARRAGE_RANGE_MULTIPLIER;

      if (isServer) {
         List<Powerup> userPowerups = new List<Powerup>();
         if (VoyageManager.isWorldMapArea(areaKey)) {
            userPowerups = new List<Powerup>();
         } else {
            userPowerups = PowerupManager.self.getPowerupsForUser(userId);
            userPowerups = getFilteredPowerup(userPowerups);
         }

         setPowerups(userPowerups, true);

         // When we enter a new scene, update powerups on the client
         rpc.Target_UpdatePowerups(connectionToClient, userPowerups);

         // When we enter a new scene, create powerup orbs for all existing powerups
         List<Powerup.Type> powerupTypes = new List<Powerup.Type>();
         foreach (Powerup powerup in userPowerups) {
            powerupTypes.Add(powerup.powerupType);
         }

         // Set ship type to default for pvp arenas since all ships should be the same and upgraded via shop
         if (VoyageManager.isPvpArenaArea(areaKey) && !WorldMapManager.self.isWorldMapArea(areaKey)) {
            shipType = Ship.Type.Type_1;
            shipSize = ShipSize.Small;
         }

         EntityLagMonitor.self.trackedShipEntities.Add(this);
      }

      if (!Util.isBatch()) {
         InputManager.self.inputMaster.Sea.Enable();
         InputManager.self.inputMaster.Land.Disable();
         // Set our sprite sheets according to our types
         StartCoroutine(CO_UpdateAllSprites());
      } else {
         if (Util.isAutoMove()) {
            // TODO: Remove player ship entity after batch test
            StartCoroutine(CO_UpdateAllSprites());
         }
      }
   }

   public List<Powerup> getFilteredPowerup (List<Powerup> userPowerups) {
      List<Powerup> tempPowerups = new List<Powerup>();

      // Cache temporary powerups
      foreach (Powerup currPowerup in userPowerups) {
         if (currPowerup.expiry == Powerup.Expiry.Timed) {
            tempPowerups.Add(currPowerup);
         }
      }

      // Clear temporary powerups on spawn
      foreach (Powerup currPowerup in tempPowerups) {
         userPowerups.Remove(currPowerup);
      }

      return userPowerups;
   }

   public void changeShipInfo (ShipInfo info) {
      // Server side data initialization
      initialize(info);
   }

   protected override void initialize (ShipInfo shipInfo) {
      // Server side data initialization
      base.initialize(shipInfo);

      for (int i = 0; i < CannonPanel.MAX_ABILITY_COUNT; i++) {
         CannonPanel.self.setAbilityIcon(i, -1);
      }

      if (isServer) {
         shipAbilities.Clear();
         int index = 0;

         foreach (int newShipAbility in shipInfo.shipAbilities.ShipAbilities) {
            ShipAbilityData shipAbilityData = ShipAbilityManager.self.getAbility(newShipAbility);
            shipAbilities.Add(newShipAbility);
            index++;
         }

         // If for some reason this user has insufficient abilities, assign the default abilities until ability count reaches 5
         if (index < CannonPanel.MAX_ABILITY_COUNT) {
            shipAbilities.Clear();
            D.debug("This user {" + shipInfo.userId + "} has invalid abilities! Assigning default abilities");
            for (int i = 0; i < ShipAbilityInfo.STARTING_ABILITIES.Count; i++) {
               shipAbilities.Add(ShipAbilityInfo.STARTING_ABILITIES[i]);
            }
         }

         updateAbilityCooldownDurations();

         // Notify client to update abilities in cannon panel
         Target_UpdateCannonPanel(connectionToClient, shipAbilities.ToArray());

         if (isInGroup()) {
            // If the player is in a voyage, not in the lobby, and not in a pvp game, don't restore max health, it should be persistent between leagues
            if (!VoyageManager.isLobbyArea(areaKey) && !VoyageManager.isPvpArenaArea(areaKey)) {
               initHealth(skipCurrentHealth: true);

               // If the player is in a pvp arena or a voyage lobby, restore their full health.
            } else {
               initHealth(skipCurrentHealth: false);
            }
         } else {
            initHealth(skipCurrentHealth: false);
         }
      }

      if (shipAbilities.Count > 0) {
         primaryAbilityId = shipAbilities[0];
      }
   }

   private void OnSeaDashPerformed (InputAction.CallbackContext ctx) {
      if (gamePadDashPressed != true) {
         pressBoost();
         gamePadDashPressed = true;
      }
   }
   private void OnSeaDashCanceled (InputAction.CallbackContext ctx) {
      if (gamePadDashPressed != false) {
         releaseBoost();
         gamePadDashPressed = false;
      }
   }

   private void initHealth (bool skipCurrentHealth = false) {
      float healthMultiplierAdditive = 1.0f + PerkManager.self.getPerkMultiplierAdditive(userId, Perk.Category.ShipHealth) + PowerupManager.self.getPowerupMultiplierAdditive(userId, Powerup.Type.IncreasedHealth);

      if (!skipCurrentHealth) {
         currentHealth = (int) (healthMultiplierAdditive * _baseHealth);
      }
      maxHealth = (int) (healthMultiplierAdditive * _baseHealth);
   }

   protected override void updateSprites () {
      base.updateSprites();

      shipBoostSpriteSwapFront.newTexture = _shipBoostSpritesFront;
      shipBoostSpriteSwapBack.newTexture = _shipBoostSpritesBack;
   }

   private void selectAbility (int abilitySlotIndex) {
      _currentAbilitySlotIndex = abilitySlotIndex;
      Cmd_ChangeAttackOption(abilitySlotIndex);
   }

   private void nextAbility () {
      selectAbility(Mathf.Clamp(_currentAbilitySlotIndex + 1, 0, 4));
   }

   private void prevAbility () {
      selectAbility(Mathf.Clamp(_currentAbilitySlotIndex - 1, 0, 4));
   }

   protected override void Update () {
      base.Update();

      // Update Ship Bars
      if (isDead() || isDisabled) {
         toggleShipDisplayInfo(false);
      } else {
         toggleShipDisplayInfo(isLocalPlayer || isMouseOver());

         float barsTargetAlpha = 1.0f;
         if (abilityCastEffectHolder != null && abilityCastEffectHolder.transform.childCount > 0) {
            barsTargetAlpha = 0.0f;
         }

         shipBars.targetAlpha = Mathf.Lerp(shipBars.targetAlpha, barsTargetAlpha, Time.deltaTime * 5.0f);
      }

      // Hide targeting UI when dead
      if (isDead()) {
         _targetCone.gameObject.SetActive(false);
         _targetCircle.gameObject.SetActive(false);
         _cannonTargeter.gameObject.SetActive(false);
      }

      updateSpeedUpDisplay();
      updateCoinTrail();
      updateAbilityCooldowns();

      // Recolor the ship flag if needed
      if (isClient) {
         Instance instance = getInstance();
         if (VoyageGroupManager.isInGroup(this) && instance != null && instance.isVoyage) {
            if (VoyageManager.isPvpArenaArea(areaKey)) {
               setFlag(Flag.Pvp);
            } else {
               // In PvE instances, we always set the group flag color
               setFlag(Flag.Group);
            }
         }

         if (!ChatManager.self.chatPanel.inputField.isFocused) {
            if (InputManager.self.inputMaster.Hud.Shortcut1.WasPerformedThisFrame()) {
               selectAbility(0);
            } else if (InputManager.self.inputMaster.Hud.Shortcut2.WasPerformedThisFrame()) {
               selectAbility(1);
            } else if (InputManager.self.inputMaster.Hud.Shortcut3.WasPerformedThisFrame()) {
               selectAbility(2);
            } else if (InputManager.self.inputMaster.Hud.Shortcut4.WasPerformedThisFrame()) {
               selectAbility(3);
            } else if (InputManager.self.inputMaster.Hud.Shortcut5.WasPerformedThisFrame()) {
               selectAbility(4);
            }

            if (InputManager.self.inputMaster.Hud.NextShortcut.WasPerformedThisFrame()) {
               nextAbility();
            } else if (InputManager.self.inputMaster.Hud.PrevShortcut.WasPerformedThisFrame()) {
               prevAbility();
            }

            if (InputManager.self.inputMaster.Sea.ReelIn.WasPerformedThisFrame()) {
               Cmd_SetIsReelingIn(true);
            } else if (InputManager.self.inputMaster.Sea.ReelIn.WasReleasedThisFrame()) {
               Cmd_SetIsReelingIn(false);
            }

            // Read input mouse scroll value and divide it to scroll magnitude to get the target ability index
            float scrollVal = InputManager.self.inputMaster.Sea.AbilitySwitch.ReadValue<float>();
            if (scrollVal >= BASE_SCROLL_VALUE || scrollVal <= -BASE_SCROLL_VALUE) {
               // The base scroll value is 120, multiply it with scroll magnitude and divide the result with scroll value to get switch value
               int switchValue = scrollVal < 0 ? -1 : 1;
               int targetAbility = Mathf.Clamp(_currentAbilitySlotIndex + switchValue, 0, 4);
               selectAbility(targetAbility);
            }
         }
      }

      if (NetworkServer.active) {
         // Add heal per second
         SeaBuffData healData = getBuffData(SeaBuff.Category.Buff, SeaBuff.Type.Heal);
         if (healData != null) {
            if ((NetworkTime.time - healData.lastBuffTick) > 1) {
               healData.lastBuffTick = NetworkTime.time;
               int healValue = (int) (healData.buffMagnitude * maxHealth);
               currentHealth += healValue;

               NetEntity casterEntity = EntityManager.self.getEntityByNetId(healData.casterId);
               if (casterEntity != null && casterEntity is ShipEntity) {
                  if (VoyageGroupManager.self.tryGetGroupById(casterEntity.voyageGroupId, out VoyageGroupInfo voyageGroup)) {
                     voyageGroup.addHealStatsForUser(casterEntity.userId, healValue);
                     ((ShipEntity) casterEntity).totalHeals = voyageGroup.getTotalHeals(casterEntity.userId);
                  }
               }
               Rpc_CastSkill(healData.buffAbilityIdReference, null, transform.position, healValue, true, true, false, this.netId);
            }
         }
      }

      // Adjust the volume on our movement audio source
      processMovementAudio();

      // If the reload is finished and a shot was scheduled, fire it
      //if (isNextShotDefined && hasReloaded()) {
      //   // Fire the scheduled shot
      //   if (!SeaManager.self.isOffensiveAbility()) {
      //      Cmd_CastAbility(SeaManager.getAttackType());
      //   } else {
      //      Cmd_FireMainCannonAtSpot(nextShotTarget, SeaManager.getAttackType(), transform.position);
      //   }
      //   isNextShotDefined = false;
      //}

      // Check if input is allowed
      if (!Util.isGeneralInputAllowed() || isDisabled || !isLocalPlayer) {
         if (_chargingWithMouse && isLocalPlayer) {
            cannonAttackReleased();
         }
         return;
      }

      checkAudioListener();

      if (InputManager.self.inputMaster.General.Interact.WasPerformedThisFrame() && !PanelManager.self.hasPanelInLinkedList()) {
         NetEntity ship = getClickedBody();
         if (ship != null && ship is PlayerShipEntity) {
            D.adminLog("ContextMenu: Interact was performed via action key sea:" +
            "{" + userId + ":" + entityName + "}{" + ship.userId + ":" + ship.entityName + "}", D.ADMIN_LOG_TYPE.Player_Menu);
            PanelManager.self.contextMenuPanel.showDefaultMenuForUser(ship.userId, ship.entityName);
         }
      }

      if (!isDead() && !isGhost && !isPerformingAttack()) {
         // Start charging attack with mouse
         if (InputManager.self.inputMaster.Sea.FireCannon.WasPressedThisFrame() || (InputManager.self.inputMaster.Sea.FireCannon.IsPressed() && !_isChargingCannon)) {
            _chargingWithMouse = true;
            cannonAttackPressed();
            // Can only start charging with spacebar if we have a valid target
         } else if (_targetSelector.getTarget() != null && (InputManager.self.inputMaster.Sea.FireCannon.WasPressedThisFrame() || (InputManager.self.inputMaster.Sea.FireCannon.IsPressed() && !_isChargingCannon))) {
            _chargingWithMouse = false;
            cannonAttackPressed();
         } else {
            if (InputManager.self.inputMaster.Sea.FireCannon.WasPressedThisFrame() && _isChargingCannon) {
               D.debug("Cannot cast, this user is still performing charging attack!");
            }
         }

         if ((InputManager.self.inputMaster.Sea.FireCannon.WasReleasedThisFrame() && _chargingWithMouse) || (InputManager.self.inputMaster.Sea.FireCannon.WasReleasedThisFrame() && !_chargingWithMouse)) {
            cannonAttackReleased();
         }
      } else {
         // TODO: Enable this for ability cast debugging
         /*
         if (InputManager.self.inputMaster.Sea.FireCannon.WasPressedThisFrame()) {
            if (isPerformingAttack()) {
               D.debug("Cannot cast, this user is still performing an attack!");
            }
         }*/
      }

      boostUpdate();

      // Try to open chest through code (instead of UI) in case if UI is blocking raycasts casted to the chest Canvas
      if (InputManager.self.inputMaster.General.Interact.WasPerformedThisFrame() && !PriorityOverProcessActionLogic.isAnyHovered()) {
         tryToOpenChest();
      }
   }

   private void LateUpdate () {
      if (isLocalPlayer) {
         // Update targeting UI
         if (_isChargingCannon) {
            updateTargeting();
         }
      }
   }

   protected override void FixedUpdate () {
      base.FixedUpdate();
      
      if (NetworkServer.active && InputManager.isInputEnabled()) {
         // If the player wants to stop the ship, we let the linear drag handle the slowdown
         if (!isDead() && _movementInputDirection != Vector2.zero) {
            float increaseAdditive = 1.0f;

            // Players with the pvp capture target won't receive any speed buffs
            if (!holdingPvpCaptureTarget) {
               increaseAdditive += PerkManager.self.getPerkMultiplierAdditive(userId, Perk.Category.ShipMovementSpeed);
               increaseAdditive += PowerupManager.self.getPowerupMultiplierAdditive(userId, Powerup.Type.SpeedUp);
            }

            float targetSpeed = getMoveSpeed() * increaseAdditive;
            if (targetSpeed > MAX_SHIP_SPEED && !isGhost) {
               targetSpeed = MAX_SHIP_SPEED;
            }

            Vector2 targetVelocity = _movementInputDirection * targetSpeed * Time.fixedDeltaTime;
            _body.velocity = Vector2.SmoothDamp(_body.velocity, targetVelocity, ref _shipDampVelocity, 0.5f);
            movementForce = _movementInputDirection * targetSpeed * _body.mass;
            D.adminLog("4) Server velocity has now been declared to do this: V:{" + _body.velocity + "} " +
               "MF:{" + movementForce + "} MID:{" + _movementInputDirection + "} TS:{" + targetSpeed + "}", D.ADMIN_LOG_TYPE.Simulation_Sea);

            // In ghost mode, clamp the position to the area bounds
            clampToMapBoundsInGhost();
         } else {
            movementForce = Vector2.zero;
         }
      }
   }

   // Enable and trigger animator to play the heal effect animation
   protected override void showHealEffect (bool isEnable) {
      _healEffect.gameObject.SetActive(isEnable);
      if (isEnable) {
         _healEffect.SetTrigger(SHOW_HEAL);
      }
   }

   private void boostUpdate () {
      // Begin charging boost
      if (InputManager.self.inputMaster.Sea.Dash.WasPressedThisFrame()) {
         pressBoost();
      } else if (InputManager.self.inputMaster.Sea.Dash.WasReleasedThisFrame()) {
         releaseBoost();
      }

      seaDashEffect.setChargeEffect(getBoostChargeAmount());

      if (!isBoostCoolingDown() && _isChargingBoost) {
         // Update the boost-timing circle
         //boostFillCircleParent.localScale = (Vector3.one * 0.5f) + (Vector3.one * 0.5f * getBoostChargeAmount());
         //boostFillCircle.color = ColorCurveReferences.self.shipBoostCircleColor.Evaluate(getBoostChargeAmount());
         //
         //boostCircleFillAnimator.SetInteger("facing", (int) facing);
         //boostCircleOutlineAnimator.SetInteger("facing", (int) facing);

         // FMOD SFX
         if (getBoostChargeAmount() == 1) {
            _boostState.setParameterByName(SoundEffectManager.SHIP_CHARGE_RELEASE_PARAM, 1);
         }
      }
   }

   private void pressBoost () {
      if (!isBoostCoolingDown() && !isDead() && InputManager.isInputEnabled()) {
         _boostChargeStartTime = NetworkTime.time;
         //boostTimingSprites.alpha = 1.0f;
         _isChargingBoost = true;

         FMODUnity.RuntimeManager.AttachInstanceToGameObject(this._boostState, this.transform, this._body);
         _boostState.start();
         _boostState.setParameterByName(SoundEffectManager.SHIP_CHARGE_RELEASE_PARAM, 0);
      }
   }

   private void releaseBoost () {
      if (!isBoostCoolingDown() && _isChargingBoost) {
         // Activate boost using FMOD SFX
         _boostState.setParameterByName(SoundEffectManager.SHIP_CHARGE_RELEASE_PARAM, 2);

         // If the player is pressing a direction, boost them that way, otherwise boost them the way they are facing
         Vector2 boostDirection = InputManager.getMovementInput();
         if (boostDirection.magnitude < 0.1f) {
            boostDirection = Util.getDirectionFromFacing(facing);
         }

         Cmd_RequestServerAddBoostForce(boostDirection, getBoostChargeAmount());
         _lastBoostTime = NetworkTime.time;
         seaDashEffect.playDashEffect(boostDirection);
         _isChargingBoost = false;

         // Trigger the tutorial
         TutorialManager3.self.tryCompletingStep(TutorialTrigger.ShipSpeedUp);
      }
   }

   private void switchCannonTargetingMode (CannonTargetingType newAttackType) {
      CannonTargetingType oldAttackType = cannonAttackType;

      // Don't do anything if our attack type hasn't changed
      if (oldAttackType == newAttackType) {
         return;
      }

      // Disable game objects from the old targeting type
      switch (oldAttackType) {
         case CannonTargetingType.Cone:
            _targetCone.gameObject.SetActive(false);
            break;
         case CannonTargetingType.Circle:
            _targetCircle.gameObject.SetActive(false);
            break;
         default:
            _cannonTargeter.gameObject.SetActive(false);
            break;
      }

      cannonAttackType = newAttackType;

      // Enable game objects for the new targeting type
      switch (newAttackType) {
         case CannonTargetingType.Cone:
            _targetCone.gameObject.SetActive(true);
            updateTargeting();
            _targetCone.updateCone(true);
            break;
         case CannonTargetingType.Circle:
            _targetCircle.gameObject.SetActive(true);
            updateTargeting();
            _targetCircle.updateCircle(true);
            break;
         case CannonTargetingType.Normal:
         case CannonTargetingType.AbilityProjectile:
            _cannonTargeter.gameObject.SetActive(true);
            updateTargeting();
            _cannonTargeter.updateTargeter();
            break;
         default:
            break;
      }
   }

   private void cannonAttackPressed () {
      if (!hasReloaded() || isPerformingAttack()) {
         return;
      }

      _cannonChargeStartTime = NetworkTime.time;
      _isChargingCannon = true;

      // Skip targetting setup if ability selected is buff type
      ShipAbilityData abilityData = ShipAbilityManager.self.getAbility(getSelectedShipAbilityId());
      switch (abilityData.selectedAttackType) {
         case Attack.Type.Heal:
         case Attack.Type.SpeedBoost:
            return;
      }

      switch (cannonAttackType) {
         case CannonTargetingType.Normal:
         case CannonTargetingType.AbilityProjectile:
            _cannonTargeter.gameObject.SetActive(true);
            updateTargeting();
            _cannonTargeter.updateTargeter();
            break;
         case CannonTargetingType.Cone:
            _targetCone.gameObject.SetActive(true);
            updateTargeting();
            _targetCone.updateCone(true);
            break;
         case CannonTargetingType.Circle:
            _targetCircle.gameObject.SetActive(true);
            updateTargeting();
            _targetCircle.updateCircle(true);
            break;
      }
   }

   private void cannonAttackReleased () {
      if (!_isChargingCannon || isPerformingAttack()) {
         D.adminLog("Cannot attack! " + (!_isChargingCannon ? "Cant Release, is charging cannon" : "") + " : " + (isPerformingAttack() ? "Cant Release, is already attacking!" : ""), D.ADMIN_LOG_TYPE.AbilityCast);
         return;
      }

      bool useCannonAttack = true;
      ShipAbilityData abilityData = ShipAbilityManager.self.getAbility(getSelectedShipAbilityId());
      if (abilityData != null) {
         switch (abilityData.selectedAttackType) {
            case Attack.Type.SpeedBoost:
            case Attack.Type.Heal:
               Cmd_CastAbility(abilityData.abilityId);
               useCannonAttack = false;
               enableTargeting();
               break;
         }
      }

      CannonTargetingType abilityAttackType = getCannonTargetingTypeFromAttackType(abilityData.selectedAttackType);

      if (useCannonAttack) {
         switch (abilityAttackType) {
            case CannonTargetingType.Normal:
            case CannonTargetingType.AbilityProjectile:
               if (abilityData.splitsAfterAttackCap) {
                  splitAttackCap(abilityData);
               } else {
                  Vector2 targetPosition;

                  if (_chargingWithMouse) {
                     targetPosition = Util.getMousePos(transform.position);
                  } else {
                     targetPosition = _targetSelector.getTarget().transform.position;
                  }

                  Cmd_FireMainCannonAtTarget(null, getCannonChargeAmount(), _cannonTargeter.barrelSocket.position, targetPosition, true, true);
                  _shouldUpdateTargeting = false;
                  _cannonTargeter.targetingConfirmed(() => enableTargeting());
                  TutorialManager3.self.tryCompletingStep(TutorialTrigger.FireShipCannon);
               }
               break;
            case CannonTargetingType.Cone:
               if (abilityData.splitsAfterAttackCap) {
                  splitAttackCap(abilityData);
               } else {
                  Vector2 toMouse = Util.getMousePos(transform.position) - transform.position;
                  Vector2 pos = transform.position;

                  float cannonballLifetime = getCannonballLifetime();
                  float rotAngle = (40.0f - (getCannonChargeAmount() * 25.0f)) / 2.0f;

                  Cmd_PlaySeaAbilitySfx(abilityData.abilityId, transform.position);

                  // Fire cone of cannonballs
                  Cmd_FireMainCannonAtTarget(null, getCannonChargeAmount(), transform.position, pos + ExtensionsUtil.Rotate(toMouse, rotAngle), false, true);
                  Cmd_FireMainCannonAtTarget(null, getCannonChargeAmount(), transform.position, Util.getMousePos(transform.position), false, false);
                  Cmd_FireMainCannonAtTarget(null, getCannonChargeAmount(), transform.position, pos + ExtensionsUtil.Rotate(toMouse, -rotAngle), false, false);
                  _shouldUpdateTargeting = false;
                  _targetCone.targetingConfirmed(() => enableTargeting());
               }
               break;
            case CannonTargetingType.Circle:
               if (_cannonBarrageCoroutine != null) {
                  StopCoroutine(_cannonBarrageCoroutine);
               }

               float circleRadius = (0.625f - (getCannonChargeAmount() * 0.125f));
               _shouldUpdateTargeting = false;
               _targetCircle.targetingConfirmed(() => enableTargeting());

               Cmd_PlaySeaAbilitySfx(abilityData.abilityId, transform.position);

               _cannonBarrageCoroutine = StartCoroutine(CO_CannonBarrage(_targetCircle.transform.position, circleRadius));
               _targetCircle.setFillColor(Color.white);
               _targetCircle.updateCircle(true);

               break;
            default:
               if (abilityData.splitsAfterAttackCap) {
                  splitAttackCap(abilityData);
               }
               break;
         }
      }
      _isChargingCannon = false;

      _currentAbilitySlotIndex = 0;
      Cmd_AbilityUsed(selectedShipAbilityIndex);
   }

   private void enableTargeting () {
      _shouldUpdateTargeting = true;
   }

   private void splitAttackCap (ShipAbilityData abilityData) {
      Vector2 toMouse = Util.getMousePos(transform.position) - transform.position;
      Vector2 pos = transform.position;

      float rotAngle = (45.0f - (getCannonChargeAmount() * 25.0f)) / 1.25f;
      float rotAngleDivider = rotAngle / abilityData.splitAttackCap;

      Cmd_PlaySeaAbilitySfx(abilityData.abilityId, transform.position);

      // Fire barrage of cannonballs
      for (int i = 1; i < (abilityData.splitAttackCap / 2) + 1; i++) {
         Cmd_FireMainCannonAtTarget(null, getCannonChargeAmount(), transform.position, pos + ExtensionsUtil.Rotate(toMouse, rotAngleDivider * i), false, false);
      }
      Cmd_FireMainCannonAtTarget(null, getCannonChargeAmount(), transform.position, Util.getMousePos(transform.position), false, true);

      for (int i = 1; i < (abilityData.splitAttackCap / 2) + 1; i++) {
         Cmd_FireMainCannonAtTarget(null, getCannonChargeAmount(), transform.position, pos + ExtensionsUtil.Rotate(toMouse, -rotAngleDivider * i), false, false);
      }

      switch (cannonAttackType) {
         case CannonTargetingType.Normal:
         case CannonTargetingType.AbilityProjectile:
            _shouldUpdateTargeting = false;
            _cannonTargeter.targetingConfirmed(() => enableTargeting());
            break;
         case CannonTargetingType.Cone:
            _shouldUpdateTargeting = false;
            _targetCone.targetingConfirmed(() => enableTargeting());
            break;
         case CannonTargetingType.Circle:
            _shouldUpdateTargeting = false;
            _targetCircle.targetingConfirmed(() => enableTargeting());
            break;
         default:
            break;
      }
   }

   private void updateTargeting () {
      if (!_shouldUpdateTargeting || isDead()) {
         return;
      }

      switch (cannonAttackType) {
         case CannonTargetingType.Normal:
         case CannonTargetingType.AbilityProjectile:

            Vector2 fireDir;

            // If firing with the mouse, aim using the mouse
            if (_chargingWithMouse) {
               fireDir = Util.getMousePos(transform.position) - transform.position;
            } else {
               // If we don't have a target to aim at, cancel attack
               if (_targetSelector.getTarget() == null) {
                  _cannonTargeter.gameObject.SetActive(false);
                  _shouldUpdateTargeting = false;
                  _isChargingCannon = false;
                  return;
               }
               // If firing with the keyboard, aim at our target automatically
               fireDir = _targetSelector.getTarget().transform.position - transform.position;
            }

            // Update cannon targeter parameters
            Vector2 targetPoint = (Vector2) _cannonTargeter.barrelSocket.position + fireDir.normalized * getAttackDistance();
            _cannonTargeter.setTarget(targetPoint);
            _cannonTargeter.parabolaHeight = getCannonballApex();
            _cannonTargeter.transform.position = transform.position;
            _cannonTargeter.chargeAmount = getCannonChargeAmount();
            _cannonTargeter.updateTargeter();

            break;
         case CannonTargetingType.Cone:
            // Update target cone parameters
            if (ShipAbilityManager.self.getAbility(getSelectedShipAbilityId()).splitsAfterAttackCap) {
               _targetCone.coneHalfAngle = (40.0f - (getCannonChargeAmount() * 25.0f)) / 1.25f;
            } else {
               _targetCone.coneHalfAngle = (40.0f - (getCannonChargeAmount() * 25.0f)) / 2.0f;
            }
            _targetCone.coneOuterRadius = getAttackDistance();
            _targetCone.transform.position = transform.position;
            _targetCone.updateCone(true);

            // Check for enemies inside cone
            Collider2D[] coneHits = Physics2D.OverlapCircleAll(transform.position, _targetCone.coneOuterRadius, LayerMask.GetMask(LayerUtil.SHIPS));
            bool enemyInCone = false;
            float middleAngle = Util.angle(Util.getMousePos(transform.position) - transform.position);

            foreach (Collider2D hit in coneHits) {
               if (hit.GetComponent<SeaEntity>() && !hit.GetComponent<PlayerShipEntity>()) {
                  if (Util.isWithinCone(transform.position, hit.transform.position, middleAngle, _targetCone.coneHalfAngle)) {
                     enemyInCone = true;
                     break;
                  }
               }
            }

            Color coneColor = (enemyInCone) ? Color.yellow : Color.white;
            _targetCone.setConeColor(coneColor);
            break;
         case CannonTargetingType.Circle:
            // Update target circle parameters
            float circleRadius = (0.625f - (getCannonChargeAmount() * 0.125f));
            _targetCircle.scaleCircle(circleRadius * 2.0f);
            _targetCircle.updateCircle(true);

            // Check for enemies inside circle
            Collider2D[] circleHits = Physics2D.OverlapCircleAll(Util.getMousePos(transform.position), circleRadius, LayerMask.GetMask(LayerUtil.SHIPS));
            bool enemyInCircle = false;
            foreach (Collider2D hit in circleHits) {
               if (hit.GetComponent<BotShipEntity>()) {
                  enemyInCircle = true;
                  break;
               }
            }

            Color circleColor = (enemyInCircle) ? Color.yellow : Color.white;
            _targetCircle.setCircleColor(circleColor);
            break;
      }
   }

   private IEnumerator CO_CannonBarrage (Vector3 targetPosition, float radius) {
      float lifetime = 0.0f;

      // Store ability id, to ensure it doesn't change during the barrage
      int abilityId = getSelectedShipAbilityId();

      bool playSound = false;

      for (int i = 0; i < 10; i++) {
         Vector3 endPos = targetPosition + Random.insideUnitSphere * radius;

         // Calculate lifetime to hit end point
         Vector3 toEndPos = endPos - transform.position;
         toEndPos.z = 0.0f;
         float dist = toEndPos.magnitude;
         lifetime = Mathf.Lerp(2.0f, 3.0f, dist / 5.0f);

         // Start playing the projectile sound effect after the first one.
         if (i > 0 && !playSound) {
            playSound = true;
         }

         Cmd_FireSpecialCannonAtTarget(null, endPos, lifetime, false, playSound, abilityId, disableColliderFor: 0.9f, false);
         yield return new WaitForSeconds(0.2f);
      }

   }

   public override bool isMoving () {
      return getVelocity().magnitude > (isLocalPlayer ? SHIP_MOVING_MAGNITUDE : NETWORK_SHIP_MOVING_MAGNITUDE);
   }

   public Vector2 getAimPosition () {
      return _currentAimPosition;
   }

   [Command]
   public void Cmd_ChangeAttackOption (int abilityIndex) {
      changeAttackOption(abilityIndex);
   }

   private void changeAttackOption (int abilityIndex) {
      // If the ability is on cooldown, don't change attack options
      if (_currentAbilityCooldowns[abilityIndex] > 0.0f) {
         // TODO: Notify the player that the ability is on cooldown?
         return;
      }

      if (shipAbilities.Count > abilityIndex) {
         selectedShipAbilityIndex = abilityIndex;

         ShipAbilityData shipAbilityData = ShipAbilityManager.self.getAbility(shipAbilities[abilityIndex]);

         if (isServerOnly) {
            cannonAttackType = getCannonTargetingTypeFromAttackType(shipAbilityData.selectedAttackType);
         }

         Target_ReceiveAttackOption(abilityIndex);
      } else {
         D.debug("Couldn't change attack option, ability index was out of range. Num abilities: " + shipAbilities.Count);
      }
   }

   [TargetRpc]
   public void Target_ReceiveAttackOption (int abilityIndex) {
      selectedShipAbilityIndex = abilityIndex;
      ShipAbilityData shipAbilityData = ShipAbilityManager.self.getAbility(shipAbilities[abilityIndex]);
      CannonPanel.self.cannonBoxList[abilityIndex].setCannons();
      CannonTargetingType newAttackType = getCannonTargetingTypeFromAttackType(shipAbilityData.selectedAttackType);

      // Enable / disable targeting elements if attack type has changed during charging
      if (_isChargingCannon) {
         switchCannonTargetingMode(newAttackType);
      } else {
         cannonAttackType = newAttackType;
      }

      _cannonTargeter.updateTargetDotStyle(abilityIndex);
   }

   [Command]
   protected void Cmd_FireMainCannonAtTarget (GameObject target, float chargeAmount, Vector3 spawnPosition, Vector2 requestedTargetPoint, bool checkReload, bool playSound) {
      if (isDead() || (checkReload && !hasReloaded()) || isAbilityOnCooldown(selectedShipAbilityIndex)) {
         return;
      }

      Rpc_NoteAttack();
      _lastAttackTime = NetworkTime.time;

      Vector2 targetPosition = (target == null) ? requestedTargetPoint : (Vector2) target.transform.position;
      Vector2 fireDirection = (targetPosition - (Vector2) transform.position).normalized;

      // Firing the cannon is considered a PvP action
      hasEnteredPvP = true;

      hasPerformedFirstActionAfterSpawn = true;

      fireCannonBallAtTarget(spawnPosition, fireDirection, chargeAmount, playSound);
      triggerPowerupsOnFire(chargeAmount);
   }

   [Command]
   protected void Cmd_FireSpecialCannonAtTarget (GameObject target, Vector2 requestedTargetPoint, float lifetime, bool checkReload, bool playSound, int abilityId, float disableColliderFor, bool checkCooldown) {
      if (isDead() || (checkReload && !hasReloaded()) || (checkCooldown && isAbilityOnCooldown(selectedShipAbilityIndex))) {
         return;
      }

      Rpc_NoteAttack();

      _lastAttackTime = NetworkTime.time;

      Vector2 startPosition = transform.position;
      Vector2 targetPosition = (target == null) ? requestedTargetPoint : (Vector2) target.transform.position;

      // Firing the cannon is considered a PvP action
      hasEnteredPvP = true;

      hasPerformedFirstActionAfterSpawn = true;

      fireSpecialCannonBallAtTarget(startPosition, targetPosition, lifetime, playSound, abilityId, disableColliderFor);
   }

   [Server]
   protected void triggerPowerupsOnFire (float chargeAmount) {
      float multiShotMultiplier = PowerupManager.self.getPowerupMultiplierAdditive(userId, Powerup.Type.MultiShots);

      // If the user has the MultiShots powerup
      if (multiShotMultiplier > 0.01f && PowerupManager.self.canPlayerUsePowerup(userId, Powerup.Type.MultiShots)) {
         // Store the activation chance for the powerup
         float activationChance = multiShotMultiplier * 2.0f;
         int maxExtraShots = (int) (multiShotMultiplier * 10.0f);
         int extraShotsCounter = 0;

         List<SeaEntity> nearbyEnemies = Util.getEnemiesInCircle(this, transform.position, getAttackDistance(chargeAmount));
         foreach (SeaEntity enemy in nearbyEnemies) {
            // If we have reached the limit of extra shots, stop checking
            if (extraShotsCounter >= maxExtraShots) {
               break;
            }

            // Roll for powerup activation chance
            if (Random.Range(0.0f, 1.0f) <= activationChance) {
               // Successfully activated, fire an extra cannonball at the enemy
               Vector2 toEnemy = enemy.transform.position - transform.position;
               fireCannonBallAtTarget(transform.position, toEnemy.normalized, chargeAmount, false);
               extraShotsCounter++;
            }
         }
      }
   }

   [ClientRpc]
   public void Rpc_RefreshSprites (int shipType, int shipSize, int shipSkinType) {
      overrideSprite((Ship.Type) shipType, (ShipSize) shipSize, (Ship.SkinType) shipSkinType);
   }

   [TargetRpc]
   public void Target_NotifyCannonEffectChange (NetworkConnection connection, int newStatusEffect) {
      ChatPanel.self.addChatInfo(new ChatInfo(0, "Changed status type to: " + ((Status.Type) newStatusEffect).ToString(), System.DateTime.Now, ChatInfo.Type.System));
   }

   private void updateSpeedUpDisplay () {
      float timeSinceBoost = (float) (NetworkTime.time - _lastBoostTime);
      float normalisedWakeTimeSinceBoost = timeSinceBoost / BOOST_WAKE_TIME;
      foreach (SpriteRenderer renderer in speedUpEffectHolders) {
         Color rendererColor = renderer.color;
         rendererColor.a = ColorCurveReferences.self.shipBoostWakeAlpha.Evaluate(normalisedWakeTimeSinceBoost);
         renderer.color = rendererColor;
      }

      if (!isLocalPlayer) {
         speedupGUI.enabled = false;
         return;
      }

      if (isBoostCoolingDown()) {
         speedupGUI.enabled = true;
         float normalisedTimeSinceBoost = Mathf.Clamp01(timeSinceBoost / boostCooldown);
         speedUpBar.fillAmount = normalisedTimeSinceBoost;
         boostCooldownBarOutline.color = ColorCurveReferences.self.shipBoostCooldownBarOutlineColor.Evaluate(normalisedTimeSinceBoost);
         speedUpBar.color = ColorCurveReferences.self.shipBoostCooldownBarColor.Evaluate(normalisedTimeSinceBoost);
      } else {
         speedupGUI.enabled = false;
      }
   }

   protected override void OnDestroy () {
      base.OnDestroy();

      // Disable pvp shop panel if its open
      if (isLocalPlayer && PvpShopPanel.self.isActive()) {
         PvpShopPanel.self.clearPanel();
         PvpShopPanel.self.hideEntirePanel();
      }

      if (_targetCone != null && _targetCone.gameObject != null) {
         Destroy(_targetCone.gameObject);
      }

      if (_targetCircle != null && _targetCircle.gameObject != null) {
         Destroy(_targetCircle.gameObject);
      }

      if (_cannonTargeter != null && _cannonTargeter.gameObject != null) {
         Destroy(_cannonTargeter.gameObject);
      }

      if (hasAuthority) {
         PanelManager.self.hidePowerupPanel();
         PvpStructureStatusPanel.self.onPlayerLeftPvpGame();
         PvpStatPanel.self.onPlayerLeftPvpGame();
         PvpInstructionsPanel.self.hide();
      }

      // Unsubscribe from input events
      if (!Util.isBatch()) {
         InputManager.self.inputMaster.Sea.Dash.performed -= OnSeaDashPerformed;
         InputManager.self.inputMaster.Sea.Dash.canceled -= OnSeaDashCanceled;
      }

      // Handle OnDestroy logic in a separate method so it can be correctly stripped
      onBeingDestroyedServer();
   }

   public bool isAiming () {
      return _isChargingCannon;
   }

   [ServerOnly]
   private void onBeingDestroyedServer () {
      // If the player is in a pvp game, remove them from the game
      PvpGame activeGame = PvpManager.self.getGameWithPlayer(this);
      if (activeGame != null && activeGame.areaKey == this.areaKey) {
         activeGame.removePlayerFromGame(this);
      }

      // We don't care when the Destroy was initiated by a warp
      if (this.isAboutToWarpOnServer) {
         return;
      }

      // Make sure the server saves our position and health when a player is disconnected (by any means other than a warp)
      if (MyNetworkManager.wasServerStarted) {
         storeCurrentShipHealth();
      }
   }

   [Server]
   public void fireCannonBallAtTarget (Vector3 spawnPosition, Vector2 fireDirection, float chargeAmount, bool playSound = true) {
      // Create the cannon ball object from the prefab
      SeaProjectile newProjectile;

      int abilityId = -1;
      float statusDuration = 3;
      float projectileSpeed = 1.0f;
      Status.Type abilityStatus = Status.Type.None;
      Attack.Type attackType = Attack.Type.None;
      if (shipAbilities.Count > 0) {
         ShipAbilityData shipAbilityData = ShipAbilityManager.self.getAbility(getSelectedShipAbilityId());
         if (shipAbilityData != null) {
            abilityId = shipAbilityData.abilityId;
            abilityStatus = (Status.Type) shipAbilityData.statusType;
            statusDuration = shipAbilityData.statusDuration;
            attackType = shipAbilityData.selectedAttackType;
            projectileSpeed = shipAbilityData.projectileSpeed;
         }
      }

      if (attackType == Attack.Type.Harpoon) {
         newProjectile = Instantiate(PrefabsManager.self.seaHarpoonPrefab, spawnPosition, Quaternion.identity);
      } else if (cannonAttackType == CannonTargetingType.AbilityProjectile) {
         newProjectile = Instantiate(PrefabsManager.self.seaProjectilePrefab, spawnPosition, Quaternion.identity);
      } else {
         newProjectile = Instantiate(PrefabsManager.self.serverCannonBallPrefab, spawnPosition, Quaternion.identity);
      }

      bool isCritical = (chargeAmount >= 0.8f && chargeAmount <= 0.99f);
      float critModifier = (isCritical) ? 1.5f : 1.0f;

      // Calculate cannonball variables
      Vector2 velocity = fireDirection * Attack.getSpeedModifier(Attack.Type.Cannon) * critModifier * projectileSpeed;
      float lobHeight = getCannonballApex(chargeAmount);
      float lifetime = getCannonballLifetime(chargeAmount) / (critModifier * projectileSpeed);

      // Setup cannonball
      newProjectile.initAbilityProjectile(this.netId, this.instanceId, Attack.ImpactMagnitude.Normal, abilityId, velocity, lobHeight, statusType: abilityStatus, statusDuration, lifetime: lifetime, isCrit: isCritical, attackType: attackType);
      newProjectile.setPlayFiringSound(playSound);

      ServerCannonBall cannonball = newProjectile as ServerCannonBall;
      if (cannonball != null) {
         // Add effectors to cannonball
         cannonball.addEffectors(PowerupManager.self.getEffectors(userId));
      }

      NetworkServer.Spawn(newProjectile.gameObject);
   }

   [Server]
   public void fireSpecialCannonBallAtTarget (Vector2 startPosition, Vector2 endPosition, float lifetime, bool playSound, int abilityId, float disableColliderFor = 0.0f) {
      // Create the cannon ball object from the prefab
      ServerCannonBall netBall = Instantiate(PrefabsManager.self.serverCannonBallPrefab, transform.position, Quaternion.identity);

      ShipAbilityData shipAbilityData = ShipAbilityManager.self.getAbility(abilityId);
      Status.Type abilityStatus = Status.Type.None;
      if (shipAbilityData != null) {
         abilityStatus = (Status.Type) shipAbilityData.statusType;
      }

      // Calculate cannonball variables
      Vector2 toEndPos = endPosition - startPosition;
      float dist = toEndPos.magnitude;
      float speed = dist / lifetime;
      float lobHeight = Mathf.Clamp(1.0f / speed, 0.3f, 1.0f);
      Vector2 velocity = speed * toEndPos.normalized;

      // Setup cannonball
      netBall.initAbilityProjectile(this.netId, this.instanceId, Attack.ImpactMagnitude.Normal, abilityId, velocity, lobHeight, statusType: abilityStatus, lifetime: lifetime, disableColliderFor: disableColliderFor);
      netBall.setPlayFiringSound(playSound);

      netBall.addEffectors(PowerupManager.self.getEffectors(userId));

      NetworkServer.Spawn(netBall.gameObject);
   }

   public override void setDataFromUserInfo (UserInfo userInfo, Item armor, Item weapon, Item hat, Item ring, Item necklace, Item trinket, ShipInfo shipInfo, GuildInfo guildInfo, GuildRankInfo guildRankInfo) {
      base.setDataFromUserInfo(userInfo, armor, weapon, hat, ring, necklace, trinket, shipInfo, guildInfo, guildRankInfo);

      // Ship stuff
      shipId = shipInfo.shipId;
      shipXmlId = shipInfo.shipXmlId;

      initialize(shipInfo);

      // Store the equipped items characteristics
      WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(weapon.itemTypeId);
      HatStatData hatData = EquipmentXMLManager.self.getHatData(hat.itemTypeId);
      ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(armor.itemTypeId);

      weaponType = weaponData == null ? 0 : weaponData.weaponType;
      armorType = armorData == null ? 0 : armorData.armorType;
      hatType = hatData == null ? 0 : hatData.hatType;

      hatColors = hat != null ? hat.paletteNames : hatData.palettes;
      weaponColors = weapon != null ? weapon.paletteNames : weaponData.palettes;
      armorColors = armor != null ? armor.paletteNames : armorData.palettes;

      weaponCount = weapon != null ? weapon.count : 1;
   }

   public override Armor getArmorCharacteristics () {
      ArmorStatData armorData = EquipmentXMLManager.self.armorStatList.Find(_ => _.armorType == armorType);

      if (armorData == null) {
         return new Armor();
      }

      return new Armor(0, armorData.sqlId, armorColors);
   }

   public override Weapon getWeaponCharacteristics () {
      WeaponStatData weaponData = EquipmentXMLManager.self.weaponStatList.Find(_ => _.weaponType == weaponType);

      if (weaponData == null) {
         return new Weapon();
      }

      return new Weapon(0, weaponData.sqlId, weaponColors);
   }

   public override Hat getHatCharacteristics () {
      HatStatData hatData = EquipmentXMLManager.self.hatStatList.Find(_ => _.hatType == hatType);

      if (hatData == null) {
         return new Hat();
      }

      return new Hat(0, hatData.sqlId, hatColors);
   }

   protected void processMovementAudio () {
      if (!Util.isBatch() && isLocalPlayer && isClient) {
         SoundEffectManager.ShipSailingType sailingType = SoundEffectManager.ShipSailingType.Stopped;

         if (Mathf.Abs(_movementInputDirection.x) > 0.1f || Mathf.Abs(_movementInputDirection.y) > 0.1f) {
            sailingType = SoundEffectManager.ShipSailingType.Movement;
         }

         SoundEffectManager.self.playShipSailingSfx(sailingType, this.transform, this._body);
      }
   }

   public float getAngleChangeSpeed () {
      switch (this.shipType) {
         case Ship.Type.Type_1:
            return 17f;
         case Ship.Type.Type_2:
            return 15f;
         case Ship.Type.Type_3:
            return 13f;
         case Ship.Type.Type_4:
            return 11f;
         case Ship.Type.Type_5:
            return 9f;
         case Ship.Type.Type_6:
            return 7f;
         case Ship.Type.Type_7:
            return 6f;
         case Ship.Type.Type_8:
            return 5f;
         default:
            return 10f;
      }
   }

   protected override void handleServerAuthoritativeMode () {
      // Make note of the time
      _lastMoveChangeTime = NetworkTime.time;

      Vector2 inputVector = InputManager.getMovementInput();

      if (inputVector != _movementInputDirection || (isSpeedingUp != InputManager.self.inputMaster.Sea.Dash.WasPressedThisFrame() && isSpeedingUp != (gamePadDashPressed == true))) {
         if (Util.isBatch() && Util.isAutoMove()) {
            D.adminLog("1) This unity is handling authoritative mode {" + userId + "}{" + entityName + "} {" + _lastMoveChangeTime.ToString("f2") + "}", D.ADMIN_LOG_TYPE.Simulation_Sea);
         }

         // If the ship wasn't moving, apply a small force locally to make up for delay
         if (inputVector != Vector2.zero && _body.velocity.sqrMagnitude < 0.025f) {
            if (Util.isBatch() && Util.isAutoMove()) {
               var force = Quaternion.AngleAxis(this.desiredAngle, Vector3.forward) * Vector3.up * getMoveSpeed() * CLIENT_SIDE_FORCE;
               D.adminLog("2) This unity is handling authoritative mode {" + userId + "}{" + entityName + "}" +
                  " LMC:{" + _lastMoveChangeTime.ToString("f2") + "} F:{" + force.ToString("f2") + "}", D.ADMIN_LOG_TYPE.Simulation_Sea);
            }
            _body.AddForce(Quaternion.AngleAxis(this.desiredAngle, Vector3.forward) * Vector3.up * getMoveSpeed() * CLIENT_SIDE_FORCE);
         }

         if (NetworkTime.time - _lastInputChangeTime > getInputDelay()) {
            // In Host mode only, we want to avoid setting _lastInputChangeTime here so cooldown validation passes in Cmd_RequestServerAddForce()
            if (!Util.isHost()) {
               _lastInputChangeTime = NetworkTime.time;
            }

            _movementInputDirection = inputVector;
            D.adminLog("3) This unity is handling authoritative mode, requesting move to server {" + userId + "}{" + entityName + "}" +
               " LMC:{" + _lastMoveChangeTime.ToString("f2") + "} MID:{" + _movementInputDirection + "}", D.ADMIN_LOG_TYPE.Simulation_Sea);
            Cmd_RequestServerAddMovementForce(inputVector, isSpeedingUp);
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.MoveShip);
         }
      }
   }

   [Command]
   protected void Cmd_RequestServerAddMovementForce (Vector2 direction, bool isSpeedingUp) {
      if (NetworkTime.time - _lastInputChangeTime > getInputDelay()) {
         Vector2 forceToApply = direction;

         if (direction != Vector2.zero) {
            float newAngle = Util.AngleBetween(Vector2.up, direction);
            desiredAngle = newAngle;
            facing = DirectionUtil.getDirectionForAngle(desiredAngle);
         }

         _movementInputDirection = forceToApply.normalized;
         D.adminLog("CMD: Server is now altering move input direction: " + _movementInputDirection + " for user {" + userId + "}{" + entityName + "} ", D.ADMIN_LOG_TYPE.Simulation_Sea);
         _lastInputChangeTime = NetworkTime.time;
         this.isSpeedingUp = isSpeedingUp;
         hasPerformedFirstActionAfterSpawn = true;
      }
   }

   [Command]
   protected void Cmd_RequestServerAddBoostForce (Vector2 direction, float chargeAmount) {
      if (isBoostCoolingDown()) {
         return;
      }

      bool isStunned = StatusManager.self.hasStatus(netId, Status.Type.Stunned);

      bool isWellTimed = (chargeAmount > 0.8f && chargeAmount < 0.99f);

      // If stunned, boost won't work unless your dash was well-timed
      if (isStunned && !isWellTimed) {
         return;
      }

      // A well-timed boost gives you the force of a fully-charged boost + 50%
      float finalBoostForce = (isWellTimed) ? boostForce * 1.5f : boostForce * chargeAmount;

      _body.AddForce(direction.normalized * finalBoostForce, ForceMode2D.Impulse);
      _lastBoostTime = NetworkTime.time;

      heldPvpCaptureTarget?.onPlayerBoosted(this);

      Rpc_NoteBoost();

      hasPerformedFirstActionAfterSpawn = true;
   }

   protected override void updateMassAndDrag (bool increasedMass) {
      // If we're not using the increased mass mode, then just let the child class handle it
      if (!increasedMass) {
         base.updateMassAndDrag(increasedMass);
      }

      float mass = 1f;
      float drag = 50f;

      // Customize the settings for the different ship types
      switch (this.shipType) {
         case Ship.Type.Type_1:
            mass = 1f;
            drag = 50f;
            break;
         case Ship.Type.Type_2:
            mass = 2f;
            drag = 32f;
            break;
         case Ship.Type.Type_3:
            mass = 4f;
            drag = 19f;
            break;
         case Ship.Type.Type_4:
            mass = 8f;
            drag = 6.25f;
            break;
         case Ship.Type.Type_5:
            mass = 16f;
            drag = 3.125f;
            break;
         case Ship.Type.Type_6:
            mass = 32f;
            drag = 1.5f;
            break;
         case Ship.Type.Type_7:
            mass = 64f;
            drag = .9f;
            break;
         case Ship.Type.Type_8:
            mass = 100f;
            drag = .5f;
            break;
      }

      // Apply the settings
      _body.mass = mass;
      _body.drag = drag;
      _body.angularDrag = 0f;
   }

   public override void setAreaParent (Area area, bool worldPositionStays) {
      this.transform.SetParent(area.userParent, worldPositionStays);

      if (!isServer) {
         return;
      }

      // Ensure the player's ship is always on water
      if (WorldMapManager.self.isWorldMapArea(area.areaKey)) {
         if (area.isOpenWaterTile(transform.position)) {
            return;
         }

         Vector3[] surroundingTiles = Util.createGridAroundPoint(transform.position, new Vector3(0.2f, 0.2f, 0.2f), new Vector3(8, 8, 1));
         Vector3[] waterTiles = surroundingTiles.Where(area.isOpenWaterTile).ToArray();
         Vector3 nearestWaterTile = Util.getNearestPoint(transform.position, waterTiles);
         transform.position = nearestWaterTile;
         return;
      }

      // Teleport ship to spawn position in case if it blocks on collider after spawning in new area
      Vector2 nearestSpawnPos = transform.position;
      float minDistance = float.MaxValue;

      foreach (SpawnManager.SpawnData spawn in SpawnManager.self.getAllSpawnsInArea(area.areaKey)) {
         if (Vector2.Distance(spawn.localPosition, transform.localPosition) < minDistance) {
            nearestSpawnPos = (Vector2) area.transform.position + spawn.localPosition;
            minDistance = Vector2.Distance(spawn.localPosition, transform.localPosition);
         }
      }

      int layerMask = LayerMask.GetMask(LayerUtil.GRID_COLLIDERS);
      RaycastHit2D[] hits = Physics2D.LinecastAll(transform.position, nearestSpawnPos, layerMask);
      if (hits.Length > 0) {
         transform.position = nearestSpawnPos;
      }
   }

   public override bool isAdversaryInPveInstance (NetEntity otherEntity) {
      // Check if the entities are in different voyage groups and in a PvE instance
      Instance instance = getInstance();
      if (instance != null && !instance.isPvP && otherEntity is PlayerShipEntity
         && this.voyageGroupId != otherEntity.voyageGroupId) {
         return true;
      } else {
         return false;
      }
   }

   public override bool canBeAttackedByPlayers () {
      Instance instance = getInstance();
      if (instance != null && instance.isPvP && !hasEnteredPvP) {
         return false;
      } else {
         return true;
      }
   }

   public override bool isPlayerShip () {
      return true;
   }

   private void setFlag (Flag flag, bool force = false) {
      if (_currentFlag == flag && !force) {
         return;
      }

      switch (flag) {
         case Flag.None:
            spritesContainer.GetComponent<RecoloredSprite>().recolor("");
            break;
         case Flag.White:
            spritesContainer.GetComponent<RecoloredSprite>().recolor(VoyageGroupManager.WHITE_FLAG_PALETTE);
            break;
         case Flag.Group:
            string flagPalette = VoyageGroupManager.getShipFlagPalette(voyageGroupId);
            spritesContainer.GetComponent<RecoloredSprite>().recolor(flagPalette);
            break;
         case Flag.Pvp:
            string shipPalette = PvpManager.getShipPaletteForTeam(pvpTeam);
            spritesContainer.GetComponent<RecoloredSprite>().recolor(shipPalette);

            // Return without updating _currentFlag if we don't have a pvp team, so it will be updated when we get one
            if (shipPalette == VoyageGroupManager.WHITE_FLAG_PALETTE) {
               return;
            }

            break;
         default:
            break;
      }

      _currentFlag = flag;
   }

   public bool isPerformingAttack () {
      return !_shouldUpdateTargeting;
   }

   public void requestRespawn () {
      Cmd_RequestRespawn();
   }

   protected IEnumerator CO_ApplyDamageAfterDelay (float delay, int damage, SeaEntity source, SeaEntity target, Attack.Type attackType) {
      // Wait until the cannon ball reaches the target
      yield return new WaitForSeconds(delay);

      // Apply the damage
      int finalDamage = applyDamage(damage, source.netId, attackType);
      target.Rpc_ShowExplosion(source.netId, target.transform.position, finalDamage, attackType, false);
   }

   protected override void onDamage (int damage) {
      rpc.assignVoyageRatingPoints(VoyageRatingManager.computeVoyageRatingPointsReward(VoyageRatingManager.RewardReason.DamageReceived));
   }

   public override void noteAttacker (NetEntity entity) {
      if (entity == null) {
         return;
      }
      base.noteAttacker(entity);

      // If we don't currently have a target selected, assign the attacker as our new target
      if (isLocalPlayer && !isDead() && _targetSelector != null && _targetSelector.getTarget() == null) {
         SelectionManager.self.setSelectedEntity((SeaEntity) entity);
      }
   }

   protected IEnumerator CO_TemporarilyDisableShip () {
      if (!isDisabled || isGhost) {
         yield break;
      }

      if (isServer) {
         setIsInvulnerable(true);
      }
      _clickableBox.gameObject.SetActive(false);
      hasPerformedFirstActionAfterSpawn = false;

      foreach (Collider2D c in GetComponents<Collider2D>()) {
         c.enabled = false;
      }

      // Do not disable sprites for auto move simulation
      if (!Util.isAutoMove()) {
         foreach (SpriteRenderer renderer in _renderers) {
            renderer.enabled = false;
         }
      }

      if (!Util.isStressTesting()) {
         while (isDisabled) {
            yield return null;
         }
      }

      _clickableBox.gameObject.SetActive(true);

      if (isDead()) {
         yield break;
      }

      foreach (Collider2D c in GetComponents<Collider2D>()) {
         c.enabled = true;
      }

      foreach (SpriteRenderer renderer in _renderers) {
         renderer.enabled = true;
      }
      // Force the character portrait to redraw
      playerPortrait.SetActive(false);
      playerPortrait.SetActive(true);

      yield return CO_HandleSpawnInvulnerability();
   }

   protected IEnumerator CO_HandleSpawnInvulnerability () {
      if (isServer) {
         setIsInvulnerable(true);
      }

      float spawnInvulnerabilityDuration = 0;
      if (VoyageManager.isAnyLeagueArea(areaKey)) {
         spawnInvulnerabilityDuration = SPAWN_INVULNERABILITY_DURATION;
      }

      if (!isInvisible && !isGhost) {
         Util.setAlphaInShader(gameObject, 0.6f);
      }

      // The ship is invulnerable until the player has made a first action, or the time runs out
      float time = Time.time;
      while (!hasPerformedFirstActionAfterSpawn && Time.time - time < spawnInvulnerabilityDuration) {
         yield return null;
      }

      if (isServer) {
         setIsInvulnerable(false);
      }

      if (!isInvisible && !isGhost) {
         Util.setAlphaInShader(gameObject, 1f);
      }
   }

   protected IEnumerator CO_AddForce (double timestamp, Vector2 force) {
      while (NetworkTime.time < timestamp) {
         yield return null;
      }

      _body.AddForce(force);
   }

   [Command]
   public void Cmd_ClearMovementInput () {
      clearMovementInput();
   }

   public void clearMovementInput () {
      _movementInputDirection = Vector2.zero;
   }

   public Vector2 getMovementInputDirection () {
      return _movementInputDirection;
   }

   [Command]
   private void Cmd_RequestRespawn () {
      if (VoyageManager.isWorldMapArea(areaKey)) {
         this.spawnInBiomeHomeTown();
      } else {
         this.spawnInNewMap(Area.STARTING_TOWN, Spawn.STARTING_SPAWN, Direction.North);
      }

      // Set the ship health back to max
      restoreMaxShipHealth();

      // Clear any powerup
      PowerupManager.self.clearPowerupsForUser(userId);
   }

   [Server]
   public void storeCurrentShipHealth () {
      Util.tryToRunInServerBackground(() => DB_Main.storeShipHealth(this.shipId, Mathf.Min(this.currentHealth, this.maxHealth)));
   }

   [Command]
   public void Cmd_SetLifeboatVisibility (bool shouldShow) {
      Rpc_SetLifeboatVisibility(shouldShow);

      if (shouldShow) {
         foreach (Collider2D col in colliderList) {
            col.enabled = true;
            if (col is CircleCollider2D) {
               ((CircleCollider2D) col).radius *= LIFEBOAT_COLLIDER_MULTIPLIER;
            }
         }

         StartCoroutine(CO_DisableLifeboatCollider());
      }
   }

   private IEnumerator CO_DisableLifeboatCollider () {
      yield return new WaitForSeconds(0.05f);

      foreach (Collider2D col in colliderList) {
         col.enabled = false;
         if (col is CircleCollider2D) {
            ((CircleCollider2D) col).radius /= LIFEBOAT_COLLIDER_MULTIPLIER;
         }
      }
   }

   [ClientRpc]
   public void Rpc_SetLifeboatVisibility (bool shouldShow) {
      setLifeboatVisibility(shouldShow);
   }

   public void setLifeboatVisibility (bool shouldShow) {
      lifeboat.SetActive(shouldShow);
      lifeboat.GetComponent<SpriteRenderer>().enabled = true;

      if (shouldShow) {
         lifeboat.transform.localScale = new Vector3(1.0f, 0.01f, 2.0f);
         lifeboat.transform.DOScaleY(1.0f, 0.5f).SetEase(Ease.OutElastic, 1.0f);
      }
   }

   private bool isBoostCoolingDown () {
      float timeSinceBoost = (float) (NetworkTime.time - _lastBoostTime);
      return (timeSinceBoost < boostCooldown);
   }

   private void checkAudioListener () {
      if (!AudioListenerManager.self.isPlayerFmodListenerActive()) {
         AudioListenerManager.self.setActiveListener();
      }
      //if (AudioListenerManager.self.getActiveListener() != _audioListener) {
      //   AudioListenerManager.self.setActiveListener(_audioListener);
      //}
   }

   private float getAttackDistance (float chargeAmount = -1.0f) {
      float cannonballDistance = getCannonballDistance(chargeAmount);

      ShipAbilityData abilityData = ShipAbilityManager.self.getAbility(getSelectedShipAbilityId());

      if (abilityData.selectedAttackType == Attack.Type.Mine) {
         cannonballDistance *= 0.5f;
      }

      return cannonballDistance;
   }

   private float getCannonballDistance (float chargeAmount = -1.0f) {
      if (chargeAmount < 0.0f) {
         chargeAmount = getCannonChargeAmount();
      }

      return (0.5f + (chargeAmount * 2.5f));
   }

   private float getCannonballLifetime (float chargeAmount = -1.0f) {
      if (chargeAmount < 0.0f) {
         chargeAmount = getCannonChargeAmount();
      }

      return (getAttackDistance(chargeAmount) / Attack.getSpeedModifier(Attack.Type.Cannon));
   }

   private float getCannonballApex (float chargeAmount = -1.0f) {
      if (chargeAmount < 0.0f) {
         chargeAmount = getCannonChargeAmount();
      }

      return chargeAmount / 4.0f;
   }

   private float getCannonChargeAmount () {
      // Returns the normalised charge amount of the cannon
      return Mathf.Clamp01((float) (NetworkTime.time - _cannonChargeStartTime) / CANNON_CHARGE_TIME);
   }

   private float getBoostChargeAmount () {
      // Returns the normalised charge amount of the boost
      return Mathf.Clamp01((float) (NetworkTime.time - _boostChargeStartTime) / BOOST_CHARGE_TIME);
   }

   [ClientRpc]
   private void Rpc_NoteBoost () {
      if (!isLocalPlayer) {
         // Play the ship boost release SFX
         FMOD.Studio.EventInstance boostEvent = FMODUnity.RuntimeManager.CreateInstance(SoundEffectManager.SHIP_LAUNCH_CHARGE);
         boostEvent.setParameterByName(SoundEffectManager.SHIP_CHARGE_RELEASE_PARAM, 2);
         FMODUnity.RuntimeManager.AttachInstanceToGameObject(boostEvent, this.transform, this._body);
         boostEvent.start();
         boostEvent.release();
         _lastBoostTime = NetworkTime.time;
      }
   }

   private void OnDisable () {
      // If we are the local player, activate the camera's audio listener
      if (isLocalPlayer && !ClientManager.isApplicationQuitting) {
         if (CameraManager.defaultCamera != null && CameraManager.defaultCamera.getFmodListener() != null) {
            AudioListenerManager.self.setActiveListener(CameraManager.defaultCamera.getFmodListener());
         } else {
            D.error("Couldn't switch audio listener back to main camera");
         }
      }

      if (isServer) {
         EntityLagMonitor.self.trackedShipEntities.Remove(this);
      }

      //if (isLocalPlayer && !ClientManager.isApplicationQuitting) {
      //   //if (!_audioListener) {
      //   //   _audioListener = GetComponent<AudioListener>();
      //   //}

      //   if (CameraManager.defaultCamera != null && CameraManager.defaultCamera.getAudioListener() != null) {
      //      AudioListenerManager.self.setActiveListener(CameraManager.defaultCamera.getAudioListener(), CameraManager.defaultCamera.getFmodListener());
      //   } else {
      //      D.error("Couldn't switch audio listener back to main camera");
      //   }
      //}
   }

   protected override void assignEntityName () {
      // Assign the ship name
      if (!Util.isEmpty(entityName)) {
         entityNameGO.SetActive(true);
         ShipBarsPlayer sbp = entityNameGO.GetComponentInParent<ShipBarsPlayer>();
         sbp.nameTextInside.text = this.entityName;
         sbp.nameTextOutside.text = this.entityName;
         sbp.nameTextInside.fontMaterial = new Material(sbp.nameTextInside.fontSharedMaterial);
         sbp.nameTextInside.fontMaterial.SetColor("_FaceColor", sbp.nameColor);
         sbp.nameTextInside.fontMaterial.SetColor("_OutlineColor", sbp.nameOutlineColor);
         sbp.nameTextInside.fontMaterial.SetFloat("_OutlineWidth", sbp.nameOutlineWidth);

         if (isLocalPlayer) {
            sbp.nameTextInside.fontMaterial.SetColor("_FaceColor", sbp.nameColorLocalPlayer);
            sbp.nameTextInside.fontMaterial.SetColor("_OutlineColor", sbp.nameOutlineColor);
         }

      }
   }

   protected override void onMaxHealthChanged (int oldValue, int newValue) {
      base.onMaxHealthChanged(oldValue, newValue);

      shipBars.initializeHealthBar();
   }

   [Command]
   public void Cmd_RespawnPlayerInInstance () {
      respawnPlayerInInstance();
   }

   [Server]
   public void respawnPlayerInInstance () {
      if (_respawningInInstanceCoroutine == null) {
         _respawningInInstanceCoroutine = StartCoroutine(CO_RespawnPlayerInInstance());
      }
   }

   [Server]
   private IEnumerator CO_RespawnPlayerInInstance () {
      D.adminLog("Respawning User {" + userId + "} in instance!", D.ADMIN_LOG_TYPE.Respawn);

      setIsInvulnerable(true);
      hasPerformedFirstActionAfterSpawn = false;
      PowerupManager.self.clearPowerupsForUser(userId);

      // Move the player to their spawn point
      Vector3 spawnPosition = transform.localPosition;

      PvpGame game = PvpManager.self.getGameWithPlayer(this);
      if (game != null) {
         spawnPosition = game.getSpawnPositionForUser(this);
      } else if (tryGetVoyage(out Voyage voyage) && voyage.isLeague) {
         spawnPosition = SpawnManager.self.getDefaultLocalPosition(areaKey);
      }

      transform.localPosition = spawnPosition;

      // Wait a small delay, so the player's camera can move, and see them respawn
      yield return new WaitForSeconds(0.5f);

      initHealth();
      restoreMaxShipHealth();

      Rpc_OnRespawnedInInstance(currentHealth);

      setIsInvulnerable(false);
      setCollisions(true);

      // Reset flag
      _hasRunOnDeath = false;

      yield return CO_HandleSpawnInvulnerability();

      _respawningInInstanceCoroutine = null;
   }

   [ClientRpc]
   public void Rpc_OnRespawnedInInstance (int newCurrentHealth) {
      if (_respawnCoroutine != null) {
         StopCoroutine(_respawnCoroutine);
      }

      _respawnCoroutine = StartCoroutine(CO_OnRespawnedInInstance(newCurrentHealth));
   }

   private IEnumerator CO_OnRespawnedInInstance (int newCurrentHealth) {
      currentHealth = newCurrentHealth;

      if (currentHealth <= 0) {
         D.error($"Received currentHealth is not valid. New health value: {currentHealth}");
      }

      // Show all the sprites
      foreach (SpriteRenderer renderer in _renderers) {
         renderer.enabled = true;
      }

      // Restart charging cannon
      _isChargingCannon = false;

      // Re-enable outline
      _outline.setVisibility(true);

      // Re-enable colliders
      setCollisions(true);

      // Raise the ship back up
      Util.setLocalY(spritesContainer.transform, 0.0f);

      // Allow this ship to play the destroyed sound and visual effect again
      _playedDestroySound = false;

      _isChargingBoost = false;

      // Reset this bool to ensure players can attack
      enableTargeting();

      // Clear powerup GUI
      if (isLocalPlayer) {
         PowerupPanel.self.clearSeaPowerups();
      }

      // Reset flag
      _hasRunOnDeath = false;

      yield return CO_HandleSpawnInvulnerability();
   }

   public void cancelCannonBarrage () {
      if (_cannonBarrageCoroutine != null) {
         StopCoroutine(_cannonBarrageCoroutine);
      }
   }

   protected override void autoMove () {
      if (Global.player == null || !isLocalPlayer) {
         return;
      }

      if (isDead()) {
         requestRespawn();
         return;
      }

      // Do not clear movement input if auto move simulation is active
      if (!Util.isGeneralInputAllowed() && !Util.isAutoMove()) {
         // Try to close any opened panel
         PanelManager.self.onEscapeKeyPressed();
         Cmd_ClearMovementInput();
         return;
      }

      // Choose an action
      int action = Random.Range(0, 4);

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
         default:
            break;
      }
   }

   public override void onDeath () {
      base.onDeath();

      if (NetworkServer.active) {
         // Process powerup expiry on death
         List<Powerup> userPowerups = PowerupManager.self.getPowerupsForUser(userId);
         userPowerups = getFilteredPowerup(userPowerups);
         setPowerups(userPowerups, true);

         // When user dies, update powerups on the client
         rpc.Target_UpdatePowerups(connectionToClient, userPowerups);
      }

      heldPvpCaptureTarget?.onPlayerDied(this);
   }

   private void updateCoinTrail () {
      // Enable / disable the coin trail effect, based on the status of the syncvar
      if (holdingPvpCaptureTarget && !coinTrailEffect.activeSelf) {
         coinTrailEffect.SetActive(true);
      } else if (!holdingPvpCaptureTarget && coinTrailEffect.activeSelf) {
         coinTrailEffect.SetActive(false);
      }
   }

   [Server]
   protected override int getRewardedXP () {
      return REWARDED_XP;
   }

   [ClientRpc]
   public void Target_UpdateSkin () {
      updateSkin(skinType);

      // Refresh flag
      setFlag(_currentFlag, force: true);
   }

   public override void showLevelUpEffect (Jobs.Type jobType) {
      base.showLevelUpEffect(jobType);

      if (levelUpEffect != null) {
         levelUpEffect.play(jobType);
      }
   }

   public void updateAbilityCooldownDurations () {
      _abilityCooldownDurations = new Dictionary<int, float>();

      for (int i = 0; i < shipAbilities.Count; i++) {
         ShipAbilityData shipAbilityData = ShipAbilityManager.self.getAbility(shipAbilities[i]);
         if (shipAbilityData != null) {
            if (!_abilityCooldownDurations.ContainsKey(shipAbilityData.abilityId)) {
               _abilityCooldownDurations.Add(shipAbilityData.abilityId, shipAbilityData.coolDown);
            }
         }
      }
   }

   protected int getSelectedShipAbilityId () {
      return shipAbilities[selectedShipAbilityIndex];
   }

   [TargetRpc]
   protected void Target_UpdateCannonPanel (NetworkConnection connectionToClient, int[] abilityIdArray) {
      for (int i = 0; i < CannonPanel.MAX_ABILITY_COUNT; i++) {
         CannonPanel.self.setAbilityIcon(i, -1);
         CannonPanel.self.cannonBoxList[i].abilityId = -1;
      }
      StartCoroutine(CO_UpdateCannonPanel(abilityIdArray));
   }

   private IEnumerator CO_UpdateCannonPanel (int[] abilityIdArray) {
      // Wait for synclist to be updated
      while (shipAbilities.Count <= 0) {
         yield return null;
      }
      ShipData shipData = ShipDataManager.self.getShipData(shipXmlId);
      string abilityNameData = "{" + (shipData != null ? shipData.shipName : "Null") + "} Ship Abilities: ";
      for (int abilityIndex = 0; abilityIndex < shipAbilities.Count; abilityIndex++) {
         int newShipAbilityId = abilityIdArray[abilityIndex];
         ShipAbilityData shipAbilityData = ShipAbilityManager.self.getAbility(newShipAbilityId);
         abilityNameData += "{" + abilityIndex + ":" + (shipAbilityData == null ? "Null" : shipAbilityData.abilityName) + "}";
         CannonPanel.self.setAbilityIcon(abilityIndex, newShipAbilityId);
         if (abilityIndex < CannonPanel.self.cannonBoxList.Count && abilityIndex >= 0) {
            CannonPanel.self.cannonBoxList[abilityIndex].abilityId = newShipAbilityId;
         } else {
            D.editorLog("Was trying to process invalid index {" + abilityIndex + "}", Color.yellow);
         }
      }
      D.editorLog(abilityNameData, Color.yellow);

      CannonPanel.self.updateCooldownDurations();
   }

   [Command]
   public void Cmd_AbilityUsed (int abilityIndex) {
      // Don't update UI if the ability we're trying to use is on cooldown
      if (isAbilityOnCooldown(abilityIndex)) {
         return;
      }

      Target_AbilityUsed(connectionToClient, abilityIndex);

      // Set cooldown for ability
      if (_abilityCooldownDurations.ContainsKey(shipAbilities[abilityIndex])) {
         _currentAbilityCooldowns[abilityIndex] += _abilityCooldownDurations[shipAbilities[abilityIndex]];
      }

      changeAttackOption(_currentAbilitySlotIndex);
   }

   [TargetRpc]
   public void Target_AbilityUsed (NetworkConnection connectionToClient, int abilityIndex) {
      CannonPanel.self.abilityUsed(abilityIndex);
   }

   protected void updateAbilityCooldowns () {
      if (!isServer) {
         return;
      }

      for (int i = 0; i < _currentAbilityCooldowns.Count; i++) {
         if (_currentAbilityCooldowns[i] == 0.0f) {
            continue;
         }

         _currentAbilityCooldowns[i] -= Time.deltaTime;

         if (_currentAbilityCooldowns[i] <= 0.0f) {
            _currentAbilityCooldowns[i] = 0.0f;
         }
      }
   }

   public bool isAbilityOnCooldown (int abilityIndex) {
      return _currentAbilityCooldowns[abilityIndex] > Mathf.Epsilon;
   }

   [Server]
   public void setPowerups (List<Powerup> powerups, bool isInitialize = false) {
      _powerups.Clear();

      List<Powerup> powerupToDiscard = new List<Powerup>();
      if (isInitialize) {
         // Take note of the powerups to discard
         foreach (Powerup powerup in powerups) {
            if (powerup.expiry == Powerup.Expiry.OnWarp) {
               powerupToDiscard.Add(powerup);
            }
         }

         // Discard powerups from this list and from powerup manager
         foreach (Powerup powerUp in powerupToDiscard) {
            powerups.Remove(powerUp);
            PowerupManager.self.removePowerupServer(userId, powerUp);
            rpc.Target_RemovePowerup(connectionToClient, powerUp);
         }
      }

      _powerups.AddRange(powerups);
   }

   private static CannonTargetingType getCannonTargetingTypeFromAttackType (Attack.Type attackType) {
      switch (attackType) {
         case Attack.Type.Cone_NoEffect:
            return CannonTargetingType.Cone;
         case Attack.Type.Circle_NoEffect:
            return CannonTargetingType.Circle;
         case Attack.Type.Mine:
         case Attack.Type.Harpoon:
            return CannonTargetingType.AbilityProjectile;
         case Attack.Type.Cannon:
         case Attack.Type.Standard_NoEffect:
         case Attack.Type.Standard_Slow:
         case Attack.Type.Standard_Stunned:
         case Attack.Type.Boulder:
         case Attack.Type.Mini_Boulder:
         case Attack.Type.Shock_Ball:
         case Attack.Type.Fire:
         case Attack.Type.Poison:
         case Attack.Type.Ice:
         case Attack.Type.Air:
         case Attack.Type.Venom:
            return CannonTargetingType.Normal;
         default:
            return CannonTargetingType.None;
      }
   }

   private void toggleShipDisplayInfo (bool show) {
      if (abilityCastEffectHolder != null && abilityCastEffectHolder.transform.childCount > 0) {
         shipInformationDisplay.alpha = NON_LOCAL_SHIP_INFO_DEFAULT_ALPHA;
      } else {
         shipInformationDisplay.alpha = show ? 1.0f : NON_LOCAL_SHIP_INFO_DEFAULT_ALPHA;
      }
   }

   private void OnCollisionEnter2D (Collision2D collision) {
      // Process only local player collisions
      if (!isLocalPlayer) {
         return;
      }

      EdgeCollider2D edgeCollider = null;
      MapEdges mapEdges = null;

      // Get the right collider
      if (collision.collider is EdgeCollider2D collider) {
         if (collider.gameObject.TryGetComponent(out mapEdges)) {
            edgeCollider = collider;
         }
      } else if (collision.otherCollider is EdgeCollider2D otherCollider) {
         if (otherCollider.gameObject.TryGetComponent(out mapEdges)) {
            edgeCollider = otherCollider;
         }
      }

      if (edgeCollider == null || mapEdges == null) {
         return;
      }

      onMapEdgeReached(mapEdges, edgeCollider);
   }

   private void onMapEdgeReached (MapEdges mapEdges, EdgeCollider2D edge) {
      if (Global.player == null) {
         return;
      }

      // Can't warp if the player is in combat
      if (Global.player.hasAttackers() && Global.player.isInCombat()) {
         int timeUntilCanLeave = (int) (IN_COMBAT_STATUS_DURATION - Global.player.getTimeSinceAttacked());
         ChatManager.self.addChat("Cannot warp until out of combat for " + (int) IN_COMBAT_STATUS_DURATION + " seconds. \n(" + timeUntilCanLeave + " seconds left)", ChatInfo.Type.System);
         return;
      }

      Direction direction = mapEdges.computeDirectionFromEdge(edge);

      if (WorldMapManager.self.getNextArea(areaKey, direction, out string nextAreaKey)) {
         // Prevent demo account from warping outside Forest and Desert biomes
         if (isDemoUser) {
            if (!AreaManager.self.tryGetAreaInfo(nextAreaKey, out Map nextMap)) {
               return;
            }

            if (nextMap.biome != Biome.Type.Forest && nextMap.biome != Biome.Type.Desert) {
               return;
            }
         }

         setupForWarpClient();
         Cmd_SpawnInNewMapSpawn(nextAreaKey, spawn: null, direction);
      }
   }

   public void trackSeaMine (SeaMine newSeaMine) {
      // First, check if any sea mines in our list have been destroyed
      for (int i = _seaMines.Count - 1; i >= 0; i--) {
         if (_seaMines[i] == null) {
            _seaMines.RemoveAt(i);
         }
      }

      _seaMines.Add(newSeaMine);

      int numExtraSeaMines = _seaMines.Count - SEA_MINE_LIMIT;

      // Destroy any sea mines over the limit, starting with the oldest ones (front of the list)
      for (int i = 0; i < numExtraSeaMines; i++) {
         SeaMine mine = _seaMines[i];

         // If the mine is exploding / exploded, don't count this one towards the limit
         if (mine.getMineState() == SeaMine.MineState.Triggered || mine.getMineState() == SeaMine.MineState.Exploded) {
            continue;
            // Otherwise, destroy the extra mine
         } else {
            NetworkServer.Destroy(mine.gameObject);
         }
      }
   }

   public override void toggleWarpInProgressEffect (bool show) {
      // Toggle only if the new state is different from the current state
      if (warpInProgressEffect == null) {
         return;
      }

      if (warpInProgressEffect.isShowing() != show) {
         warpInProgressEffect.toggle(show);

         if (show) {
            if (!warpInProgressEffect.isPlaying()) {
               warpInProgressEffect.play();
            }
         } else {
            if (warpInProgressEffect.isPlaying()) {
               warpInProgressEffect.stop();
            }
         }
      }
   }

   [Command]
   public void Cmd_SetIsReelingIn (bool newValue) {
      isReelingIn = newValue;

      if (newValue) {
         reelInStartTime = (float) NetworkTime.time;
      }
   }

   public bool getIsChargingCannon () {
      return _isChargingCannon;
   }

   #region Private Variables

   // Our ship movement sound
   //protected AudioSource _movementAudioSource = default;

   // Our target selector
   protected PlayerTargetSelector _targetSelector = default;

   // The position the player is currently aiming at
   private Vector2 _currentAimPosition = default;

   // The desired direction of the ship
   private Vector2 _movementInputDirection = default;

   // The velocity at which we're currently damping the velocity of the ship
   private Vector2 _shipDampVelocity = default;

   // A multiplier for the force added locally in order to mask delay   
   private const float CLIENT_SIDE_FORCE = 0.1f;

   // When the player started charging their cannon
   private double _cannonChargeStartTime = -3.0f;

   // Is the player currently charging up a cannon attack
   private bool _isChargingCannon = false;

   // If the targeting indicators should update with the player's inputs
   private bool _shouldUpdateTargeting = true;

   // A reference to the object used to target circular player attacks
   private TargetCircle _targetCircle = default;

   // A reference to the object used to target conical player attacks
   private TargetCone _targetCone = default;

   // A reference to the object used to target the player's regular cannon attack
   private CannonTargeter _cannonTargeter = default;

   // The current flag being displayed by the ship
   private Flag _currentFlag = Flag.None;

   // When the player last boosted
   private double _lastBoostTime = -3.0f;

   // When the player began charging their boost
   private double _boostChargeStartTime = -3.0f;

   // Whether the player is charging a shot with the mouse or keyboard
   private bool _chargingWithMouse = false;

   // A reference to the audio listener that follows the ship
   //private AudioListener _audioListener = default;

   // A reference to the FMOD Studio Listener that follows the ship
   private FMODUnity.StudioListener _fmodListener = default;

   // How long it takes to charge up the ship's cannon
   private const float CANNON_CHARGE_TIME = 1.0f;

   // How long is takes to charge up the ship's boost
   private const float BOOST_CHARGE_TIME = 0.5f;

   // How long the boost wake effects show for after boosting
   private const float BOOST_WAKE_TIME = 1.0f;

   // Set to true when the player is charging their boost
   private bool _isChargingBoost = false;

   // A reference to a coroutine responsible for respawning the player in their instance
   private Coroutine _respawningInInstanceCoroutine = null;

   // FMOD event instance for managing ship's boost SFX
   FMOD.Studio.EventInstance _boostState = default;

   // How much lifeboat collider size should be increased for a short period of time to make sure that boat is not near shore
   private const float LIFEBOAT_COLLIDER_MULTIPLIER = 3.0f;

   // A reference to the coroutine responsible for respawning this player in the same instance
   private Coroutine _respawnCoroutine = null;

   // A reference to the coroutine responsible for firing the cannon barrage ability
   private Coroutine _cannonBarrageCoroutine = null;

   // The current cooldown for each of the player's abilities
   private List<float> _currentAbilityCooldowns = new List<float>() { 0.0f, 0.0f, 0.0f, 0.0f, 0.0f };

   // How long a cooldown each ability will have after casting it
   private Dictionary<int, float> _abilityCooldownDurations = new Dictionary<int, float>();

   // The default alpha for the ship info of non-local ships
   private const float NON_LOCAL_SHIP_INFO_DEFAULT_ALPHA = 0.2f;

   // The maximum number of seconds player ships are invulnerable after spawning
   private const float SPAWN_INVULNERABILITY_DURATION = 20;

   // Current ability index
   private int _currentAbilitySlotIndex = 0;

   // A list of references to any active sea mines
   private List<SeaMine> _seaMines = new List<SeaMine>();

   // A reference to the player's heal effect
   private Animator _healEffect;

   // The maximum number of active sea mines the player can have at a time
   private const int SEA_MINE_LIMIT = 4;

   // Trigger parameter for the heal animation
   private const string SHOW_HEAL = "Show";

   #endregion
}
