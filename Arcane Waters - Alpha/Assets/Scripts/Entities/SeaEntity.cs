using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using DigitalRuby.LightningBolt;
using Mirror;
using System;
using TMPro;
using Pathfinding;
using DG.Tweening;

public class SeaEntity : NetEntity
{
   #region Public Variables

   [Header("Debugging")]

   // The debug canvas
   public GameObject debugImageHolder;

   // Screen gui for debugging sea entities
   public GameObject onScreenGUI;

   // Displays the desired stats for the user
   public Text statTextDisplay;

   [Header("Combat")]

   // The amount of damage we do
   [SyncVar]
   public float damage = 25;

   // Determines if this is a pvp entity which will behave differently
   [SyncVar]
   public bool isPvpAI = false;

   // If this entity should start regenerating health
   [SyncVar]
   public bool regenerateHealth;

   // The regeneration rate of the health
   public const float HEALTH_REGEN_RATE = 1f;

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

   // When set to true, the sprites for this sea entity will 'sink' on death
   [SyncVar]
   public bool sinkOnDeath = true;

   #region Enemy AI

   [Header("AI")]

   // The current seconds roaming around its territory
   public float currentSecondsPatrolingTerritory = 0;

   // The initial position of this monster spawn
   public Vector3 initialPosition;

   // The distance gap between current position to the starting position
   public float distanceFromInitialPosition;

   // How big the aggro cone is in degrees
   public float aggroConeDegrees;

   // How far the cone extends ahead of the ship
   public float aggroConeRadius;

   // The maximum amount of seconds chasing a target before finding a new path
   public float maxSecondsOnChasePath = 2.0f;

   // The radius of which we'll pick new points to attack from
   public float attackingWaypointsRadius = 1.0f;

   // The seconds spent idling between finding patrol routes
   public float secondsBetweenFindingPatrolRoutes = 5.0f;

   // The seconds spent idling between finding attack routes
   public float secondsBetweenFindingAttackRoutes = 0.0f;

   // The radius of which we'll pick new points to patrol to, when there is no treasure sites in the map
   public float newWaypointsRadius = 10.0f;

   // The seconds to patrol the current TreasureSite before choosing another one
   public float secondsPatrolingUntilChoosingNewTreasureSite = 30.0f;

   // The radius of which we'll pick new points to patrol to
   public float patrolingWaypointsRadius = 1.0f;

   // The radius that indicates if the pvp monster should retreat to spawn point
   public const float PVP_MONSTER_TERRITORY_RADIUS = 2.5f;

   // The radius that indicates if the pvp monster should stop chasing players
   public const float PVP_MONSTER_CHASE_RADIUS = 3f;

   // The idle duration the pvp monster before moving around its territory
   public const float PVP_MONSTER_IDLE_DURATION = 5f;

   #endregion

   // The xml id of this enemy data
   public int dataXmlId;

   [Header("Components")]

   // The container for our sprites
   public GameObject spritesContainer;

   // The container for our ripples
   public GameObject ripplesContainer;

   // The transform that will contain all of our orbs (powerup orbs and ability orbs)
   public Transform orbHolder;

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

      if (useSeaEnemyAI() && isServer) {
         initSeeker();
         detectPlayerSpawns();

         InvokeRepeating(nameof(checkEnemiesToAggro), 0.0f, 0.5f);
         StartCoroutine(CO_AttackEnemiesInRange(0.25f));
      }

      // Adding of powerup orbs is now disabled, code to be removed once we no longer need it as a reference for ability orbs.
      // InvokeRepeating(nameof(checkPowerupOrbs), 0.0f, 0.1f);

