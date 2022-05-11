using System.Collections;
using System.Collections.Generic;
using MapCreationTool.Serialization;
using Mirror;
using UnityEngine;
using Pathfinding;
using UnityEngine.Events;
using System.Linq;

public class SeaMonsterEntity : SeaEntity, IMapEditorDataReceiver
{
   #region Public Variables

   public enum Type {
      None = 0, Tentacle = 1, Horror = 2, Worm = 3, Reef_Giant = 4, Fishman = 5, SeaSerpent = 6, Horror_Tentacle = 7, PirateShip = 8, SeaSerpentMinions = 9
   }

   // Holds the trigger containing the death effect
   public GameObject deathBubbleEffect;

   public static bool isSeaMonster (int subVarietyId) {
      return subVarietyId < 1;
   }

   // List of children dependencies
   public List<SeaMonsterEntity> seaMonsterChildrenList = new List<SeaMonsterEntity>();

   // The parent entity of this sea monster
   public SeaMonsterEntity seaMonsterParentEntity;

   // Current target entity
   public NetEntity targetEntity = null;

   // The Name of the NPC Seamonster
   [SyncVar]
   public string monsterName;

   // The unique data for each seamonster
   public SeaMonsterEntityData seaMonsterData;

   // The minimum magnitude to determine the movement of the unit
   public const float MIN_MOVEMENT_MAGNITUDE = .05f;

   // Determines if this unit should play the attack sprite sheet
   public bool isAttacking = false;

   // Determines the variety of the monster sprite if there is one
   [SyncVar]
   public int variety = 0;

   // Determines the monster type index of this unit
   [SyncVar]
   public SeaMonsterEntity.Type monsterType = 0;

   // The instance difficulty
   [SyncVar]
   public int difficulty = 1;

   // Determines the location of this unit in relation to its spawn point
   public Vector2 directionFromSpawnPoint;

   // Multiplies the direction from the spawn point to get the relative position
   public float distanceFromSpawnPoint = 1f;

   // Holds the info of the seamonster health Bars
   public SeaMonsterBars seaMonsterBars;

   // List of participants in this boss fight
   public List<int> bossCombatParticipants = new List<int>();

   // The time an attack animation plays
   // TODO: Confirm if this needs to be in the web tool, this variable will result in the enemy holding its last animation frame after attack before moving again
   public const float ATTACK_DURATION = .65f;// Old Value = .3f;

   // Determines if a minion is planning its own behavior
   public bool isMinionPlanning = false;

   // The limit of the overlap collider check to avoid too much checking
   public const int MAX_COLLISION_COUNT = 40;

   // Holds the corpse object
   public GameObject corpseHolder;

   // Determines if this unit is logging info
   public bool isLoggingData;

   // Gets set to true when the entity doesn't plan any move (or attack)
   public bool isStationary = false;

   // The number of chests dropped by this monster when defeated
   public int chestDropCount = 1;

   // The animation speed cached from the xml database
   public float cachedAnimSpeed = .25f;

   // The attack animation speed cached from the xml database
   public float cachedAttackAnimSpeed = .35f;

   // The base damage of the seamonster
   public const int BASE_SEAMONSTER_DAMAGE = 25;

   // TODO: Setup sea monster web tool to have variable that can be edited so these dont have to be hard coded
   // Sort points that will be adjusted depending if the seamonster is a standalone or a boss
   public const float MINION_SORT_POINT = -0.132f;
   public const float BOSS_SORT_POINT = -0.218f;

   // The probability that the tentacle monster will attack targets in range, apart from its main target
   public const float TENTACLE_EXTRA_TARGET_CHANCE = 0.3f;

   // The number of seconds between tentacle secondary attacks without considering the difficulty
   public const float TENTACLE_SECONDARY_ATTACK_BASE_INTERVAL = 3f;

   // The monsters selected ability, used for enemies with multiple ability
   public int selectedAbilityId;

   // The next available time a major ability can be used
   public double nextMajorSkillUseTime;

   // Even triggered on death
   [HideInInspector]
   public UnityEvent hasDiedEvent = new UnityEvent();

   // Seamonster Animation
   public enum SeaMonsterAnimState {
      Idle,
      Attack,
      EndAttack,
      Die,
      Move,
      MoveStop
   }

   // Determines the current behavior of the Monster
   public enum MonsterBehavior {
      Idle = 0,
      MoveToPosition = 1,
      MoveToAttackPosition = 2,
      Attack = 3,
   }

   #endregion

   #region Unity Lifecycle

