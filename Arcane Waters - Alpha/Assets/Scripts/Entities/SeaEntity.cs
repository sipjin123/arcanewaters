using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using DigitalRuby.LightningBolt;
using Mirror;
using System;
using TMPro;

public class SeaEntity : NetEntity
{
   #region Public Variables

   // The amount of damage we do
   [SyncVar]
   public float damage = 25;

   // How long we have to wait to reload
   [SyncVar]
   public float reloadDelay = 1.5f;

   // Keeps track of the consecutive attack count
   [SyncVar]
   public float attackCounter = 0f;

   // The prefab we use for creating Attack Circles, for self shots
   public AttackCircle localAttackCirclePrefab;

   // The prefab we use for creating Attack Circles, for other's shots
   public AttackCircle defaultAttackCirclePrefab;

   // The container for our sprites
   public GameObject spritesContainer;

   // The container for our ripples
   public GameObject ripplesContainer;

   // Convenient object references to the left and right side of our entity
   public GameObject leftSideTarget;
   public GameObject rightSideTarget;

   // Determines if this entity can be damaged
   public bool invulnerable;

   // The position data where the projectile starts
   public List<DirectionalTransform> projectileSpawnLocations;

   // Holds the list of colliders for this obj
   public List<Collider2D> colliderList;

   // Cache the impact type of the ability
   public Attack.ImpactMagnitude currentImpactMagnitude = Attack.ImpactMagnitude.None;

   // The combat collider
   public GenericCombatCollider combatCollider;

   // The total damage dealt
   [SyncVar]
   public int totalDamageDealt = 0;

   #endregion

   protected virtual bool isBot () { return true; }

   protected override void Start () {
      base.Start();

      // Keep track in our Sea Manager
      SeaManager.self.storeEntity(this);

      // Removed to improve performance. If used again, add a kinematic rigidbody2D.
      // Make sea entities clickable on the client
      //if (isClient) {
      //   ClickTrigger clickTrigger = Instantiate(PrefabsManager.self.clickTriggerPrefab);
      //   clickTrigger.transform.SetParent(this.transform);
      //}

      // Set our sprite sheets according to our types
      if (!Util.isBatch()) {
         StartCoroutine(CO_UpdateAllSprites());
      }
   }

   [Server]
   public int applyDamage (int amount, uint damageSourceNetId) {
      float damageMultiplier = 1.0f;

      // Apply damage reduction, if there is any
      if (this is PlayerShipEntity) {
         // Hard cap damage reduction at 75%, and damage addition at 100% extra
         damageMultiplier = Mathf.Clamp(damageMultiplier - PowerupManager.self.getPowerupMultiplierAdditive(userId, Powerup.Type.DamageReduction), 0.25f, 2.0f);
      }
      amount = (int) (amount * damageMultiplier);
      currentHealth -= amount;

      noteAttacker(damageSourceNetId);

      if (isDead()) {
         onDeath();
      }

      // Return the final amount of damage dealt
      return amount;
   }

   public virtual void onDeath () {
      if (_hasRunOnDeath) {
         return;
      }

      _hasRunOnDeath = true;
   }

   public GenericCombatCollider getCombatCollider () {
      if (combatCollider == null) {
         combatCollider = GetComponentInChildren<GenericCombatCollider>();
      }

      return combatCollider;
   }

   public void reloadSprites () {
      if (!Util.isBatch()) {
         StartCoroutine(CO_UpdateAllSprites());
      }
   }

   protected override void Update () {
      base.Update();

      // If we've died, start slowing moving our sprites downward
      if (isDead()) {
         _outline.setVisibility(false);
         disableCollisions();

         foreach (Coroutine coroutine in _burningCoroutines) {
            if (coroutine != null) {
               StopCoroutine(coroutine);
            }
         }

         if (this is SeaMonsterEntity) {
            SeaMonsterEntity monsterEntity = GetComponent<SeaMonsterEntity>();

            if (monsterEntity.seaMonsterData.roleType == RoleType.Minion) {
               monsterEntity.corpseHolder.SetActive(true);
               spritesContainer.SetActive(false);
               return;
            } else {
               foreach (SpriteRenderer renderer in _renderers) {
                  Util.setLocalY(spritesContainer.transform, spritesContainer.transform.localPosition.y - .03f * Time.smoothDeltaTime);
                  if (renderer.enabled) {
                     float newAlpha = Mathf.Lerp(1f, 0f, spritesContainer.transform.localPosition.y * -10f);
                     Util.setMaterialBlockAlpha(renderer, newAlpha);
                  }
               }
            }
         } else if (!_playedDestroySound && this is ShipEntity && isClient) {
            _playedDestroySound = true;
            SoundManager.play2DClip(SoundManager.Type.Ship_Destroyed);

            // Hide all the sprites
            foreach (SpriteRenderer renderer in _renderers) {
               renderer.enabled = false;
            }

            // Play the explosion effect
            Instantiate(isBot() ? PrefabsManager.self.pirateShipExplosionEffect : PrefabsManager.self.playerShipExplosionEffect, transform.position, Quaternion.identity);
         }

         Util.setLocalY(spritesContainer.transform, spritesContainer.transform.localPosition.y - .03f * Time.smoothDeltaTime);
      }
   }

   public virtual void playAttackSound () {
      // Let other classes override and implement this functionality
   }