      editorGenerateAggroCone();
      initialPosition = transform.localPosition;
   }

   [Server]
   public virtual int applyDamage (int amount, uint damageSourceNetId) {
      float damageMultiplier = 1.0f;

      // Apply damage reduction, if there is any
      if (this.isPlayerShip()) {
         // Hard cap damage reduction at 75%, and damage addition at 100% extra
         damageMultiplier = Mathf.Clamp(damageMultiplier - PowerupManager.self.getPowerupMultiplierAdditive(userId, Powerup.Type.DamageReduction), 0.25f, 2.0f);
      }

      // If we're invulnerable, take 0 damage
      if (_isInvulnerable) {
         damageMultiplier = 0.0f;
      }

      if (isDead()) {
         // Do not apply any damage if the entity is already dead
         return 0;
      }

      amount = (int) (amount * damageMultiplier);
      currentHealth -= amount;

      // Keep track of the damage each attacker has done on this entity
      NetEntity entity = MyNetworkManager.fetchEntityFromNetId<NetEntity>(damageSourceNetId);
      if (entity != null && entity.userId > 0) {
         if (_damageReceivedPerAttacker.ContainsKey(entity.userId)) {
            _damageReceivedPerAttacker[entity.userId] += amount;
         } else {
            _damageReceivedPerAttacker[entity.userId] = amount;
         }

         customRegisterDamageReceived(entity.userId, amount);
      }

      noteAttacker(damageSourceNetId);
      Rpc_NoteAttacker(damageSourceNetId);

      if (isDead()) {
         onDeath();
      }

      // Return the final amount of damage dealt
      return amount;
   }


   [Server]
   protected virtual void customRegisterDamageReceived (int userId, int amount) {
   }

   public virtual void onDeath () {
      if (_hasRunOnDeath) {
         return;
      }

      if (isServer) {
         NetEntity lastAttacker = MyNetworkManager.fetchEntityFromNetId<NetEntity>(_lastAttackerNetId);

         if (lastAttacker != null) {
            GameStatsManager gameStatsManager = GameStatsManager.self;

            if (gameStatsManager != null) {
               if (lastAttacker.isPlayerShip()) {
                  int silverReward = SilverManager.computeSilverReward(lastAttacker, this);
                  gameStatsManager.addSilverRank(lastAttacker.userId, 1);
                  gameStatsManager.addSilverAmount(lastAttacker.userId, silverReward);
                  Target_ReceiveSilverCurrency(lastAttacker.getPlayerShipEntity().connectionToClient, silverReward, SilverManager.SilverRewardReason.Kill);
               }

               if (this.isPlayerShip()) {
                  gameStatsManager.addPlayerKillCount(lastAttacker.userId);
                  gameStatsManager.resetSilverRank(this.userId);
                  gameStatsManager.addDeathCount(this.userId);
                  this.rpc.broadcastPvPKill(lastAttacker, this);

                  int silverPenalty = SilverManager.computeSilverPenalty(this);
                  gameStatsManager.addSilverAmount(this.userId, -silverPenalty);
                  Target_ReceiveSilverCurrency(this.connectionToClient, -silverPenalty, SilverManager.SilverRewardReason.Death);

                  PvpGame pvpGame = PvpManager.self.getGameWithPlayer(this.userId);

                  if (pvpGame != null) {
                     pvpGame.noteDeath(this.userId);
                  }
               }

               if (this.isBotShip()) {
                  gameStatsManager.addShipKillCount(lastAttacker.userId);
               }

               if (this.isSeaMonster()) {
                  gameStatsManager.addMonsterKillCount(lastAttacker.userId);
               }

               if (this.isSeaStructure()) {
                  gameStatsManager.addBuildingDestroyedCount(lastAttacker.userId);
               }

               // Add assist points to the non last attacker user
               foreach (KeyValuePair<uint, double> attacker in _attackers) {
                  NetEntity attackerEntity = MyNetworkManager.fetchEntityFromNetId<NetEntity>(attacker.Key);
                  if (attackerEntity != null && attackerEntity.userId != lastAttacker.userId) {
                     gameStatsManager.addAssistCount(attackerEntity.userId);

                     if (attackerEntity.isPlayerShip()) {
                        int assistReward = SilverManager.computeAssistReward(lastAttacker, attackerEntity, this);
                        gameStatsManager.addSilverAmount(attackerEntity.userId, assistReward);
                        Target_ReceiveSilverCurrency(attackerEntity.getPlayerShipEntity().connectionToClient, assistReward, SilverManager.SilverRewardReason.Assist);
                     }
                  }
               }
            }
         }

         rewardXPToAllAttackers();

         Rpc_OnDeath();
         _hasRunOnDeath = true;
      }

      removeAllPowerupOrbs();
   }

   [ClientRpc]
   protected void Rpc_OnDeath () {
      onDeath();
   }

   [Server]
   protected void rewardXPToAllAttackers () {
      Instance instance = getInstance();
      if (instance == null) {
         return;
      }

      int rewardedXP = getRewardedXP();
      if (rewardedXP <= 0) {
         return;
      }

      Jobs.Type jobType = Jobs.Type.Sailor;
      rewardedXP = instance.difficulty > 0 ? rewardedXP * instance.difficulty : rewardedXP;

      // Make sure we correctly divide the xp among attackers, based on the total damage done
      int totalDamage = 0;
      foreach (int amount in _damageReceivedPerAttacker.Values) {
         totalDamage += amount;
      }

      // Reward xp to each attacker that has done damage on this entity
      foreach (KeyValuePair<int, int> KV in _damageReceivedPerAttacker) {
         int targetUserId = KV.Key;
         int xp = Mathf.RoundToInt(((float) KV.Value / totalDamage) * rewardedXP);

         // Background thread
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            DB_Main.addJobXP(targetUserId, jobType, xp);
            Jobs jobs = DB_Main.getJobXP(targetUserId);

            // Back to the Unity thread
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               NetEntity entity = EntityManager.self.getEntity(targetUserId);
               if (entity != null) {
                  entity.Target_GainedXP(entity.connectionToClient, xp, jobs, jobType, 0, true);
               }
            });
         });
      }
   }

   [Server]
   protected virtual int getRewardedXP () {
      return 0;
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

      // Regenerate health
      if (NetworkServer.active && isSeamonsterPvp() && regenerateHealth && currentHealth < maxHealth && currentHealth > 0) {
         currentHealth += (int) HEALTH_REGEN_RATE;
      }

      if (!isDead()) {
         updatePowerupOrbs();
         updateAbilityOrbs();
      }

      // If we've died, start slowing moving our sprites downward
      if (isDead()) {
         _outline.setVisibility(false);

         // Don't disable colliders on destroyed player ships - colliders are dealt with separately in PlayerShipEntity.cs when activating lifeboat 
         if (!(this is PlayerShipEntity)) {
            setCollisions(false);
         }

         foreach (Coroutine coroutine in _burningCoroutines) {
            if (coroutine != null) {
               StopCoroutine(coroutine);
            }
         }

         // If the tutorial is waiting for a bot ship defeat, test if the conditions are met
         if (Global.player != null && TutorialManager3.self.getCurrentTrigger() == TutorialTrigger.DefeatPirateShip && !_isDefeatShipTutorialTriggered && hasBeenAttackedBy(Global.player)) {
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.DefeatPirateShip);
            _isDefeatShipTutorialTriggered = true;
         }

         if (this.isSeaMonster()) {
            SeaMonsterEntity monsterEntity = GetComponent<SeaMonsterEntity>();

            if (monsterEntity.seaMonsterData.roleType == RoleType.Minion) {
               monsterEntity.corpseHolder.SetActive(true);
               spritesContainer.SetActive(false);
               if (sinkOnDeath) {
                  Util.setLocalY(monsterEntity.corpseHolder.transform, monsterEntity.corpseHolder.transform.localPosition.y - .09f * Time.smoothDeltaTime);
               }
            }

            if (sinkOnDeath) {
               Util.setLocalY(spritesContainer.transform, spritesContainer.transform.localPosition.y - .06f * Time.smoothDeltaTime);
               foreach (SpriteRenderer renderer in _renderers) {
                  if (renderer.enabled) {
                     float newAlpha = Mathf.Lerp(1f, 0f, spritesContainer.transform.localPosition.y * -10f);
                     Util.setMaterialBlockAlpha(renderer, newAlpha);
                  }
               }
            }
         } else if (!_playedDestroySound && this is ShipEntity && isClient) {
            _playedDestroySound = true;

            if (this.isBot()) {
               SoundEffectManager.self.playFmodSfx(SoundEffectManager.ENEMY_SHIP_DESTROYED, this.transform);
            } else {
               SoundEffectManager.self.playFmodSfx(SoundEffectManager.PLAYER_SHIP_DESTROYED, this.transform);
            }

            // Hide all the sprites
            foreach (SpriteRenderer renderer in _renderers) {
               renderer.enabled = false;
            }

            // Play the explosion effect
            Instantiate(isBot() ? PrefabsManager.self.pirateShipExplosionEffect : PrefabsManager.self.playerShipExplosionEffect, transform.position, Quaternion.identity);
         }

         if (sinkOnDeath) {
            Util.setLocalY(spritesContainer.transform, spritesContainer.transform.localPosition.y - .03f * Time.smoothDeltaTime);
         }
      }
   }

   protected override void FixedUpdate () {
      base.FixedUpdate();

      // Return if we're not a living AI on the server
      if (!isServer || isDead() || !useSeaEnemyAI()) {
         return;
      }

      // If pvp sea monster moves away from its territory, retreat back to it
      if (isSeamonsterPvp()) {
         distanceFromInitialPosition = Vector2.Distance(transform.localPosition, initialPosition);
         if (distanceFromInitialPosition > PVP_MONSTER_TERRITORY_RADIUS) {
            retreatToSpawn();
         }
      }

      // Don't let minions pathfind and move
      if (isSeaMonsterMinion()) {
         return;
      }

      checkHasArrivedInLane();
      moveAlongCurrentPath();
      checkForPathUpdate();

      bool wasChasingLastFrame = _isChasingEnemy;
      _isChasingEnemy = _attackers != null && _attackers.Count > 0;

      if (!wasChasingLastFrame && _isChasingEnemy) {
         _chaseStartTime = NetworkTime.time;
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

   public void setCollisions (bool enabled) {
      foreach (Collider2D col in colliderList) {
         col.enabled = enabled;
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

      List<Vector2> lightningPositions = new List<Vector2>();
      foreach (Collider2D collidedEntity in hits) {
         if (collidedEntity != null) {
            if (collidedEntity.GetComponent<SeaEntity>() != null) {
               SeaEntity seaEntity = collidedEntity.GetComponent<SeaEntity>();
               if (this.isEnemyOf(seaEntity) && !collidedEntities.ContainsKey(seaEntity) && !seaEntity.isDead() && seaEntity.instanceId == this.instanceId) {
                  int finalDamage = seaEntity.applyDamage(damage, attackerNetId);
                  seaEntity.Rpc_ShowExplosion(attackerNetId, collidedEntity.transform.position, finalDamage, Attack.Type.None, false);

                  // Registers the action electrocuted to the userID to the achievement database for recording
                  AchievementManager.registerUserAchievement(seaEntity, ActionType.Electrocuted);

                  lightningPositions.Add(seaEntity.spritesContainer.transform.position);

                  collidedEntities.Add(seaEntity, collidedEntity.transform);
                  targetIDList.Add(seaEntity.netId);
               }
            }
         }
      }

      Rpc_ChainLightning(lightningPositions.ToArray(), primaryTargetNetID, sourcePos);
   }

   [Server]
   public void cannonballChainLightning (uint attackerNetId, Vector2 sourcePos, uint primaryTargetNetID, float chainRadius, float damage) {
      Collider2D[] hits = Physics2D.OverlapCircleAll(sourcePos, chainRadius);
      Dictionary<NetEntity, Transform> collidedEntities = new Dictionary<NetEntity, Transform>();
      List<uint> targetNetIdList = new List<uint>();
      int damageInt = (int) damage;

      List<Vector2> lightningPositions = new List<Vector2>();
      foreach (Collider2D hit in hits) {
         if (hit != null) {
            if (hit.GetComponent<SeaEntity>() != null) {
               SeaEntity hitEntity = hit.GetComponent<SeaEntity>();
               if (this.isEnemyOf(hitEntity) && !collidedEntities.ContainsKey(hitEntity) && !hitEntity.isDead() && hitEntity.instanceId == this.instanceId && hitEntity.netId != primaryTargetNetID) {
                  int finalDamage = hitEntity.applyDamage(damageInt, attackerNetId);
                  hitEntity.Rpc_ShowDamage(Attack.Type.None, hitEntity.transform.position, finalDamage);
                  if (hitEntity.spritesContainer == null) {
                     D.debug("Sprite container for chain lighting is missing!");
                  }
                  lightningPositions.Add(hitEntity.spritesContainer == null ? hitEntity.transform.position : hitEntity.spritesContainer.transform.position);
                  collidedEntities.Add(hitEntity, hit.transform);
                  targetNetIdList.Add(hitEntity.netId);
               }
            }
         }
      }

      Rpc_ChainLightning(lightningPositions.ToArray(), primaryTargetNetID, sourcePos);
   }

   [ClientRpc]
   public void Rpc_SpawnBossVenomResidue (uint creatorNetId, int instanceId, Vector3 location) {
      VenomResidue venomResidue = Instantiate(PrefabsManager.self.bossVenomResiduePrefab, location, Quaternion.identity);
      venomResidue.creatorNetId = creatorNetId;
      venomResidue.instanceId = instanceId;
      ExplosionManager.createSlimeExplosion(location);
      SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Coralbow_Attack, this.transform.position);
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
   private void Rpc_ChainLightning (Vector2[] targetPosGround, uint primaryTargetNetID, Vector2 sourcePos) {
      SeaEntity parentEntity = SeaManager.self.getEntity(primaryTargetNetID);
      if (parentEntity == null) {
         D.debug("Sea manager does not contain Entity! " + primaryTargetNetID);
         return;
      }

      if (parentEntity.spritesContainer == null) {
         D.debug("Entity {" + primaryTargetNetID + "} has no sprite container");
         return;
      }

      float zAxis = 2;
      Transform parentTransform = parentEntity.spritesContainer.transform;
      GameObject shockResidue = Instantiate(PrefabsManager.self.lightningResiduePrefab);
      shockResidue.transform.SetParent(parentTransform);
      shockResidue.transform.position = new Vector3(sourcePos.x, sourcePos.y, zAxis);
      EffectManager.self.create(Effect.Type.Shock_Collision, sourcePos);

      foreach (Vector2 targetPos in targetPosGround) {
         // Setup lightning chain
         LightningBoltScript lightning = Instantiate(PrefabsManager.self.lightningChainPrefab);
         lightning.transform.SetParent(shockResidue.transform, false);
         lightning.StartObject.transform.position = lightning.transform.position;
         lightning.EndObject.transform.position = targetPos;
         lightning.EndObject.transform.SetParent(shockResidue.transform);
         if (lightning.GetComponent<LineRenderer>() != null) {
            lightning.GetComponent<LineRenderer>().enabled = true;
         } else {
            D.debug("Lightning has no Line Renderer!!");
         }

         GameObject subShockResidue = Instantiate(PrefabsManager.self.lightningResiduePrefab);
         subShockResidue.GetComponent<ZSnap>().enabled = false;
         subShockResidue.transform.position = new Vector3(targetPos.x, targetPos.y, zAxis);

         EffectManager.self.create(Effect.Type.Shock_Collision, targetPos);
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

      //AudioClipManager.AudioClipData audioClipData = AudioClipManager.self.getAudioClipData(abilityData.castSFXPath);
      //if (audioClipData.audioPath.Length > 1) {
      //   AudioClip clip = audioClipData.audioClip;
      //   if (clip != null) {
      //      //SoundManager.playClipAtPoint(clip, Camera.main.transform.position);
      //   }
      //} else {
      //   //SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Attack_Fire, this.transform.position);
      //}

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
         if (attackType == Attack.Type.Tentacle || attackType == Attack.Type.Poison_Circle) {
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

         // Play the damage sound (FMOD SFX)
         // SoundEffectManager.self.playEnemyHitSfx(this is ShipEntity, this.transform);
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

         // FMOD sfx for water
         SoundEffectManager.self.playCannonballImpact(CannonballSfxType.Water_Impact, pos);
         //SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Splash_Cannon_1, pos);
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
            //SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Boulder, location);
            break;
         case Attack.Type.Venom:
            // Apply the status effect
            StatusManager.self.create(Status.Type.Slowed, 0.3f, 3f, attackerNetID);
            ExplosionManager.createSlimeExplosion(location);
            //SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Attack_Fire, location);
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
      if (entity != null) {
         noteAttacker(entity.netId);
      }
   }

   public virtual void noteAttacker (uint netId) {
      _attackers[netId] = NetworkTime.time;
      _lastAttackerNetId = netId;

      // If this is a seamonster and is attacked by a tower, immediately retreat to spawn point
      if (isServer && isSeamonsterPvp()) {
         NetEntity entity = MyNetworkManager.fetchEntityFromNetId<NetEntity>(_lastAttackerNetId);
         if (entity && entity is SeaStructure) {
            retreatToSpawn();
         } else {
            // Stop health regeneration when hit by player and combat begins
            if (regenerateHealth) {
               setHealthRegeneration(false);
            }
         }
      }
   }

   protected void retreatToSpawn () {
      setHealthRegeneration(true);
      _attackers.Clear();
      _currentAttacker = 0;
      _attackingWaypointState = WaypointState.FINDING_PATH;
      _patrolingWaypointState = WaypointState.RETREAT;

      // Timer reset
      if (_currentSecondsPatroling > 0) {
         _currentSecondsPatroling = 0;
      }
      if (_currentSecondsBetweenPatrolRoutes > 0) {
         _currentSecondsBetweenPatrolRoutes = 0;
      }

      // If previous state was not retreating, reinitialize path and timer
      bool isAlreadyRetreating = _patrolingWaypointState == WaypointState.RETREAT;
      if (!isAlreadyRetreating) {
         _currentPath.Clear();
         currentSecondsPatrolingTerritory = 0;
      }

      updateState(ref _patrolingWaypointState, 0.0f, 0, ref _currentSecondsBetweenPatrolRoutes,
         ref _currentSecondsPatroling, (x) => { return initialPosition; });
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

      if (isDead() || (!hasReloaded()
         && shipAbility.selectedAttackType != Attack.Type.Mini_Boulder
         && shipAbility.selectedAttackType != Attack.Type.Tentacle
         && shipAbility.selectedAttackType != Attack.Type.Poison_Circle)) {
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
         case Attack.Type.Poison_Circle:
            // Fire multiple projectiles in a circle around the target
            float innerRadius = .7f;
            float outerRadius = .9f;
            float angleStep = 60f;

            // Inner circle
            for (float angle = 0; angle < 360f; angle += angleStep) {
               StartCoroutine(CO_FireAtSpotSingle(spot + new Vector2(innerRadius * Mathf.Cos(Mathf.Deg2Rad * angle), innerRadius * Mathf.Sin(Mathf.Deg2Rad * angle)), abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, spawnPosition));
            }

            // Outer circle
            for (float angle = angleStep / 2; angle < 360f; angle += angleStep) {
               StartCoroutine(CO_FireAtSpotSingle(spot + new Vector2(outerRadius * Mathf.Cos(Mathf.Deg2Rad * angle), outerRadius * Mathf.Sin(Mathf.Deg2Rad * angle)), abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, spawnPosition));
            }
            break;

         default:
            D.adminLog("Trigger generic projectile at spot, Delay is: " + attackDelay, D.ADMIN_LOG_TYPE.Sea);
            StartCoroutine(CO_FireAtSpotSingle(spot, abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, spawnPosition));
            break;
      }

      Debug.Log(shipAbility.selectedAttackType);

      _lastAttackTime = NetworkTime.time;
      attackCounter++;

      Rpc_RegisterAttackTime(attackDelay);
      Rpc_NoteAttack();
   }

   [Server]
   private IEnumerator CO_FireAtSpotSingle (Vector2 spot, int abilityId, Attack.Type attackType, float attackDelay, float launchDelay, Vector2 spawnPosition = new Vector2()) {
      // Wait for the attack delay, if any
      yield return new WaitForSeconds(attackDelay);

      fireProjectileAtTarget(spawnPosition, spot, abilityId);
   }

   [Server]
   protected void fireProjectileAtTarget (Vector2 startPosition, Vector2 endPosition, int abilityId) {
      float distanceToTarget = Vector2.Distance(startPosition, endPosition);
      float timeToReachTarget = Mathf.Clamp(distanceToTarget, 0.55f, 1.65f);

      // Load ability data
      ShipAbilityData abilityData = getSeaAbility(abilityId);
      Attack.Type attackType = Attack.Type.None;
      Attack.ImpactMagnitude attackMagnitude = Attack.ImpactMagnitude.None;
      bool hasArch = true;

      if (abilityData != null) {
         attackType = abilityData.selectedAttackType;
         attackMagnitude = abilityData.impactMagnitude;
         hasArch = abilityData.hasArch;
      }

      // Load projectile data
      ProjectileStatData projectileData = getProjectileDataFromAbility(abilityId);

      SeaProjectile projectile = Instantiate(PrefabsManager.self.seaProjectilePrefab, startPosition, Quaternion.identity);

      // Modify projectile speed based on attack type
      timeToReachTarget /= Attack.getSpeedModifier(attackType);

      // Calculate projectile values
      float speed = distanceToTarget / timeToReachTarget;
      Vector2 toEndPos = endPosition - startPosition;
      Vector2 projectileVelocity = speed * toEndPos.normalized;
      float lobHeight = Mathf.Clamp(1.0f / speed, 0.3f, 1.0f);

      if (!hasArch) {
         lobHeight = 0.0f;
      }

      projectile.initAbilityProjectile(netId, instanceId, attackMagnitude, abilityId, projectileVelocity, lobHeight, lifetime: timeToReachTarget, attackType: attackType, disableColliderFor: 0.8f, minDropShadowScale: 0.5f);
      NetworkServer.Spawn(projectile.gameObject);

      Rpc_SpawnProjectileIndicator(endPosition, timeToReachTarget);
   }

   [ClientRpc]
   protected void Rpc_SpawnProjectileIndicator (Vector2 spawnPosition, float lifetime) {
      ProjectileTargetingIndicator targetingIndicator = Instantiate(PrefabsManager.self.projectileTargetingIndicatorPrefab, spawnPosition, Quaternion.identity);
      targetingIndicator.init(lifetime);
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

                     StatusManager.self.create(Status.Type.Stunned, 1.0f, 2f, targetEntity.netId);
                  } else if (attackType == Attack.Type.Venom) {
                     StatusManager.self.create(Status.Type.Slowed, 0.3f, 1f, targetEntity.netId);
                  }
                  enemyHitList.Add(targetEntity);
               }
            }
         }
      }

      if (attackType == Attack.Type.Tentacle || attackType == Attack.Type.Poison_Circle) {
         // Only spawn a venom residue on sea tiles
         Area area = AreaManager.self.getArea(areaKey);
         if (area != null && !area.hasLandTile(circleCenter)) {
            SeaEntity sourceEntity = SeaManager.self.getEntity(netId);
            VenomResidue venomResidue = Instantiate(PrefabsManager.self.bossVenomResiduePrefab, circleCenter, Quaternion.identity);
            venomResidue.creatorNetId = netId;
            venomResidue.instanceId = instanceId;
            sourceEntity.Rpc_SpawnBossVenomResidue(netId, instanceId, circleCenter);
         }
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
      // Sea Entity cannot be stunned more than once. Also, it becomes stun invulnerable for a short time after stun effect ends
      if (statusType == Status.Type.Stunned && (StatusManager.self.hasStatus(netId, Status.Type.Stunned) || StatusManager.self.hasStatus(netId, Status.Type.StunInvulnerable))) {
         return;
      }

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
            statusIcon.transform.localPosition = Vector3.zero;
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
      GameObject centerExplosion = Instantiate(PrefabsManager.self.requestCannonExplosionPrefab(Attack.ImpactMagnitude.Strong), position, Quaternion.identity, null);
   }

   [ClientRpc]
   public void Rpc_PlayHitSfx (bool isShip, bool isCrit, CannonballEffector.Type effectorType, Vector3 position) {
      SoundEffectManager.self.playEnemyHitSfx(isShip, isCrit, effectorType, position);
   }

   #region Enemy AI

   private bool useSeaEnemyAI () {
      return (this is SeaMonsterEntity || this is BotShipEntity);
   }

   [Server]
   private void setPath_Asynchronous (Path newPath) {
      _currentPath = newPath.vectorPath;
      _currentPathIndex = 0;
      _seeker.CancelCurrentPathRequest(true);
   }

   [Server]
   private void checkEnemiesToAggro () {
      if (instanceId <= 0) {
         D.log("AI SeaEntity needs to be placed in an instance");
         return;
      }

      Instance instance = InstanceManager.self.getInstance(instanceId);
      if (instance == null) {
         return;
      }

      if (isSeamonsterPvp() && _patrolingWaypointState == WaypointState.RETREAT && distanceFromInitialPosition > PVP_MONSTER_TERRITORY_RADIUS) {
         return;
      }

      float degreesToDot = aggroConeDegrees / 90.0f;

      foreach (NetworkBehaviour iBehaviour in instance.getEntities()) {
         NetEntity iEntity = iBehaviour as NetEntity;

         // If the entity our ally, ignore it
         if (iEntity == null || !this.isEnemyOf(iEntity)) {
            continue;
         }

         if (isSeamonsterPvp() && iEntity is SeaStructure) {
            continue;
         }

         // Bot ships shouldn't target invulnerable sea structures
         if (isBotShip() && iEntity is SeaStructure) {
            SeaStructure iStructure = iEntity as SeaStructure;
            if (iStructure._isInvulnerable) {
               continue;
            }
         }

         // Ignore admins in ghost mode
         if (iEntity.isGhost) {
            continue;
         }

         // Reset the z axis value to ensure the square magnitude is not compromised by the z axis
         Vector3 currentPosition = transform.position;
         currentPosition.z = 0;
         Vector3 targetPosition = iEntity.transform.position;
         targetPosition.z = 0;

         // If enemy isn't within radius, early out
         if ((currentPosition - targetPosition).sqrMagnitude > aggroConeRadius * aggroConeRadius) {
            continue;
         }

         // If enemy isn't within cone aggro range, early out
         if (Vector2.Dot(Util.getDirectionFromFacing(facing), (targetPosition - currentPosition).normalized) < 1.0f - degreesToDot) {
            continue;
         }

         // We can see the enemy, attack it
         _attackers[iEntity.netId] = NetworkTime.time;
      }
   }

   protected void initSeeker () {
      // Ensure this entity has a 'Seeker' component attached
      _seeker = GetComponent<Seeker>();
      if (_seeker == null) {
         D.error("There has to be a Seeker Script attached to the SeaEntity Prefab");
      }

      // Set up seeker component
      Area area = AreaManager.self.getArea(areaKey);
      GridGraph graph = area.getGraph();
      _seeker.graphMask = GraphMask.FromGraph(graph);
      _seeker.pathCallback = setPath_Asynchronous;
      _originalPosition = transform.position;
   }

   protected void detectPlayerSpawns () {
      Area area = AreaManager.self.getArea(areaKey);
      List<WarpTreasureSite> treasureSites = new List<WarpTreasureSite>(area.GetComponentsInChildren<WarpTreasureSite>());

      Spawn[] playerSpawns = area.GetComponentsInChildren<Spawn>();
      foreach (Spawn spawn in playerSpawns) {
         bool add = true;
         foreach (WarpTreasureSite treasureSite in treasureSites) {
            if (Vector2.Distance(spawn.transform.position, treasureSite.transform.position) < 1.0f) {
               add = false;
            }
         }
         if (add) {
            _playerSpawnPoints.Add(spawn.transform.position);
         }
      }

      _minDistanceToSpawn = area.getAreaSizeWorld().x * MIN_DISTANCE_TO_SPAWN_PERCENT;
      _minDistanceToSpawnPath = area.getAreaSizeWorld().x * MIN_DISTANCE_TO_SPAWN_PATH_PERCENT;
   }

   [Server]
   protected virtual IEnumerator CO_AttackEnemiesInRange (float delayInSecondsWhenNotReloading) {
      yield break;
   }

   protected virtual bool isInRange (Vector2 position) {
      return false;
   }

   protected void editorGenerateAggroCone () {
      if (!useSeaEnemyAI() || !Application.isEditor) {
         return;
      }

      float coneRadians = aggroConeDegrees * Mathf.Deg2Rad;
      float singleDegreeInRadian = 1 * Mathf.Deg2Rad;

      _editorConeAggroGizmoMesh = new Mesh();
      List<Vector3> positions = new List<Vector3>();
      List<Vector3> normals = new List<Vector3>();

      // Center of the cone fan
      positions.Add(Vector3.zero);
      normals.Add(Vector3.back);

      for (float iPoint = -coneRadians; iPoint < coneRadians; iPoint += singleDegreeInRadian) {
         positions.Add(new Vector3(Mathf.Cos(iPoint), Mathf.Sin(iPoint), 0.0f) * aggroConeRadius);

         // Point normals towards the camera
         normals.Add(Vector3.back);
      }

      List<int> triangles = new List<int>(positions.Count * 3);
      for (int iVertex = 2; iVertex < positions.Count; iVertex += 1) {
         triangles.Add(0);
         triangles.Add(iVertex);
         triangles.Add(iVertex - 1);
      }

      _editorConeAggroGizmoMesh.SetVertices(positions);
      _editorConeAggroGizmoMesh.SetNormals(normals);
      _editorConeAggroGizmoMesh.SetIndices(triangles, MeshTopology.Triangles, 0);
   }

   protected virtual NetEntity getAttackerInRange () {
      // Check if any of our attackers are within range
      foreach (uint attackerId in _attackers.Keys) {
         NetEntity attacker = MyNetworkManager.fetchEntityFromNetId<NetEntity>(attackerId);
         if (attacker == null || attacker.isDead()) {
            continue;
         }

         Vector2 attackerPosition = attacker.transform.position;
         if (!isInRange(attackerPosition)) {
            continue;
         }

         // If we have found an attacker, exit early
         if (attacker) {
            return attacker;
         }
      }

      return null;
   }

   [Server]
   private void moveAlongCurrentPath () {
      if (Global.freezeShips) {
         return;
      }

      if (_currentPath != null && _currentPathIndex < _currentPath.Count) {
         // Only change our movement if enough time has passed
         double moveTime = NetworkTime.time - _lastMoveChangeTime;
         if (moveTime >= MOVE_CHANGE_INTERVAL) {
            Vector2 direction = (Vector2) _currentPath[_currentPathIndex] - (Vector2) transform.position;

            // Update our facing direction
            Direction newFacingDirection = DirectionUtil.getDirectionForVelocity(direction);
            if (newFacingDirection != facing) {
               facing = newFacingDirection;
            }

            _body.AddForce(direction.normalized * getMoveSpeed());
            _lastMoveChangeTime = NetworkTime.time;
         }

         // If ship got too close to spawn point - change path
         if (!_isChasingEnemy && !_disableSpawnDistanceTmp) {
            foreach (Vector3 spawn in _playerSpawnPoints) {
               float dist = Vector2.Distance(spawn, _currentPath[_currentPathIndex]);
               if (dist < _minDistanceToSpawn) {
                  _currentPathIndex = int.MaxValue - 1;
                  _disableSpawnDistanceTmp = true;
                  _lastSpawnPosition = spawn;
                  return;
               }
            }
         }

         // Advance along the path as the unit comes close enough to the current waypoint
         float distanceToWaypoint = Vector2.Distance(_currentPath[_currentPathIndex], transform.position);
         if (distanceToWaypoint < .1f) {
            ++_currentPathIndex;
         }
      }
   }

   [Server]
   private void checkForPathUpdate () {
      // Check if there are attackers to chase
      if (_attackers != null && _attackers.Count > 0) {
         double chaseDuration = NetworkTime.time - _chaseStartTime;
         if (!_isChasingEnemy || (chaseDuration > maxSecondsOnChasePath)) {
            _currentSecondsBetweenAttackRoutes = 0.0f;
            _attackingWaypointState = WaypointState.FINDING_PATH;
            if (_currentPath != null) {
               _currentPath.Clear();
            }
            findAndSetPath_Asynchronous(findAttackerVicinityPosition(true));
            _chaseStartTime = NetworkTime.time;

            // Update attacking state with only Minor updates
            float tempMajorRef = 0.0f;
            updateState(ref _attackingWaypointState, secondsBetweenFindingAttackRoutes, 9001.0f, ref _currentSecondsBetweenAttackRoutes, ref tempMajorRef, findAttackerVicinityPosition, true);
         } else {
            if (isSeamonsterPvp()) {
               // Immediately stop chasing enemy if the enemy is too far, this will cause monster to return to spawn point and regenerate
               if (NetworkIdentity.spawned.ContainsKey(_currentAttacker) && _currentAttacker != 0) {
                  NetworkIdentity attackerIdentity = NetworkIdentity.spawned[_currentAttacker];
                  float enemyDist = Vector2.Distance(transform.position, attackerIdentity.transform.position);
                  if (attackerIdentity && enemyDist > PVP_MONSTER_CHASE_RADIUS) {
                     retreatToSpawn();
                  }
               }
            }
         }
         // If there are no attackers to chase, and we're in a pvp game, move towards the current objective
      } else if (pvpTeam != PvpTeamType.None) {
         // If we haven't reached the middle of the lane yet, move towards it
         if (_pvpLaneTarget != null) {
            updateState(ref _patrolingWaypointState, 0.0f, secondsPatrolingUntilChoosingNewTreasureSite,
               ref _currentSecondsBetweenPatrolRoutes, ref _currentSecondsPatroling, (x) => { return _pvpLaneTarget.position; });

            // If we've already reached the middle of the lane, look for the next target structure to attack
         } else {
            // Sea monsters should not engage buildings
            if (isSeamonsterPvp()) {
               return;
            }

            SeaStructure targetStructure = getTargetSeaStructure();
            if (targetStructure) {
               updateState(ref _patrolingWaypointState, 0.0f, secondsPatrolingUntilChoosingNewTreasureSite,
                  ref _currentSecondsBetweenPatrolRoutes, ref _currentSecondsPatroling, (x) => { return targetStructure.transform.position; });
            } else {
               updateState(ref _patrolingWaypointState, 0.0f, secondsPatrolingUntilChoosingNewTreasureSite,
                  ref _currentSecondsBetweenPatrolRoutes, ref _currentSecondsPatroling, findRandomVicinityPosition);
            }
         }
      } else {
         // Use treasure site only in maps with more than two treasure sites
         Func<bool, Vector3> findingFunction = findRandomVicinityPosition;

         if (_isChasingEnemy) {
            _currentSecondsBetweenPatrolRoutes = 0.0f;
            _currentSecondsPatroling = 0.0f;
            _patrolingWaypointState = WaypointState.FINDING_PATH;
            if (_currentPath != null) {
               _currentPath.Clear();
            }
            findAndSetPath_Asynchronous(findingFunction(true));
         }

         // Default behavior state of sea monster pvp should be patrolling around its spawn point
         if (isSeamonsterPvp()) {
            _patrolingWaypointState = WaypointState.RETREAT;
         }

         updateState(ref _patrolingWaypointState, secondsBetweenFindingPatrolRoutes, secondsPatrolingUntilChoosingNewTreasureSite,
            ref _currentSecondsBetweenPatrolRoutes, ref _currentSecondsPatroling, findingFunction);
      }
   }

   [Server]
   private void findAndSetPath_Asynchronous (Vector3 targetPosition) {
      if (!_seeker.IsDone()) {
         _seeker.CancelCurrentPathRequest();
      }

      _seeker.StartPath((Vector2) transform.position, (Vector2) targetPosition);
      if (_disableSpawnDistanceTmp) {
         Invoke(nameof(enableSpawnDistance), 3.5f);
      }
   }

   private void enableSpawnDistance () {
      _disableSpawnDistanceTmp = false;
   }

   [Server]
   protected virtual Vector3 findAttackerVicinityPosition (bool newAttacker) {
      return findAttackerVicinityPosition(newAttacker, _attackers);
   }

   [Server]
   protected Vector3 findAttackerVicinityPosition (bool newAttacker, Dictionary<uint, double> attackers) {
      bool hasAttacker = _currentAttacker != 0 && attackers.ContainsKey(_currentAttacker);

      // If we should choose a new attacker, and we have a selection available, pick a unique one randomly, which could be the same as the previous
      if ((!hasAttacker || (hasAttacker && newAttacker)) && attackers.Count > 0) {
         _currentAttacker = attackers.RandomKey();
         hasAttacker = true;
      }

      // If we fail to get a non-null attacker, return somewhere around yourself
      if (!hasAttacker) {
         return findPositionAroundPosition(transform.position, attackingWaypointsRadius);
      }

      // If we have a registered attacker but it's not in the scene list of spawned NetworkIdentities, then return somewhere around yourself
      if (!NetworkIdentity.spawned.ContainsKey(_currentAttacker) || _currentAttacker == 0) {
         return findPositionAroundPosition(transform.position, attackingWaypointsRadius);
      }

      // If we have gotten a NetworkIdentity but it's by some chance null, then return somewhere around yourself
      NetworkIdentity attackerIdentity = NetworkIdentity.spawned[_currentAttacker];
      if (attackerIdentity == null) {
         return findPositionAroundPosition(transform.position, attackingWaypointsRadius);
      }

      NetEntity enemyEntity = attackerIdentity.GetComponent<NetEntity>();
      Vector3 projectedPosition = attackerIdentity.transform.position;

      if (enemyEntity) {
         // Find a point ahead of the target to path find to
         projectedPosition = enemyEntity.getProjectedPosition(maxSecondsOnChasePath / 2.0f);
      }

      // If we have an attacker, find new position around it
      return findPositionAroundPosition(projectedPosition, attackingWaypointsRadius);
   }

   [Server]
   private Vector3 findPositionAroundPosition (Vector3 position, float radius) {
      return position + UnityEngine.Random.insideUnitCircle.ToVector3() * radius;
   }

   [Server]
   private void updateState (ref WaypointState state, float secondsBetweenMinorSearch, float secondsBetweenMajorSearch, ref float currentSecondsBetweenMinorSearch, ref float currentSecondsBetweenMajorSearch, System.Func<bool, Vector3> targetPositionFunction, bool isAtk = false) {
      switch (state) {
         case WaypointState.RETREAT:
            if (isSeamonsterPvp() && _seeker != null) {
               _attackingWaypointState = WaypointState.FINDING_PATH;
               _attackers.Clear();
               _currentAttacker = 0;

               // Start path finding
               if (_seeker.IsDone() && (_currentPath == null || _currentPath.Count <= 0)) {
                  findAndSetPath_Asynchronous(targetPositionFunction(true));
               }

               // If path finding has ended
               if (_currentPathIndex >= _currentPath.Count || currentSecondsPatrolingTerritory > PVP_MONSTER_IDLE_DURATION) {
                  // If this unit is retreating and is fully regenerated, disable regenerate health command
                  if (distanceFromInitialPosition > 1) {
                     if (!regenerateHealth && currentHealth < maxHealth && !hasAnyCombat()) {
                        setHealthRegeneration(true);
                     }
                  } else if (regenerateHealth && currentHealth >= maxHealth) {
                     setHealthRegeneration(false);
                  }

                  currentSecondsPatrolingTerritory = 0.0f;

                  // Move around the spawn point
                  findAndSetPath_Asynchronous(targetPositionFunction(true));
               }

               currentSecondsPatrolingTerritory += Time.fixedDeltaTime;
            } else {
               state = WaypointState.FINDING_PATH;
            }
            break;
         case WaypointState.FINDING_PATH:
            if (_seeker.IsDone() && (_currentPath == null || _currentPath.Count <= 0)) {
               // Try to find another path as the previous one wasn't valid
               findAndSetPath_Asynchronous(targetPositionFunction(false));
            }

            if (_currentPath != null && _currentPath.Count > 0) {
               // If we have a valid path, start moving to the target
               state = WaypointState.MOVING_TO;
            }
            break;

         case WaypointState.MOVING_TO:
            // If there are no nodes left in the path, change into a patrolling state
            if (_currentPathIndex >= _currentPath.Count) {
               state = WaypointState.PATROLING;
               currentSecondsBetweenMinorSearch = 0.0f;
               currentSecondsBetweenMajorSearch = 0.0f;
            }
            break;

         case WaypointState.PATROLING:
            currentSecondsBetweenMajorSearch += Time.fixedDeltaTime;

            // If we've still got a path to complete, don't try to find a new one
            if (_currentPath != null && _currentPathIndex < _currentPath.Count) {
               break;
            }

            // Find a new Major target
            if (currentSecondsBetweenMajorSearch >= secondsBetweenMajorSearch) {
               findAndSetPath_Asynchronous(targetPositionFunction(true));
               state = WaypointState.FINDING_PATH;
               break;
            }

            currentSecondsBetweenMinorSearch += Time.fixedDeltaTime;

            // Find a new Minor target
            if (currentSecondsBetweenMinorSearch >= secondsBetweenMinorSearch) {
               _currentSecondsBetweenPatrolRoutes = 0.0f;
               findAndSetPath_Asynchronous(targetPositionFunction(false));
               state = WaypointState.FINDING_PATH;
            }
            break;
      }
   }

   [Server]
   private Vector3 findRandomVicinityPosition (bool placeholder) {
      Vector3 start = _body.transform.position;

      // If this unit is a sea monster pvp and is retreating, find a target position around the spawn point
      if (isSeamonsterPvp() && _patrolingWaypointState == WaypointState.RETREAT) {
         return findPositionAroundPosition(_originalPosition, .5f);
      }

      // If ship is near original position - try to find new distant location to move to
      if (Vector2.Distance(start, _originalPosition) < 1.0f) {
         const int MAX_RETRIES = 50;
         int retries = 0;
         bool retry = true;

         Vector3 dir = start - _lastSpawnPosition;
         dir.Normalize();
         while (retry && retries < MAX_RETRIES) {
            retry = false;
            Vector3 end = _disableSpawnDistanceTmp ? findPositionAroundPosition(start + dir * newWaypointsRadius, newWaypointsRadius) : findPositionAroundPosition(start, newWaypointsRadius);
            foreach (Vector3 spawn in _playerSpawnPoints) {
               if (Vector2.Distance(spawn, end) < _minDistanceToSpawnPath) {
                  retry = true;
                  retries++;
                  break;
               }
            }

            if (!retry) {
               return end;
            }
         }

         return findPositionAroundPosition(start, newWaypointsRadius);
      }

      // Otherwise - go back to original location of the ship
      return findPositionAroundPosition(_originalPosition, patrolingWaypointsRadius);
   }

   [Server]
   public void setPvpLaneTarget (Transform laneTarget) {
      _pvpLaneTarget = laneTarget;
   }

   [Server]
   public void setPvpTargetStructures (List<SeaStructure> targetStructures) {
      _pvpTargetStructures = targetStructures;
   }

   private void checkHasArrivedInLane () {
      // Once we have arrived in the middle of the lane, set our lane target to null, and we will move to our target structure, the first enemy tower
      const float ARRIVE_DIST = 2.0f;
      if (pvpTeam != PvpTeamType.None && _pvpLaneTarget != null) {
         Vector2 toLaneCenter = _pvpLaneTarget.position - transform.position;
         if (toLaneCenter.sqrMagnitude < ARRIVE_DIST * ARRIVE_DIST) {
            _pvpLaneTarget = null;
         }
      }
   }

   private SeaStructure getTargetSeaStructure () {
      if (_pvpTargetStructures != null) {
         foreach (SeaStructure structure in _pvpTargetStructures) {
            if (structure != null && !structure.isDead()) {
               return structure;
            }
         }
      }

      return null;
   }

   [Server]
   public void setIsInvulnerable (bool value) {
      _isInvulnerable = value;
   }

   private void OnDrawGizmosSelected () {
      if (Application.isPlaying) {
         Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 0.5f);
         Gizmos.DrawMesh(_editorConeAggroGizmoMesh, 0, transform.position, Quaternion.Euler(0.0f, 0.0f, -Vector2.SignedAngle(Util.getDirectionFromFacing(facing), Vector2.right)), Vector3.one);
         Gizmos.color = Color.white;
      }
   }

   public void setHealthRegeneration (bool isOn) {
      if (isSeamonsterPvp()) {
         // Do not enable regeneration if this entity is already dead
         if (isOn && currentHealth < 1) {
            return;
         }
         regenerateHealth = isOn;
      }
   }

   public bool isSeamonsterPvp () {
      return this is SeaMonsterEntity && isPvpAI;
   }

   #endregion

   #region Powerup VFX

   protected void removePowerupOrb (Powerup.Type powerupType) {
      PowerupOrb orbToRemove = _powerupOrbs.Find((x) => x.powerupType == powerupType);
      if (orbToRemove) {
         _powerupOrbs.Remove(orbToRemove);
         Destroy(orbToRemove.gameObject);
      }
   }

   protected void removeAllPowerupOrbs () {
      foreach (PowerupOrb orb in _powerupOrbs) {
         Destroy(orb.gameObject);
      }
      _powerupOrbs.Clear();
   }

   protected void updatePowerupOrbs () {
      if (Util.isBatch()) {
         return;
      }

      _powerupOrbRotation += Time.deltaTime * POWERUP_ORB_ROTATION_SPEED;

      float orbSpacing = 1.0f / _powerupOrbs.Count;
      
      for (int i = 0; i < _powerupOrbs.Count; i++) {
         PowerupOrb orb = _powerupOrbs[i];
         float targetValue = _powerupOrbRotation + orbSpacing * i;
         float newValue = Mathf.SmoothStep(orb.rotationValue, targetValue, Time.deltaTime * 10.0f);
         orb.rotationValue = newValue;

         Util.setLocalXY(orb.transform, Util.getPointOnEllipse(POWERUP_ORB_ELLIPSE_WIDTH, POWERUP_ORB_ELLIPSE_HEIGHT, newValue));
      }
   }

   protected void checkPowerupOrbs () {
      if (Util.isBatch() || isDead()) {
         return;
      }

      // Store the latest powerups from the server in a list
      List<Powerup.Type> powerupTypes = new List<Powerup.Type>();
      foreach (Powerup powerup in _powerups) {
         powerupTypes.Add(powerup.powerupType);
      }
      int orbsCreated = 0;

      // If the list from the server is larger, we need to create new orbs
      if (powerupTypes.Count > _powerupOrbs.Count) {
         orbsCreated = powerupTypes.Count - _powerupOrbs.Count;

         // Create any new orbs needed
         for (int i = 0; i < orbsCreated; i++) {
            PowerupOrb newOrb = Instantiate(PrefabsManager.self.powerupOrbPrefab, transform.position + Vector3.up * POWERUP_ORB_ELLIPSE_HEIGHT, Quaternion.identity, orbHolder);
            _powerupOrbs.Add(newOrb);
            newOrb.rotationValue = _powerupOrbRotation + (1.0f / _powerupOrbs.Count) * (_powerupOrbs.Count - 1);
         }

      // If the list from the server is smaller, we need to remove some orbs
      } else if (_powerupOrbs.Count > powerupTypes.Count) {
         int orbsToRemove = _powerupOrbs.Count - powerupTypes.Count;

         // Remove any orbs that aren't needed
         for (int i = 0; i < orbsToRemove; i++) {
            PowerupOrb orbToRemove = _powerupOrbs[_powerupOrbs.Count - 1];
            _powerupOrbs.Remove(orbToRemove);
            Destroy(orbToRemove.gameObject);
         }
      }

      // Update types of orbs
      for (int i = 0; i < _powerupOrbs.Count; i++) {
         bool isNewOrb = (i >= _powerupOrbs.Count - orbsCreated);
         _powerupOrbs[i].init(powerupTypes[i], transform, isNewOrb);
      }

      bool particlesEnabled = _powerupOrbs.Count <= POWERUP_MAX_ORB_PARTICLES;
      foreach (PowerupOrb orb in _powerupOrbs) {
         orb.setParticleVisibility(particlesEnabled);
      }
   }

   #endregion

   #region Ability Orbs

   protected void updateAbilityOrbs () {
      // Ignore this process on cloud build server
      if (Util.isBatch()) {
         return;
      }
      _powerupOrbRotation += Time.deltaTime * POWERUP_ORB_ROTATION_SPEED;

      float orbSpacing = 1.0f / _abilityOrbs.Count;

      for (int i = 0; i < _abilityOrbs.Count; i++) {
         AbilityOrb orb = _abilityOrbs[i];
         if (!orb.isSnapping && (orb.attachedToTarget && orb.targetUserId == userId)) {
            float targetValue = _powerupOrbRotation + orbSpacing * i;
            float newValue = Mathf.SmoothStep(orb.rotationValue, targetValue, Time.deltaTime * 10.0f);
            orb.rotationValue = newValue;
            Util.setLocalXY(orb.localOrbRotator, Util.getPointOnEllipse(POWERUP_ORB_ELLIPSE_WIDTH, POWERUP_ORB_ELLIPSE_HEIGHT, newValue));
         }
      }
   }

   [Server]
   public void addServerAbilityOrbs (List<Attack.Type> attackTypes, int targetUser, bool snapToTargetInstantly) {
      foreach (Attack.Type atkType in attackTypes) {
         _abilityOrbData.Add(new AbilityOrbData {
            attackType = atkType,
            targetUserId = targetUser,
            snapToTargetInstantly = snapToTargetInstantly
         });
      }
      Rpc_AddAClientbilityOrbs(userId, attackTypes, targetUser, snapToTargetInstantly);
   }

   [ClientRpc]
   public void Rpc_AddAClientbilityOrbs (int ownerId, List<Attack.Type> attackTypes, int targetUser, bool snapToTargetInstantly) {
      foreach (Attack.Type attackType in attackTypes) {
         AbilityOrb newOrb = Instantiate(PrefabsManager.self.abilityOrbPrefab, transform.position + Vector3.up * POWERUP_ORB_ELLIPSE_HEIGHT, Quaternion.identity, transform);
         newOrb.init(ownerId, attackType, transform, targetUser, snapToTargetInstantly);
         _abilityOrbs.Add(newOrb);
         newOrb.rotationValue = _powerupOrbRotation + (1.0f / _abilityOrbs.Count) * (_abilityOrbs.Count - 1);
      }
   }

   [Server]
   public void removeServerAbilityOrbs (List<Attack.Type> attackTypes, int targetUser) {
      foreach (Attack.Type atkType in attackTypes) {
         AbilityOrbData orbToRemove = _abilityOrbData.Find((x) => x.attackType == atkType && x.targetUserId == targetUser);
         if (orbToRemove != null) {
            _abilityOrbData.Remove(orbToRemove);
         }
      }
      Rpc_RemoveClientAbilityOrb(attackTypes, targetUser);
   }

   [ClientRpc]
   public void Rpc_RemoveClientAbilityOrb (List<Attack.Type> attackType, int targetUser) {
      foreach (Attack.Type powerupType in attackType) {
         AbilityOrb orbToRemove = _abilityOrbs.Find((x) => x.attackType == powerupType && x.targetUserId == targetUser);
         if (orbToRemove) {
            _abilityOrbs.Remove(orbToRemove);
            Destroy(orbToRemove.gameObject);
         }
      }
   }

   [ClientRpc]
   public void Rpc_RemoveAllAbilityOrbs () {
      foreach (AbilityOrb orb in _abilityOrbs) {
         Destroy(orb.gameObject);
      }
      _abilityOrbs.Clear();
   }

   #endregion

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

   #region Enemy AI

   // The Seeker that handles Pathfinding
   protected Seeker _seeker;

   // The current path to the destination
   [SerializeField]
   protected List<Vector3> _currentPath = new List<Vector3>();

   // The current Point Index of the path
   [SerializeField]
   protected int _currentPathIndex;

   // In case there are no TreasureSites to pursue, use this to patrol the vicinity
   private Vector3 _originalPosition;

   // The generated mesh for showing the cone of aggro in the Editor
   private Mesh _editorConeAggroGizmoMesh;

   // Spawn points placed in the area that sea enemies should avoid
   private List<Vector3> _playerSpawnPoints = new List<Vector3>();

   // Const values to calculate distance to spawn points
   private static float MIN_DISTANCE_TO_SPAWN_PERCENT = 0.3f;
   private static float MIN_DISTANCE_TO_SPAWN_PATH_PERCENT = 0.4f;

   // Distance values that bot ship should keep from the spawn points, calculated for current area
   private float _minDistanceToSpawn;
   private float _minDistanceToSpawnPath;

   // Are we currently chasing an enemy
   [SerializeField]
   private bool _isChasingEnemy;

   // The flag which temporarily disables avoiding spawn points
   private bool _disableSpawnDistanceTmp = false;

   // The last spawn point that bot ship was nearby and had to change its path
   private Vector3 _lastSpawnPosition = Vector3.zero;

   // The time at which we started chasing an enemy, on this path
   private double _chaseStartTime = 0.0f;

   // How many seconds have passed since we last stopped on an attack route
   [SerializeField]
   private float _currentSecondsBetweenAttackRoutes;

   private enum WaypointState
   {
      NONE = 0,
      FINDING_PATH = 1,
      MOVING_TO = 2,
      PATROLING = 3,
      RETREAT = 4,
   }

   // In what state the Attack Waypoint traversing is in
   [SerializeField]
   private WaypointState _attackingWaypointState = WaypointState.FINDING_PATH;

   // In what state the Patrol Waypoint traversing is in
   [SerializeField]
   private WaypointState _patrolingWaypointState = WaypointState.FINDING_PATH;

   // The first and closest attacker that we've aggroed
   [SerializeField]
   private uint _currentAttacker;

   // How many seconds have passed since we last stopped on a patrol route
   [SerializeField]
   private float _currentSecondsBetweenPatrolRoutes;

   // How many seconds have passed since we've started patrolling the current TreasureSite
   [SerializeField]
   private float _currentSecondsPatroling;

   // If in a pvp game, this is the list of enemy structures we are trying to destroy, in priority order
   private List<SeaStructure> _pvpTargetStructures;

   // If in a pvp game, this is a target point in the middle of the lane, used to help the units follow the lane
   private Transform _pvpLaneTarget;

   // When set to true, this sea entity won't take any damage
   [SyncVar]
   private bool _isInvulnerable = false;

   // Gets set to true when the 'defeatship' tutorial has been triggered
   protected bool _isDefeatShipTutorialTriggered = false;

   // The damage amount each attacker has done on this entity
   protected Dictionary<int, int> _damageReceivedPerAttacker = new Dictionary<int, int>();

   // A list of references to any active powerup orbs, used to visually indicate what powerups this entity has
   protected List<PowerupOrb> _powerupOrbs = new List<PowerupOrb>();

   // A list of references to any active ability orbs, used to visually indicate what ability this entity has
   protected List<AbilityOrb> _abilityOrbs = new List<AbilityOrb>();

   // A value that controls the rotation of the powerup orbs as it is incremented (0.0f - 1.0f is one rotation)
   protected float _powerupOrbRotation = 0.0f;

   // The width of the ellipse around which powerup orbs move
   private const float POWERUP_ORB_ELLIPSE_WIDTH = 0.375f;

   // The height of the ellipse around which powerup orbs move
   private const float POWERUP_ORB_ELLIPSE_HEIGHT = 0.1875f;

   // A modifier affecting how fast the powerup orbs will rotate
   private const float POWERUP_ORB_ROTATION_SPEED = 0.3f;

   // The max number of orbs that can have particles at once. If there are more orbs than this number, all orbs will have their particles disabled.
   private const int POWERUP_MAX_ORB_PARTICLES = 6;

   // The powerups that this sea entity currently has
   protected SyncList<Powerup> _powerups = new SyncList<Powerup>();

   // The ability that this sea entity currently has
   protected SyncList<AbilityOrbData> _abilityOrbData = new SyncList<AbilityOrbData>();

   #endregion

   #endregion
}