   private void initData (SeaMonsterEntityData entityData) {
      seaMonsterData = entityData;

      _simpleAnim = spritesContainer.GetComponent<SimpleAnimation>();
      _simpleAnimRipple = ripplesContainer.GetComponent<SimpleAnimation>();

      if (!Util.isBatch()) {
         ripplesContainer.GetComponent<SpriteRenderer>().sprite = ImageManager.getSprite(seaMonsterData.defaultRippleSpritePath);

         Sprite rippleTextureSprite = ImageManager.getSprite(seaMonsterData.defaultRippleTexturePath);
         Texture2D croppedTexture = new Texture2D((int) rippleTextureSprite.rect.width, (int) rippleTextureSprite.rect.height);
         Color[] pixels = rippleTextureSprite.texture.GetPixels((int) rippleTextureSprite.textureRect.x,
                                                   (int) rippleTextureSprite.textureRect.y,
                                                   (int) rippleTextureSprite.textureRect.width,
                                                   (int) rippleTextureSprite.textureRect.height);
         croppedTexture.SetPixels(pixels);
         ripplesContainer.GetComponent<SpriteSwap>().newTexture = rippleTextureSprite == null ? ImageManager.self.blankTexture : croppedTexture;
         ripplesContainer.transform.localPosition += seaMonsterData.rippleLocOffset;
         ripplesContainer.GetComponent<FixedZ>().newZ += seaMonsterData.rippleLocOffset.z;
         deathBubbleEffect.transform.localPosition += seaMonsterData.rippleLocOffset;

         if (seaEntityShadowContainer != null) {
            seaEntityShadowContainer.SetActive(monsterType == Type.Horror);
         }

         // Scale Update
         ripplesContainer.transform.localScale = new Vector3(seaMonsterData.rippleScaleOverride, seaMonsterData.rippleScaleOverride, seaMonsterData.rippleScaleOverride);
         spritesContainer.transform.localScale = new Vector3(seaMonsterData.scaleOverride, seaMonsterData.scaleOverride, seaMonsterData.scaleOverride);
         spritesContainer.transform.GetChild(0).localScale = new Vector3(seaMonsterData.outlineScaleOverride, seaMonsterData.outlineScaleOverride, seaMonsterData.outlineScaleOverride);

         // Update animation of the sea monster ripple sprite
         _simpleAnimRipple.group = seaMonsterData.animGroup;
         _simpleAnimRipple.frameLengthOverride = seaMonsterData.rippleAnimationSpeedOverride;
         _simpleAnimRipple.enabled = true;

         // Cached animation speed
         cachedAnimSpeed = seaMonsterData.animationSpeedOverride;
         cachedAttackAnimSpeed = seaMonsterData.attackAnimationSpeed;

         D.adminLog("SeaMonster: " + monsterType +
            " AnimSpdNew: " + cachedAnimSpeed +
            " AnimSpd: " + seaMonsterData.attackAnimationSpeed +
            " AtknimSpd: " + cachedAttackAnimSpeed, D.ADMIN_LOG_TYPE.Sea);

         // Update animation of the sea monster sprite
         _simpleAnim.group = seaMonsterData.animGroup;
         _simpleAnim.frameLengthOverride = cachedAnimSpeed;

         _simpleAnim.enabled = true;

         gameObject.name = "SeaMonster_" + monsterType;

         // Alter the sort point if this is a large boss monster
         if (monsterType == Type.Horror) {
            sortPoint.transform.localPosition = new Vector3(sortPoint.transform.localPosition.x, BOSS_SORT_POINT, sortPoint.transform.localPosition.z);
         }
      }

      if (seaMonsterData.roleType == RoleType.Minion) {
         sinkOnDeath = false;
      }

      // Update the collider scale and offset
      if (getCombatCollider() != null) {
         getCombatCollider().setScale(new Vector3(seaMonsterData.battleColliderScaleX, seaMonsterData.battleColliderScaleY, 1));
      }

      if (isServer) {
         // Get the instance difficulty
         Instance instance = getInstance();
         if (instance != null) {
            difficulty = instance.difficulty;
         }

         float reloadModifier = 1 + (((float) difficulty - 1) / (Voyage.getMaxDifficulty() - 1));
         reloadDelay = seaMonsterData.reloadDelay / (difficulty > 0 ? reloadModifier : 1);
         reloadDelay *= AdminGameSettingsManager.self.settings.seaAttackCooldown;
         maxHealth = Mathf.RoundToInt(seaMonsterData.maxHealth * difficulty * AdminGameSettingsManager.self.settings.seaMaxHealth);
         currentHealth = maxHealth;
         setIsInvulnerable(seaMonsterData.isInvulnerable);

         foreach (int newSkillId in seaMonsterData.skillIdList) {
            ShipAbilityData newAbility = ShipAbilityManager.self.getAbility(newSkillId);
            if (newAbility != null) {
               _abilityCooldownTracker.Add(newSkillId, newAbility.coolDown);
            }
         }
      }

      // Seamonster aggro for pvp is radius based instead of facing direction based
      if (isPvpAI) {
         aggroConeDegrees = 360f;
         seaMonsterBars.initializeHealthBar();
      }

      if (seaMonsterData.projectileSpawnLocations.Count > 0) {
         foreach (DirectionalPositions directionalPos in seaMonsterData.projectileSpawnLocations) {
            projectileSpawnLocations.Find(_ => _.direction == directionalPos.direction).spawnTransform.localPosition = directionalPos.spawnTransform;
         }
      }

      if (seaMonsterData.isInvulnerable) {
         seaMonsterBars.gameObject.SetActive(false);
         spritesContainer.transform.GetChild(0).gameObject.SetActive(false);
      }

      if (!Util.isBatch()) {
         if (variety != 0 && seaMonsterData.secondarySpritePath != null) {
            spritesContainer.GetComponent<SpriteRenderer>().sprite = ImageManager.getSprite(seaMonsterData.secondarySpritePath);
         } else {
            spritesContainer.GetComponent<SpriteRenderer>().sprite = ImageManager.getSprite(seaMonsterData.defaultSpritePath);
         }
      }

      // AI parameters
      switch (seaMonsterData.seaMonsterType) {
         case Type.Horror:
            newWaypointsRadius = seaMonsterData.territoryRadius;
            aggroConeDegrees = 360f;
            editorGenerateAggroCone();
            break;
         case Type.Horror_Tentacle:
            aggroConeDegrees = 360f;
            editorGenerateAggroCone();
            break;
      }
   }