   public bool hasRecentCombat () {
      // Did we recently attack?
      if (NetworkTime.time - _lastAttackTime < RECENT_COMBAT_COOLDOWN) {
         return true;
      }

      // Did we recently take damage?
      if (NetworkTime.time - _lastDamagedTime < RECENT_COMBAT_COOLDOWN) {
         return true;
      }

      return false;
   }

   public override bool hasAnyCombat () {
      return (hasAttackers() || hasRecentCombat());
   }

   public void disableCollisions () {
      foreach (Collider2D col in colliderList) {
         col.enabled = false;
      }
   }

   [ClientRpc]
   private void Rpc_TriggerAttackAnim (Anim.Type animType) {
      requestAnimationPlay(animType);
   }

   public virtual void requestAnimationPlay (Anim.Type animType) {

   }

   [TargetRpc]
   public void Target_CreateLocalAttackCircle (NetworkConnection connection, Vector2 startPos, Vector2 endPos, double startTime, double endTime) {
      // Create a new Attack Circle object from the prefab
      AttackCircle attackCircle = Instantiate(localAttackCirclePrefab, endPos, Quaternion.identity);
      attackCircle.creator = this;
      attackCircle.startPos = startPos;
      attackCircle.endPos = endPos;
      attackCircle.startTime = startTime;
      attackCircle.endTime = endTime;
   }

   [Server]
   public void chainLightning (uint attackerNetId, Vector2 sourcePos, uint primaryTargetNetID) {
      Collider2D[] hits = Physics2D.OverlapCircleAll(sourcePos, 1);
      Dictionary<NetEntity, Transform> collidedEntities = new Dictionary<NetEntity, Transform>();
      List<uint> targetIDList = new List<uint>();
      ShipAbilityData abilityData = getSeaAbility(Attack.Type.Shock_Ball);
      ProjectileStatData projectileData = ProjectileStatManager.self.getProjectileData(abilityData.projectileId);
      int damage = (int) (projectileData.projectileDamage * Attack.getDamageModifier(Attack.Type.Shock_Ball));

      foreach (Collider2D collidedEntity in hits) {
         if (collidedEntity != null) {
            if (collidedEntity.GetComponent<SeaEntity>() != null) {
               SeaEntity seaEntity = collidedEntity.GetComponent<SeaEntity>();
               if (this.isEnemyOf(seaEntity) && !collidedEntities.ContainsKey(seaEntity) && !seaEntity.isDead() && seaEntity.instanceId == this.instanceId) {
                  int finalDamage = seaEntity.applyDamage(damage, attackerNetId);
                  seaEntity.Rpc_ShowExplosion(attackerNetId, collidedEntity.transform.position, finalDamage, Attack.Type.None, false);

                  // Registers the action electrocuted to the userID to the achievement database for recording
                  AchievementManager.registerUserAchievement(seaEntity, ActionType.Electrocuted);

                  collidedEntities.Add(seaEntity, collidedEntity.transform);
                  targetIDList.Add(seaEntity.netId);
               }
            }
         }
      }

      Rpc_ChainLightning(targetIDList.ToArray(), primaryTargetNetID, sourcePos);
   }

   [Server]
   public void cannonballChainLightning (uint attackerNetId, Vector2 sourcePos, uint primaryTargetNetID, float chainRadius, float damage) {
      Collider2D[] hits = Physics2D.OverlapCircleAll(sourcePos, chainRadius);
      Dictionary<NetEntity, Transform> collidedEntities = new Dictionary<NetEntity, Transform>();
      List<uint> targetNetIdList = new List<uint>();
      int damageInt = (int) damage;

      foreach (Collider2D hit in hits) {
         if (hit != null && hit.GetComponent<SeaEntity>() != null) {
            SeaEntity hitEntity = hit.GetComponent<SeaEntity>();
            if (this.isEnemyOf(hitEntity) && !collidedEntities.ContainsKey(hitEntity) && !hitEntity.isDead() && hitEntity.instanceId == this.instanceId && hitEntity.netId != primaryTargetNetID) {
               int finalDamage = hitEntity.applyDamage(damageInt, attackerNetId);
               hitEntity.Rpc_ShowDamage(Attack.Type.None, hitEntity.transform.position, finalDamage);

               collidedEntities.Add(hitEntity, hit.transform);
               targetNetIdList.Add(hitEntity.netId);
            }
         }
      }

      Rpc_ChainLightning(targetNetIdList.ToArray(), primaryTargetNetID, sourcePos);
   }

