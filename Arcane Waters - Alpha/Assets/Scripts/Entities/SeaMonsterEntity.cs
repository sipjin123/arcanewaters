using System.Collections;
using System.Collections.Generic;
using MapCreationTool.Serialization;
using Mirror;
using UnityEngine;
using Pathfinding;

public class SeaMonsterEntity : SeaEntity, IMapEditorDataReceiver
{
   #region Public Variables

   public enum Type
   {
      None = 0, Tentacle = 1, Horror = 2, Worm = 3, Reef_Giant = 4, Fishman = 5, SeaSerpent = 6, Horror_Tentacle = 7, PirateShip = 8
   }

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

   // A flag to determine if the object has died
   public bool hasDied = false;

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

   // Determines the location of this unit in relation to its spawn point
   public Vector2 distanceFromSpawnPoint;

   // Holds the info of the seamonster health Bars
   public SeaMonsterBars seaMonsterBars;

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

   // Seamonster Animation
   public enum SeaMonsterAnimState
   {
      Idle,
      Attack,
      EndAttack,
      Die,
      Move,
      MoveStop
   }

   // Determines the current behavior of the Monster
   public enum MonsterBehavior
   {
      Idle = 0,
      MoveToPosition = 1,
      MoveToAttackPosition = 2,
      Attack = 3,
   }

   #endregion

   #region Unity Lifecycle

   public void initData (SeaMonsterEntityData entityData) {
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

      // Update the collider scale and offset
      if (getCombatCollider() != null) {
         getCombatCollider().setScale(new Vector3(seaMonsterData.battleColliderScaleX, seaMonsterData.battleColliderScaleY, 1));
      }

      reloadDelay = seaMonsterData.reloadDelay;
      currentHealth = seaMonsterData.maxHealth;
      maxHealth = seaMonsterData.maxHealth;
      invulnerable = seaMonsterData.isInvulnerable;

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
   }

   protected override void Start () {
      base.Start();

      // Initializes the data from the scriptable object
      SeaMonsterEntityData monsterData = SeaMonsterManager.self.seaMonsterDataList.Find(_ => _.seaMonsterType == monsterType);

      if (monsterData == null) {
         D.debug("Sea Monster data is null for: " + monsterType);
         Destroy(gameObject);
         return;
      }

      initData(monsterData);

      if (isServer) {
         _seeker = GetComponent<Seeker>();
         if (_seeker == null) {
            D.debug("There has to be a Seeker Script attached to the SeaMonsterEntity Prefab");
         }

         // Only use the graph in this area to calculate paths
         GridGraph graph = AreaManager.self.getArea(areaKey).getGraph();
         _seeker.graphMask = GraphMask.FromGraph(graph);

         _seeker.pathCallback = setPath_Asynchronous;

         startAnalysis();
      }

      // Note our spawn position
      _spawnPos = sortPoint.transform.position;

      _simpleAnim.playAnimation(Anim.Type.Idle_North);
   }

