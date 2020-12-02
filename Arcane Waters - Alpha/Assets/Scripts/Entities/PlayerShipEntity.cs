using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class PlayerShipEntity : ShipEntity
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
   public GameObject speedUpEffectHolder;
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

   #endregion

   protected override bool isBot () { return false; }

   protected override void Start () {
      base.Start();

      // Player ships spawn hidden and invulnerable, until the client finishes loading the area
      if (isDisabled) {
         StartCoroutine(CO_TemporarilyDisableShip());
      }

      if (isLocalPlayer) {
         // Create a ship movement sound for our own ship
         _movementAudioSource = SoundManager.createLoopedAudio(SoundManager.Type.Ship_Movement, this.transform);
         _movementAudioSource.gameObject.AddComponent<MatchCameraZ>();
         _movementAudioSource.volume = 0f;

         // Notify UI panel to display the current skills this ship has
         rpc.Cmd_RequestShipAbilities(shipId);
         Cmd_RequestAbilityList();

         speedUpEffectHolder.SetActive(false);

         _targetSelector = GetComponentInChildren<PlayerTargetSelector>();
      } else if (Util.isServer()) {
         _serverSideMoveAngle = desiredAngle;
      } else {
         // Disable our collider if we are not either the local player or the server
         getMainCollider().isTrigger = true;
      }
   }

   protected override void updateSprites () {
      base.updateSprites();

      shipBoostSpriteSwapFront.newTexture = _shipBoostSpritesFront;
      shipBoostSpriteSwapBack.newTexture = _shipBoostSpritesBack;
   }

   protected override void Update () {
      base.Update();

      if (!isLocalPlayer) {
         // Display the ship boost meter for remote players based on velocity
         if (getVelocity().magnitude > NETWORK_SHIP_SPEEDUP_MAGNITUDE) {
            updateSpeedUpDisplay(0, true, false);
         } else if (speedUpEffectHolder.activeSelf) {
            updateSpeedUpDisplay(0, false, false);
         }

         return;
      }

      // Adjust the volume on our movement audio source
      adjustMovementAudio();

      // If we're dead, ask the server to respawn
      if (isDead() && !_hasSentRespawnRequest) {
         StartCoroutine(CO_RequestRespawnAfterDelay(3f));
         _hasSentRespawnRequest = true;
      }

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

      if (!isDead() && SeaManager.getAttackType() != Attack.Type.Air) {
         SeaEntity target = _targetSelector.getTarget();
         if (target != null && hasReloaded() && InputManager.isFireCannonKeyDown()) {
            Cmd_FireMainCannonAtTarget(target.gameObject, Vector2.zero);
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.FireShipCannon);
         }

         if (hasReloaded() && InputManager.isFireCannonMouseDown()) {
            Cmd_FireMainCannonAtTarget(null, Util.getMousePos());
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.FireShipCannon);
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
            shipBoostCooldownObj.SetActive(false);
         } else {
            isReadyToSpeedup = false;
            isSpeedingUp = false;
            updateSpeedUpDisplay(speedMeter, false, false);

            // Trigger the tutorial
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.ShipSpeedUp);
         }
      }
   }

   public override bool isMoving () {
      return getVelocity().magnitude > SHIP_MOVING_MAGNITUDE;
   }

   public Vector2 getAimPosition () {
      return _currentAimPosition;
   }

   [Command]
   public void Cmd_ModifyMoveAngle (float modifier) {
      if (isDead() || NetworkTime.time - _lastAngleChangeTime < getAngleDelay()) {
         return;
      }

      _lastAngleChangeTime = NetworkTime.time;
      _serverSideMoveAngle += modifier * getAngleChangeSpeed();

      // Set the new facing direction
      Direction newFacingDirection = DirectionUtil.getDirectionForAngle(_serverSideMoveAngle);
      if (newFacingDirection != facing) {
         _serverSideMoveAngle = DirectionUtil.getAngle(newFacingDirection);
         desiredAngle = _serverSideMoveAngle;
         facing = newFacingDirection;
      }
   }

   [Command]
   protected void Cmd_FireMainCannonAtTarget (GameObject target, Vector2 requestedTargetPoint) {
      if (isDead() || !hasReloaded()) {
         return;
      }

      Rpc_NoteAttack();

      _lastAttackTime = NetworkTime.time;
      
      Vector2 startPosition = transform.position;
      Vector2 targetPosition = (target == null) ? requestedTargetPoint : (Vector2) target.transform.position;

      fireCannonBallAtTarget(startPosition, targetPosition);
   }

   [Command]
   private void Cmd_RequestAbilityList () {
      Target_ReceiveAbilityList(connectionToClient, shipAbilities.ToArray());
   }

   [TargetRpc]
   public void Target_ReceiveAbilityList (NetworkConnection connection, int[] abilityIds) {
      shipAbilities = new List<int>(abilityIds);
   }

   private void updateSpeedUpDisplay (float meter, bool isOn, bool isReadySpeedup) {
      // Handle GUI
      if (isLocalPlayer) {
         if (meter < SPEEDUP_METER_MAX) {
            speedupGUI.enabled = true;
            speedUpBar.fillAmount = meter / SPEEDUP_METER_MAX;
         } else {
            speedupGUI.enabled = false;
         }
      } else {
         speedupGUI.enabled = false;
      }
      speedUpBar.color = isReadySpeedup ? defaultColor : recoveringColor;

      // Handle sprite effects
      if (isOn) {
         speedUpEffectHolder.SetActive(true);
      } else {
         speedUpEffectHolder.SetActive(false);
      }
   }

   protected override void OnDestroy () {
      base.OnDestroy();

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
         Util.tryToRunInServerBackground(() => DB_Main.storeShipHealth(this.shipId, this.currentHealth));
      }
   }

   [Server]
   public void fireCannonBallAtTarget (Vector2 startPosition, Vector2 endPosition) {
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

      Vector2 velocity = direction * Attack.getSpeedModifier(Attack.Type.Cannon);

      netBall.init(this.netId, this.instanceId, Attack.ImpactMagnitude.Normal, abilityId, startPosition, velocity, damageMultiplier: Attack.getDamageModifier(Attack.Type.Cannon));

      NetworkServer.Spawn(netBall.gameObject);
   }

   public override void setDataFromUserInfo (UserInfo userInfo, Item armor, Item weapon, Item hat, ShipInfo shipInfo, GuildInfo guildInfo) {
      base.setDataFromUserInfo(userInfo, armor, weapon, hat, shipInfo, guildInfo);

      // Ship stuff
      shipId = shipInfo.shipId;

      ShipData shipData = ShipDataManager.self.getShipData(shipInfo.shipType);
      shipSize = shipData.shipSize;

      initialize(shipData);

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

      // Store the guild icon layers and colors
      guildIconBorder = guildInfo.iconBorder;
      guildIconBackground = guildInfo.iconBackground;
      guildIconSigil = guildInfo.iconSigil;
      guildIconBackPalettes = guildInfo.iconBackPalettes;
      guildIconSigilPalettes = guildInfo.iconSigilPalettes;
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

   protected override void handleArrowsMoveMode () {
      // Make note of the time
      _lastMoveChangeTime = NetworkTime.time;

      // Check if enough time has passed for us to change our facing direction
      bool canChangeDirection = (NetworkTime.time - _lastAngleChangeTime > getAngleDelay());

      if (canChangeDirection) {
         if (InputManager.getKeyAction(KeyAction.MoveLeft)) {
            Cmd_ModifyMoveAngle(+1);
            _lastAngleChangeTime = NetworkTime.time;
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.MoveShip);
         } else if (InputManager.getKeyAction(KeyAction.MoveRight)) {
            Cmd_ModifyMoveAngle(-1);
            _lastAngleChangeTime = NetworkTime.time;
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.MoveShip);
         }
      }

      if (NetworkTime.time - _lastInputChangeTime > getInputDelay()) {
         _lastInputChangeTime = NetworkTime.time;

         if (InputManager.getKeyAction(KeyAction.MoveUp)) {
            Cmd_RequestMovement();
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.MoveShip);
         }
      }
   }

   protected override void handleServerAuthoritativeMode () {
      // Make note of the time
      _lastMoveChangeTime = NetworkTime.time;

      if (InputManager.getKeyAction(KeyAction.MoveUp)) {
         // If the ship wasn't moving, apply a small force locally to make up for delay
         if (_body.velocity.sqrMagnitude < 0.025f) {          
            _body.AddForce(Quaternion.AngleAxis(this.desiredAngle, Vector3.forward) * Vector3.up * getMoveSpeed() * CLIENT_SIDE_FORCE);
         }
      }

      if (NetworkTime.time - _lastInputChangeTime > getInputDelay()) {
         _lastInputChangeTime = NetworkTime.time;
         Vector2 inputVector = InputManager.getMovementInput();

         if (inputVector.x != 0 || inputVector.y != 0) {
            Cmd_RequestServerAddForce(inputVector, isSpeedingUp);
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.MoveShip);
         }
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

   protected IEnumerator CO_RequestRespawnAfterDelay (float delay) {
      yield return new WaitForSeconds(delay);

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

      foreach (Collider2D c in GetComponents<Collider2D>()) {
         c.enabled = true;
      }

      foreach (SpriteRenderer renderer in _renderers) {
         renderer.enabled = true;
      }
   }

   protected IEnumerator CO_AddForce (double timestamp, Vector2 force) {
      while (NetworkTime.time < timestamp) {
         yield return null;
      }

      _body.AddForce(force);
   }

   [Command]
   protected void Cmd_RequestMovement () {
      Vector2 forceToApply = Quaternion.AngleAxis(this.desiredAngle, Vector3.forward) * Vector3.up;
      Rpc_AddForce(NetworkTime.time + getAddForceDelay(), forceToApply * getMoveSpeed());
   }

   [Command]
   protected void Cmd_RequestServerAddForce (Vector2 direction, bool isSpeedingUp) {
      if (!Util.isServerNonHost() || NetworkTime.time - _lastInputChangeTime > getInputDelay()) {         
         Vector2 forceToApply = direction * getMoveSpeed();

         if (direction != Vector2.zero) {
            float newAngle = Util.AngleBetween(Vector2.up, direction);
            desiredAngle = newAngle;
            facing = DirectionUtil.getDirectionForAngle(desiredAngle);
         }

         _body.AddForce(forceToApply);
         _lastInputChangeTime = NetworkTime.time;
         this.isSpeedingUp = isSpeedingUp;
      }
   }

   [ClientRpc]
   protected void Rpc_AddForce (double timestamp, Vector2 force) {
      StartCoroutine(CO_AddForce(timestamp, force));
   }

   [Command]
   private void Cmd_RequestRespawn () {
      this.spawnInNewMap(Area.STARTING_TOWN, Spawn.STARTING_SPAWN, Direction.North);

      // Set the ship health back to max
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.storeShipHealth(this.shipId, this.maxHealth);
      });
   }

   #region Private Variables

   // Gets set to true after we ask the server to respawn
   protected bool _hasSentRespawnRequest = false;

   // Our ship movement sound
   protected AudioSource _movementAudioSource;

   // Our target selector
   protected PlayerTargetSelector _targetSelector;

   // The position the player is currently aiming at
   private Vector2 _currentAimPosition;

   // The direction of the ship server-side
   private float _serverSideMoveAngle;

   // A multiplier for the force added locally in order to mask delay   
   private const float CLIENT_SIDE_FORCE = 0.1f;

   #endregion
}