   [ClientRpc]
   public void Rpc_SpawnVenomResidue (uint creatorNetId, int instanceId, Vector3 location) {
      VenomResidue venomResidue = Instantiate(PrefabsManager.self.venomResiduePrefab, location, Quaternion.identity);
      venomResidue.creatorNetId = creatorNetId;
      venomResidue.instanceId = instanceId;
      ExplosionManager.createSlimeExplosion(location);
      SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Coralbow_Attack, this.transform.position);
   }

   [ClientRpc]
   private void Rpc_ChainLightning (uint[] targetNetIDList, uint primaryTargetNetID, Vector2 sourcePos) {
      SeaEntity parentEntity = SeaManager.self.getEntity(primaryTargetNetID);

      GameObject shockResidue = Instantiate(PrefabsManager.self.lightningResiduePrefab);
      shockResidue.transform.SetParent(parentEntity.spritesContainer.transform, false);
      EffectManager.self.create(Effect.Type.Shock_Collision, sourcePos);

      foreach (uint attackerNetID in targetNetIDList) {
         SeaEntity seaEntity = SeaManager.self.getEntity(attackerNetID);
         LightningBoltScript lightning = Instantiate(PrefabsManager.self.lightningChainPrefab);
         lightning.transform.SetParent(shockResidue.transform, false);

         lightning.StartObject.transform.position = lightning.transform.position;

         lightning.EndObject.transform.position = seaEntity.spritesContainer.transform.position;
         lightning.EndObject.transform.SetParent(seaEntity.spritesContainer.transform);

         lightning.GetComponent<LineRenderer>().enabled = true;

         GameObject subShockResidue = Instantiate(PrefabsManager.self.lightningResiduePrefab);
         subShockResidue.transform.SetParent(seaEntity.spritesContainer.transform, false);

         EffectManager.self.create(Effect.Type.Shock_Collision, seaEntity.transform.position);
      }
   }

   [ClientRpc]
   public void Rpc_CreateAttackCircle (Vector2 startPos, Vector2 endPos, double startTime, double endTime, int abilityId, bool showCircle) {
      ShipAbilityData abilityData = getSeaAbility(abilityId);

      if (showCircle) {
         // Create a new Attack Circle object from the prefab
         AttackCircle attackCircle = Instantiate(defaultAttackCirclePrefab, endPos, Quaternion.identity);
         attackCircle.creator = this;
         attackCircle.startPos = startPos;
         attackCircle.endPos = endPos;
         attackCircle.startTime = startTime;
         attackCircle.endTime = endTime;
      }

      GenericSeaProjectile seaEntityProjectile = Instantiate(PrefabsManager.self.seaEntityProjectile, startPos, Quaternion.identity);
      seaEntityProjectile.init(startTime, endTime, startPos, endPos, this, abilityData.abilityId);

      AudioClipManager.AudioClipData audioClipData = AudioClipManager.self.getAudioClipData(abilityData.castSFXPath);
      if (audioClipData.audioPath.Length > 1) {
         AudioClip clip = audioClipData.audioClip;
         if (clip != null) {
            SoundManager.playClipAtPoint(clip, Camera.main.transform.position);
         }
      } else {
         SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Attack_Fire, this.transform.position);
      }

      if (abilityData.selectedAttackType == Attack.Type.Shock_Ball) {
         seaEntityProjectile.setDirection((Direction) facing);
      }

      // Create a cannon smoke effect
      Vector2 direction = endPos - startPos;
      Vector2 offset = direction.normalized * .1f;
      Instantiate(PrefabsManager.self.poofPrefab, startPos + offset, Quaternion.identity);

      // If it was our ship, shake the camera
      if (isLocalPlayer) {
         CameraManager.shakeCamera();
      }
   }

   [ClientRpc]
   public void Rpc_FireHomingCannonBall (GameObject source, GameObject target, double startTime, double endTime) {
      // Create a cannon smoke effect
      Vector2 direction = target.transform.position - source.transform.position;
      Vector3 offset = direction.normalized * .1f;
      Vector3 startPos = source.transform.position + offset;
      Instantiate(PrefabsManager.self.poofPrefab, startPos, Quaternion.identity);

      // Create a cannon ball
      GenericSeaProjectile ball = Instantiate(PrefabsManager.self.seaEntityProjectile, startPos, Quaternion.identity);

      // TODO: Update and confirm if this system will be used
      D.editorLog("Update homing attack system!", Color.red);
      ball.init(startTime, endTime, startPos, target.transform.position, this, -1, target);

      // Play an appropriate sound
      playAttackSound();
   }

   [ClientRpc]
   public void Rpc_ShowExplosion (uint attackerNetId, Vector2 pos, int damage, Attack.Type attackType, bool isCrit) {
      _lastDamagedTime = NetworkTime.time;

      if (attackType == Attack.Type.None) {
         List<Effect.Type> effectTypes = EffectManager.getEffects(attackType);
         EffectManager.show(effectTypes, pos);

         // Play the damage sound
         SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Ship_Hit_1, pos);
      } else {
         if (attackType == Attack.Type.Tentacle) {
            // If tentacle attack, calls tentacle collision effect
            Instantiate(PrefabsManager.self.tentacleCollisionPrefab, this.transform.position + new Vector3(0f, 0), Quaternion.identity);
         } else if (attackType == Attack.Type.Venom) {
            // If worm attack, calls slime collision effect
            ExplosionManager.createSlimeExplosion(pos);
         } else if (attackType == Attack.Type.Ice) {
            // TODO: Add ice effect logic here
         } else {
            ShipAbilityData shipData = getSeaAbility(attackType);
            if (shipData == null) {
               // Show generic explosion
               Instantiate(PrefabsManager.self.requestCannonExplosionPrefab(currentImpactMagnitude), pos, Quaternion.identity);
            } else {
               EffectManager.createDynamicEffect(shipData.collisionSpritePath, pos, shipData.abilitySpriteFXPerFrame);
               // TODO: Add sfx here for ability collision
            }
         }

         // Show the damage text
         ShipDamageText damageText = Instantiate(PrefabsManager.self.getTextPrefab(attackType), pos, Quaternion.identity);
         damageText.setDamage(damage);

         if (isCrit) {
            GameObject critEffect = Instantiate(PrefabsManager.self.shipCritPrefab, pos, Quaternion.identity);
            Destroy(critEffect, 2.0f);
         }

         noteAttacker(attackerNetId);

         // Play the damage sound
         SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Ship_Hit_2, pos);
      }

      // If it was our ship, shake the camera
      if (isLocalPlayer) {
         CameraManager.shakeCamera();
      }
   }

   [ClientRpc]
   public void Rpc_ShowDamageTaken (int damage, bool shakeCamera) {
      // Show the damage text
      ShipDamageText damageText = Instantiate(PrefabsManager.self.getTextPrefab(Attack.Type.None), transform.position, Quaternion.identity);
      damageText.setDamage(damage);

      if (shakeCamera && isLocalPlayer) {
         CameraManager.shakeCamera();
      }
   }

   [ClientRpc]
   public void Rpc_ShowTerrainHit (Vector3 pos, Attack.ImpactMagnitude impactMagnitude) {
      if (Util.hasLandTile(pos)) {
         Instantiate(PrefabsManager.self.requestCannonSmokePrefab(impactMagnitude), pos, Quaternion.identity);
         SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Slash_Lightning, pos);
      } else {
         Instantiate(PrefabsManager.self.requestCannonSplashPrefab(impactMagnitude), pos + new Vector3(0f, -.1f), Quaternion.identity);
         SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Splash_Cannon_1, pos);
      }
   }

   [ClientRpc]
   public void Rpc_AttachEffect (int damage, Attack.Type attackType) {
      Transform targetTransform = spritesContainer.transform;

      // If venom attack calls slime effect
      if (attackType == Attack.Type.Venom) {
         GameObject stickyInstance = Instantiate(PrefabsManager.self.venomStickyPrefab, targetTransform);
         stickyInstance.transform.localPosition = Vector3.zero;
      }
   }

   [ClientRpc]
   public void Rpc_NetworkProjectileDamage (uint attackerNetID, Attack.Type attackType, Vector3 location) {
      SeaEntity sourceEntity = SeaManager.self.getEntity(attackerNetID);
      noteAttacker(sourceEntity);

      switch (attackType) {
         case Attack.Type.Boulder:
            // Apply the status effect
            ExplosionManager.createRockExplosion(location);
            SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Boulder, location);
            break;
         case Attack.Type.Venom:
            // Apply the status effect
            StatusManager.self.create(Status.Type.Slowed, 0.3f, 3f, attackerNetID);
            ExplosionManager.createSlimeExplosion(location);
            SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Attack_Fire, location);
            break;
      }
   }

   [ClientRpc]
   protected void Rpc_NoteAttack () {
      // Note the time at which we last performed an attack
      _lastAttackTime = NetworkTime.time;
   }

   public double getLastAttackTime () {
      return _lastAttackTime;
   }

   [ClientRpc]
   public void Rpc_NoteAttacker (uint netId) {
      noteAttacker(netId);
   }

   public virtual void noteAttacker (NetEntity entity) {
      noteAttacker(entity.netId);
   }

   public virtual void noteAttacker (uint netId) {
      _attackers[netId] = NetworkTime.time;
      _lastAttackerNetId = netId;
   }

   public bool hasReloaded () {
      double timeSinceAttack = NetworkTime.time - _lastAttackTime;
      return timeSinceAttack > this.reloadDelay;
   }

   public int getDamageForShot (Attack.Type attackType, float distanceModifier) {
      return (int) (this.damage * Attack.getDamageModifier(attackType) * distanceModifier);
   }

   public int getDamageForShot (int baseDamage, float distanceModifier) {
      return (int) (baseDamage * distanceModifier);
   }

   protected IEnumerator CO_UpdateAllSprites () {
      // Wait until we receive data of the player, but if we're a bot we skip the wait as we already have the data
      while (!isBot() && Util.isEmpty(entityName)) {
         yield return null;
      }

      updateSprites();

      assignEntityName();

   }

   protected virtual void assignEntityName () {
     
   }

   [Command]
   public void Cmd_MeleeAtSpot (Vector2 spot, Attack.Type attackType) {
      // We handle the logic in a non-Cmd function so that it can be called directly on the server if needed
      meleeAtSpot(spot, attackType);
   }

   [Server]
   public void meleeAtSpot (Vector2 spot, Attack.Type attackType) {
      if (isDead() || !hasReloaded()) {
         return;
      }

      // Note the time at which we last successfully attacked
      _lastAttackTime = NetworkTime.time;

      float distance = Vector2.Distance(this.transform.position, spot);
      float delay = Mathf.Clamp(distance, .5f, 1.5f);

      // Have the server check for collisions after the attack reaches the target
      StartCoroutine(CO_CheckCircleForCollisions(this, delay, spot, attackType, true, 1f, currentImpactMagnitude, -1));

      // Make note on the clients that the ship just attacked
      Rpc_NoteAttack();
   }

   [Server]
   public void fireAtSpot (Vector2 spot, int abilityId, float attackDelay, float launchDelay, Vector2 spawnPosition = new Vector2()) {
      // Get the ability data
      ShipAbilityData shipAbility = getSeaAbility(abilityId);

      if (shipAbility == null) {
         return;
      }

      if (isDead() || (!hasReloaded() && shipAbility.selectedAttackType != Attack.Type.Mini_Boulder && shipAbility.selectedAttackType != Attack.Type.Tentacle)) {
         return;
      }

      switch (this.facing) {
         case Direction.North:
            Rpc_TriggerAttackAnim(Anim.Type.Attack_North);
            break;
         case Direction.South:
            Rpc_TriggerAttackAnim(Anim.Type.Attack_South);
            break;
         default:
            Rpc_TriggerAttackAnim(Anim.Type.Attack_East);
            break;
      }

      switch (shipAbility.selectedAttackType) {
         case Attack.Type.Venom:
         case Attack.Type.Boulder:
            // Create networked projectile
            D.adminLog("Trigger timed projectile: " + shipAbility.selectedAttackType, D.ADMIN_LOG_TYPE.Sea);
            fireTimedGenericProjectile(spawnPosition, spot, abilityId, launchDelay);
            break;
         case Attack.Type.Mini_Boulder:
            // Fire multiple projectiles around the entity
            float offset = .2f;
            float target = .5f;
            float diagonalValue = offset;
            float diagonalTargetValue = target;
            Vector2 sourcePos = spawnPosition;

            if (attackCounter % 2 == 0) {
               // North East West South Attack Pattern
               StartCoroutine(CO_FireAtSpotSingle(sourcePos + new Vector2(0, target), abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, sourcePos + new Vector2(0, offset)));
               StartCoroutine(CO_FireAtSpotSingle(sourcePos + new Vector2(0, -target), abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, sourcePos + new Vector2(0, -offset)));
               StartCoroutine(CO_FireAtSpotSingle(sourcePos + new Vector2(target, 0), abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, sourcePos + new Vector2(offset, 0)));
               StartCoroutine(CO_FireAtSpotSingle(sourcePos + new Vector2(-target, 0), abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, sourcePos + new Vector2(-offset, 0)));
            } else {
               // Diagonal Attack Pattern
               StartCoroutine(CO_FireAtSpotSingle(sourcePos + new Vector2(diagonalTargetValue, target), abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, sourcePos + new Vector2(diagonalValue, offset)));
               StartCoroutine(CO_FireAtSpotSingle(sourcePos + new Vector2(diagonalTargetValue, -target), abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, sourcePos + new Vector2(diagonalValue, -offset)));
               StartCoroutine(CO_FireAtSpotSingle(sourcePos + new Vector2(-target, diagonalTargetValue), abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, sourcePos + new Vector2(-offset, diagonalValue)));
               StartCoroutine(CO_FireAtSpotSingle(sourcePos + new Vector2(-target, -diagonalTargetValue), abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, sourcePos + new Vector2(-offset, -diagonalValue)));
            }
            break;
         default:
            D.adminLog("Trigger generic projectile at spot, Delay is: " + attackDelay, D.ADMIN_LOG_TYPE.Sea);
            StartCoroutine(CO_FireAtSpotSingle(spot, abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, spawnPosition));
            break;
      }

      _lastAttackTime = NetworkTime.time;
      attackCounter++;
      Rpc_RegisterAttackTime(attackDelay);

      Rpc_NoteAttack();
   }

   [Server]
   private IEnumerator CO_FireAtSpotSingle (Vector2 spot, int abilityId, Attack.Type attackType, float attackDelay, float launchDelay, Vector2 spawnPosition = new Vector2()) {
      float distance = Vector2.Distance(this.transform.position, spot);
      float timeToReachTarget = Mathf.Clamp(distance, .5f, 1.5f) * 1.1f;

      // Modify projectile speed based on attack type
      timeToReachTarget /= Attack.getSpeedModifier(attackType);

      // Speed modifiers for the projectile types
      ProjectileStatData projectileData = getProjectileDataFromAbility(abilityId);
      if (projectileData != null) {
         // The higher the mass, the slower the projectile will reach its target
         timeToReachTarget /= projectileData.projectileMass;
      }

      // Wait for the attack delay if any
      yield return new WaitForSeconds(attackDelay);

      // Prevent sea monsters from damaging other sea monsters
      bool targetPlayersOnly = this is SeaMonsterEntity;

      D.adminLog("TimeNow:" + NetworkTime.time.ToString("f1") +
         " AttackDelay:" + attackDelay +
         " TimeToReachTarget:" + timeToReachTarget.ToString("f1") +
         " LaunchDelay: " + launchDelay, D.ADMIN_LOG_TYPE.Sea);

      spawnProjectileAndIndicatorsOnClients(spot, abilityId, spawnPosition, launchDelay + timeToReachTarget);
      StartCoroutine(CO_CheckCircleForCollisions(this, launchDelay + timeToReachTarget, spot, attackType, targetPlayersOnly, 1f, currentImpactMagnitude, abilityId));
   }

   [Server]
   protected void spawnProjectileAndIndicatorsOnClients (Vector2 spot, int abilityId, Vector2 spawnPosition, float delay) {
      // Creates the projectile and the target circle
      if (this is PlayerShipEntity) {
         Target_CreateLocalAttackCircle(connectionToClient, this.transform.position, spot, NetworkTime.time, NetworkTime.time + delay);
         Rpc_CreateAttackCircle(spawnPosition, spot, NetworkTime.time, NetworkTime.time + delay, abilityId, false);
      } else {
         Rpc_CreateAttackCircle(spawnPosition, spot, NetworkTime.time, NetworkTime.time + delay, abilityId, true);
      }
   }

   [Server]
   protected IEnumerator CO_CheckCircleForCollisions (SeaEntity attacker, float delay, Vector2 circleCenter, Attack.Type attackType, bool targetPlayersOnly,
      float distanceModifier, Attack.ImpactMagnitude impactMagnitude, int abilityId) {
      // Wait until the cannon ball reaches the target
      yield return new WaitForSeconds(delay);

      bool hitEnemy = false;
      List<NetEntity> enemyHitList = new List<NetEntity>();
      Collider2D[] getColliders = attackType == Attack.Type.Tentacle_Range ? getHitColliders(circleCenter, .1f) : getHitColliders(circleCenter);
      // Check for collisions inside the circle
      foreach (Collider2D hit in getColliders) {
         if (hit != null) {
            SeaEntity targetEntity = hit.GetComponent<SeaEntity>();

            if (targetEntity != null && !enemyHitList.Contains(targetEntity)) {
               if (targetPlayersOnly && hit.GetComponent<ShipEntity>() == null) {
                  continue;
               }

               // Make sure we don't hit ourselves
               if (targetEntity == this) {
                  continue;
               }

               // Check if the attacker and the target are allies
               if (attacker.isAllyOf(targetEntity)) {
                  continue;
               }

               // Prevent players from damaging each other in PvE voyage instances
               if (attacker.isAdversaryInPveInstance(targetEntity)) {
                  continue;
               }

               // Make sure the target is in our same instance
               if (targetEntity.instanceId == this.instanceId) {
                  // Prevent players from being damaged by other players if they have not entered PvP yet
                  if (attacker.isPlayerShip() && !targetEntity.canBeAttackedByPlayers()) {
                     continue;
                  }

                  hitEnemy = true;

                  if (!targetEntity.invulnerable) {
                     int damage = getDamageForShot(attackType, distanceModifier);
                     if (abilityId > 0) {
                        ShipAbilityData shipAbilityData = getSeaAbility(abilityId);
                        ProjectileStatData projectileData = getProjectileDataFromAbility(abilityId);
                        float abilityDamageModifier = projectileData.projectileDamage * shipAbilityData.damageModifier;
                        float baseSkillDamage = projectileData.projectileDamage + abilityDamageModifier;

                        damage = getDamageForShot((int) baseSkillDamage, distanceModifier);

                        // TODO: Observe damage formula on live build
                        D.adminLog("Damage fetched for sea entity logic"
                           + " DistanceDamage: " + damage
                           + " Computed: " + baseSkillDamage
                           + " Ability: " + abilityDamageModifier
                           + " Dist Modifier: " + distanceModifier
                           + " Name: " + getSeaAbility(abilityId).abilityName
                           + " ID: " + getSeaAbility(abilityId).abilityId
                           + " Projectile ID: " + projectileData.projectileId, D.ADMIN_LOG_TYPE.Sea);
                     }

                     int finalDamage = targetEntity.applyDamage(damage, attacker.netId);
                     int targetHealthAfterDamage = targetEntity.currentHealth - finalDamage;

                     if (this is PlayerShipEntity) {
                        if (targetEntity is SeaMonsterEntity) {
                           if (targetHealthAfterDamage <= 0) {
                              // Registers the action Sea Monster Killed to the achievement database for recording
                              AchievementManager.registerUserAchievement(this, ActionType.KillSeaMonster);
                           }
                           // Registers the sea monster cannon hit
                           AchievementManager.registerUserAchievement(this, ActionType.HitSeaMonster);
                        } else if (targetEntity is BotShipEntity) {
                           if (targetHealthAfterDamage <= 0) {
                              // Registers the action Sunked Ships to the achievement database for recording
                              AchievementManager.registerUserAchievement(this, ActionType.SinkedShips);
                           }
                           // Registers the cannon hit action specifically for other player ship to the achievement database 
                           AchievementManager.registerUserAchievement(this, ActionType.HitEnemyShips);
                        } else if (targetEntity is PlayerShipEntity) {
                           if (targetHealthAfterDamage <= 0) {
                              // Registers the ship death action of the user to the achievement database for recording of death count
                              AchievementManager.registerUserAchievement(targetEntity, ActionType.ShipDie);
                           }
                           // Registers the cannon hit action specifically for other player ship to the achievement database 
                           AchievementManager.registerUserAchievement(this, ActionType.HitPlayerWithCannon);
                        }

                        // Registers the cannon hit action of the user to the achievement database for recording of accuracy
                        AchievementManager.registerUserAchievement(this, ActionType.CannonHits);
                     }

                     if (this is SeaMonsterEntity) {
                        if (targetEntity is PlayerShipEntity && targetHealthAfterDamage <= 0) {
                           // Registers the ship death action of the user to the achievement database for recording of death count
                           AchievementManager.registerUserAchievement(targetEntity, ActionType.ShipDie);
                        }
                     }

                     targetEntity.Rpc_ShowExplosion(attacker.netId, circleCenter, finalDamage, attackType, false);

                     if (attackType == Attack.Type.Shock_Ball) {
                        chainLightning(attacker.netId, targetEntity.transform.position, targetEntity.netId);
                     }
                  } else {
                     targetEntity.Rpc_ShowExplosion(attacker.netId, circleCenter, 0, Attack.Type.None, false);
                  }
                  targetEntity.noteAttacker(attacker);

                  // Apply any status effects from the attack
                  if (attackType == Attack.Type.Ice) {
                     // If enemy ship freezes a player ship
                     if (this is BotShipEntity) {
                        if (targetEntity is PlayerShipEntity) {
                           // Registers the frozen action status to the achievementdata for recording
                           AchievementManager.registerUserAchievement(targetEntity, ActionType.Frozen);
                        }
                     }

                     StatusManager.self.create(Status.Type.Frozen, 1.0f, 2f, targetEntity.netId);
                  } else if (attackType == Attack.Type.Venom) {
                     StatusManager.self.create(Status.Type.Slowed, 0.3f, 1f, targetEntity.netId);
                  }
                  enemyHitList.Add(targetEntity);
               }
            }
         }
      }

      if (attackType == Attack.Type.Tentacle) {
         SeaEntity sourceEntity = SeaManager.self.getEntity(netId);
         VenomResidue venomResidue = Instantiate(PrefabsManager.self.venomResiduePrefab, circleCenter, Quaternion.identity);
         venomResidue.creatorNetId = netId;
         venomResidue.instanceId = instanceId;
         sourceEntity.Rpc_SpawnVenomResidue(netId, instanceId, circleCenter);
      }

      // If we didn't hit an enemy, show an effect based on whether we hit land or water
      if (!hitEnemy) {
         Rpc_ShowTerrainHit(circleCenter, impactMagnitude);
      }
   }

   public static Collider2D[] getHitColliders (Vector2 circleCenter, float radius = .20f) {
      // Check for collisions inside the circle
      Collider2D[] hits = new Collider2D[40];
      Physics2D.OverlapCircleNonAlloc(circleCenter, radius, hits, LayerMask.GetMask(LayerUtil.SHIPS, LayerUtil.SEA_MONSTERS));

      return hits;
   }

   [Server]
   private void fireTimedGenericProjectile (Vector2 startPos, Vector2 targetPos, int abilityId, float launchDelay) {
      if (isDead()) {
         return;
      }

      ShipAbilityData abilityData = getSeaAbility(abilityId);
      int attackCount = 1;
      if (abilityData.splitsAfterAttackCap) {
         // Shoots 3 projectiles each 3 attacks
         if (attackCounter % abilityData.splitAttackCap == 0) {
            attackCount = abilityData.splitAttackCap;
         }
      }

      // We either fire out the left or right side depending on which was clicked
      for (int i = 0; i < attackCount; i++) {
         Vector2 direction = targetPos - (Vector2) startPos;
         direction = direction.normalized;

         if (attackCount > 1) {
            direction = direction.Rotate(i * 10f);
         }

         // Figure out the desired velocity
         Vector2 velocity = direction.normalized * abilityData.projectileSpeed;//NetworkedVenomProjectile.MOVE_SPEED;

         // Delay the firing a little bit to compensate for lag
         double timeToStartFiring = NetworkTime.time + launchDelay + 0.150f;
         D.adminLog("TimeNow:" + NetworkTime.time +
            " TimeToFire:" + timeToStartFiring +
            " Seconds:" + (NetworkTime.time - timeToStartFiring +
            " LaunchDelay: " + launchDelay), D.ADMIN_LOG_TYPE.Sea);

         // Note the time at which we last successfully attacked
         _lastAttackTime = NetworkTime.time;

         // Make note on the clients that the ship just attacked
         Rpc_NoteAttack();

         // Tell all clients to fire the venom projectile at the same time
         Rpc_FireTimedGenericProjectile(timeToStartFiring, velocity, startPos, targetPos, abilityId);

         // Standalone Server needs to call this as well
         if (!MyNetworkManager.isHost) {
            StartCoroutine(CO_FireTimedGenericProjectile(timeToStartFiring, velocity, startPos, targetPos, abilityId));
         }
      }
   }

   [ClientRpc]
   public void Rpc_FireTimedGenericProjectile (double startTime, Vector2 velocity, Vector3 startPos, Vector3 endPos, int abilityId) {
      StartCoroutine(CO_FireTimedGenericProjectile(startTime, velocity, startPos, endPos, abilityId));
   }

   protected IEnumerator CO_FireTimedGenericProjectile (double startTime, Vector2 velocity, Vector3 startPos, Vector3 endPos, int abilityId) {
      while (NetworkTime.time < startTime) {
         yield return null;
      }

      // Create the projectile object from the prefab
      GameObject projectileObj = Instantiate(PrefabsManager.self.networkProjectilePrefab, startPos, Quaternion.identity);
      NetworkedProjectile networkProjectile = projectileObj.GetComponent<NetworkedProjectile>();
      networkProjectile.init(this.netId, this.instanceId, currentImpactMagnitude, abilityId, startPos);
      networkProjectile.setDirection((Direction) facing, endPos);

      // Add velocity to the projectile
      networkProjectile.body.velocity = velocity;

      // Destroy the projectile after a couple seconds
      Destroy(projectileObj, getSeaAbility(abilityId).lifeTime);
   }

   [ClientRpc]
   public void Rpc_RegisterAttackTime (float delayTime) {
      _attackStartAnimateTime = Time.time + delayTime;
      _hasAttackAnimTriggered = false;
   }

   protected virtual void updateSprites () { }

   private ShipAbilityData getSeaAbility (int abilityId) {
      if (_cachedSeaAbilityList.Exists(_ => _.abilityId == abilityId)) {
         return _cachedSeaAbilityList.Find(_ => _.abilityId == abilityId);
      }

      ShipAbilityData abilityData = ShipAbilityManager.self.getAbility(abilityId);
      _cachedSeaAbilityList.Add(abilityData);
      return abilityData;
   }

   private ShipAbilityData getSeaAbility (Attack.Type attackType) {
      if (_cachedSeaAbilityList.Exists(_ => _.selectedAttackType == attackType)) {
         return _cachedSeaAbilityList.Find(_ => _.selectedAttackType == attackType);
      }

      ShipAbilityData abilityData = ShipAbilityManager.self.getAbility(attackType);
      _cachedSeaAbilityList.Add(abilityData);
      return abilityData;
   }

   private ProjectileStatData getProjectileDataFromAbility (int abilityId) {
      ShipAbilityData ability = getSeaAbility(abilityId);
      int projectileId = ability.projectileId;

      if (_cachedProjectileList.Exists(_ => _.projectileId == projectileId)) {
         return _cachedProjectileList.Find(_ => _.projectileId == projectileId);
      }

      ProjectileStatData projectileData = ProjectileStatManager.self.getProjectileData(projectileId);
      _cachedProjectileList.Add(projectileData);
      return projectileData;
   }

   public void applyStatus (Status.Type statusType, float strength, float duration, uint attackerNetId) {
      Status newStatus = StatusManager.self.create(statusType, strength, duration, netId);

      if (statusType == Status.Type.Burning) {
         _burningCoroutines.Add(applyDamageOverTime((int) strength, 1.0f, duration, attackerNetId));
      }

      Rpc_ApplyStatusIcon(statusType, duration);
   }

   [ClientRpc]
   public void Rpc_ApplyStatusIcon (Status.Type statusType, float length) {
      // If this net entity doesn't have its prefab set up for status icons yet, don't try to add one
      if (statusEffectContainer) {
         // If this entity already has an icon of this type, update it
         if (_statusIcons.ContainsKey(statusType) && _statusIcons[statusType] != null) {
            StatusIcon existingIcon = _statusIcons[statusType];
            existingIcon.setLongestLifetime(length);

            // If the status effect is new, create a new icon
         } else {
            StatusIcon statusIcon = StatusManager.self.getStatusIcon(statusType, length, statusEffectContainer).GetComponent<StatusIcon>();
            statusIcon.setLifetime(length);
            statusIcon.statusType = statusType;
            statusIcon.GetComponent<RectTransform>().sizeDelta = Vector2.one * 16;
            _statusIcons[statusType] = statusIcon;
         }
      }
   }

   public Coroutine applyDamageOverTime (int tickDamage, float tickInterval, float duration, uint attackerNetId) {
      // Applies a damage over time effect to this net entity, dealing 'tickDamage' damage every 'tickInterval' seconds, for 'duration' seconds
      return StartCoroutine(CO_DamageOverTime(tickDamage, tickInterval, duration, attackerNetId));
   }

   private IEnumerator CO_DamageOverTime (int tickDamage, float tickInterval, float duration, uint attackerNetId) {
      float totalTimer = 0.0f;
      float tickTimer = 0.0f;

      while (totalTimer <= duration) {
         totalTimer += Time.deltaTime;
         tickTimer += Time.deltaTime;

         // If enough time has passed for a damage tick, apply it
         if (tickTimer >= tickInterval) {
            int finalTickDamage = applyDamage(tickDamage, attackerNetId);

            Rpc_ShowDamage(Attack.Type.Fire, transform.position, finalTickDamage);

            tickTimer -= tickInterval;
         }

         yield return null;
      }
   }

   [ClientRpc]
   public void Rpc_ShowExplosiveShotEffect (Vector2 position, float radius) {
      GameObject effect = Instantiate(PrefabsManager.self.explosiveShotEffectPrefab, position, Quaternion.identity, null);
      float tempEffectScale = 3.0f;
      effect.transform.localScale = Vector3.one * tempEffectScale * radius;
   }

   #region Private Variables

   // The cached sea ability list
   private List<ShipAbilityData> _cachedSeaAbilityList = new List<ShipAbilityData>();

   // The cached sea projectile list
   private List<ProjectileStatData> _cachedProjectileList = new List<ProjectileStatData>();

   // Current Spawn Transform
   protected Transform _projectileSpawnLocation;

   // The time at which we last fired an attack
   protected double _lastAttackTime = float.MinValue;

   // The time at which we last took damage
   protected double _lastDamagedTime = float.MinValue;

   // How far back in time we check to see if this ship was recently involved in some combat action
   protected static float RECENT_COMBAT_COOLDOWN = 5f;

   // The time expected to play the animation
   protected double _attackStartAnimateTime = 100;

   // The time expected to reset the animation
   protected double _attackEndAnimateTime = 0;

   // A flag to check if the attack anim has been triggered
   protected bool _hasAttackAnimTriggered = false;

   // Check if sound used when ship is being destroyed, was already played
   protected bool _playedDestroySound = false;

   // A dictionary of references to all the status icons currently on this sea entity
   private Dictionary<Status.Type, StatusIcon> _statusIcons = new Dictionary<Status.Type, StatusIcon>();

   // A reference to any damage over time coroutines caused by burning
   protected List<Coroutine> _burningCoroutines = new List<Coroutine>();

   // Set to true when 'onDeath(...)' has run on this SeaEntity
   protected bool _hasRunOnDeath = false;

   #endregion
}