   protected override void Start () {
      base.Start();

      deathBubbleEffect.SetActive(false);

      // Initializes the data from the scriptable object
      SeaMonsterEntityData monsterData = SeaMonsterManager.self.seaMonsterDataList.Find(_ => _.seaMonsterType == monsterType);

      if (monsterData == null) {
         D.debug("Sea Monster data is null for: " + monsterType);
         Destroy(gameObject);
         return;
      }

      initData(monsterData);

      // Note our spawn position
      _spawnPos = sortPoint.transform.position;

      playAnimation(Anim.Type.Idle_North);

      if (!isServer) {
         return;
      }

      // Custom behaviors
      if (monsterType == Type.Horror) {
         InvokeRepeating(nameof(commandMinionTentaclesToUseSecondaryAttack), 0, TENTACLE_SECONDARY_ATTACK_BASE_INTERVAL + (Voyage.getMaxDifficulty() / difficulty));
      }
   }

   protected override void Update () {
      base.Update();

      // If we're dead and have finished sinking, remove us
      if (isServer && isDead() && spritesContainer.transform.localPosition.y < -.25f) {
         InstanceManager.self.removeEntityFromInstance(this);

         // Destroy the object
         NetworkServer.Destroy(this.gameObject);
      }

      // Alters the simple animation data
      handleAnimations();

      // Handles attack animations
      if (NetworkTime.time > _attackStartAnimateTime && !_hasAttackAnimTriggered) {
         _simpleAnim.stayAtLastFrame = true;
         _simpleAnimRipple.stayAtLastFrame = true;
         isAttacking = true;
         _hasAttackAnimTriggered = true;
         _attackEndAnimateTime = NetworkTime.time + getAttackDuration();
         forceStop();
      } else {
         if (isAttacking && (NetworkTime.time > _attackEndAnimateTime)) {
            modifyAnimationSpeed(cachedAnimSpeed);
            _attackStartAnimateTime = NetworkTime.time + 50;
            isAttacking = false;
            _simpleAnim.stayAtLastFrame = false;
            _simpleAnimRipple.stayAtLastFrame = false;
            isMinionPlanning = false;
         }
      }

      if (!isServer) {
         return;
      }

      // Sets up where this unit is facing
      if (seaMonsterData.roleType != RoleType.Minion) {
         if (targetEntity != null && _currentPathIndex >= _currentPath.Count) {
            // Look at target entity
            this.facing = (Direction) SeaMonsterUtility.getDirectionToFace(targetEntity, sortPoint.transform.position);
         }
      } else {
         // Forces minions to look at direction in relation to the parent
         if (directionFromSpawnPoint.x < 0) {
            facing = Direction.West;
         } else if (directionFromSpawnPoint.x == 0) {
            facing = directionFromSpawnPoint.y == 1 ? Direction.East : Direction.West;
         } else {
            facing = Direction.East;
         }
      }
   }

   protected override void FixedUpdate () {
      base.FixedUpdate();

      // Only the server updates waypoints and movement forces
      if (!isServer || (isDead() && seaMonsterData.roleType != RoleType.Minion)) {
         return;
      }

      // Only change our movement if enough time has passed
      if (NetworkTime.time - _lastMoveChangeTime < (seaMonsterData.roleType != RoleType.Minion ? MOVE_CHANGE_INTERVAL : MOVE_CHANGE_INTERVAL / 2)) {
         return;
      }

      if (isStationary) {
         return;
      }

      if (freezeMovement) {
         return;
      }

      // If this entity is a Minion, snap to its parent
      if (seaMonsterData.roleType == RoleType.Minion && seaMonsterParentEntity != null) {
         Vector2 targetLocation = SeaMonsterUtility.getFixedPositionAroundPosition(seaMonsterParentEntity.sortPoint.transform.position, directionFromSpawnPoint, distanceFromSpawnPoint);
         Vector2 waypointDirection = (targetLocation - (Vector2) sortPoint.transform.position).normalized;

         // Teleports the Minions if too far away from Parent
         if (Vector2.Distance(sortPoint.transform.position, seaMonsterParentEntity.transform.position) > 2) {
            _body.MovePosition(targetLocation);
         }

         if (Vector2.Distance(targetLocation, sortPoint.transform.position) > .05f) {
            _body.AddForce(waypointDirection.normalized * (getMoveSpeed() / 1.75f));
         }
         return;
      }

      if (_currentPathIndex < _currentPath.Count) {
         // Move towards our current waypoint
         Vector2 waypointDirection = ((Vector2) _currentPath[_currentPathIndex] - (Vector2) sortPoint.transform.position).normalized;

         _body.AddForce(waypointDirection * getMoveSpeed());
         _lastMoveChangeTime = NetworkTime.time;

         // Clears a node as the unit passes by
         float sqrDistanceToWaypoint = Vector2.SqrMagnitude(_currentPath[_currentPathIndex] - sortPoint.transform.position);
         if (sqrDistanceToWaypoint < .01f) {
            ++_currentPathIndex;
         }
      }
   }

   #endregion

   private void resetMeleeParameters () {
      _isPerformingAttack = false;
   }

   private void resurface () {
      isSubmerging = false;
      StartCoroutine(CO_Resurface());
      if (targetEntity != null) {
         Vector3 targetPOt = targetEntity.transform.position;
         transform.position = (targetPOt);

         Destroy(EffectManager.self.create(Effect.Type.Crop_Harvest, targetPOt).gameObject, 2);
         Destroy(EffectManager.self.create(Effect.Type.Crop_Dirt_Large, targetPOt).gameObject, 2);
      }

      Invoke(nameof(resetDefaultAIBehavior), 1);
   }

   private void resetDefaultAIBehavior () {
      freezeMovement = false;
      _isPerformingAttack = false;
      setIsInvulnerable(false);

      Rpc_ToggleSprites(true);
      Rpc_ToggleColliders(true);
   }

