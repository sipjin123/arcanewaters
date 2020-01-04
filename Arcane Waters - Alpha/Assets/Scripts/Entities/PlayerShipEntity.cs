using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PlayerShipEntity : ShipEntity {
   #region Public Variables

   // The maximum number of scheduled shots at any time
   public static int MAX_SCHEDULED_SHOTS = 1;

   // The ID of this ship in the database
   [SyncVar]
   public int shipId;

   // Gets set to true when the next shot is scheduled
   public bool isNextShotDefined = false;

   // The coordinates of the next shot
   public Vector2 nextShotTarget = new Vector2(0, 0);

   #endregion

   protected override void Start () {
      base.Start();

      // Create a ship movement sound for our own ship
      if (isLocalPlayer) {
         _movementAudioSource = SoundManager.createLoopedAudio(SoundManager.Type.Ship_Movement, this.transform);
         _movementAudioSource.gameObject.AddComponent<MatchCameraZ>();
         _movementAudioSource.volume = 0f;

         CannonPanel.self.setForThisType(this.shipType);
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
         Cmd_FireMainCannonAtSpot(nextShotTarget, SeaManager.selectedAttackType, transform.position);
         isNextShotDefined = false;
      }

      // Ignore any input if a panel is opened or the player is writing in chat
      if (PanelManager.self.hasPanelInStack() || ChatPanel.self.inputField.isFocused) {
         return;
      }

      // Right-click to attack in a circle
      if (Input.GetMouseButtonUp(1) && !isDead() && SeaManager.selectedAttackType != Attack.Type.Air) {
         // If the ship is reloading, set the next shot
         if (!hasReloaded()) {
            nextShotTarget = clampToRange(Util.getMousePos());
            isNextShotDefined = true;
         } else {
            Cmd_FireMainCannonAtSpot(Util.getMousePos(), SeaManager.selectedAttackType, transform.position);
         }
      }

      // If the right mouse button is being held and the left mouse button is clicked, clear the next shot
      if (Input.GetMouseButton(1) && Input.GetMouseButtonDown(0)) {
         isNextShotDefined = false;
      }

      // Space to fire at the selected ship
      /*if (Input.GetKeyUp(KeyCode.Space) && !ChatManager.isTyping() && SelectionManager.self.selectedEntity != null && SeaManager.combatMode == SeaManager.CombatMode.Select) {
         Cmd_FireAtTarget(SelectionManager.self.selectedEntity.gameObject);
      }*/

      // Right click to fire out the sides
      if (Input.GetMouseButtonUp(1) && SeaManager.selectedAttackType == Attack.Type.Air) {
         Cmd_FireTimedCannonBall(Util.getMousePos());
      }
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

   public void setDataFromUserInfo (UserInfo userInfo, Armor armor, Weapon weapon, ShipInfo shipInfo) {
      this.entityName = userInfo.username;
      this.adminFlag = userInfo.adminFlag;
      this.classType = userInfo.classType;
      this.specialty = userInfo.specialty;
      this.faction = userInfo.faction;
      this.guildId = userInfo.guildId;
      this.gender = userInfo.gender;

      // Ship stuff
      this.shipType = shipInfo.shipType;
      this.skinType = shipInfo.skinType;
      this.shipId = shipInfo.shipId;
      this.currentHealth = shipInfo.health;
      this.maxHealth = shipInfo.maxHealth;
      this.attackRangeModifier = shipInfo.attackRange;
      this.speed = shipInfo.speed;
      this.sailors = shipInfo.sailors;
      this.rarity = shipInfo.rarity;
   }

   protected void adjustMovementAudio() {
      float volumeModifier = isMoving() ? Time.deltaTime * .5f : -Time.deltaTime * .5f;
      _movementAudioSource.volume += volumeModifier;
   }

   public float getAngleChangeSpeed () {
      switch (this.shipType) {
         case Ship.Type.Caravel:
            return 20f;
         case Ship.Type.Brigantine:
            return 15f;
         case Ship.Type.Nao:
            return 10f;
         case Ship.Type.Carrack:
            return 9f;
         case Ship.Type.Cutter:
            return 8f;
         case Ship.Type.Buss:
            return 7f;
         case Ship.Type.Galleon:
            return 6f;
         case Ship.Type.Barge:
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
         case Ship.Type.Caravel:
            mass = 1f;
            drag = 50f;
            break;
         case Ship.Type.Brigantine:
            mass = 2f;
            drag = 32f;
            break;
         case Ship.Type.Nao:
            mass = 4f;
            drag = 19f;
            break;
         case Ship.Type.Carrack:
            mass = 8f;
            drag = 6.25f;
            break;
         case Ship.Type.Cutter:
            mass = 16f;
            drag = 3.125f;
            break;
         case Ship.Type.Buss:
            mass = 32f;
            drag = 1.5f;
            break;
         case Ship.Type.Galleon:
            mass = 64f;
            drag = .9f;
            break;
         case Ship.Type.Barge:
            mass = 100f;
            drag = .5f;
            break;
      }

      // Apply the settings
      _body.mass = mass;
      _body.drag = drag;
      _body.angularDrag = 0f;
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
      target.Rpc_ShowDamageText(damage, source.userId, attackType);
      target.noteAttacker(source);
   }

   protected IEnumerator CO_FireTimedCannonBall (float startTime, Vector2 velocity) {
      float delay = startTime - TimeManager.self.getSyncedTime();

      yield return new WaitForSeconds(delay);

      // Create the cannon ball object from the prefab
      GameObject ballObject = Instantiate(PrefabsManager.self.networkedCannonBallPrefab, this.transform.position, Quaternion.identity);
      NetworkedCannonBall netBall = ballObject.GetComponent<NetworkedCannonBall>();
      netBall.creatorUserId = this.userId;
      netBall.instanceId = this.instanceId;

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
      Spawn spawn = SpawnManager.self.getSpawn(AreaManager.self.areaKeyForSunkenPlayers, AreaManager.self.spawnKeyForSunkenPlayers);
      this.spawnInNewMap(Area.STARTING_TOWN, spawn, Direction.North);

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
