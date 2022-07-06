using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using DigitalRuby.LightningBolt;
using Mirror;
using System;
using Pathfinding;
using System.Linq;
using SeaBuff;

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

   // The regeneration rate of the health
   public const float HEALTH_REGEN_RATE = 1f;

   // How long we have to wait to reload
   [SyncVar]
   public float reloadDelay = 1.5f;

   // Keeps track of the consecutive attack count
   public float attackCounter = 0f;

   // The buff content this user currently has
   public SyncDictionary<SeaBuff.Type, SeaBuffData> buffContent = new SyncDictionary<SeaBuff.Type, SeaBuffData>();

   // The prefab we use for creating Attack Circles, for self shots
   public AttackCircle localAttackCirclePrefab;

   // The prefab we use for creating Attack Circles, for other's shots
   public AttackCircle defaultAttackCirclePrefab;

   // Convenient object references to the left and right side of our entity
   public GameObject leftSideTarget;
   public GameObject rightSideTarget;

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

   // The total damage taken
   [SyncVar]
   public int totalDamageTaken = 0;

   // The total heals given
   [SyncVar]
   public int totalHeals = 0;

   // The total buffs provided
   [SyncVar]
   public int totalBuffs = 0;

   // When set to true, the sprites for this sea entity will 'sink' on death
   [SyncVar]
   public bool sinkOnDeath = true;

   // The total healed value of this entity
   public int totalHealed;

   // A reference to a sea harpoon attached to this entity, if there is one
   [HideInInspector]
   public List<uint> attachedByHarpoonNetIds = new List<uint>();

   #region Enemy AI

   [Header("AI")]

   // The current seconds roaming around its territory
   public float currentSecondsPatrolingTerritory = 0;

   // The initial position of this monster spawn
   public Vector3 initialPosition;

   // Freezes pathfinding
   public bool freezeMovement;

   // Is submerging
   public bool isSubmerging;

   // The submerge speed
   public const float SUBMERGE_SPEED = 1.1f;

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
   [SyncVar]
   public int dataXmlId;

   [Header("Components")]

   // The container for our sprites
   public GameObject spritesContainer;

   // The container for our ripples
   public GameObject ripplesContainer;

   // The shadow used by some boss entities
   public GameObject seaEntityShadowContainer;

   // The transform that will contain all of our orbs (powerup orbs and ability orbs)
   public Transform orbHolder;

   // The container for the residue effects
   public Transform residueHolder;

   // The current force being applied to move this entity
   public Vector2 movementForce;

   // The pause delay in seconds after attacking
   public const float AFTER_ATTACK_PAUSE = 1;

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
      if (!Util.isBatch() || (Util.isBatch() && this is PlayerShipEntity && Util.isAutoMove())) {
         StartCoroutine(CO_UpdateAllSprites());
      }

      if (useSeaEnemyAI() && isServer) {
         initSeeker();
         detectPlayerSpawns();

         InvokeRepeating(nameof(checkEnemiesToAggro), 0.0f, 0.5f);
         StartCoroutine(CO_AttackEnemiesInRange(0.25f));
      }

      if (useSeaEnemyAI()) {
         GenericEffector.setEffectorCollisions(getMainCollider(), false, GenericEffector.Type.Current);
      }

      if (isServer) {
         if (GroupManager.self.tryGetGroupById(groupId, out Group groupInfo)) {
            totalDamageDealt = groupInfo.getTotalDamage(userId);
            totalDamageTaken = groupInfo.getTotalTank(userId);
            totalBuffs = groupInfo.getTotalBuffs(userId);
            totalHeals = groupInfo.getTotalHeals(userId);
         }
      }

      editorGenerateAggroCone();
      initialPosition = transform.localPosition;

      InvokeRepeating(nameof(checkBuffOrbs), 0.2f, 0.2f);

      if (isServer) {
         InvokeRepeating(nameof(updateBuffs), 1.0f, 1.0f);
      }
   }

   [Server]
   public virtual int applyDamage (int amount, uint damageSourceNetId, Attack.Type attackType) {
      float damageMultiplier = 1.0f;

      // Apply damage reduction, if there is any
      if (this.isPlayerShip()) {
         // Hard cap damage reduction at 75%, and damage addition at 100% extra
         damageMultiplier = Mathf.Clamp(damageMultiplier - PowerupManager.self.getPowerupMultiplierAdditive(userId, Powerup.Type.DamageReduction), 0.25f, 2.0f);
      }

      // If we're invulnerable, take 0 damage
      if (getIsInvulnerable()) {
         damageMultiplier = 0.0f;
      }

      if (isDead()) {
         // Do not apply any damage if the entity is already dead
         return 0;
      }

      amount = (int) (amount * damageMultiplier);
      currentHealth -= amount;

      // Don't trigger damage-related functions if we didn't take any damage
      if (amount == 0) {
         return 0;
      }

      // Keep track of the damage each attacker has done on this entity
      NetEntity sourceEntity = MyNetworkManager.fetchEntityFromNetId<NetEntity>(damageSourceNetId);

      // If we damaged ourselves, don't include us in attackers, etc.
      if (damageSourceNetId == netId) {
         sourceEntity = null;
      }

      if (sourceEntity != null) {
         if (sourceEntity.userId > 0 && sourceEntity is PlayerShipEntity) {
            if (_damageReceivedPerAttacker.ContainsKey(sourceEntity.userId)) {
               _damageReceivedPerAttacker[sourceEntity.userId] += amount;
            } else {
               _damageReceivedPerAttacker[sourceEntity.userId] = amount;
            }

            customRegisterDamageReceived(sourceEntity.userId, amount);

            // Cache the source damage record inflicted
            if (!_totalAttackers.ContainsKey(damageSourceNetId)) {
               _totalAttackers.Add(damageSourceNetId, new DamageRecord());
            }

            // Make sure class is initialized
            if (_totalAttackers[damageSourceNetId] == null) {
               _totalAttackers[damageSourceNetId] = new DamageRecord();
            }

            _totalAttackers[damageSourceNetId].lastAttackTime = NetworkTime.time;
            _totalAttackers[damageSourceNetId].totalDamage += amount;
         }
      }

      if (damageSourceNetId != netId) {
         noteAttacker(damageSourceNetId);
         Rpc_NoteAttacker(damageSourceNetId);
      }

      if (isDead()) {
         onDeath();
      }

      onDamage(amount);
      if (sourceEntity != null && sourceEntity is SeaEntity) {
         SeaEntity seaEntity = (SeaEntity) sourceEntity;

         if (GroupManager.self.tryGetGroupById(groupId, out Group groupInfo)) {
            groupInfo.addTankStatsForUser(userId, amount);
            totalDamageTaken = groupInfo.getTotalTank(userId);
         }
         if (GroupManager.self.tryGetGroupById(seaEntity.groupId, out Group targetGroup)) {
            targetGroup.addDamageStatsForUser(seaEntity.userId, amount);
            seaEntity.totalDamageDealt = targetGroup.getTotalDamage(seaEntity.userId);
         }
      }

      tryTriggerDamageAchievements(damageSourceNetId, attackType);

      // Return the final amount of damage dealt
      return amount;
   }

   private void tryTriggerDamageAchievements (uint damageSourceNetId, Attack.Type attackType) {
      NetEntity sourceNetEntity = MyNetworkManager.fetchEntityFromNetId<NetEntity>(damageSourceNetId);
      if (!(sourceNetEntity is SeaEntity)) {
         return;
      }

      SeaEntity sourceEntity = sourceNetEntity as SeaEntity;
      SeaEntity targetEntity = this;

      // Register achievements for player damaging things
      if (sourceEntity is PlayerShipEntity) {
         tryRegisterAttackTypeAchievement(attackType, sourceEntity, false);
         if (targetEntity is SeaMonsterEntity) {
            if (targetEntity.currentHealth <= 0) {
               AchievementManager.registerUserAchievement(sourceEntity, ActionType.KillSeaMonster);
            }

            AchievementManager.registerUserAchievement(sourceEntity, ActionType.HitSeaMonster);
         } else if (targetEntity is BotShipEntity) {
            if (targetEntity.currentHealth <= 0) {
               AchievementManager.registerUserAchievement(sourceEntity, ActionType.SinkedShips);
            }

            AchievementManager.registerUserAchievement(sourceEntity, ActionType.HitEnemyShips);
         } else if (targetEntity is PlayerShipEntity) {
            if (targetEntity.currentHealth <= 0) {
               AchievementManager.registerUserAchievement(sourceEntity, ActionType.SinkedShips);
            }

            AchievementManager.registerUserAchievement(sourceEntity, ActionType.HitPlayerWithCannon);
            AchievementManager.registerUserAchievement(sourceEntity, ActionType.HitEnemyShips);
         }
      }

      // Register achievements for player getting damaged by things
      if (targetEntity is PlayerShipEntity) {
         if (targetEntity.currentHealth <= 0) {
            AchievementManager.registerUserAchievement(targetEntity, ActionType.ShipDie);
         }

         tryRegisterAttackTypeAchievement(attackType, targetEntity, true);
      }
   }

   private void tryRegisterAttackTypeAchievement (Attack.Type attackType, NetEntity achievementEarner, bool wasDamageReceiver) {
      // Register achievements for being hit by various attack types
      if (wasDamageReceiver) {
         switch (attackType) {
            case Attack.Type.Ice:
               AchievementManager.registerUserAchievement(achievementEarner, ActionType.Frozen);
               break;
            case Attack.Type.Poison:
            case Attack.Type.Poison_Circle:
            case Attack.Type.Venom:
               AchievementManager.registerUserAchievement(achievementEarner, ActionType.Poisoned);
               break;
            case Attack.Type.Electric:
            case Attack.Type.Shock_Ball:
               AchievementManager.registerUserAchievement(achievementEarner, ActionType.Electrocuted);
               break;
         }

         // Register achievements for hitting entities with various attack types
      } else {
         switch (attackType) {
            case Attack.Type.Ice:
               AchievementManager.registerUserAchievement(achievementEarner, ActionType.FreezeEnemy);
               break;
            case Attack.Type.Poison:
            case Attack.Type.Poison_Circle:
            case Attack.Type.Venom:
               AchievementManager.registerUserAchievement(achievementEarner, ActionType.PoisonEnemy);
               break;
         }
      }
   }

   [Server]
   protected virtual void customRegisterDamageReceived (int userId, int amount) {
   }

   protected virtual void onDamage (int damage) { }

   public virtual void onDeath () {
      if (_hasRunOnDeath) {
         return;
      }

      if (isServer) {
         if (openWorldRespawnData != null) {
            if (openWorldRespawnData.respawnTime > 0) {
               if (this is SeaMonsterEntity) {
                  EnemyManager.self.processSeamonsterSpawn(openWorldRespawnData);
               }
               if (this is BotShipEntity) {
                  EnemyManager.self.processBotshipSpawn(openWorldRespawnData);
               }
            }
         }
         
         NetEntity lastAttacker = MyNetworkManager.fetchEntityFromNetId<NetEntity>(_lastAttackerNetId);

         int healthValMAx = maxHealth + totalHealed;
         if (lastAttacker != null) {
            GameStatsManager gameStatsManager = GameStatsManager.self;

            if (gameStatsManager != null) {
               if (lastAttacker.isPlayerShip() && GameStatsManager.self.isUserRegistered(lastAttacker.userId)) {
                  int totalSilverReward = SilverManager.computeSilverReward(this);
                  if (_totalAttackers.Count > 0) {
                     foreach (KeyValuePair<uint, DamageRecord> damagerData in _totalAttackers) {
                        try {
                           NetEntity damagerEntity = MyNetworkManager.fetchEntityFromNetId<NetEntity>(damagerData.Key);
                           if (damagerEntity != null) {
                              // Toggle on if silver reward is based on total damage inflicted
                              bool splitBasedOnDamage = false;
                              float damagePercentage = splitBasedOnDamage ? (((float) damagerData.Value.totalDamage / (float) healthValMAx) * totalSilverReward) : totalSilverReward;
                              gameStatsManager.addSilverAmount(damagerEntity.userId, (int) damagePercentage);
                              Target_ReceiveSilverCurrency(damagerEntity.getPlayerShipEntity().connectionToClient, (int) damagePercentage, SilverManager.SilverRewardReason.Kill);
                           } else {
                              D.debug("Error, damager entity {" + damagerData.Key + "} is missing!");
                           }
                        } catch {
                           NetEntity damagerEntity = MyNetworkManager.fetchEntityFromNetId<NetEntity>(damagerData.Key);
                           D.debug("Something went wrong with Silver rewards! {" + damagerEntity + "} {" + damagerEntity == null ? "NUll" : damagerEntity.getPlayerShipEntity() + "} {" + damagerData + "}");
                        }
                     }
                  } else {
                     gameStatsManager.addSilverAmount(lastAttacker.userId, totalSilverReward);
                     Target_ReceiveSilverCurrency(lastAttacker.getPlayerShipEntity().connectionToClient, totalSilverReward, SilverManager.SilverRewardReason.Kill);
                  }
               }

               if (this.isPlayerShip()) {
                  if (WorldMapManager.isWorldMapArea(areaKey)) {
                     rpc.processBadgeReward(_lastAttackerNetId);
                  }

                  gameStatsManager.addPlayerKillCount(lastAttacker.userId);
                  gameStatsManager.addDeathCount(this.userId);
                  this.rpc.broadcastPvPKill(lastAttacker, this);

                  if (GameStatsManager.self.isUserRegistered(lastAttacker.userId)) {
                     gameStatsManager.addSilverRank(lastAttacker.userId, 1);
                  }

                  if (GameStatsManager.self.isUserRegistered(this.userId)) {
                     gameStatsManager.resetSilverRank(this.userId);
                     int silverPenalty = SilverManager.computeSilverPenalty(this);
                     gameStatsManager.addSilverAmount(this.userId, -silverPenalty);
                     Target_ReceiveSilverCurrency(this.connectionToClient, -silverPenalty, SilverManager.SilverRewardReason.Death);
                  }

                  rpc.assignVoyageRatingPoints(VoyageRatingManager.computeVoyageRatingPointsReward(VoyageRatingManager.RewardReason.DeathAtSea));

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
               if (this is PlayerShipEntity) {
                  foreach (KeyValuePair<uint, double> attacker in _attackers) {
                     NetEntity attackerEntity = MyNetworkManager.fetchEntityFromNetId<NetEntity>(attacker.Key);
                     if (attackerEntity != null && attackerEntity.userId != lastAttacker.userId) {
                        gameStatsManager.addAssistCount(attackerEntity.userId);

                        if (attackerEntity.isPlayerShip() && GameStatsManager.self.isUserRegistered(attackerEntity.userId)) {
                           int assistReward = SilverManager.computeAssistReward(this);
                           gameStatsManager.addSilverAmount(attackerEntity.userId, assistReward);
                           Target_ReceiveSilverCurrency(attackerEntity.getPlayerShipEntity().connectionToClient, assistReward, SilverManager.SilverRewardReason.Assist);
                        }
                     }
                  }
               }
            }
         }

         rewardXPToAllAttackers();
         _buffs.Clear();
         Rpc_OnDeath();
         _hasRunOnDeath = true;
      }

      removeAllBuffOrbs();
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

         // Trap for cases when xp is calculated as negative
         if (xp < 0) {
            D.error("Negative Sailor XP is detected! KV.Key = " + KV.Key.ToString() + ", KV.Value = " + KV.Value.ToString() + ", totalDamage = " + totalDamage.ToString() + ", rewardedXP = " + rewardedXP.ToString());
            xp = 10;
         }
         NetEntity entity = EntityManager.self.getEntity(targetUserId);

         // Background thread
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            // Rewards are greater for users that have enabled their pvp status in world maps
            if (entity != null && entity.enablePvp && WorldMapManager.isWorldMapArea(areaKey)) {
               xp = (int) (xp * 1.5f);
            }

            DB_Main.addJobXP(targetUserId, jobType, xp);
            Jobs jobs = DB_Main.getJobXP(targetUserId);

            // Back to the Unity thread
            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
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
      // Remove player ship entity after batch test
      if (!Util.isBatch() || (this is PlayerShipEntity && Util.isAutoMove())) {
         StartCoroutine(CO_UpdateAllSprites());
      }
   }

   protected override void Update () {
      base.Update();

      // Regenerate health
      if (NetworkServer.active && isSeaMonsterPvp() && currentHealth < maxHealth && currentHealth > 0) {
         if (hasRegenerationBuff()) {
            if (this is BotShipEntity || this is SeaMonsterEntity) {
               totalHealed += (int) HEALTH_REGEN_RATE;
            }
            currentHealth += (int) HEALTH_REGEN_RATE;
         }
      }

      if (!isDead()) {
         updateBuffOrbs();
      }

      // If we've died, start slowing moving our sprites downward
      if (isDead()) {
         if (_outline != null) {
            _outline.setVisibility(false);
         }

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
                  Util.setLocalY(monsterEntity.corpseHolder.transform, monsterEntity.corpseHolder.transform.localPosition.y - .01f * Time.smoothDeltaTime);
               }
            }
         } else if (!_playedDestroySound && this is ShipEntity && isClient) {
            _playedDestroySound = true;

            if (this.isBot()) {
               SoundEffectManager.self.playFmodSfx(SoundEffectManager.ENEMY_SHIP_DESTROYED, this.transform.position);
            } else {
               SoundEffectManager.self.playFmodSfx(SoundEffectManager.PLAYER_SHIP_DESTROYED, this.transform.position);
            }

            // Hide all the sprites
            foreach (SpriteRenderer renderer in _renderers) {
               renderer.enabled = false;
            }

            // Play the explosion effect
            Instantiate(isBot() ? PrefabsManager.self.pirateShipExplosionEffect : PrefabsManager.self.playerShipExplosionEffect, transform.position, Quaternion.identity);
         }

         if (sinkOnDeath) {
            if (this.isSeaMonster()) {
               Util.setLocalY(spritesContainer.transform, spritesContainer.transform.localPosition.y - .01f * Time.smoothDeltaTime);
               foreach (SpriteRenderer renderer in _renderers) {
                  if (renderer.enabled) {
                     float newAlpha = Mathf.Lerp(1f, 0f, spritesContainer.transform.localPosition.y * -50f);
                     Util.setMaterialBlockAlpha(renderer, newAlpha);
                  }
               }
            } else {
               Util.setLocalY(spritesContainer.transform, spritesContainer.transform.localPosition.y - .03f * Time.smoothDeltaTime);
            }
         }
      }
   }

   protected override void FixedUpdate () {
      base.FixedUpdate();

      // Return if we're not a living AI on the server
      if (!isServer || isDead() || !useSeaEnemyAI() || freezeMovement) {
         return;
      }

      // If pvp sea monster moves away from its territory, retreat back to it
      if (isSeaMonsterPvp()) {
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
   protected void Rpc_TriggerAttackAnim (Anim.Type animType) {
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

      List<uint> lightningTargets = new List<uint>();
      foreach (Collider2D collidedEntity in hits) {
         if (collidedEntity != null) {
            if (collidedEntity.GetComponent<SeaEntity>() != null) {
               SeaEntity seaEntity = collidedEntity.GetComponent<SeaEntity>();
               if (primaryTargetNetID == seaEntity.netId) {
                  continue;
               }
               if (this.isEnemyOf(seaEntity) && !collidedEntities.ContainsKey(seaEntity) && !seaEntity.isDead() && seaEntity.instanceId == this.instanceId) {
                  if (seaEntity.spritesContainer == null) {
                     D.debug("Sprite container for chain lighting is missing!");
                  }
                  Vector3 newPosition = seaEntity.spritesContainer == null ? seaEntity.transform.position : seaEntity.spritesContainer.transform.position;
                  float distanceToTarget = Vector2.Distance(sourcePos, newPosition);
                  if (distanceToTarget <= 1.1f) {
                     lightningTargets.Add(seaEntity.netId);
                     int finalDamage = seaEntity.applyDamage(damage, attackerNetId, Attack.Type.Shock_Ball);
                     seaEntity.Rpc_ShowExplosion(attackerNetId, collidedEntity.transform.position, finalDamage, Attack.Type.Shock_Ball, false);

                     collidedEntities.Add(seaEntity, collidedEntity.transform);
                     targetIDList.Add(seaEntity.netId);
                  }
               }
            }
         }
      }

      Rpc_ChainLightning(lightningTargets, primaryTargetNetID, sourcePos);
   }

   [Server]
   public void cannonballChainLightning (uint attackerNetId, Vector2 sourcePos, uint primaryTargetNetID, float chainRadius, float damage) {
      List<SeaEntity> enemiesInRange = Util.getEnemiesInCircle(this, sourcePos, chainRadius);
      Dictionary<NetEntity, Transform> collidedEntities = new Dictionary<NetEntity, Transform>();
      List<uint> targetNetIdList = new List<uint>();
      int damageInt = (int) damage;

      List<uint> lightningTargets = new List<uint>();
      foreach (SeaEntity enemy in enemiesInRange) {
         if (this.isEnemyOf(enemy) && !collidedEntities.ContainsKey(enemy) && !enemy.isDead() && enemy.instanceId == this.instanceId && enemy.netId != primaryTargetNetID) {
            int finalDamage = enemy.applyDamage(damageInt, attackerNetId, Attack.Type.Shock_Ball);
            enemy.Rpc_ShowDamage(Attack.Type.None, enemy.transform.position, finalDamage);
            if (enemy.spritesContainer == null) {
               D.debug("Sprite container for chain lighting is missing!");
            }
            lightningTargets.Add(enemy.netId);
            collidedEntities.Add(enemy, enemy.transform);
            targetNetIdList.Add(enemy.netId);
         }
      }

      Rpc_ChainLightning(lightningTargets, primaryTargetNetID, sourcePos);
   }

   [ClientRpc]
   public void Rpc_SpawnBossVenomResidue (uint creatorNetId, int instanceId, Vector3 location, bool spawnResidue) {
      if (spawnResidue) {
         VenomResidue venomResidue = Instantiate(PrefabsManager.self.bossVenomResiduePrefab, location, Quaternion.identity);
         venomResidue.creatorNetId = creatorNetId;
         venomResidue.instanceId = instanceId;
      }

      ExplosionManager.createSlimeExplosion(location);

      //SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Coralbow_Attack, this.transform.position);
   }

   [ClientRpc]
   public void Rpc_SpawnVenomResidue (uint creatorNetId, int instanceId, Vector3 location) {
      VenomResidue venomResidue = Instantiate(PrefabsManager.self.venomResiduePrefab, location, Quaternion.identity);
      venomResidue.creatorNetId = creatorNetId;
      venomResidue.instanceId = instanceId;
      ExplosionManager.createSlimeExplosion(location);

      //SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Coralbow_Attack, this.transform.position);
   }

   [ClientRpc]
   private void Rpc_ChainLightning (List<uint> targetPosGround, uint primaryTargetNetID, Vector2 sourcePos) {
      SeaEntity targetEntity = SeaManager.self.getEntity(primaryTargetNetID);
      if (targetEntity == null) {
         D.debug("Sea manager does not contain Target Entity! " + primaryTargetNetID);
         return;
      }

      if (targetEntity.spritesContainer == null) {
         D.debug("Entity {" + primaryTargetNetID + "} has no sprite container");
         return;
      }

      Transform targetTransform = targetEntity.spritesContainer.transform;
      EffectManager.self.create(Effect.Type.Shock_Collision, sourcePos);
      foreach (uint targetNetId in targetPosGround) {
         if (targetNetId == primaryTargetNetID) {
            continue;
         }

         SeaEntity chainedEntity = SeaManager.self.getEntity(targetNetId);
         if (chainedEntity == null) {
            D.debug("Sea manager does not contain Chained Entity! " + targetNetId);
            continue;
         }

         Vector2 targetPos = chainedEntity.spritesContainer.transform.position;
         float distanceBetweenChain = Vector2.Distance(targetTransform.position, targetPos);
         if (distanceBetweenChain <= 1.25f) {
            // Setup lightning chain
            LightningBoltScript lightning = Instantiate(PrefabsManager.self.lightningChainPrefab);
            lightning.transform.position = new Vector3(sourcePos.x, sourcePos.y, lightning.transform.position.z);

            // Set the chain links
            lightning.StartObject.transform.SetParent(targetEntity.transform);
            lightning.EndObject.transform.SetParent(chainedEntity.transform);
            Destroy(lightning.StartObject, 2);
            Destroy(lightning.EndObject, 2);

            // Update chain links to their designated coordinates
            lightning.StartObject.transform.position = new Vector3(sourcePos.x, sourcePos.y, lightning.transform.position.z);//new Vector3(sourcePos.x, sourcePos.y, lightning.transform.position.z);//new Vector3(lightning.transform.position.x, lightning.transform.position.y, lightning.transform.position.z);
            lightning.EndObject.transform.position = new Vector3(targetPos.x, targetPos.y, lightning.transform.position.z);
            lightning.hasActivated = true;

            // Make sure the attached lightning keeps track of the entity position to prevent the lightning to latch across the entire screen during respawn
            AttachedLightning attachedLightning = lightning.GetComponent<AttachedLightning>();
            if (attachedLightning != null) {
               attachedLightning.startPoint = lightning.StartObject.transform;
               attachedLightning.endPoint = lightning.EndObject.transform;
               attachedLightning.hasActivated = true;
            }

            if (lightning.GetComponent<LineRenderer>() != null) {
               lightning.GetComponent<LineRenderer>().enabled = true;
            } else {
               D.debug("Lightning has no Line Renderer!!");
            }

            EffectManager.self.create(Effect.Type.Shock_Collision, targetPos);
         } else {
            D.debug("Chain was too far {" + distanceBetweenChain + "}");
         }
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
         //SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Ship_Hit_1, pos);
      } else {
         if (attackType == Attack.Type.Tentacle || attackType == Attack.Type.Poison_Circle) {
            // If tentacle attack, calls tentacle collision effect
            Instantiate(PrefabsManager.self.tentacleCollisionPrefab, this.transform.position + new Vector3(0f, 0), Quaternion.identity);
         } else if (attackType == Attack.Type.Venom || attackType == Attack.Type.Poison) {
            // If worm attack, calls slime collision effect
            ExplosionManager.createSlimeExplosion(pos);

            // Play attached SFX
            SoundEffectManager.self.playAttachedWithPath(SoundEffectManager.HORROR_BLOB_DAMAGE, this.gameObject);
         } else if (attackType == Attack.Type.Ice) {
            // TODO: Add ice effect logic here
         } else {
            ShipAbilityData shipData = getSeaAbility(attackType);
            if (shipData == null) {
               // Show generic explosion
               Instantiate(PrefabsManager.self.requestCannonExplosionPrefab(currentImpactMagnitude), pos, Quaternion.identity);
            } else {
               EffectManager.createDynamicEffect(shipData.collisionSpritePath, pos, shipData.abilitySpriteFXPerFrame, null);
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
      ShipDamageText textPrefab = PrefabsManager.self.getTextPrefab(Attack.Type.None);
      if (textPrefab) {
         ShipDamageText damageText = Instantiate(textPrefab, transform.position, Quaternion.identity);
         damageText.setDamage(damage);
      }

      if (shakeCamera && isLocalPlayer) {
         CameraManager.shakeCamera();
      }
   }

   [ClientRpc]
   public void Rpc_ShowTerrainHit (Vector3 pos, Attack.ImpactMagnitude impactMagnitude) {
      if (Util.hasLandTile(pos)) {
         Instantiate(PrefabsManager.self.requestCannonSmokePrefab(impactMagnitude), pos, Quaternion.identity);
         //SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Slash_Lightning, pos);
      } else {
         Instantiate(PrefabsManager.self.requestCannonSplashPrefab(impactMagnitude), pos + new Vector3(0f, -.1f), Quaternion.identity);

         // FMOD sfx for water
         //SoundEffectManager.self.playCannonballImpact(SoundEffectManager.Cannonball.Water_Impact, pos);
         //SoundManager.playEnvironmentClipAtPoint(SoundManager.Type.Splash_Cannon_1, pos);
      }
   }

   [ClientRpc]
   public void Rpc_NetworkProjectileDamage (uint attackerNetID, Attack.Type attackType, Vector3 location) {
      SeaEntity sourceEntity = SeaManager.self.getEntity(attackerNetID);

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

   [ClientRpc]
   public void Rpc_ToggleColliders (bool isEnabled) {
      foreach (Collider2D newCollider in colliderList) {
         newCollider.enabled = isEnabled;
      }
   }

   [ClientRpc]
   public void Rpc_ToggleSprites (bool isEnabled) {
      spritesContainer.SetActive(isEnabled);
   }

   public virtual void noteAttacker (NetEntity entity) {
      if (entity != null) {
         noteAttacker(entity.netId);
      }
   }

   public virtual void noteAttacker (uint netId) {
      _lastAttackerNetId = netId;

      if (_patrolingWaypointState == WaypointState.RETREAT) {
         return;
      }

      _attackers[netId] = NetworkTime.time;

      // If this is a seamonster and is attacked by a tower, immediately retreat to spawn point
      if (isServer && isSeaMonsterPvp()) {
         NetEntity entity = MyNetworkManager.fetchEntityFromNetId<NetEntity>(_lastAttackerNetId);
         if (entity && entity is SeaStructure) {
            retreatToSpawn();
         } else {
            // Stop health regeneration when hit by player and combat begins
            if (hasRegenerationBuff()) {
               setEnemyHealthRegeneration(false);
            }
         }
      }
   }

   protected void retreatToSpawn () {
      setEnemyHealthRegeneration(true);
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
         ref _currentSecondsPatroling, findRandomVicinityPosition);
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
   public void meleeAtSpot (Vector2 spot, int selectedAbilityId, float distanceGap) {
      if (isDead() || !hasReloaded()) {
         return;
      }

      // Note the time at which we last successfully attacked
      _lastAttackTime = NetworkTime.time;

      float distance = Vector2.Distance(this.transform.position, spot);
      float delay = Mathf.Clamp(distance, .5f, 1.5f);

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

      // Have the server check for collisions after the attack reaches the target
      StartCoroutine(CO_CheckCircleForCollisions(this, delay, spot, Attack.Type.None, true, distanceGap, currentImpactMagnitude, selectedAbilityId));

      // Make note on the clients that the ship just attacked
      Rpc_NoteAttack();
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

      switch (shipAbility.selectedAttackType) {
         case Attack.Type.Venom:
         case Attack.Type.Boulder:
            // Create networked projectile
            D.adminLog("Trigger timed projectile: " + shipAbility.selectedAttackType, D.ADMIN_LOG_TYPE.Sea);
            StartCoroutine(CO_FireAtSpot(spot, abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, spawnPosition));
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
               StartCoroutine(CO_FireAtSpot(sourcePos + new Vector2(0, target), abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, sourcePos + new Vector2(0, offset)));
               StartCoroutine(CO_FireAtSpot(sourcePos + new Vector2(0, -target), abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, sourcePos + new Vector2(0, -offset)));
               StartCoroutine(CO_FireAtSpot(sourcePos + new Vector2(target, 0), abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, sourcePos + new Vector2(offset, 0)));
               StartCoroutine(CO_FireAtSpot(sourcePos + new Vector2(-target, 0), abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, sourcePos + new Vector2(-offset, 0)));
            } else {
               // Diagonal Attack Pattern
               StartCoroutine(CO_FireAtSpot(sourcePos + new Vector2(diagonalTargetValue, target), abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, sourcePos + new Vector2(diagonalValue, offset)));
               StartCoroutine(CO_FireAtSpot(sourcePos + new Vector2(diagonalTargetValue, -target), abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, sourcePos + new Vector2(diagonalValue, -offset)));
               StartCoroutine(CO_FireAtSpot(sourcePos + new Vector2(-target, diagonalTargetValue), abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, sourcePos + new Vector2(-offset, diagonalValue)));
               StartCoroutine(CO_FireAtSpot(sourcePos + new Vector2(-target, -diagonalTargetValue), abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, sourcePos + new Vector2(-offset, -diagonalValue)));
            }
            break;
         case Attack.Type.Poison_Circle:
            // Fire multiple projectiles in a circle around the target
            float innerRadius = .7f;
            float outerRadius = .9f;
            float angleStep = 60f;

            // We will pick a random shot to skip, so there is a gap in the circle to escape through.
            int totalNumShots = 2 * (Mathf.RoundToInt(360.0f / angleStep));
            int shotToSkip = UnityEngine.Random.Range(0, totalNumShots);
            int shotCounter = -1;

            // Play sfx for cluster
            SoundEffectManager.self.playHorrorPoisonSfx(SoundEffectManager.HorrorAttackType.Cluster, spawnPosition);

            // Inner circle
            for (float angle = 0; angle < 360f; angle += angleStep) {
               shotCounter++;
               if (shotCounter == shotToSkip) {
                  continue;
               }
               StartCoroutine(CO_FireAtSpot(spot + new Vector2(innerRadius * Mathf.Cos(Mathf.Deg2Rad * angle), innerRadius * Mathf.Sin(Mathf.Deg2Rad * angle)), abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, spawnPosition));
            }

            // Outer circle
            for (float angle = angleStep / 2; angle < 360f; angle += angleStep) {
               shotCounter++;
               if (shotCounter == shotToSkip) {
                  continue;
               }
               StartCoroutine(CO_FireAtSpot(spot + new Vector2(outerRadius * Mathf.Cos(Mathf.Deg2Rad * angle), outerRadius * Mathf.Sin(Mathf.Deg2Rad * angle)), abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, spawnPosition));
            }
            break;

         default:
            D.adminLog("Trigger generic projectile at spot, Delay is: " + attackDelay, D.ADMIN_LOG_TYPE.Sea);
            StartCoroutine(CO_FireAtSpot(spot, abilityId, shipAbility.selectedAttackType, attackDelay, launchDelay, spawnPosition));
            break;
      }

      _lastAttackTime = NetworkTime.time;
      attackCounter++;

      Rpc_RegisterAttackTime(attackDelay);
      Rpc_NoteAttack();
   }

   [Server]
   private IEnumerator CO_FireAtSpot (Vector2 spot, int abilityId, Attack.Type attackType, float attackDelay, float launchDelay, Vector2 spawnPosition = new Vector2()) {
      if (isDead()) {
         yield break;
      }

      ShipAbilityData abilityData = ShipAbilityManager.self.getAbility(abilityId);
      if (abilityData != null) {
         if (abilityData.splitsAfterAttackCap && attackCounter > abilityData.splitAttackCap) {
            attackCounter = 0;
            for (int i = 0; i < abilityData.splitAttackCap; i++) {
               float offsetValue = .75f;
               Vector3 randomPos = findPositionAroundPosition(spot, offsetValue);
               fireProjectileAtTarget(spawnPosition, randomPos, abilityId);
            }
            yield return null;
         }
      }
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
      float projectileSpeed = 1.0f;

      SoundEffectManager.SeaAbilityType seaAbilityType = SoundEffectManager.SeaAbilityType.None;

      if (abilityData != null) {
         attackType = abilityData.selectedAttackType;
         attackMagnitude = abilityData.impactMagnitude;
         hasArch = abilityData.hasArch;
         projectileSpeed = abilityData.projectileSpeed;
         seaAbilityType = abilityData.sfxType;
      }

      // Load projectile data
      ProjectileStatData projectileData = getProjectileDataFromAbility(abilityId);

      SeaProjectile projectile = Instantiate(PrefabsManager.self.seaProjectilePrefab, startPosition, Quaternion.identity);

      // Modify projectile speed based on attack type
      timeToReachTarget /= Attack.getSpeedModifier(attackType);
      timeToReachTarget /= projectileSpeed;

      // Calculate projectile values
      float speed = distanceToTarget / timeToReachTarget;
      Vector2 toEndPos = endPosition - startPosition;
      Vector2 projectileVelocity = speed * toEndPos.normalized;
      float lobHeight = Mathf.Clamp(1.0f / speed, 0.3f, 1.0f);
      float disableColliderFor = 0.95f;

      if (!hasArch) {
         lobHeight = 0.0f;
         disableColliderFor = 0.0f;
      }

      projectile.initAbilityProjectile(netId, instanceId, attackMagnitude, abilityId, projectileVelocity, lobHeight, lifetime: timeToReachTarget, attackType: attackType, disableColliderFor: disableColliderFor, minDropShadowScale: 0.5f);
      NetworkServer.Spawn(projectile.gameObject);

      Rpc_SpawnProjectileIndicator(endPosition, timeToReachTarget, projectileData.projectileScale, seaAbilityType);
   }

   [ClientRpc]
   protected void Rpc_SpawnProjectileIndicator (Vector2 spawnPosition, float lifetime, float scaleModifier, SoundEffectManager.SeaAbilityType seaAbilityType) {
      ProjectileTargetingIndicator targetingIndicator = Instantiate(PrefabsManager.self.projectileTargetingIndicatorPrefab, spawnPosition, Quaternion.identity);
      targetingIndicator.init(lifetime, scaleModifier);

      // Play SFX for sea abilities
      SoundEffectManager.self.playSeaAbilitySfx(seaAbilityType, targetPosition: spawnPosition);
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
               // Check all conditions if this target can be damaged
               if (!canDamageTargetUsingCircleCollision(attacker, targetEntity, targetPlayersOnly)) {
                  continue;
               }

               hitEnemy = true;

               if (!targetEntity.getIsInvulnerable()) {
                  int damage = 0;

                  // If no ability assigned, get damage based on attack type
                  if (abilityId <= 0) {
                     damage = getDamageForShot(attackType, distanceModifier);
                  }
                  ShipAbilityData shipAbilityData = null;
                  ProjectileStatData projectileData = null;
                  string abilityName = "";
                  string projectileName = "";

                  if (abilityId > 0) {
                     shipAbilityData = getSeaAbility(abilityId);
                     projectileData = getProjectileDataFromAbility(abilityId);
                     float abilityDamageModifier = projectileData.projectileDamage * shipAbilityData.damageModifier;
                     float baseSkillDamage = projectileData.projectileDamage + abilityDamageModifier;
                     abilityName = shipAbilityData.abilityName;
                     projectileName = projectileData.projectileName;

                     // Ability data attack type will be followed
                     if (attackType == Attack.Type.None) {
                        attackType = shipAbilityData.selectedAttackType;
                     }

                     damage = getDamageForShot((int) baseSkillDamage, distanceModifier);

                     if (this is SeaMonsterEntity) {
                        SeaMonsterEntity seamonsterEntity = (SeaMonsterEntity) this;
                        if (seamonsterEntity.seaMonsterData.roleType == RoleType.Master) {
                           D.editorLog("{" + seamonsterEntity.monsterType + "} CO Circle Collision: {" + shipAbilityData.abilityName + "} {" + attackType + "} {" + damage + "} {" + projectileName + "}", Color.yellow);
                        }
                     }

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

                  int finalDamage = targetEntity.applyDamage(damage, attacker.netId, attackType);
                  processAchievements(targetEntity, finalDamage, attackType);

                  targetEntity.Rpc_ShowExplosion(attacker.netId, circleCenter, finalDamage, attackType, false);

                  // Trigger status based effects here
                  switch (attackType) {
                     case Attack.Type.Shock_Ball:
                        chainLightning(attacker.netId, targetEntity.transform.position, targetEntity.netId);
                        break;
                     case Attack.Type.Ice:
                        StatusManager.self.create(Status.Type.Stunned, 1.0f, 2f, targetEntity.netId);
                        break;
                     case Attack.Type.Venom:
                        StatusManager.self.create(Status.Type.Slowed, 0.3f, 1f, targetEntity.netId);
                        break;
                     case Attack.Type.Poison:
                        targetEntity.applyStatus(Status.Type.Poisoned, 1, shipAbilityData == null ? 1 : shipAbilityData.statusDuration, netId);
                        break;
                  }
               } else {
                  targetEntity.Rpc_ShowExplosion(attacker.netId, circleCenter, 0, Attack.Type.None, false);
               }

               enemyHitList.Add(targetEntity);
            }
         }
      }

      if (attackType == Attack.Type.Tentacle || attackType == Attack.Type.Poison_Circle) {

         Area area = AreaManager.self.getArea(areaKey);
         bool hitSeaTile = !area.hasLandTile(circleCenter);
         if (area != null) {
            SeaEntity sourceEntity = SeaManager.self.getEntity(netId);

            // Only spawn a venom residue on sea tiles
            if (hitSeaTile) {
               VenomResidue venomResidue = Instantiate(PrefabsManager.self.bossVenomResiduePrefab, circleCenter, Quaternion.identity);
               venomResidue.creatorNetId = netId;
               venomResidue.instanceId = instanceId;
            }

            sourceEntity.Rpc_SpawnBossVenomResidue(netId, instanceId, circleCenter, hitSeaTile);
         }
      }

      // If we didn't hit an enemy, show an effect based on whether we hit land or water
      if (!hitEnemy) {
         Rpc_ShowTerrainHit(circleCenter, impactMagnitude);
      }
   }

   private bool canDamageTargetUsingCircleCollision (SeaEntity attacker, NetEntity targetEntity, bool targetPlayersOnly) {
      if (targetPlayersOnly && targetEntity.GetComponent<ShipEntity>() == null) {
         return false;
      }

      // Make sure we don't hit ourselves
      if (targetEntity == this) {
         return false;
      }

      // Check if the attacker and the target are allies
      if (attacker.isAllyOf(targetEntity)) {
         return false;
      }

      // Prevent players from damaging each other in PvE instances
      if (attacker.isAdversaryInPveInstance(targetEntity)) {
         return false;
      }

      // Make sure the target is in our same instance
      if (targetEntity.instanceId != this.instanceId) {
         return false;
      }

      // Prevent players from being damaged by other players if they have not entered PvP yet
      if (attacker.isPlayerShip() && !targetEntity.canBeAttackedByPlayers()) {
         return false;
      }

      return true;
   }

   private void processAchievements (NetEntity targetEntity, int finalDamage, Attack.Type attackType) {
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

      switch (attackType) {
         case Attack.Type.Ice:
            // If enemy ship freezes a player ship
            if (this is BotShipEntity) {
               if (targetEntity is PlayerShipEntity) {
                  // Registers the frozen action status to the achievementdata for recording
                  AchievementManager.registerUserAchievement(targetEntity, ActionType.Frozen);
               }
            }
            break;
      }
   }

   public static Collider2D[] getHitColliders (Vector2 circleCenter, float radius = .20f) {
      // Check for collisions inside the circle
      Collider2D[] hits = new Collider2D[40];
      Physics2D.OverlapCircleNonAlloc(circleCenter, radius, hits, LayerMask.GetMask(LayerUtil.SHIPS, LayerUtil.SEA_MONSTERS));

      return hits;
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
      switch (statusType) {
         case Status.Type.Burning:
            strength = maxHealth * Battle.BURN_DAMAGE_PER_TICK_PERCENTAGE;
            _burningCoroutines.Add(applyDamageOverTime((int) strength, 1.0f, duration, attackerNetId, Attack.Type.Fire));
            break;
         case Status.Type.Poisoned:
            strength = currentHealth * Battle.POISON_DAMAGE_PER_TICK_PERCENTAGE;
            _burningCoroutines.Add(applyDamageOverTime((int) strength, 1.0f, duration, attackerNetId, Attack.Type.Poison));
            break;
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
      } else {
         D.debug("No status effect holder for this object: " + gameObject.name);
      }
   }

   public Coroutine applyDamageOverTime (int tickDamage, float tickInterval, float duration, uint attackerNetId, Attack.Type attackType) {
      // Applies a damage over time effect to this net entity, dealing 'tickDamage' damage every 'tickInterval' seconds, for 'duration' seconds
      return StartCoroutine(CO_DamageOverTime(tickDamage, tickInterval, duration, attackerNetId, attackType));
   }

   private IEnumerator CO_DamageOverTime (int tickDamage, float tickInterval, float duration, uint attackerNetId, Attack.Type attackType) {
      float totalTimer = 0.0f;
      float tickTimer = 0.0f;

      while (totalTimer <= duration) {
         totalTimer += Time.deltaTime;
         tickTimer += Time.deltaTime;

         // If enough time has passed for a damage tick, apply it
         if (tickTimer >= tickInterval) {
            switch (attackType) {
               case Attack.Type.Poison:
                  tickDamage = (int) (currentHealth * Battle.POISON_DAMAGE_PER_TICK_PERCENTAGE);
                  break;
            }
            int finalTickDamage = applyDamage(tickDamage, attackerNetId, attackType);

            Rpc_ShowDamage(attackType, transform.position, finalTickDamage);

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
   public void Rpc_PlayHitSfx (bool isShip, SeaMonsterEntity.Type seaMonsterType, bool isCrit, CannonballEffector.Type effectorType) {
      SoundEffectManager.self.playSeaEnemyHitSfx(isShip, seaMonsterType, isCrit, effectorType, this.gameObject);
   }

   protected virtual Vector2 getEntityAimPoint (SeaEntity entity) {
      // Returns the point at which we should aim when trying to hit an entity
      return entity.transform.position;
   }

   #region Enemy AI

   private bool useSeaEnemyAI () {
      return (this is SeaMonsterEntity || this is BotShipEntity);
   }

   [Server]
   private void setPath_Asynchronous (Path newPath) {
      _currentPath = newPath.vectorPath;
      _currentPathIndex = 1;
      _seeker.CancelCurrentPathRequest(true);
   }

   [Server]
   protected void checkEnemiesToAggro () {
      if (instanceId <= 0) {
         D.log("AI SeaEntity needs to be placed in an instance");
         return;
      }

      Instance instance = InstanceManager.self.getInstance(instanceId);
      if (instance == null) {
         return;
      }

      if (isSeaMonsterPvp() && _patrolingWaypointState == WaypointState.RETREAT) {
         return;
      }

      if (shouldIgnoreAttackers()) {
         return;
      }

      float degreesToDot = aggroConeDegrees / 90.0f;

      foreach (NetworkBehaviour iBehaviour in instance.getEntities()) {
         NetEntity iEntity = iBehaviour as NetEntity;

         // If the entity our ally, ignore it
         if (iEntity == null || !this.isEnemyOf(iEntity)) {
            continue;
         }

         if (isSeaMonsterPvp() && (iEntity is SeaStructure || iEntity is PvpCaptureTarget)) {
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

   protected virtual bool isInRange (Vector2 position, bool logData = false) {
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

   protected virtual SeaEntity getAttackerInRange (bool logData = false) {
      // Check if any of our attackers are within range
      foreach (uint attackerId in _attackers.Keys) {
         SeaEntity attacker = SeaManager.self.getEntity(attackerId);
         if (attacker == null || attacker.isDead()) {
            continue;
         }

         Vector2 attackerPosition = attacker.transform.position;
         if (!isInRange(attackerPosition, logData)) {
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
      // While after attack is cooling down, prevent this unit from facing direction
      if (attackCoolingDown() && this is SeaMonsterEntity) {
         return;
      }

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

            Vector3 moveAcceleration = direction.normalized * getMoveSpeed();
            movementForce = moveAcceleration * _body.mass;

            _body.AddForce(moveAcceleration);
            _lastMoveChangeTime = NetworkTime.time;
         }

         // If ship got too close to spawn point - change path
         if (!_isChasingEnemy && !_disableSpawnDistanceTmp && !isSeaMonsterPvp() && pvpTeam == PvpTeamType.None) {
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

   protected void forceStop () {
      _body.velocity = Vector3.zero;
      _body.angularVelocity = 0;
      _body.Sleep();
   }

   protected bool attackCoolingDown () {
      return NetworkTime.time - _lastAttackTime < AFTER_ATTACK_PAUSE;
   }

   [Server]
   private void checkForPathUpdate () {
      // While after attack is cooling down, stop path finding
      if (attackCoolingDown() && this is SeaMonsterEntity) {
         forceStop();
         return;
      }

      // Check if there are attackers to chase
      if (_attackers != null && _attackers.Count > 0) {

         // If we are a bot ship in a pvp game
         if (pvpTeam != PvpTeamType.None && isBotShip()) {
            uint currentAttackerNetId = _currentAttacker;
            SeaEntity currentAttacker = SeaManager.self.getEntity(currentAttackerNetId);

            if (currentAttacker != null) {
               BotShipEntity shipEntity = this as BotShipEntity;
               float currentAttackerDistance = (currentAttacker.transform.position - transform.position).magnitude;

               // If our current attacker is significantly out of attack range, or too far from its objective
               bool attackerIsTooFar = currentAttackerDistance >= shipEntity.getAttackRange() * 2.0f;
               bool tooFarFromObjective = isTooFarFromObjective();
               if (attackerIsTooFar || tooFarFromObjective) {

                  // Ignore enemies for a duration, and move back to current objective
                  activateLeash();
               }
            }
         }

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
            if (isSeaMonsterPvp()) {
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
         if (!_hasReachedLaneTarget) {
            updateState(ref _patrolingWaypointState, 0.0f, secondsPatrolingUntilChoosingNewTreasureSite,
               ref _currentSecondsBetweenPatrolRoutes, ref _currentSecondsPatroling, (x) => { return _pvpLaneTarget.position; });

            // If we've already reached the middle of the lane, look for the next target structure to attack
         } else {
            // Sea monsters should not engage buildings
            if (isSeaMonsterPvp()) {
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
         if (isSeaMonsterPvp()) {
            float distanceToSpawn = Vector2.Distance(transform.position, _lastSpawnPosition);
            bool inTerritory = (distanceFromInitialPosition < PVP_MONSTER_TERRITORY_RADIUS);
            _patrolingWaypointState = (inTerritory) ? WaypointState.PATROLING : WaypointState.RETREAT;
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
            if (isSeaMonsterPvp() && _seeker != null) {
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
                     if (!hasRegenerationBuff() && currentHealth < maxHealth && !hasAnyCombat()) {
                        setEnemyHealthRegeneration(true);
                     }
                  } else if (hasRegenerationBuff() && currentHealth >= maxHealth) {
                     setEnemyHealthRegeneration(false);
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

            if (isSeaMonsterPvp() && hasRegenerationBuff() && currentHealth >= maxHealth) {
               setEnemyHealthRegeneration(false);
            }

            break;
      }
   }

   [Server]
   private Vector3 findRandomVicinityPosition (bool placeholder) {
      Vector3 start = _body.transform.position;

      // If this unit is a sea monster pvp, find a target position around the spawn point
      if (isSeaMonsterPvp()) {
         float moveRadius = (_patrolingWaypointState == WaypointState.RETREAT) ? PVP_MONSTER_TERRITORY_RADIUS * 0.5f : PVP_MONSTER_TERRITORY_RADIUS;
         return findPositionAroundPosition(_originalPosition, moveRadius);
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
      if (pvpTeam != PvpTeamType.None && _pvpLaneTarget != null && !_hasReachedLaneTarget) {
         Vector2 toLaneCenter = _pvpLaneTarget.position - transform.position;
         if (toLaneCenter.sqrMagnitude < ARRIVE_DIST * ARRIVE_DIST) {
            _hasReachedLaneTarget = true;
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
      // Do not disable invulnerability if this is a master type monster that still has minions
      if (value == false && this is SeaMonsterEntity) {
         SeaMonsterEntity monsterEntity = (SeaMonsterEntity) this;
         if (monsterEntity.hasActiveMinions() && monsterEntity.seaMonsterData.roleType == RoleType.Master) {
            return;
         }
      }
      _isInvulnerable = value;
   }

   public bool getIsInvulnerable () {
      return _isInvulnerable;
   }

   private void OnDrawGizmosSelected () {
      if (Application.isPlaying) {
         Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 0.5f);
         Gizmos.DrawMesh(_editorConeAggroGizmoMesh, 0, transform.position, Quaternion.Euler(0.0f, 0.0f, -Vector2.SignedAngle(Util.getDirectionFromFacing(facing), Vector2.right)), Vector3.one);
         Gizmos.color = Color.white;

         Gizmos.color = Color.cyan;
         Gizmos.DrawLine(transform.position, transform.position + (Vector3) movementForce * 0.001f);
      }
   }

   public void setEnemyHealthRegeneration (bool isOn) {
      if (isSeaMonsterPvp()) {
         // Do not enable regeneration if this entity is already dead
         if (isOn && currentHealth < 1) {
            return;
         }
         if (!buffContent.ContainsKey(SeaBuff.Type.Heal)) {
            buffContent.Add(SeaBuff.Type.Heal, new SeaBuffData {
               buffType = SeaBuff.Type.Heal,
               casterId = 0,
               isActive = isOn
            });
         }
         buffContent[SeaBuff.Type.Heal].isActive = isOn;
      }
   }

   public bool isSeaMonsterPvp () {
      return this is SeaMonsterEntity && isPvpAI;
   }

   private bool isTooFarFromObjective () {
      // By creating a line between our previous and next target structures, we will detect if a ship is too far away from this 'objective line'

      // Ensure this is a pvp ship, and has target structures
      if (pvpTeam == PvpTeamType.None || _pvpTargetStructures == null || _pvpTargetStructures.Count <= 0) {
         return false;
      }

      float distanceFromObjectiveLineSquared;
      SeaStructure targetSeaStructure = getTargetSeaStructure();
      Vector3 targetPoint = (_hasReachedLaneTarget) ? targetSeaStructure.transform.position : _pvpLaneTarget.position;

      // If we are moving towards our lane target, we will use a line from our spawn position, to the lane target
      if (!_hasReachedLaneTarget) {
         Vector3 initialPositionWorld = transform.parent.TransformPoint(initialPosition);
         float distanceFromObjectiveLine = Util.distanceFromPointToLineSegment(transform.position, initialPositionWorld, targetPoint);
         distanceFromObjectiveLineSquared = distanceFromObjectiveLine * distanceFromObjectiveLine;

         // If we are moving towards the first sea structure we will use a line from our lane target to the structure
      } else if (targetSeaStructure == _pvpTargetStructures[0]) {
         float distanceFromObjectiveLine = Util.distanceFromPointToLineSegment(transform.position, _pvpLaneTarget.position, targetPoint);
         distanceFromObjectiveLineSquared = distanceFromObjectiveLine * distanceFromObjectiveLine;

         // If we are moving towards the final target structure, just calculate distance from it
      } else if (targetSeaStructure == _pvpTargetStructures[_pvpTargetStructures.Count - 1]) {
         distanceFromObjectiveLineSquared = (transform.position - targetPoint).sqrMagnitude;

         // Otherwise, use the previous and current objectives to make the objective line
      } else {
         int currentObjectiveIndex = _pvpTargetStructures.FindIndex((x) => x == targetSeaStructure);

         if (currentObjectiveIndex == -1) {
            return false;
         }
         SeaStructure previousTarget = _pvpTargetStructures[currentObjectiveIndex - 1];

         float distanceFromObjectiveLine = Util.distanceFromPointToLineSegment(transform.position, previousTarget.transform.position, _pvpLaneTarget.position);
         distanceFromObjectiveLineSquared = distanceFromObjectiveLine * distanceFromObjectiveLine;
      }

      bool tooFarFromObjective = distanceFromObjectiveLineSquared > LEASH_DISTANCE * LEASH_DISTANCE;
      return tooFarFromObjective;
   }

   private void activateLeash () {
      _ignoreEnemiesUntil = NetworkTime.time + LEASH_ENEMY_IGNORE_TIMER;
      _attackers.Clear();
      _currentAttacker = 0;
      _currentPath.Clear();
      _patrolingWaypointState = WaypointState.FINDING_PATH;
   }

   #endregion

   [Server]
   public void attachResidue (Attack.Type attackType, uint creatorNetId, int damagePerTick) {
      EffectResidue residue = attachResidueCommon(attackType, damagePerTick);
      if (residue == null) {
         return;
      }

      residue.creatorNetId = creatorNetId;

      Rpc_AttachResidue(attackType);
   }

   [ClientRpc]
   public void Rpc_AttachResidue (Attack.Type attackType) {
      if (isServer) {
         return;
      }

      attachResidueCommon(attackType, 0);
   }

   public EffectResidue attachResidueCommon (Attack.Type attackType, int damagePerTick) {
      EffectResidue residue = null;

      // Check if this residue type is already affecting this entity
      foreach (EffectResidue activeResidue in _activeResidueList) {
         if (activeResidue.attackType == attackType) {
            residue = activeResidue;
            break;
         }
      }

      if (residue == null) {
         // If the entity is not currently being affected by this residue type, instantiate a new one
         switch (attackType) {
            case Attack.Type.Venom:
               residue = Instantiate(PrefabsManager.self.venomStickyPrefab, residueHolder);
               residue.transform.localPosition = Vector3.zero;
               _activeResidueList.Add(residue);
               break;
            default:
               D.error($"The effect residue for the attack type {attackType} is not defined!");
               break;
         }
      }

      residue.damagePerTick = damagePerTick;
      residue.seaEntity = this;
      residue.restart();

      return residue;
   }

   public void removeResidue (EffectResidue residue) {
      _activeResidueList.Remove(residue);
   }

   protected bool shouldIgnoreAttackers () {
      return (_ignoreEnemiesUntil > NetworkTime.time);
   }

   [Server]
   public void addBuff (uint buffSourceNetId, SeaBuff.Category buffCategory, SeaBuff.Type buffType, float buffMagnitude, float buffDuration, int abilityXmlId) {
      double buffStartTime = NetworkTime.time;
      double buffEndTime = buffStartTime + buffDuration;
      SyncList<SeaBuffData> buffList = getBuffList(buffCategory);
      if (buffList != null) {
         buffList.Add(new SeaBuffData(buffStartTime, buffEndTime, buffType, buffMagnitude, abilityXmlId, buffSourceNetId));
      }
   }

   [Server]
   public void addBuff (uint buffSourceNetId, SeaBuff.Category buffCategory, SeaBuff.Type buffType, ShipAbilityData shipAbilityData, double newEndTime = -1) {
      double buffStartTime = NetworkTime.time;
      double buffEndTime = newEndTime > 0 ? newEndTime : buffStartTime + shipAbilityData.statusDuration;

      SyncList<SeaBuffData> buffList = getBuffList(buffCategory);
      if (buffList != null) {
         buffList.Add(new SeaBuffData(buffStartTime, buffEndTime, buffType, shipAbilityData.damageModifier, shipAbilityData.abilityId, buffSourceNetId));
      }

      Rpc_ShowReceivedAbilityBuff(buffSourceNetId, shipAbilityData);
   }

   [ClientRpc]
   protected void Rpc_TriggerHealEffect (bool isEnable) {
      showHealEffect(isEnable);
   }


   [ClientRpc]
   protected void Rpc_TriggerHealSfx (bool isPlay) {
      triggerHealSfx(isPlay);
   }

   [ClientRpc]
   public void Rpc_ShowReceivedAbilityBuff (uint buffSourceNetId, ShipAbilityData shipAbilityData) {
      // Don't show an icon for buffs received from self
      if (this.netId == buffSourceNetId) {
         return;
      }

      EffectManager.createBuffEffect(shipAbilityData.skillIconPath, new Vector2(0.0f, 0.025f), transform, true);
   }

   protected virtual void showHealEffect (bool isEnable) {
      // Override this method to implement heal effect on inheriting class
   }

   protected virtual void triggerHealSfx (bool isPlay) {
      // Override this method to implement heal sound effect on inheriting class
   }

   public float getBuffValue (SeaBuff.Category buffCategory, SeaBuff.Type buffType) {
      // Returns the magnitude of the powerup of the specified type and category, with the highest value.
      SyncList<SeaBuffData> buffList = getBuffList(buffCategory);
      if (buffList != null) {
         if (buffList.Count > 0) {
            return buffList.Max((x) => x.buffMagnitude);
         }
      }

      return 0.0f;
   }

   [Server]
   public bool hasBuffDataWithType (SeaBuff.Category buffCategory, SeaBuff.Type buffType) {
      // Get all existing buff data of player with buff category
      SyncList<SeaBuffData> buffList = getBuffList(buffCategory);
      if (buffList != null) {
         return buffList.Any(_ => _.buffType == buffType);
      }

      return false;
   }

   [Server]
   public List<SeaBuffData> getAllBuffDataWithType (SeaBuff.Category buffCategory, SeaBuff.Type buffType) {
      // Get all existing buff data of player with buff category
      SyncList<SeaBuffData> buffList = getBuffList(buffCategory);
      if (buffList != null) {
         if (buffList.Any(_ => _.buffType == buffType)) {
            return buffList.FindAll(_ => _.buffType == buffType);
         }
      }

      return null;
   }

   [Server]
   public SeaBuffData getBuffData (SeaBuff.Category buffCategory, SeaBuff.Type buffType) {
      // Get all existing buff data of player with buff category
      SyncList<SeaBuffData> buffList = getBuffList(buffCategory);
      if (buffList != null) {
         if (buffList.Any(_ => _.buffType == buffType)) {
            return buffList.FindAll(_ => _.buffType == buffType)[0];
         }
      }

      return null;
   }

   public bool hasRegenerationBuff () {
      if (buffContent.ContainsKey(SeaBuff.Type.Heal)) {
         return buffContent[SeaBuff.Type.Heal].isActive;
      }

      return false;
   }

   [Server]
   protected void updateBuffs () {
      updateCategoryBuffs(SeaBuff.Category.Buff);
      updateCategoryBuffs(SeaBuff.Category.Debuff);
   }

   protected void updateCategoryBuffs (SeaBuff.Category category) {
      SyncList<SeaBuffData> categoryBuffs = getBuffList(category);

      // Check if the list has any valid buff data, if not, continue
      if (categoryBuffs == null || categoryBuffs.Count <= 0) {
         return;
      }

      // Check if any buffs have expired
      for (int i = categoryBuffs.Count - 1; i >= 0; i--) {
         SeaBuffData buffData = categoryBuffs[i];

         // If this buff has expired, remove it from the list
         if (NetworkTime.time > buffData.buffEndTime) {
            categoryBuffs.Remove(buffData);
            // Check if expired buff data is a heal
            if (buffData.buffType == SeaBuff.Type.Heal) {
               // Disable heal effect and sfx if no existing heal buff data left
               if (!hasBuffDataWithType(Category.Buff, SeaBuff.Type.Heal)) {
                  Rpc_TriggerHealEffect(false);
                  Rpc_TriggerHealSfx(false);
               }
            }
         }
      }
   }

   #region Buff Orbs

   protected void updateBuffOrbs () {
      if (Util.isBatch()) {
         return;
      }

      _buffOrbRotation += Time.deltaTime * BUFF_ORB_ROTATION_SPEED;

      float orbSpacing = 1.0f / _buffOrbs.Count;

      for (int i = 0; i < _buffOrbs.Count; i++) {
         BuffOrb orb = _buffOrbs[i];
         float targetValue = _buffOrbRotation + orbSpacing * i;
         float newValue = Mathf.SmoothStep(orb.rotationValue, targetValue, Time.deltaTime * 10.0f);
         orb.rotationValue = newValue;

         Util.setLocalXY(orb.transform, Util.getPointOnEllipse(BUFF_ORB_ELLIPSE_WIDTH, BUFF_ORB_ELLIPSE_HEIGHT, newValue));
      }
   }

   protected void checkBuffOrbs () {
      // Store the latest powerups from the server in a list
      List<SeaBuffData> serverBuffs = new List<SeaBuffData>();
      List<SeaBuff.Type> buffTypes = new List<SeaBuff.Type>();

      SyncList<SeaBuffData> buffs = getBuffList(SeaBuff.Category.Buff);
      if (buffs == null) {
         return;
      }

      foreach (SeaBuffData buffData in buffs) {
         // Exclude heal from buffs where orbs will spawn
         if (buffData.buffType == SeaBuff.Type.Heal) {
            continue;
         }

         // Only count one buff of each type
         if (!buffTypes.Contains(buffData.buffType)) {
            serverBuffs.Add(buffData);
            buffTypes.Add(buffData.buffType);
         }
      }

      int orbsCreated = 0;

      // If the list from the server is larger, we need to create new orbs
      if (serverBuffs.Count > _buffOrbs.Count) {
         orbsCreated = serverBuffs.Count - _buffOrbs.Count;

         // Create any new orbs needed
         for (int i = 0; i < orbsCreated; i++) {
            BuffOrb newOrb = Instantiate(PrefabsManager.self.buffOrbPrefab, transform.position + Vector3.up * BUFF_ORB_ELLIPSE_HEIGHT, Quaternion.identity, transform);
            _buffOrbs.Add(newOrb);
            newOrb.rotationValue = _buffOrbRotation + (1.0f / _buffOrbs.Count) * (_buffOrbs.Count - 1);
         }

         // If the list from the server is smaller, we need to remove orbs
      } else if (serverBuffs.Count < _buffOrbs.Count) {
         int numOrbsToRemove = _buffOrbs.Count - serverBuffs.Count;

         // Remove any orbs that aren't needed
         for (int i = 0; i < numOrbsToRemove; i++) {
            BuffOrb orbToRemove = _buffOrbs[_buffOrbs.Count - 1];
            _buffOrbs.Remove(orbToRemove);
            Destroy(orbToRemove.gameObject);
         }
      }

      // Update types of orbs
      for (int i = 0; i < _buffOrbs.Count; i++) {
         bool isNewOrb = (i >= _buffOrbs.Count - orbsCreated);
         Attack.Type attackType = SeaBuffData.getAttackType(buffs[i].buffType);
         _buffOrbs[i].init(this.netId, attackType, orbHolder, this.netId, true, isNewOrb);
      }

      bool particlesEnabled = _buffOrbs.Count <= MAX_BUFF_ORBS_WITH_PARTICLES;
      foreach (BuffOrb orb in _buffOrbs) {
         orb.setParticleVisibility(particlesEnabled);
      }
   }

   [ClientRpc]
   protected void Rpc_ShowBuffAlly (uint targetNetId, Attack.Type attackType) {
      BuffOrb newOrb = Instantiate(PrefabsManager.self.buffOrbPrefab, transform.position, Quaternion.identity, orbHolder);
      newOrb.init(this.netId, attackType, orbHolder, targetNetId, false, true);
   }

   [ClientRpc]
   protected void Rpc_RemoveBuffAlly (uint targetNetId, Attack.Type attackType) {
      BuffOrb newOrb = Instantiate(PrefabsManager.self.buffOrbPrefab, transform.position, Quaternion.identity, orbHolder);
      newOrb.init(this.netId, attackType, orbHolder, targetNetId, false, true);
   }

   protected void removeAllBuffOrbs () {
      foreach (BuffOrb orb in _buffOrbs) {
         Destroy(orb.gameObject);
      }
      _buffOrbs.Clear();
   }

   protected SyncList<SeaBuffData> getBuffList (SeaBuff.Category category) {
      // Returns the list of buff type to be altered by the fetcher
      switch (category) {
         case SeaBuff.Category.Buff:
            return _buffs;
         case SeaBuff.Category.Debuff:
            return _debuffs;
         default:
            return null;
      }
   }

   #endregion

   public void setOpenWorldData (Instance instance, Area area, Vector2 localPosition, bool isPositionRandomized, bool useWorldPosition, int guildId, GroupInstance.Difficulty difficulty, bool isOpenWorldSpawn, float respawnTime) {
      openWorldRespawnData = new EnemyManager.OpenWorldRespawnData {
         instance = instance,
         area = area,
         localPosition = localPosition,
         isPositionRandomized = isPositionRandomized,
         useWorldPosition = useWorldPosition,
         guildId = guildId,
         difficulty = difficulty,
         isOpenWorldSpawn = isOpenWorldSpawn,
         respawnTime = respawnTime
      };
   }

   #region Private Variables

   // The data needed for respawning in open world
   private EnemyManager.OpenWorldRespawnData openWorldRespawnData = null;

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
   protected Seeker _seeker = default;

   // The current path to the destination
   [SerializeField]
   protected List<Vector3> _currentPath = new List<Vector3>();

   // The current Point Index of the path
   [SerializeField]
   protected int _currentPathIndex = default;

   // In case there are no TreasureSites to pursue, use this to patrol the vicinity
   private Vector3 _originalPosition = default;

   // The generated mesh for showing the cone of aggro in the Editor
   private Mesh _editorConeAggroGizmoMesh = default;

   // Spawn points placed in the area that sea enemies should avoid
   private List<Vector3> _playerSpawnPoints = new List<Vector3>();

   // Const values to calculate distance to spawn points
   private static float MIN_DISTANCE_TO_SPAWN_PERCENT = 0.3f;
   private static float MIN_DISTANCE_TO_SPAWN_PATH_PERCENT = 0.4f;

   // Distance values that bot ship should keep from the spawn points, calculated for current area
   private float _minDistanceToSpawn = default;
   private float _minDistanceToSpawnPath = default;

   // Are we currently chasing an enemy
   [SerializeField]
   private bool _isChasingEnemy = default;

   // The flag which temporarily disables avoiding spawn points
   private bool _disableSpawnDistanceTmp = false;

   // The last spawn point that bot ship was nearby and had to change its path
   private Vector3 _lastSpawnPosition = Vector3.zero;

   // The time at which we started chasing an enemy, on this path
   private double _chaseStartTime = 0.0f;

   // How many seconds have passed since we last stopped on an attack route
   [SerializeField]
   private float _currentSecondsBetweenAttackRoutes = default;

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
   private uint _currentAttacker = default;

   // How many seconds have passed since we last stopped on a patrol route
   [SerializeField]
   private float _currentSecondsBetweenPatrolRoutes = default;

   // How many seconds have passed since we've started patrolling the current TreasureSite
   [SerializeField]
   private float _currentSecondsPatroling = default;

   // If in a pvp game, this is the list of enemy structures we are trying to destroy, in priority order
   private List<SeaStructure> _pvpTargetStructures = new List<SeaStructure>();

   // If in a pvp game, this is a target point in the middle of the lane, used to help the units follow the lane
   private Transform _pvpLaneTarget = default;

   // When set to true, this sea entity won't take any damage
   [SyncVar]
   private bool _isInvulnerable = false;

   // Gets set to true when the 'defeatship' tutorial has been triggered
   protected bool _isDefeatShipTutorialTriggered = false;

   // The damage amount each attacker has done on this entity
   protected Dictionary<int, int> _damageReceivedPerAttacker = new Dictionary<int, int>();

   // A list of references to any active ability orbs, used to visually indicate what ability this entity has
   protected List<BuffOrb> _abilityOrbs = new List<BuffOrb>();

   // A value that controls the rotation of the powerup orbs as it is incremented (0.0f - 1.0f is one rotation)
   protected float _buffOrbRotation = 0.0f;

   // The width of the ellipse around which powerup orbs move
   private const float BUFF_ORB_ELLIPSE_WIDTH = 0.375f;

   // The height of the ellipse around which powerup orbs move
   private const float BUFF_ORB_ELLIPSE_HEIGHT = 0.1875f;

   // A modifier affecting how fast the powerup orbs will rotate
   private const float BUFF_ORB_ROTATION_SPEED = 0.5f;

   // The max number of orbs that can have particles at once. If there are more orbs than this number, all orbs will have their particles disabled.
   private const int MAX_BUFF_ORBS_WITH_PARTICLES = 6;

   // The powerups that this sea entity currently has
   protected SyncList<Powerup> _powerups = new SyncList<Powerup>();

   // The ability that this sea entity currently has
   protected SyncList<BuffOrbData> _abilityOrbData = new SyncList<BuffOrbData>();

   // The list of residues currently affecting this entity
   protected List<EffectResidue> _activeResidueList = new List<EffectResidue>();

   // A timestamp for how long this entity will be ignoring enemies for
   private double _ignoreEnemiesUntil = 0.0;

   // After chasing an enemy too far in a pvp game, this sea entity will ignore enemies for this amount of time, as it heads back to its objective
   private const double LEASH_ENEMY_IGNORE_TIMER = 2.0;

   // If a sea entity travels this distance away from its allowed area, it will be 'leashed' back to its current objective, ignoring enemies for a short time.
   private const float LEASH_DISTANCE = 2.0f;

   // Set to true once this entity reaches its lane target, causing it to move on and target sea structures
   private bool _hasReachedLaneTarget = false;

   // All buffs currently active on this sea entity
   protected SyncList<SeaBuffData> _buffs = new SyncList<SeaBuffData>();

   // All debuffs currently active on this sea entity
   protected SyncList<SeaBuffData> _debuffs = new SyncList<SeaBuffData>();

   // A list of references to any active buff orbs, used to visually indicate any buffs being applied to this entity
   protected List<BuffOrb> _buffOrbs = new List<BuffOrb>();

   #endregion

   #endregion
}