   private IEnumerator CO_Resurface () {
      yield return new WaitForSeconds(0.2f);
      if (!isSubmerging) {
         while (spritesContainer.transform.localPosition.y < 0) {
            float newYValue = spritesContainer.transform.localPosition.y + (Time.deltaTime * SUBMERGE_SPEED);
            spritesContainer.transform.localPosition = new Vector2(spritesContainer.transform.localPosition.x, newYValue);
         }
      }
   }

   private IEnumerator CO_Submerge () {
      yield return new WaitForSeconds(0.2f);
      if (isSubmerging) {
         float targetHeight = -.5f;
         while (spritesContainer.transform.localPosition.y > targetHeight) {
            float newYValue = spritesContainer.transform.localPosition.y - (Time.deltaTime * SUBMERGE_SPEED);
            spritesContainer.transform.localPosition = new Vector2(spritesContainer.transform.localPosition.x, newYValue);
         }

         Rpc_ToggleSprites(false);
         Rpc_ToggleColliders(false);
         spritesContainer.transform.localPosition = new Vector2(spritesContainer.transform.localPosition.x, targetHeight);
         isSubmerging = false;      
      }
   }

   #region External Entity Related Functions

   public override void requestAnimationPlay (Anim.Type animType) {
      isAttacking = true;
      _attackEndAnimateTime = NetworkTime.time + getAttackDuration();
      if (_simpleAnim != null) {
         modifyAnimationSpeed(cachedAttackAnimSpeed);
         playAnimation(animType);
      }
   }

   private float getAttackDuration () {
      // Implement attack duration altering here
      return ATTACK_DURATION;
   }

   public override void noteAttacker (uint netId) {
      base.noteAttacker(netId);
      if (seaMonsterParentEntity != null) {
         seaMonsterParentEntity.noteAttacker(netId);

         // Register participants in boss fight for reward purposes
         if (monsterType == Type.Horror || seaMonsterParentEntity.monsterType == Type.Horror) {
            NetEntity currEntity = MyNetworkManager.fetchEntityFromNetId<NetEntity>(netId);
            if (currEntity != null) {
               if (!bossCombatParticipants.Contains(currEntity.userId)) {
                  bossCombatParticipants.Add(currEntity.userId);
               }
               if (!seaMonsterParentEntity.bossCombatParticipants.Contains(currEntity.userId)) {
                  seaMonsterParentEntity.bossCombatParticipants.Add(currEntity.userId);
               }
            }
         }
      }
   }

   protected void scanTargetsInArea () {
      if (isDead() || !isServer || !seaMonsterData.isAggressive) {
         return;
      }

      int hitCount = Physics2D.OverlapCircleNonAlloc(sortPoint.transform.position, seaMonsterData.detectRadius, _hits,
         LayerMask.GetMask(LayerUtil.SHIPS));

      for (int i = 0; i < hitCount; i++) {
         if (_hits[i] == null) {
            continue;
         }

         ShipEntity ship = _hits[i].GetComponent<ShipEntity>();
         if (ship != null && ship.instanceId == instanceId && !_attackers.ContainsKey(ship.netId) &&
            !ship.isDead()) {
            noteAttacker(ship);
            Rpc_NoteAttacker(ship.netId);
         }
      }
   }

   protected NetEntity getNearestTarget () {
      NetEntity nearestEntity = null;
      float oldDistanceGap = 100;

      foreach (uint attackerId in _attackers.Keys) {
         NetEntity attacker = MyNetworkManager.fetchEntityFromNetId<NetEntity>(attackerId);
         if (attacker == null || attacker == this || attacker.isDead() || (attacker is SeaMonsterEntity)) {
            continue;
         }

         float newDistanceGap = Vector2.SqrMagnitude(attacker.transform.position - sortPoint.transform.position);

         if (newDistanceGap < oldDistanceGap) {
            oldDistanceGap = newDistanceGap;
            nearestEntity = attacker;
         }
      }

      return nearestEntity;
   }

   protected List<NetEntity> getAllTargetsInAttackRange () {
      List<NetEntity> targets = new List<NetEntity>();

      foreach (uint attackerId in _attackers.Keys) {
         NetEntity attacker = MyNetworkManager.fetchEntityFromNetId<NetEntity>(attackerId);
         if (attacker == null || attacker == this || attacker.isDead() || (attacker is SeaMonsterEntity)) {
            continue;
         }

         if (isWithinRangedAttackDistance(attacker)) {
            targets.Add(attacker);
         }
      }

      return targets;
   }

   #endregion

   #region Behavior functions

   public bool canAttack () {
      double timeSinceAttack = NetworkTime.time - _lastAttackTime;
      return timeSinceAttack > seaMonsterData.attackFrequency;
   }

   #endregion

   private void handleAnimations () {
      // Server should not process animations
      if (Util.isBatch()) {
         return;
      }

      if (isDead()) {
         modifyAnimationSpeed(SimpleAnimation.DEFAULT_TIME_PER_FRAME * 0.75f);
         playAnimation(Anim.Type.Death_East);
         return;
      }

      if (!isAttacking) {
         modifyAnimationSpeed(cachedAnimSpeed);

         _simpleAnim.isPaused = false;
         _simpleAnimRipple.isPaused = false;
         if (getVelocity().magnitude > MIN_MOVEMENT_MAGNITUDE) {
            switch (this.facing) {
               case Direction.North:
                  playAnimation(Anim.Type.Run_North);
                  break;
               case Direction.South:
                  playAnimation(Anim.Type.Run_South);
                  break;
               default:
                  playAnimation(Anim.Type.Run_East);
                  break;
            }
         } else {
            switch (this.facing) {
               case Direction.North:
                  playAnimation(Anim.Type.Idle_North);
                  break;
               case Direction.South:
                  playAnimation(Anim.Type.Idle_South);
                  break;
               default:
                  playAnimation(Anim.Type.Idle_East);
                  break;
            }
         }
      }
   }

