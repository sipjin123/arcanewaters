using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class PlayerShipEntity : ShipEntity, IPointerEnterHandler, IPointerExitHandler
{
   #region Public Variables

   // The ID of this ship in the database
   [SyncVar]
   public int shipId;

   // Gets set to true when the next shot is scheduled
   public bool isNextShotDefined = false;

   // The coordinates of the next shot
   public Vector2 nextShotTarget = new Vector2(0, 0);

   // Ability Reference
   public List<int> shipAbilities = new List<int>();

   // The equipped weapon characteristics
   [SyncVar]
   public int weaponType = 0;
   [SyncVar]
   public string weaponColors;

   // The status effect that the player's cannonballs will apply to targets hit
   [SyncVar]
   public Status.Type cannonEffectType = Status.Type.None;

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

   // True when the local player is aiming   
   public bool isAiming;

   // The effect that indicates this ship is speeding up
   public SpriteRenderer[] speedUpEffectHolders;
   public Canvas speedupGUI;
   public Image speedUpBar;

   // Color indications if the fuel is usable or not
   public Color recoveringColor, defaultColor;

   // Speedup variables
   public float speedMeter = 10;
   public static float SPEEDUP_METER_MAX = 10;
   public bool isReadyToSpeedup = true;
   public float boostDepleteValue = 2;
   public float boostRecoverValue = 1.6f;

   // Gets set to true when the player ship is hidden and cannot be damaged or controlled
   [SyncVar]
   public bool isDisabled = true;

   // The object icon indicating the shoop boost cooling down
   public GameObject shipBoostCooldownObj;

   // The icon indicating the shoop boost cooling down
   public Image shipBoostCooldownIcon;

   // List of icon sprites of the cooldown icon
   public Sprite[] cooldownIconSprites;

   // The anim reference indicating ship boost meter is full
   public Animator maxBoostEffectAnimation;

   // Reference to the sprite swap
   public SpriteSwap shipBoostSpriteSwapFront, shipBoostSpriteSwapBack;

   // The different flags the ship can display
   public enum Flag
   {
      None = 0,
      White = 1,
      Group = 2
   }

   #endregion

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
         GetComponent<PlayerShipEntity>().hideGuildIcon();
      }
      hideEntityName();
   }

   protected override bool isBot () { return false; }

   protected override void Start () {
      base.Start();

      // Player ships spawn hidden and invulnerable, until the client finishes loading the area
      if (isDisabled) {
         StartCoroutine(CO_TemporarilyDisableShip());
      } else {
         foreach (SpriteRenderer spriteRender in speedUpEffectHolders) {
            spriteRender.enabled = false;
         }
      }

      if (isLocalPlayer) {
         // Create a ship movement sound for our own ship
         _movementAudioSource = SoundManager.createLoopedAudio(SoundManager.Type.Ship_Movement, this.transform);
         _movementAudioSource.gameObject.AddComponent<MatchCameraZ>();
         _movementAudioSource.volume = 0f;

         // Notify UI panel to display the current skills this ship has
         rpc.Cmd_RequestShipAbilities(shipId);
         Cmd_RequestAbilityList();

         _targetSelector = GetComponentInChildren<PlayerTargetSelector>();
      } else if (isServer) {
         _movementInputDirection = Vector2.zero;
      } else {
         // Disable our collider if we are not either the local player or the server
         getMainCollider().isTrigger = true;
      }

      // Create targeting objects
      GameObject targetCirclePrefab = Resources.Load<GameObject>("Prefabs/Targeting/TargetCircle");
      GameObject targetConePrefab = Resources.Load<GameObject>("Prefabs/Targeting/TargetConeDots");

      _targetCircle = Instantiate(targetCirclePrefab, transform.parent).GetComponent<TargetCircle>();
      _targetCone = Instantiate(targetConePrefab, transform.parent).GetComponent<TargetCone>();

      _targetCircle.gameObject.SetActive(false);
      _targetCone.gameObject.SetActive(false);
   }

   protected override void updateSprites () {
      base.updateSprites();

      shipBoostSpriteSwapFront.newTexture = _shipBoostSpritesFront;
      shipBoostSpriteSwapBack.newTexture = _shipBoostSpritesBack;
   }

   protected override void Update () {
      base.Update();

      if (isDead()) {
         _targetCone.gameObject.SetActive(false);
         _targetCircle.gameObject.SetActive(false);
      }

      // Recolor the ship flag if needed
      if (isClient) {
         Instance instance = getInstance();
         if (VoyageGroupManager.isInGroup(this) && instance != null && instance.isVoyage) {
            if (instance.isPvP) {
               // In PvP instances, the flag is white until the player enters PvP
               if (hasEnteredPvP) {
                  setFlag(Flag.Group);
               } else {
                  setFlag(Flag.White);
               }
            } else {
               // In PvE instances, we always set the group flag color
               setFlag(Flag.Group);
            }
         }
      }

      if (!isLocalPlayer) {
         // Display the ship boost meter for remote players based on velocity
         if (getVelocity().magnitude > NETWORK_SHIP_SPEEDUP_MAGNITUDE) {
            updateSpeedUpDisplay(0, true, false);
         } else {
            updateSpeedUpDisplay(0, false, false);
         }

         return;
      }

      // Adjust the volume on our movement audio source
      adjustMovementAudio();

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
      if (!Util.isGeneralInputAllowed() || isDisabled) {
         return;
      }

      if (InputManager.isLeftClickKeyPressed() && !PanelManager.self.hasPanelInLinkedList()) {
         NetEntity ship = getClickedBody();
         if (ship != null && ship is PlayerShipEntity) {
            PanelManager.self.contextMenuPanel.showDefaultMenuForUser(ship.userId, ship.entityName);
         }
      }

      if (!isDead() && SeaManager.getAttackType() != Attack.Type.Air) {
         SeaEntity target = _targetSelector.getTarget();
         if (target != null && hasReloaded() && InputManager.isFireCannonKeyDown()) {
            Cmd_FireMainCannonAtTarget(target.gameObject, Vector2.zero, true, false, -1.0f, -1.0f, true);
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.FireShipCannon);
         }

         if (InputManager.isFireCannonMouseDown()) {
            cannonAttackPressed();
         }

         if (InputManager.isFireCannonMouseUp()) {
            cannonAttackReleased();
         }

         if (Input.GetKey(KeyCode.X) && Input.GetKeyDown(KeyCode.F10)) {
            _cannonAttackType = (CannonAttackType)(((int)_cannonAttackType + 1) % 3);
            ChatPanel.self.addChatInfo(new ChatInfo(0, "Changed ship attack type to: " + _cannonAttackType.ToString(), System.DateTime.Now, ChatInfo.Type.System));
         }

         if (Input.GetKey(KeyCode.X) && Input.GetKeyDown(KeyCode.F11)) {
            Status.Type newStatusType = (Status.Type) (((int) cannonEffectType + 1) % System.Enum.GetValues(typeof(Status.Type)).Length);
            Cmd_ChangeCannonEffectType();
         }
      }

      // Speed ship boost feature
      if (!isSpeedingUp) {
         if (isReadyToSpeedup && InputManager.isSpeedUpKeyPressed() && getVelocity().magnitude >= SHIP_SPEEDUP_MAGNITUDE) {
            isSpeedingUp = true;

            // Let the server and other clients know we started speeding up
            updateSpeedUpDisplay(speedMeter, true, false);
         }

         if (speedMeter < SPEEDUP_METER_MAX) {
            speedMeter += Time.deltaTime * boostRecoverValue;

            shipBoostCooldownObj.SetActive(true);
            shipBoostCooldownIcon.sprite = cooldownIconSprites[(int) speedMeter];

            if (speedMeter >= SPEEDUP_METER_MAX) {
               maxBoostEffectAnimation.Play("Trigger");
            }
         } else {
            shipBoostCooldownObj.SetActive(false);
            isReadyToSpeedup = true;
         }
      } else {
         if (InputManager.isSpeedUpKeyReleased() || (InputManager.isSpeedUpKeyPressed() && getVelocity().magnitude < SHIP_SLOWDOWN_MAGNITUDE)) {
            isSpeedingUp = false;
            updateSpeedUpDisplay(speedMeter, false, false);

            // Trigger the tutorial
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.ShipSpeedUp);
         }

         if (speedMeter > 0) {
            speedMeter -= Time.deltaTime * boostDepleteValue;

            shipBoostCooldownObj.SetActive(true);
            shipBoostCooldownIcon.sprite = cooldownIconSprites[(int) speedMeter];
         } else {
            shipBoostCooldownObj.SetActive(false);
            isReadyToSpeedup = false;
            isSpeedingUp = false;
            updateSpeedUpDisplay(speedMeter, false, false);

            // Trigger the tutorial
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.ShipSpeedUp);
         }
      }
   }

   private void LateUpdate () {
      // Update targeting UI
      if (_isChargingCannon) {
         _cannonChargeAmount = Mathf.Clamp01(_cannonChargeAmount + Time.deltaTime);
         updateTargeting();
      }
   }

   protected override void FixedUpdate () {
      base.FixedUpdate();

      if (NetworkServer.active) {
         // If the player wants to stop the ship, we let the linear drag handle the slowdown
         if (!isDead() && _movementInputDirection != Vector2.zero) {
            Vector2 targetVelocity = _movementInputDirection * getMoveSpeed() * Time.fixedDeltaTime;
            _body.velocity = Vector2.SmoothDamp(_body.velocity, targetVelocity, ref _shipDampVelocity, 0.5f);
         }
      }
   }

   private void cannonAttackPressed () {
      if (!hasReloaded() || isPerformingAttack()) {
         return;
      }

      switch (_cannonAttackType) {
         case CannonAttackType.Normal:
            Cmd_FireMainCannonAtTarget(null, Util.getMousePos(), true, false, -1.0f, -1.0f, true);
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.FireShipCannon);
            break;
         case CannonAttackType.Cone:
            _isChargingCannon = true;
            _targetCone.gameObject.SetActive(true);
            updateTargeting();
            _targetCone.updateCone(true);
            break;
         case CannonAttackType.Circle:
            _isChargingCannon = true;
            _targetCircle.gameObject.SetActive(true);
            updateTargeting();
            _targetCircle.updateCircle(true);
            break;
      }
   }

   private void cannonAttackReleased () {
      if (!_isChargingCannon || isPerformingAttack()) {
         return;
      }

      switch (_cannonAttackType) {
         case CannonAttackType.Cone:
            Vector2 toMouse = Util.getMousePos() - transform.position;
            Vector2 pos = transform.position;
            float rotAngle = (40.0f - (_cannonChargeAmount * 25.0f)) / 2.0f;

            float cannonballDist = 1.5f + (_cannonChargeAmount * 1.5f);
            float cannonballLifetime = cannonballDist / Attack.getSpeedModifier(Attack.Type.Cannon);

            // Fire cone of cannonballs
            Cmd_FireMainCannonAtTarget(null, pos + ExtensionsUtil.Rotate(toMouse, rotAngle), false, false, cannonballLifetime, -1.0f, true);
            Cmd_FireMainCannonAtTarget(null, Util.getMousePos(), false, false, cannonballLifetime, -1.0f, false);
            Cmd_FireMainCannonAtTarget(null, pos + ExtensionsUtil.Rotate(toMouse, -rotAngle), false, false, cannonballLifetime, -1.0f, false);

            _shouldUpdateTargeting = false;
            _targetCone.targetingConfirmed(() => _shouldUpdateTargeting = true);

            break;
         case CannonAttackType.Circle:
            float circleRadius = (0.625f - (_cannonChargeAmount * 0.125f));
            _shouldUpdateTargeting = false;
            _targetCircle.targetingConfirmed(() => {
               _shouldUpdateTargeting = true;
            });

            StartCoroutine(CO_CannonBarrage(Util.getMousePos(), circleRadius));
            _targetCircle.setFillColor(Color.white);
            _targetCircle.updateCircle(true);

            break;
      }

      _isChargingCannon = false;
      _cannonChargeAmount = 0.0f;
   }

   private void updateTargeting () {
      if (!_shouldUpdateTargeting || isDead()) {
         return;
      }

      switch (_cannonAttackType) {
         case CannonAttackType.Cone:
            _targetCone.coneHalfAngle = (40.0f - (_cannonChargeAmount * 25.0f)) / 2.0f;
            _targetCone.coneOuterRadius = 1.5f + (_cannonChargeAmount * 1.5f);
            _targetCone.transform.position = transform.position;
            _targetCone.updateCone(true);

            // Check for enemies inside cone
            Collider2D[] coneHits = Physics2D.OverlapCircleAll(transform.position, _targetCone.coneOuterRadius, LayerMask.GetMask(LayerUtil.SHIPS));
            bool enemyInCone = false;
            float middleAngle = Util.angle(Util.getMousePos() - transform.position);

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
         case CannonAttackType.Circle:
            float circleRadius = (0.625f - (_cannonChargeAmount * 0.125f));
            _targetCircle.scaleCircle(circleRadius * 2.0f);
            _targetCircle.updateCircle(true);

            // Check for enemies inside circle
            Collider2D[] circleHits = Physics2D.OverlapCircleAll(Util.getMousePos(), circleRadius, LayerMask.GetMask(LayerUtil.SHIPS));
            bool enemyInCircle = false;
            foreach(Collider2D hit in circleHits) {
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

      for (int i = 0; i < 10; i++) {
         Vector3 endPos = targetPosition + Random.insideUnitSphere * radius;

         // Calculate lifetime to hit end point
         Vector3 toEndPos = endPos - transform.position;
         toEndPos.z = 0.0f;
         float dist = toEndPos.magnitude;

         lifetime = Mathf.Lerp(2.0f, 3.0f, dist / 5.0f);
         float speed = dist / lifetime;

         Cmd_FireMainCannonAtTarget(null, endPos, false, true, lifetime, speed, true);
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
   protected void Cmd_FireMainCannonAtTarget (GameObject target, Vector2 requestedTargetPoint, bool checkReload, bool isLobbed, float lifetime, float speedOverride, bool playSound) {
      if (isDead() || (checkReload && !hasReloaded())) {
         return;
      }

      Rpc_NoteAttack();

      _lastAttackTime = NetworkTime.time;
      
      Vector2 startPosition = transform.position;
      Vector2 targetPosition = (target == null) ? requestedTargetPoint : (Vector2) target.transform.position;

      // Firing the cannon is considered a PvP action
      hasEnteredPvP = true;
	  
      fireCannonBallAtTarget(startPosition, targetPosition, isLobbed, lifetime, speedOverride, playSound);
   }

   [Command]
   private void Cmd_RequestAbilityList () {
      Target_ReceiveAbilityList(connectionToClient, shipAbilities.ToArray());
   }

   [TargetRpc]
   public void Target_ReceiveAbilityList (NetworkConnection connection, int[] abilityIds) {
      shipAbilities = new List<int>(abilityIds);
   }

   [Command]
   private void Cmd_ChangeCannonEffectType () {
      cannonEffectType = (Status.Type) (((int) cannonEffectType + 1) % System.Enum.GetValues(typeof(Status.Type)).Length);
      Target_NotifyCannonEffectChange(connectionToClient, (int) cannonEffectType);
   }

   [TargetRpc]
   public void Target_NotifyCannonEffectChange (NetworkConnection connection, int newStatusEffect) {
      ChatPanel.self.addChatInfo(new ChatInfo(0, "Changed status type to: " + ((Status.Type)newStatusEffect).ToString(), System.DateTime.Now, ChatInfo.Type.System));
   }

   private void updateSpeedUpDisplay (float meter, bool isOn, bool isReadySpeedup) {
      // Handle GUI
      if (isLocalPlayer) {
         if (meter < SPEEDUP_METER_MAX) {
            speedupGUI.enabled = true;
            speedUpBar.fillAmount = meter / SPEEDUP_METER_MAX;
         } 
         speedUpBar.color = isReadySpeedup ? defaultColor : recoveringColor;
      } else {
         speedupGUI.enabled = false;
      }

      // Handle sprite effects
      foreach (SpriteRenderer spriteRender in speedUpEffectHolders) {
         spriteRender.enabled = isOn;
      }
   }

   protected override void OnDestroy () {
      base.OnDestroy();

      if (_targetCone != null && _targetCone.gameObject != null) {
         Destroy(_targetCone.gameObject);
      }

      if (_targetCircle != null && _targetCircle.gameObject != null) {
         Destroy(_targetCircle.gameObject);
      }

      // Handle OnDestroy logic in a separate method so it can be correctly stripped
      onBeingDestroyedServer();
   }

   [ServerOnly]
   private void onBeingDestroyedServer () {
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
   public void fireCannonBallAtTarget (Vector2 startPosition, Vector2 endPosition, bool isLobbed, float lifetime = -1.0f, float speedOverride = -1.0f, bool playSound = true) {
      // Calculate the direction of the ball
      Vector2 direction = endPosition - startPosition;
      direction.Normalize();

      // Create the cannon ball object from the prefab
      ServerCannonBall netBall = Instantiate(PrefabsManager.self.serverCannonBallPrefab, startPosition, Quaternion.identity);

      int abilityId = -1;
      if (shipAbilities.Count > 0) {
         ShipAbilityData shipAbilityData = ShipAbilityManager.self.getAbility(shipAbilities[0]);
         if (shipAbilityData != null) {
            abilityId = shipAbilityData.abilityId;
         }
      }

      // If no speed override, use the default attack speed
      float speed = (speedOverride > 0.0f) ? speedOverride : Attack.getSpeedModifier(Attack.Type.Cannon);
      Vector2 velocity = direction * speed;

      if (isLobbed) {
         float lobHeight = Mathf.Clamp(1.0f / speed, 0.3f, 1.0f);
         netBall.initLob(this.netId, this.instanceId, Attack.ImpactMagnitude.Normal, abilityId, startPosition, velocity, lobHeight, damageMultiplier: Attack.getDamageModifier(Attack.Type.Cannon), lifetime: lifetime, statusType: cannonEffectType, statusDuration: 3.0f, playFiringSound: playSound);
      } else {
         netBall.init(this.netId, this.instanceId, Attack.ImpactMagnitude.Normal, abilityId, startPosition, velocity, damageMultiplier: Attack.getDamageModifier(Attack.Type.Cannon), lifetime: lifetime, statusType: cannonEffectType, statusDuration: 3.0f, playFiringSound: playSound);
      }

      NetworkServer.Spawn(netBall.gameObject);
   }

   public override void setDataFromUserInfo (UserInfo userInfo, Item armor, Item weapon, Item hat, ShipInfo shipInfo, GuildInfo guildInfo, GuildRankInfo guildRankInfo) {
      base.setDataFromUserInfo(userInfo, armor, weapon, hat, shipInfo, guildInfo, guildRankInfo);

      // Ship stuff
      shipId = shipInfo.shipId;

      initialize(shipInfo);

      foreach (int newShipAbility in shipInfo.shipAbilities.ShipAbilities) {
         shipAbilities.Add(newShipAbility);
      }

      if (shipAbilities.Count > 0) {
         primaryAbilityId = shipAbilities[0];
      }

      // Store the equipped items characteristics
      weaponType = weapon.itemTypeId;
      armorType = armor.itemTypeId;
      hatType = hat.itemTypeId;
      armorColors = armor.paletteNames;
   }

   public override Armor getArmorCharacteristics () {
      return new Armor(0, armorType, armorColors);
   }

   public override Weapon getWeaponCharacteristics () {
      return new Weapon(0, weaponType, weaponColors);
   }

   protected void adjustMovementAudio () {
      float volumeModifier = isMoving() ? Time.deltaTime * .5f : -Time.deltaTime * .5f;
      _movementAudioSource.volume += volumeModifier;
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

      if (inputVector != _movementInputDirection || isSpeedingUp != InputManager.isSpeedUpKeyPressed()) {
         // If the ship wasn't moving, apply a small force locally to make up for delay
         if (inputVector != Vector2.zero && _body.velocity.sqrMagnitude < 0.025f) {
            _body.AddForce(Quaternion.AngleAxis(this.desiredAngle, Vector3.forward) * Vector3.up * getMoveSpeed() * CLIENT_SIDE_FORCE);
         }

         if (NetworkTime.time - _lastInputChangeTime > getInputDelay()) {
            // In Host mode only, we want to avoid setting _lastInputChangeTime here so cooldown validation passes in Cmd_RequestServerAddForce()
            if (!Util.isHost()) {
               _lastInputChangeTime = NetworkTime.time;
            }

            _movementInputDirection = inputVector;
            Cmd_RequestServerAddForce(inputVector, isSpeedingUp);
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.MoveShip);
         }
      }
   }

   [Command]
   protected void Cmd_RequestServerAddForce (Vector2 direction, bool isSpeedingUp) {
      if (NetworkTime.time - _lastInputChangeTime > getInputDelay()) {
         Vector2 forceToApply = direction;

         if (direction != Vector2.zero) {
            float newAngle = Util.AngleBetween(Vector2.up, direction);
            desiredAngle = newAngle;
            facing = DirectionUtil.getDirectionForAngle(desiredAngle);
         }

         _movementInputDirection = forceToApply.normalized;

         _lastInputChangeTime = NetworkTime.time;
         this.isSpeedingUp = isSpeedingUp;
      }
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

   private void setFlag (Flag flag) {
      if (_currentFlag == flag) {
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
      target.currentHealth -= damage;
      target.Rpc_ShowExplosion(source.netId, target.transform.position, damage, attackType);
      target.noteAttacker(source);
   }

   public override void noteAttacker (NetEntity entity) {
      base.noteAttacker(entity);

      // If we don't currently have a target selected, assign the attacker as our new target
      if (isLocalPlayer && !isDead() && _targetSelector.getTarget() == null) {
         SelectionManager.self.setSelectedEntity((SeaEntity)entity);
      }
   }

   protected IEnumerator CO_TemporarilyDisableShip () {
      if (!isDisabled) {
         yield break;
      }

      invulnerable = true;
      _clickableBox.gameObject.SetActive(false);

      foreach (Collider2D c in GetComponents<Collider2D>()) {
         c.enabled = false;
      }

      foreach (SpriteRenderer renderer in _renderers) {
         renderer.enabled = false;
      }

      while (isDisabled) {
         yield return null;
      }

      invulnerable = false;
      _clickableBox.gameObject.SetActive(true);

      if (!isDead()) {
         foreach (Collider2D c in GetComponents<Collider2D>()) {
            c.enabled = true;
         }

         foreach (SpriteRenderer renderer in _renderers) {
            renderer.enabled = true;
         }

         foreach (SpriteRenderer spriteRender in speedUpEffectHolders) {
            spriteRender.enabled = false;
         }
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

   [Command]
   private void Cmd_RequestRespawn () {
      this.spawnInNewMap(Area.STARTING_TOWN, Spawn.STARTING_SPAWN, Direction.North);

      // Set the ship health back to max
      restoreMaxShipHealth();
   }

   [Server]
   public void storeCurrentShipHealth () {
      Util.tryToRunInServerBackground(() => DB_Main.storeShipHealth(this.shipId, Mathf.Min(this.currentHealth, this.maxHealth)));
   }

   [Server]
   public void restoreMaxShipHealth () {
      Util.tryToRunInServerBackground(() => DB_Main.storeShipHealth(this.shipId, this.maxHealth));
   }

   #region Private Variables

   // Our ship movement sound
   protected AudioSource _movementAudioSource;

   // Our target selector
   protected PlayerTargetSelector _targetSelector;

   // The position the player is currently aiming at
   private Vector2 _currentAimPosition;

   // The desired direction of the ship
   private Vector2 _movementInputDirection;

   // The velocity at which we're currently damping the velocity of the ship
   private Vector2 _shipDampVelocity;

   // A multiplier for the force added locally in order to mask delay   
   private const float CLIENT_SIDE_FORCE = 0.1f;

   // Defines the possible attack types for the player's right-click attack
   private enum CannonAttackType { Normal = 0, Cone = 1, Circle = 2 }

   // Determines what type of attack will trigger when right-clicking
   private CannonAttackType _cannonAttackType = CannonAttackType.Normal;

   // How long the player has charged up their attack for
   private float _cannonChargeAmount = 0.0f;

   // Is the player currently charging up a cannon attack
   private bool _isChargingCannon = false;

   // If the targeting indicators should update with the player's inputs
   private bool _shouldUpdateTargeting = true;

   // Reference to the object used to target circular player attacks
   private TargetCircle _targetCircle;

   // Reference to the object used to target conical player attacks
   private TargetCone _targetCone;

   // The current flag being displayed by the ship
   private Flag _currentFlag = Flag.None;

   #endregion
}
