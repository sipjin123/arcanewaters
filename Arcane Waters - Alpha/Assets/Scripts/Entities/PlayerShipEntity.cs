using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class PlayerShipEntity : ShipEntity {
   #region Public Variables

   // The ID of this ship in the database
   [SyncVar]
   public int shipId;

   // Gets set to true when the next shot is scheduled
   public bool isNextShotDefined = false;

   // The coordinates of the next shot
   public Vector2 nextShotTarget = new Vector2(0, 0);

   // Ability Reference
   public ShipAbilityInfo shipAbilities = new ShipAbilityInfo();

   // The equipped weapon characteristics
   [SyncVar]
   public int weaponType = 0;
   [SyncVar]
   public string weaponColor1;
   [SyncVar]
   public string weaponColor2;

   // The equipped armor characteristics
   [SyncVar]
   public int armorType = 0;
   [SyncVar]
   public string armorColor1;
   [SyncVar]
   public string armorColor2;

   // The equipped hat characteristics
   [SyncVar]
   public int hatType = 0;
   [SyncVar]
   public string hatColor1;
   [SyncVar]
   public string hatColor2;

   // The effect that indicates this ship is speeding up
   public GameObject speedUpEffect;
   public Canvas speedupGUI;
   public Transform speedupEffectPivot;
   public Image speedUpBar;

   // Color indications if the fuel is usable or not
   public Color recoveringColor, defaultColor;

   // Speedup variables
   public float speedMeter = 0;
   public static float SPEEDUP_METER_MAX = 10;
   public bool isReadyToSpeedup = true;
   public float fuelDepleteValue = 2;
   public float fuelRecoverValue = 1.2f;

   #endregion

   protected override bool isBot () { return false; }

   protected override void Start () {
      base.Start();

      if (isLocalPlayer) {
         // Create a ship movement sound for our own ship
         _movementAudioSource = SoundManager.createLoopedAudio(SoundManager.Type.Ship_Movement, this.transform);
         _movementAudioSource.gameObject.AddComponent<MatchCameraZ>();
         _movementAudioSource.volume = 0f;

         // Notify UI panel to display the current skills this ship has
         rpc.Cmd_RequestShipAbilities(shipId);
      }
   }

   protected override void Update () {
      base.Update();

      if (!isLocalPlayer) {
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
      if (isNextShotDefined && hasReloaded()) {
         // Fire the scheduled shot
         if (!SeaManager.self.isOffensiveAbility()) {
            Cmd_CastAbility(SeaManager.selectedAttackType);
         } else {
            Cmd_FireMainCannonAtSpot(nextShotTarget, SeaManager.selectedAttackType, transform.position);
         }
         isNextShotDefined = false;
      }

      // Check if input is allowed
      if (!Util.isGeneralInputAllowed()) {
         return;
      }

      // Right-click to attack in a circle
      if (Input.GetMouseButtonUp(1) && !isDead() && SeaManager.selectedAttackType != Attack.Type.Air && !VoyageGroupPanel.self.isMouseOverAnyMemberCell()) {
         // If the ship is reloading, set the next shot
         if (!hasReloaded()) {
            nextShotTarget = clampToRange(Util.getMousePos());
            isNextShotDefined = true;
         } else {
            if (!SeaManager.self.isOffensiveAbility()) {
               Cmd_CastAbility(SeaManager.selectedAttackType);
            } else {
               Cmd_FireMainCannonAtSpot(Util.getMousePos(), SeaManager.selectedAttackType, transform.position);
            }
         }
      }

      // Speed ship boost feature
      if (Input.GetKey(KeyCode.LeftShift) && isReadyToSpeedup) {
         isSpeedingUp = true;
         if (speedMeter > 0) {
            speedMeter -= Time.deltaTime * fuelDepleteValue;
            Cmd_UpdateSpeedupDisplay(true);
         } else {
            isReadyToSpeedup = false;
            isSpeedingUp = false;
            Cmd_UpdateSpeedupDisplay(false);
         }
      } else {
         // Only notify other clients once if disabling
         if (isSpeedingUp) {
            Cmd_UpdateSpeedupDisplay(false);
            isSpeedingUp = false;
         }

         if (speedMeter < SPEEDUP_METER_MAX) {
            speedMeter += Time.deltaTime * fuelRecoverValue;
         } else {
            isReadyToSpeedup = true;
         }
      }

      updateSpeedUpDisplay(speedMeter, isSpeedingUp, isReadyToSpeedup, false);

      // If the right mouse button is being held and the left mouse button is clicked, clear the next shot
      if (Input.GetMouseButton(1) && Input.GetMouseButtonDown(0)) {
         isNextShotDefined = false;
      }

      // Space to fire at the selected ship
      /*if (Input.GetKeyUp(KeyCode.Space) && !ChatManager.isTyping() && SelectionManager.self.selectedEntity != null && SeaManager.combatMode == SeaManager.CombatMode.Select) {
         Cmd_FireAtTarget(SelectionManager.self.selectedEntity.gameObject);
      }*/

      // Right click to fire out the sides
      if (Input.GetMouseButtonUp(1) && SeaManager.selectedAttackType == Attack.Type.Air && !VoyageGroupPanel.self.isMouseOverAnyMemberCell()) {
         Cmd_FireTimedCannonBall(Util.getMousePos());
      }
   }

   private void updateSpeedUpDisplay (float meter, bool isOn, bool isReadySpeedup, bool forceDisable) {
      // Handle GUI
      if (!forceDisable && (meter < SPEEDUP_METER_MAX)) {
         speedupGUI.enabled = true;
         speedUpBar.fillAmount = meter / SPEEDUP_METER_MAX;
      } else {
         speedupGUI.enabled = false;
      }

      speedUpBar.color = isReadySpeedup ? defaultColor : recoveringColor;

      // Handle sprite effects
      if (isOn) {
         speedUpEffect.SetActive(true);
         speedupEffectPivot.transform.localEulerAngles = new Vector3(0, 0, -Util.getAngle(facing));
      } else {
         speedUpEffect.SetActive(false);
      }
   }

   [Command]
   void Cmd_UpdateSpeedupDisplay (bool isOn) {
      Rpc_UpdateSpeedupDisplay(isOn);
   }

   [ClientRpc]
   public void Rpc_UpdateSpeedupDisplay (bool isOn) {
      updateSpeedUpDisplay(0, isOn, false, true);
   }

   [ServerOnly]
   protected override void OnDestroy () {
      base.OnDestroy();

      // We don't care when the Destroy was initiated by a warp
      if (this.isAboutToWarpOnServer) {
         return;
      }

      // Make sure the server saves our position and health when a player is disconnected (by any means other than a warp)
      if (MyNetworkManager.wasServerStarted) {
         Util.tryToRunInServerBackground(() => DB_Main.storeShipHealth(this.shipId, this.currentHealth));
      }
   }

   [ClientRpc]
   public void Rpc_FireTimedCannonBall (float startTime, Vector2 velocity) {
      StartCoroutine(CO_FireTimedCannonBall(startTime, velocity));
   }

   public override void setDataFromUserInfo (UserInfo userInfo, Item armor, Item weapon, Item hat, ShipInfo shipInfo) {
      base.setDataFromUserInfo(userInfo, armor, weapon, hat, shipInfo);

      // Ship stuff
      initialize(shipInfo);
      shipId = shipInfo.shipId;
      shipAbilities = shipInfo.shipAbilities;

      // Store the equipped items characteristics
      weaponType = weapon.itemTypeId;
      armorType = armor.itemTypeId;
      hatType = hat.itemTypeId;
      armorColor1 = armor.paletteName1;
      armorColor2 = armor.paletteName2;
   }

   public override Armor getArmorCharacteristics () {
      return new Armor(0, armorType, armorColor1, armorColor2);
   }

   public override Weapon getWeaponCharacteristics () {
      return new Weapon(0, weaponType, weaponColor1, weaponColor2);
   }

   protected void adjustMovementAudio() {
      float volumeModifier = isMoving() ? Time.deltaTime * .5f : -Time.deltaTime * .5f;
      _movementAudioSource.volume += volumeModifier;
   }

   public float getAngleChangeSpeed () {
      switch (this.shipType) {
         case Ship.Type.Type_1:
            return 20f;
         case Ship.Type.Type_2:
            return 15f;
         case Ship.Type.Type_3:
            return 10f;
         case Ship.Type.Type_4:
            return 9f;
         case Ship.Type.Type_5:
            return 8f;
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
      // Check if enough time has passed for us to change our facing direction
      bool canChangeDirection = (Time.time - _lastAngleChangeTime > getAngleDelay());

      if (canChangeDirection) {
         if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) {
            Cmd_ModifyAngle(+1);
            _lastAngleChangeTime = Time.time;
         } else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) {
            Cmd_ModifyAngle(-1);
            _lastAngleChangeTime = Time.time;
         }
      }

      // Figure out the force vector we should apply
      if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) {
         Vector2 forceToApply = Quaternion.AngleAxis(this.desiredAngle, Vector3.forward) * Vector3.up;
         _body.AddForce(forceToApply.normalized * getMoveSpeed());

         // Make note of the time
         _lastMoveChangeTime = Time.time;
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

   protected IEnumerator CO_FireTimedCannonBall (float startTime, Vector2 velocity) {
      float delay = startTime - TimeManager.self.getSyncedTime();

      yield return new WaitForSeconds(delay);

      // Create the cannon ball object from the prefab
      GameObject ballObject = Instantiate(PrefabsManager.self.networkedCannonBallPrefab, this.transform.position, Quaternion.identity);
      NetworkedCannonBall netBall = ballObject.GetComponent<NetworkedCannonBall>();

      int abilityId = -1;
      if (shipAbilities.ShipAbilities.Length > 0) {
         ShipAbilityData shipAbilityData = ShipAbilityManager.self.getAbility(shipAbilities.ShipAbilities[0]);
         if (shipAbilityData != null) {
            abilityId = shipAbilityData.abilityId;
         }
      } 
      netBall.init(this.netId, this.instanceId, currentImpactMagnitude, abilityId, this.transform.position);

      // Add velocity to the ball
      netBall.body.velocity = velocity;

      // Destroy the cannon ball after a couple seconds
      Destroy(ballObject, NetworkedCannonBall.LIFETIME);
   }

   [Command]
   void Cmd_FireTimedCannonBall (Vector2 mousePos) {
      if (isDead() || !hasReloaded()) {
         return;
      }

      // We either fire out the left or right side depending on which was clicked
      for (int i = 0; i < 5; i++) {
         Vector2 direction = mousePos - (Vector2) this.transform.position;
         direction = direction.normalized;
         direction = direction.Rotate(i * 3f);

         // Figure out the desired velocity
         Vector2 velocity = direction.normalized * NetworkedCannonBall.MOVE_SPEED;

         // Delay the firing a little bit to compensate for lag
         float timeToStartFiring = TimeManager.self.getSyncedTime() + .150f;

         // Note the time at which we last successfully attacked
         _lastAttackTime = Time.time;

         // Make note on the clients that the ship just attacked
         Rpc_NoteAttack();

         // Tell all clients to fire the cannon ball at the same time
         Rpc_FireTimedCannonBall(timeToStartFiring, velocity);

         // Standalone Server needs to call this as well
         if (!MyNetworkManager.isHost) {
            StartCoroutine(CO_FireTimedCannonBall(timeToStartFiring, velocity));
         }
      }
   }

   [Command]
   void Cmd_RequestRespawn () {
      this.spawnInNewMap(Area.STARTING_TOWN, Spawn.STARTING_SPAWN, Direction.North);

      // Set the ship health back to max
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         DB_Main.storeShipHealth(this.shipId, this.maxHealth);
      });
   }

   [Command]
   public void Cmd_ModifyAngle (float modifier) {
      if (this is PlayerShipEntity) {
         PlayerShipEntity ship = (PlayerShipEntity) this;
         ship.desiredAngle += modifier * getAngleChangeSpeed();

         // Set the new facing direction
         Direction newFacingDirection = DirectionUtil.getDirectionForAngle(ship.desiredAngle);
         if (newFacingDirection != ship.facing) {
            ship.facing = newFacingDirection;
         }
      }
   }

   [Command]
   void Cmd_FireAtTarget (GameObject target) {
      if (isDead() || !hasReloaded() || target == null) {
         return;
      }

      Vector2 spot = target.transform.position;

      // The target point is clamped to the attack range
      spot = clampToRange(spot);
      
      // Note the time at which we last successfully attacked
      _lastAttackTime = Time.time;

      // Check how long we should take to get there
      float distance = Vector2.Distance(this.transform.position, spot);
      float delay = Mathf.Clamp(distance, .5f, 1.5f);

      // Start moving a cannon ball from the source to the target
      Rpc_FireHomingCannonBall(this.gameObject, target, Time.time, Time.time + delay);

      // Apply damage after the delay
      StartCoroutine(CO_ApplyDamageAfterDelay(delay, this.damage, this, target.GetComponent<SeaEntity>(), Attack.Type.Cannon));

      // Make note on the clients that the ship just attacked
      Rpc_NoteAttack();
   }

   #region Private Variables

   // Gets set to true after we ask the server to respawn
   protected bool _hasSentRespawnRequest = false;

   // Our ship movement sound
   protected AudioSource _movementAudioSource;

   #endregion
}