   protected void playAnimation (Anim.Type animType) {
      // Server should not process animations
      if (Util.isBatch()) {
         return;
      }

      _simpleAnim.playAnimation(animType);
      if (monsterType == Type.Horror || monsterType == Type.Horror_Tentacle) {
         _simpleAnimRipple.playAnimation(Anim.Type.Idle_East);
      } else {
         _simpleAnimRipple.playAnimation(animType);
      }
   }

   protected void modifyAnimationSpeed (float frameLenght) {
      if (_simpleAnim.frameLengthOverride != frameLenght) {
         _simpleAnim.modifyAnimSpeed(frameLenght);
         _simpleAnimRipple.modifyAnimSpeed(frameLenght);
      }
   }

   public static int fetchReceivedData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.SEA_ENEMY_DATA_KEY) == 0) {
            // Get ID from seaMonster data field
            if (field.tryGetIntValue(out int id)) {
               return id;
            }
         }
      }
      return 0;
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.SEA_ENEMY_DATA_KEY) == 0) {
            // Get ID from seaMonster data field
            int id = int.Parse(field.v.Split(':')[0]);
         }
      }
   }

   public override void onDeath () {
      if (_hasRunOnDeath) {
         return;
      }

      base.onDeath();

      if (Global.player != null) {
         // If the tutorial is waiting for a sea monster boss to be defeated, test if the conditions are met
         if (hasBeenAttackedBy(Global.player) && TutorialManager3.self.getCurrentTrigger() == TutorialTrigger.KillBoss && monsterType == Type.Horror) {
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.KillBoss);
         }
      }

      // Play SFX
      SoundEffectManager.self.playSeaEnemyDeathSfx(monsterType, this.transform.position);

      if (!isSeaMonsterMinion() && monsterType != Type.Horror) {
         deathBubbleEffect.SetActive(true);
      }

      if (isServer) {
         if (seaMonsterData.shouldDropTreasure && !isPvpAI) {
            NetEntity lastAttacker = MyNetworkManager.fetchEntityFromNetId<NetEntity>(_lastAttackerNetId);
            if (lastAttacker) {
               spawnChest(lastAttacker.userId);
            } else {
               D.error("Sea monster couldn't drop a chest, due to not being able to locate last attacker");
            }
         }
      }

      if (_currentBehaviorCoroutine != null) {
         StopCoroutine(_currentBehaviorCoroutine);
      }

      handleAnimations();
      hasDiedEvent.Invoke();

      _clickableBox.gameObject.SetActive(false);
      seaMonsterBars.gameObject.SetActive(false);

      if (!isServer) {
         return;
      }

      // Reduces the life of the parent entity if there is one
      if (seaMonsterData.roleType == RoleType.Minion && seaMonsterParentEntity != null) {
         seaMonsterParentEntity.currentHealth -= Mathf.CeilToInt(seaMonsterParentEntity.maxHealth / 6);
         if (seaMonsterParentEntity.isDead()) {
            seaMonsterParentEntity.onDeath();
         }
      }

      if (seaMonsterData.roleType == RoleType.Master) {
         foreach (SeaMonsterEntity childEntity in seaMonsterChildrenList) {
            childEntity.currentHealth = 0;
            childEntity.sinkOnDeath = true;
         }
      }
   }

   [Server]
   protected void spawnChest (int killerUserId) {
      if (killerUserId > 0) {
         Instance currentInstance = InstanceManager.self.getInstance(this.instanceId);
         for (int i = 0; i < chestDropCount; i++) {
            // When multiple chests are dropped, spawn them in a random area around the monster position
            Vector2 offset = new Vector2(0, 0);
            if (i > 0) {
               offset += Random.insideUnitCircle * 0.3f;
            }

            TreasureManager.self.createSeaMonsterChest(currentInstance, transform.position + (Vector3) offset, seaMonsterData.xmlId, killerUserId, _attackers.Keys.ToArray(), bossCombatParticipants);
         }
      }
   }

   [Server]
   protected void faceVelocityDirection () {
      Direction newFacingDirection = DirectionUtil.getDirectionForVelocity(_body.velocity);
      if (newFacingDirection != this.facing) {
         this.facing = newFacingDirection;
      }
   }

   [Server]
   protected void launchProjectile (SeaEntity target, int abilityId, float attackDelay, float launchDelay) {
      StartCoroutine(CO_LaunchProjectile(target, abilityId, attackDelay, launchDelay));
   }

   protected IEnumerator CO_LaunchProjectile (SeaEntity target, int abilityId, float attackDelay, float launchDelay) {
      _isPerformingAttack = true;

      yield return new WaitForSeconds(attackDelay);
      
      this.facing = (Direction) SeaMonsterUtility.getDirectionToFace(target, sortPoint.transform.position);

      // Set attack animation trigger values on server side
      isAttacking = true;
      forceStop();
      _attackStartAnimateTime = NetworkTime.time;
      _attackEndAnimateTime = NetworkTime.time + getAttackDuration();

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

      yield return new WaitForSeconds(launchDelay);

      int accuracy = Random.Range(1, 4);
      Vector2 targetLoc = new Vector2(0, 0);
      if (accuracy == 1) {
         targetLoc = (Vector2)target.transform.position + (target.getVelocity());
      } else {
         targetLoc = target.transform.position;
      }

      launchAtTargetPosition(targetLoc, abilityId);
   }

   private void launchAtTargetPosition (Vector2 targetLoc, int abilityId, bool endPerformAttack = true) {
      // Clamp the target to the monster's range radius
      if ((targetLoc - (Vector2) sortPoint.transform.position).magnitude > seaMonsterData.maxProjectileDistanceGap) {
         targetLoc = ((targetLoc - (Vector2) sortPoint.transform.position).normalized * seaMonsterData.maxProjectileDistanceGap) + (Vector2) sortPoint.transform.position;
      }

      Vector2 spawnPosition = new Vector2(0, 0);
      // Determines the origin of the projectile
      if (projectileSpawnLocations == null || projectileSpawnLocations.Count < 1) {
         spawnPosition = transform.position;
      } else {
         if (this.facing != 0) {
            _projectileSpawnLocation = projectileSpawnLocations.Find(_ => _.direction == (Direction) this.facing).spawnTransform;
            spawnPosition = _projectileSpawnLocation.position;
         }
      }

      fireAtSpot(targetLoc, abilityId, 0.0f, 0.0f, spawnPosition);
      if (endPerformAttack) {
         _isPerformingAttack = false;
      }
   }

   public override void setAreaParent (Area area, bool worldPositionStays) {
      this.transform.SetParent(area.seaMonsterParent, worldPositionStays);
   }

   #region Utilities

   private void OnDrawGizmos () {
      if (seaMonsterData != null) {
         if (!seaMonsterData.showDebugGizmo) {
            return;
         }
         if (targetEntity != null) {
            Gizmos.color = Color.cyan;
            Gizmos.DrawSphere(targetEntity.transform.position, .5f);
         }

         // Draws the range of the monster territory
         Gizmos.color = Color.yellow;
         float sizex = seaMonsterData.territoryRadius;
         Gizmos.DrawWireSphere(_spawnPos, sizex);

         // Draws the range of the monsters search radius
         Gizmos.color = Color.red;
         sizex = seaMonsterData.detectRadius;
         Gizmos.DrawWireSphere(sortPoint.transform.position, sizex);

         // Draws the range of the monsters search radius
         Gizmos.color = Color.red;
         sizex = seaMonsterData.maxMeleeDistanceGap;
         Gizmos.DrawWireSphere(sortPoint.transform.position, sizex);

         // Draws the range of the monsters follow radius
         Gizmos.color = Color.blue;
         sizex = seaMonsterData.maxDistanceGap;
         Gizmos.DrawWireSphere(sortPoint.transform.position, sizex);

         // Draws the range of the monsters attack radius
         Gizmos.color = Color.black;
         sizex = seaMonsterData.maxProjectileDistanceGap;
         Gizmos.DrawWireSphere(sortPoint.transform.position, sizex);
      }
   }

   protected bool isWithinMoveDistance (NetEntity entity) {
      float sqrDistance = Vector2.SqrMagnitude(entity.transform.position - sortPoint.transform.position);
      return sqrDistance < seaMonsterData.maxDistanceGap * seaMonsterData.maxDistanceGap;
   }

   protected bool isWithinTerritory (NetEntity entity) {
      float sqrDistance = Vector2.SqrMagnitude(_spawnPos - (Vector2) entity.transform.position);
      return sqrDistance < seaMonsterData.territoryRadius * seaMonsterData.territoryRadius;
   }

   protected bool isWithinRangedAttackDistance (NetEntity entity) {
      return isWithinRangedAttackDistance(Vector2.SqrMagnitude(sortPoint.transform.position - entity.transform.position));
   }

   protected bool isWithinRangedAttackDistance (float sqrDistance) {
      return sqrDistance < seaMonsterData.maxProjectileDistanceGap * seaMonsterData.maxProjectileDistanceGap;
   }

   protected bool isWithinMeleeAttackDistance (float sqrDistance) {
      return sqrDistance < seaMonsterData.maxMeleeDistanceGap * seaMonsterData.maxMeleeDistanceGap;
   }

   public override bool isSeaMonster () { return true; }

   public override bool isSeaMonsterMinion () {
      return seaMonsterData.roleType == RoleType.Minion;
   }

   protected override bool isInRange (Vector2 position) {
      Vector2 myPosition = transform.position;
      float sqrDistance = Vector2.SqrMagnitude(position - myPosition);
      if (seaMonsterData.isMelee) {
         return isWithinMeleeAttackDistance(sqrDistance);
      } else {
         return isWithinRangedAttackDistance(sqrDistance);
      }
   }

   [Server]
   protected override IEnumerator CO_AttackEnemiesInRange (float delayInSecondsWhenNotReloading) {
      while (!isDead()) {

         // Wait for the reload to finish
         while (!hasReloaded() || !canAttack() || shouldIgnoreAttackers() || _isPerformingAttack) {
            yield return null;
         }

         targetEntity = getAttackerInRange();
         if (targetEntity != null) {
            attackTarget();
         }

         yield return null;
      }
   }

   [Server]
   private void attackTarget () {
      // TODO: Setup a more efficient method for attack type setup
      Attack.Type attackType = Attack.Type.None;
      ShipAbilityData seaEntityAbilityData = null;
      if (seaMonsterData.skillIdList.Count > 0) {
         // TODO: Do logic here that determines which ability a special monster can use based on situation

         int randomIndex = seaMonsterData.skillIdList.Count < 1 ? 0 : Random.Range(0, seaMonsterData.skillIdList.Count);
         int randomSkillId = seaMonsterData.skillIdList[randomIndex];
         seaEntityAbilityData = ShipAbilityManager.self.getAbility(randomSkillId);
         string skillString = "";
         foreach (var temp in seaMonsterData.skillIdList) {
            skillString += temp + ":";
         }
         skillString.Remove(skillString.Length-1);
         if (seaEntityAbilityData != null) {
            attackType = seaEntityAbilityData.selectedAttackType;
            if (seaMonsterData.roleType == RoleType.Master) {
               selectedAbilityId = randomSkillId;
               D.editorLog("{" + monsterType + "} Attack skill: {" + randomSkillId + ":" + seaEntityAbilityData.abilityName + "}" +
                  " {" + attackType + "} {" + skillString + "}", Color.white);
            }
         }
      }

      // TODO: Make this block of code dynamic and web tool dependent for flexibility
      // Handle custom master behavior here
      if (seaMonsterData.roleType == RoleType.Master && seaEntityAbilityData != null) {
         // Determine if ability has already cooled down, if not then select default ability
         bool isCooledDown = NetworkTime.time - _abilityCooldownTracker[selectedAbilityId] > seaEntityAbilityData.coolDown;
         bool isLastAbilityTimeLapsed = NetworkTime.time > nextMajorSkillUseTime;
         if (isCooledDown && isLastAbilityTimeLapsed) {
            // Hard coded abilities: 51 - Submerge ability / 50 - Summon Minions
            switch (selectedAbilityId) {
               case 51:
                  // Do submerge ability here
                  CancelInvoke(nameof(checkEnemiesToAggro));
                  InvokeRepeating(nameof(checkEnemiesToAggro), seaEntityAbilityData.statusDuration, 0.5f);

                  forceStop();
                  freezeMovement = true;
                  setIsInvulnerable(true);

                  isSubmerging = true;
                  StartCoroutine(CO_Submerge());
                  Invoke(nameof(resurface), seaEntityAbilityData.statusDuration - 1);

                  _isPerformingAttack = true;
                  launchAtTargetPosition(new Vector2(transform.position.x, transform.position.y), selectedAbilityId, false);
                  _abilityCooldownTracker[selectedAbilityId] = NetworkTime.time;
                  nextMajorSkillUseTime = NetworkTime.time + Random.Range(5, 15);
                  return;
               case 50:
                  // Summoning ability logic
                  D.editorLog("{" + monsterType + "} Summon skill: {" + attackType + "} {" + seaEntityAbilityData.abilityName + "}", Color.green);
                  EnemyManager.self.spawnSeaMonsters(transform.position, seaEntityAbilityData.summonSeamonsterId, instanceId, areaKey, seaEntityAbilityData.summonCount);
                  _abilityCooldownTracker[selectedAbilityId] = NetworkTime.time;
                  nextMajorSkillUseTime = NetworkTime.time + Random.Range(5, 15);
                  return;
            }
         } else {
            // Default skill selected if cooldown does not meet
            selectedAbilityId = seaMonsterData.skillIdList[0];
            seaEntityAbilityData = ShipAbilityManager.self.getAbility(seaMonsterData.skillIdList[0]);
            attackType = seaEntityAbilityData.selectedAttackType;
         }

         if (seaMonsterData.isMelee && isCooledDown) {
            // Default skill selected if cooldown does not meet
            selectedAbilityId = seaMonsterData.skillIdList[0];
            seaEntityAbilityData = ShipAbilityManager.self.getAbility(seaMonsterData.skillIdList[0]);
            attackType = seaEntityAbilityData.selectedAttackType;
         }
      }

      // Attack
      if (attackType != Attack.Type.None) {
         if (seaMonsterData.isMelee) {
            _isPerformingAttack = true;
            Invoke(nameof(resetMeleeParameters), 1);
            if (seaMonsterData.roleType == RoleType.Master) {
               D.editorLog("{" + monsterType + "} Melee skill: {" + attackType + "}", Color.green);
               meleeAtSpot(targetEntity.transform.position, selectedAbilityId, seaMonsterData.maxMeleeDistanceGap);
               _abilityCooldownTracker[selectedAbilityId] = NetworkTime.time;
            } else {
               meleeAtSpot(targetEntity.transform.position, seaMonsterData.attackType);
            }
         } else if (seaMonsterData.isRanged) {
            int abilityId = -1;
            if (seaMonsterData.skillIdList.Count > 0) {
               ShipAbilityData shipAbility = ShipAbilityManager.self.getAbility(seaMonsterData.skillIdList[0]);
               abilityId = shipAbility.abilityId;
            }
            if (seaMonsterData.roleType == RoleType.Master) {
               D.editorLog("{" + monsterType + "} Ranged skill: {" + attackType + "}{" + abilityId + "}", Color.green);
            }

            // TODO: Confirm later on if this needs to be dynamic
            float launchDelay = getAttackDuration();
            float projectileDelay = seaMonsterData.projectileDelay;
            D.adminLog("{" + monsterType + "} Launch delay: {" + launchDelay + "} ProjectileDelay: {" + projectileDelay + "}", D.ADMIN_LOG_TYPE.SeaAbility);
            launchProjectile(targetEntity.GetComponent<SeaEntity>(), abilityId, projectileDelay, launchDelay);
            _abilityCooldownTracker[selectedAbilityId] = NetworkTime.time;

            if (monsterType == Type.Horror_Tentacle) {
               if (_isNextAttackSecondary) {
                  // Tentacle Minions can be commanded to fire a secondary attack
                  attackRandomTargetWithTentacleSecondaryAttack();
                  _isNextAttackSecondary = false;
               } else {
                  // Tentacles also attack other targets in range
                  foreach (KeyValuePair<uint, double> KV in _attackers) {
                     NetEntity entity = MyNetworkManager.fetchEntityFromNetId<NetEntity>(KV.Key);
                     if (entity != null && entity != targetEntity && Random.value < TENTACLE_EXTRA_TARGET_CHANCE && isInRange(entity.transform.position)) {
                        launchProjectile(entity.GetComponent<SeaEntity>(), abilityId, projectileDelay, launchDelay);
                     }
                  }
               }
            }
         }
      }
   }

   [Server]
   public void attackRandomTargetWithTentacleSecondaryAttack () {
      if (monsterType != Type.Horror_Tentacle || _attackers.Count <= 0) {
         return;
      }

      // Use the second monster skill
      if (seaMonsterData.skillIdList.Count < 2) {
         return;
      }
      int abilityId = seaMonsterData.skillIdList[1];

      // List the attackers that are in range
      List<NetEntity> attackersInRange = new List<NetEntity>();
      foreach (KeyValuePair<uint, double> KV in _attackers) {
         NetEntity entity = MyNetworkManager.fetchEntityFromNetId<NetEntity>(KV.Key);
         if (entity != null && isInRange(entity.transform.position)) {
            attackersInRange.Add(entity);
         }
      }

      // Pick a random attacker
      NetEntity targetEntity = attackersInRange.ChooseRandom();

      float launchDelay = .4f;
      float projectileDelay = seaMonsterData.projectileDelay;
      launchProjectile(targetEntity.GetComponent<SeaEntity>(), abilityId, projectileDelay, launchDelay);
   }

   #endregion

   #region Enemy AI

   [Server]
   protected override Vector3 findAttackerVicinityPosition (bool newAttacker) {
      if (monsterType != Type.Horror) {
         return base.findAttackerVicinityPosition(newAttacker);
      }

      // Horror monsters don't pursue attackers beyond their territory
      Dictionary<uint, double> attackersInTerritory = new Dictionary<uint, double>();
      foreach (KeyValuePair<uint, double> KV in _attackers) {
         NetEntity attackerEntity = MyNetworkManager.fetchEntityFromNetId<NetEntity>(KV.Key);
         if (attackerEntity != null && isWithinTerritory(attackerEntity)) {
            // Build a new dictionary with only attackers inside the territory
            attackersInTerritory.Add(KV.Key, KV.Value);
         }
      }

      return findAttackerVicinityPosition(newAttacker, attackersInTerritory);
   }

   [Server]
   public void commandMinionTentaclesToUseSecondaryAttack () {
      // List the child entities that are under attack
      List<SeaMonsterEntity> childsUnderAttack = new List<SeaMonsterEntity>();
      foreach (SeaMonsterEntity childEntity in seaMonsterChildrenList) {
         if (!childEntity.isDead() && childEntity.hasAttackers()) {
            childsUnderAttack.Add(childEntity);
         }
      }

      // Clear all pending secondary attack commands
      foreach (SeaMonsterEntity childEntity in seaMonsterChildrenList) {
         if (!childEntity.isDead()) {
            childEntity.clearSecondaryAttackSchedule();
         }
      }

      // Command a random child to schedule a secondary attack
      if (childsUnderAttack.Count > 0) {
         childsUnderAttack.ChooseRandom().scheduleSecondaryAttack();
      }
   }

   [Server]
   private void scheduleSecondaryAttack () {
      _isNextAttackSecondary = true;
   }

   [Server]
   private void clearSecondaryAttackSchedule () {
      _isNextAttackSecondary = false;
   }

   protected override void onMaxHealthChanged (int oldValue, int newValue) {
      base.onMaxHealthChanged(oldValue, newValue);
      seaMonsterBars.initializeHealthBar();
   }


   [Server]
   protected override void customRegisterDamageReceived (int userId, int amount) {
      if (seaMonsterData.roleType == RoleType.Minion && seaMonsterParentEntity != null) {
         seaMonsterParentEntity.registerDamageReceivedByMinion(userId, amount);
      }
   }

   [Server]
   protected void registerDamageReceivedByMinion (int userId, int amount) {
      if (_damageReceivedPerAttacker.ContainsKey(userId)) {
         _damageReceivedPerAttacker[userId] += amount;
      } else {
         _damageReceivedPerAttacker[userId] = amount;
      }
   }

   [Server]
   protected override int getRewardedXP () {
      return seaMonsterData.rewardedExp;
   }

   #endregion

   #region Private Variables

   // The handling for monster sprite animation
   protected SimpleAnimation _simpleAnim;

   // The handling for ripple sprite animation
   protected SimpleAnimation _simpleAnimRipple;

   // The position we spawned at
   protected Vector2 _spawnPos;

   // Keeps reference to the behavior coroutine so that it can be manually stopped
   private Coroutine _currentBehaviorCoroutine = null;

   // A working array to use with OverlapCircle
   private Collider2D[] _hits = new Collider2D[MAX_COLLISION_COUNT];

   // The current behavior
   private MonsterBehavior _currentBehavior = MonsterBehavior.Idle;

   // The ability cooldown tracking dictionary
   private Dictionary<int, double> _abilityCooldownTracker = new Dictionary<int, double>();

   // The current targets in attack range
   private List<NetEntity> _targetsInAttackRange = new List<NetEntity>();

   // Gets set to true when the next attack must be a secondary attack
   private bool _isNextAttackSecondary = false;

   // Set to true when an attack is in the process of being performed, to prevent additional attacks from occurring
   protected bool _isPerformingAttack = false;

   #endregion
}