   protected override void Update () {
      base.Update();

      // If we're dead and have finished sinking, remove us
      if (seaMonsterData.roleType != RoleType.Minion) {
         if (isServer && isDead() && spritesContainer.transform.localPosition.y < -.25f) {
            InstanceManager.self.removeEntityFromInstance(this);

            // Destroy the object
            NetworkServer.Destroy(this.gameObject);
         }
      }

      // Alters the simple animation data
      handleAnimations();

      // Kills the unit
      if (hasDied == false && isDead()) {
         killUnit();
      }

      // Handles attack animations
      if (NetworkTime.time > _attackStartAnimateTime && !_hasAttackAnimTriggered) {
         _simpleAnim.stayAtLastFrame = true;
         isAttacking = true;
         _hasAttackAnimTriggered = true;
         _attackEndAnimateTime = NetworkTime.time + getAttackDuration();
      } else {
         if (isAttacking && (NetworkTime.time > _attackEndAnimateTime)) {
            _simpleAnim.modifyAnimSpeed(cachedAnimSpeed);
            _attackStartAnimateTime = NetworkTime.time + 50;
            isAttacking = false;
            _simpleAnim.stayAtLastFrame = false;
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
         } else if (getVelocity().magnitude > MIN_MOVEMENT_MAGNITUDE) {
            // Update our facing direction
            faceVelocityDirection();
         }
      } else {
         // Forces minions to look at direction in relation to the parent
         if (distanceFromSpawnPoint.x < 0) {
            facing = Direction.West;
         } else if (distanceFromSpawnPoint.x == 0) {
            facing = distanceFromSpawnPoint.y == 1 ? Direction.East : Direction.West;
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
      if (NetworkTime.time - _lastMoveChangeTime < (seaMonsterData.roleType != RoleType.Minion ? MOVE_CHANGE_INTERVAL : MOVE_CHANGE_INTERVAL/2)) {
         return;
      }

      if (isStationary) {
         return;
      }

      // If this entity is a Minion, snap to its parent
      if (seaMonsterData.roleType == RoleType.Minion && seaMonsterParentEntity != null) {
         Vector2 targetLocation = SeaMonsterUtility.getFixedPositionAroundPosition(seaMonsterParentEntity.sortPoint.transform.position, distanceFromSpawnPoint);
         Vector2 waypointDirection = (targetLocation - (Vector2) sortPoint.transform.position).normalized;

         // Teleports the Minions if too far away from Parent
         if (Vector2.SqrMagnitude(sortPoint.transform.position - seaMonsterParentEntity.transform.position) > 2 * 2) {
            _body.MovePosition(targetLocation);
         }

         float sqrDistanceToWaypoint = Vector2.SqrMagnitude(targetLocation - (Vector2)sortPoint.transform.position);
         if (sqrDistanceToWaypoint > .025f) {
            _body.AddForce(waypointDirection.normalized * (getMoveSpeed() / 1.75f));
         }
         return;
      }

      if (_currentPathIndex < _currentPath.Count) {
         // Move towards our current waypoint
         Vector2 waypointDirection = ((Vector2) _currentPath[_currentPathIndex] - (Vector2)sortPoint.transform.position).normalized;

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

   #region External Entity Related Functions

   public override void requestAnimationPlay (Anim.Type animType) {
      isAttacking = true;
      _attackEndAnimateTime = NetworkTime.time + getAttackDuration();
      _simpleAnim.modifyAnimSpeed(cachedAttackAnimSpeed);
      _simpleAnim.playAnimation(animType);
   }

   private float getAttackDuration () {
      // Implement attack duration altering here
      return ATTACK_DURATION;
   }

   public override void noteAttacker (uint netId) {
      base.noteAttacker(netId);
      if (seaMonsterParentEntity != null) {
         seaMonsterParentEntity.noteAttacker(netId);
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

   #endregion

   #region Behavior functions

   private void startAnalysis () {
      switch (seaMonsterData.roleType) {
         case RoleType.Standalone:
         case RoleType.Master:
            InvokeRepeating(nameof(standaloneBehavior), Random.Range(0f, 1f), 0.5f);
            break;
         case RoleType.Minion:
            InvokeRepeating(nameof(minionBehavior), Random.Range(0f, 1f), 0.5f);
            break;
         default:
            break;
      }
   }

   private void standaloneBehavior () {
      if (_currentBehavior == MonsterBehavior.Idle) {
         // Continue attacking the current target
         if (canCurrentTargetBeAttacked()) {
            _currentBehaviorCoroutine = StartCoroutine(CO_AttackTargetOnce());
            noteAttacker(targetEntity);
            Rpc_NoteAttacker(targetEntity.netId);
            return;
         }

         // Check if there are targets around
         scanTargetsInArea();
         targetEntity = getNearestTarget();

         if (canCurrentTargetBeAttacked()) {
            // Attack the target in range
            _currentBehaviorCoroutine = StartCoroutine(CO_AttackTargetOnce());
         } else if (targetEntity != null && !targetEntity.isDead() && isWithinMoveDistance(targetEntity) && isWithinTerritory(targetEntity)) {
            // Move closer to the target
            _currentBehaviorCoroutine = StartCoroutine(CO_MoveToPosition(
               (Vector2)targetEntity.transform.position + Random.insideUnitCircle * 0.5f, 0.1f,
               MonsterBehavior.MoveToAttackPosition));
         } else {
            // Move around
            _currentBehaviorCoroutine = StartCoroutine(CO_MoveToPosition(
               _spawnPos + Random.insideUnitCircle * 0.5f, seaMonsterData.moveFrequency,
               MonsterBehavior.MoveToPosition));
         }
      } else if (_currentBehavior == MonsterBehavior.MoveToPosition) {
         // Check if there are targets around
         scanTargetsInArea();
         targetEntity = getNearestTarget();

         if (targetEntity != null && !targetEntity.isDead() && isWithinMoveDistance(targetEntity) && isWithinTerritory(targetEntity)) {
            // Interrupt the move around behavior
            StopCoroutine(_currentBehaviorCoroutine);
            _currentPathIndex = _currentPath.Count;
            _currentBehavior = MonsterBehavior.Idle;
         }
      }
   }

   private void minionBehavior () {
      if (_currentBehavior == MonsterBehavior.Idle) {
         // Continue attacking the current target
         if (canCurrentTargetBeAttacked()) {
            _currentBehaviorCoroutine = StartCoroutine(CO_AttackTargetOnce());
            return;
         }

         // Verify if there are targets around
         scanTargetsInArea();
         targetEntity = getNearestTarget();
      }
   }

   private IEnumerator CO_MoveToPosition (Vector2 pos, float endDelay, MonsterBehavior behavior) {
      _currentBehavior = behavior;

      yield return findAndSetPath_Asynchronous(pos);

      // The movement is performed in FixedUpdate
      while (_currentPathIndex < _currentPath.Count) {
         yield return new WaitForSeconds(0.1f);
      }

      if (endDelay >= 0) {
         yield return new WaitForSeconds(endDelay);
      }

      _currentBehavior = MonsterBehavior.Idle;
   }

   private IEnumerator CO_AttackTargetOnce () {
      _currentBehavior = MonsterBehavior.Attack;

      // Wait for the reload to finish
      while (!hasReloaded() && !canAttack()) {
         yield return null;
      }

      // If the target is no more valid, stop here
      if (!canCurrentTargetBeAttacked()) {
         _currentBehavior = MonsterBehavior.Idle;
         yield break;
      }

      // TODO: Setup a more efficient method for attack type setup
      Attack.Type attackType = Attack.Type.None;
      if (seaMonsterData.skillIdList.Count > 0) {
         ShipAbilityData seaEntityAbilityData = ShipAbilityManager.self.getAbility(seaMonsterData.skillIdList[0]);
         if (seaEntityAbilityData != null) {
            attackType = seaEntityAbilityData.selectedAttackType;
         }
      }

      // Attack
      if (attackType != Attack.Type.None) {
         if (seaMonsterData.isMelee) {
            meleeAtSpot(targetEntity.transform.position, seaMonsterData.attackType);
         } else if (seaMonsterData.isRanged) {
            int abilityId = -1;
            if (seaMonsterData.skillIdList.Count > 0) {
               ShipAbilityData shipAbility = ShipAbilityManager.self.getAbility(seaMonsterData.skillIdList[0]);
               abilityId = shipAbility.abilityId;
            }

            // TODO: Confirm later on if this needs to be dynamic
            float launchDelay = .4f; 
            float projectileDelay = seaMonsterData.projectileDelay;
            launchProjectile(targetEntity.transform.position, targetEntity.GetComponent<SeaEntity>(), abilityId, projectileDelay, launchDelay);
         }
      }

      _currentBehavior = MonsterBehavior.Idle;
   }

   private bool canCurrentTargetBeAttacked () {
      if (targetEntity == null || targetEntity.isDead() || !isWithinTerritory(targetEntity)) {
         return false;
      }

      float sqrDistanceToTarget = Vector2.SqrMagnitude(sortPoint.transform.position - targetEntity.transform.position);
      if ((seaMonsterData.isMelee && !isWithinMeleeAttackDistance(sqrDistanceToTarget)) ||
            (seaMonsterData.isRanged && !isWithinRangedAttackDistance(sqrDistanceToTarget))) {
         return false;
      }

      return true;
   }

   public bool canAttack () {
      double timeSinceAttack = NetworkTime.time - _lastAttackTime;
      return timeSinceAttack > seaMonsterData.attackFrequency;
   }

   #endregion

   private void handleAnimations () {
      if (hasDied) {
         _simpleAnim.playAnimation(Anim.Type.Death_East);
         return;
      }

      if (!isAttacking) {
         if (_simpleAnim.frameLengthOverride != cachedAnimSpeed) {
            _simpleAnim.modifyAnimSpeed(cachedAnimSpeed);
         }

         _simpleAnim.isPaused = false;
         if (getVelocity().magnitude > MIN_MOVEMENT_MAGNITUDE) {
            switch (this.facing) {
               case Direction.North:
                  _simpleAnim.playAnimation(Anim.Type.Run_North);
                  break;
               case Direction.South:
                  _simpleAnim.playAnimation(Anim.Type.Run_South);
                  break;
               default:
                  _simpleAnim.playAnimation(Anim.Type.Run_East);
                  break;
            }
         } else {
            switch (this.facing) {
               case Direction.North:
                  _simpleAnim.playAnimation(Anim.Type.Idle_North);
                  break;
               case Direction.South:
                  _simpleAnim.playAnimation(Anim.Type.Idle_South);
                  break;
               default:
                  _simpleAnim.playAnimation(Anim.Type.Idle_East);
                  break;
            }
         }
      }
   }

   protected virtual void killUnit () {
      if (_currentBehaviorCoroutine != null) {
         StopCoroutine(_currentBehaviorCoroutine);
      }

      hasDied = true;
      handleAnimations();

      _clickableBox.gameObject.SetActive(false);
      seaMonsterBars.gameObject.SetActive(false);

      // Reduces the life of the parent entity if there is one
      if (seaMonsterData.roleType == RoleType.Minion && seaMonsterParentEntity != null) {
         seaMonsterParentEntity.currentHealth -= 1000;
      }

      if (seaMonsterData.roleType == RoleType.Master) {
         foreach (SeaMonsterEntity childEntity in seaMonsterChildrenList) {
            NetworkServer.Destroy(childEntity.gameObject);
         }
      }

      // Drops the treasure bag if this entity can spawn one
      if (seaMonsterData.shouldDropTreasure) {
         NetEntity lastAttacker = MyNetworkManager.fetchEntityFromNetId<NetEntity>(_lastAttackerNetId);
         if (lastAttacker) {
            spawnChest(lastAttacker.userId);
         }
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

   [Server]
   protected void spawnChest (int killerUserId) {
      Instance currentInstance = InstanceManager.self.getInstance(this.instanceId);
      TreasureManager.self.createSeaMonsterChest(currentInstance, sortPoint.transform.position, seaMonsterData.seaMonsterType, killerUserId);
   }

   [Server]
   protected void faceVelocityDirection () {
      Direction newFacingDirection = DirectionUtil.getDirectionForVelocity(_body.velocity);
      if (newFacingDirection != this.facing) {
         this.facing = newFacingDirection;
      }
   }

   [Server]
   protected void launchProjectile (Vector2 spot, SeaEntity attacker, int abilityId, float attackDelay, float launchDelay) {
      this.facing = (Direction) SeaMonsterUtility.getDirectionToFace(attacker, sortPoint.transform.position);

      int accuracy = Random.Range(1, 4);
      Vector2 targetLoc = new Vector2(0, 0);
      if (accuracy == 1) {
         targetLoc = spot + (attacker.getVelocity());
      } else {
         targetLoc = spot;
      }

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

      // Set attack animation trigger values on server side
      isAttacking = true;
      _attackStartAnimateTime = NetworkTime.time;
      _attackEndAnimateTime = NetworkTime.time + getAttackDuration();

      fireAtSpot(targetLoc, abilityId, attackDelay, launchDelay, spawnPosition);
   }

   private IEnumerator findAndSetPath_Asynchronous (Vector3 targetPosition) {
      if (!_seeker.IsDone()) {
         _seeker.CancelCurrentPathRequest();
      }
      _seeker.StartPath(sortPoint.transform.position, targetPosition);

      while (!_seeker.IsDone()) {
         yield return null;
      }
   }

   private void setPath_Asynchronous (Path newPath) {
      _currentPath = newPath.vectorPath;
      _currentPathIndex = 0;
      _seeker.CancelCurrentPathRequest(true);
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

   protected bool isWithinRangedAttackDistance (float sqrDistance) {
      return sqrDistance < seaMonsterData.maxProjectileDistanceGap * seaMonsterData.maxProjectileDistanceGap;
   }

   protected bool isWithinMeleeAttackDistance (float sqrDistance) {
      return sqrDistance < seaMonsterData.maxMeleeDistanceGap * seaMonsterData.maxMeleeDistanceGap;
   }

   public override bool isSeaMonster () { return true; }

   #endregion

   #region Private Variables

   // The Seeker that handles Pathfinding
   protected Seeker _seeker;

   // The handling for monster sprite animation
   protected SimpleAnimation _simpleAnim;

   // The handling for ripple sprite animation
   protected SimpleAnimation _simpleAnimRipple;

   // The position we spawned at
   protected Vector2 _spawnPos;

   // The current waypoint List
   protected List<Vector3> _currentPath = new List<Vector3>();

   // The current Point Index of the path
   private int _currentPathIndex;

   // Keeps reference to the behavior coroutine so that it can be manually stopped
   private Coroutine _currentBehaviorCoroutine = null;

   // A working array to use with OverlapCircle
   private Collider2D[] _hits = new Collider2D[MAX_COLLISION_COUNT];

   // The current behavior
   private MonsterBehavior _currentBehavior = MonsterBehavior.Idle;

   #endregion
}