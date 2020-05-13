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
      None = 0, Tentacle = 1, Horror = 2, Worm = 3, Reef_Giant = 4, Fishman = 5, SeaSerpent = 6
   }

   // List of children dependencies
   public List<SeaMonsterEntity> seaMonsterChildrenList = new List<SeaMonsterEntity>();

   // The parent entity of this sea monster
   public SeaMonsterEntity seaMonsterParentEntity;

   // Determines the current behavior of the monster
   public MonsterBehavior monsterBehavior;

   // Determines if this monster is engaging a ship
   public bool isEngaging = false;

   // Current target entity
   public NetEntity targetEntity;

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
   public const float ATTACK_DURATION = .3f;

   // Determines if a minion is planning its own behavior
   public bool isMinionPlanning = false;

   // The limit of the overlap collider check to avoid too much checking
   public const int MAX_COLLISION_COUNT = 40;

   // Holds the corpse object
   public GameObject corpseHolder;

   // Determines if this unit is logging info
   public bool isLoggingData;

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
      MoveAround = 1,
      MoveToTarget = 2,
      AttackTarget = 3,
      Die = 4
   }

   #endregion

   #region Unity Lifecycle

   public void initData (SeaMonsterEntityData entityData) {
      seaMonsterData = entityData;

      _simpleAnim = spritesContainer.GetComponent<SimpleAnimation>();
      _simpleAnimRipple = ripplesContainer.GetComponent<SimpleAnimation>();

      if (ImageManager.self.imageDataList.Count > 0 && !Util.isBatch()) {
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

         ripplesContainer.transform.localScale = new Vector3(seaMonsterData.rippleScaleOverride, seaMonsterData.rippleScaleOverride, seaMonsterData.rippleScaleOverride);
         spritesContainer.transform.localScale = new Vector3(seaMonsterData.scaleOverride, seaMonsterData.scaleOverride, seaMonsterData.scaleOverride);
         spritesContainer.transform.GetChild(0).localScale = new Vector3(seaMonsterData.outlineScaleOverride, seaMonsterData.outlineScaleOverride, seaMonsterData.outlineScaleOverride);

         _simpleAnimRipple.group = seaMonsterData.animGroup;
         _simpleAnimRipple.frameLengthOverride = seaMonsterData.rippleAnimationSpeedOverride;
         _simpleAnimRipple.enabled = true;

         _simpleAnim.group = seaMonsterData.animGroup;
         _simpleAnim.frameLengthOverride = seaMonsterData.animationSpeedOverride;
         _simpleAnim.enabled = true;
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

      if (ImageManager.self.imageDataList.Count > 0 && !Util.isBatch()) {
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
            D.error("There has to be a Seeker Script attached to the SeaMonsterEntity Prefab");
         }
         _seeker.pathCallback = setPath_Asynchronous;

         planNextMove();
      }

      // Note our spawn position
      _spawnPos = transform.position;

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
      if (Util.netTime() > _attackStartAnimateTime && !_hasAttackAnimTriggered) {
         _simpleAnim.stayAtLastFrame = true;
         isAttacking = true;
         _hasAttackAnimTriggered = true;
         _attackEndAnimateTime = Util.netTime() + ATTACK_DURATION;
      } else {
         if (isAttacking && (Util.netTime() > _attackEndAnimateTime)) {
            _attackStartAnimateTime = Util.netTime() + 50;
            isAttacking = false;
            _simpleAnim.stayAtLastFrame = false;
            isMinionPlanning = false;
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
      if (Time.time - _lastMoveChangeTime < MOVE_CHANGE_INTERVAL) {
         return;
      }

      // Sets up where this unit is facing
      if (seaMonsterData.roleType != RoleType.Minion) {
         handleFaceDirection();
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

      // If this entity is a Minion, snap to its parent
      if (seaMonsterData.roleType == RoleType.Minion) {
         if (!isMinionPlanning) {
            isMinionPlanning = true;
            planNextMove();
         }
         if (seaMonsterParentEntity != null) {
            Vector2 targetLocation = SeaMonsterUtility.getFixedPositionAroundPosition(seaMonsterParentEntity.transform.position, distanceFromSpawnPoint);
            Vector2 waypointDirection = (targetLocation - (Vector2) transform.position).normalized;

            // Teleports the Minions if too far away from Parent
            if (Vector3.Distance(transform.position, seaMonsterParentEntity.transform.position) > 2) {
               transform.position = targetLocation;
            }

            float distanceToWaypoint = Vector2.Distance(targetLocation, transform.position);
            if (distanceToWaypoint > .05f) {
               _body.AddForce(waypointDirection.normalized * (getMoveSpeed() / 1.75f));
            }
         }
         return;
      }

      // Process movement towards route
      if (monsterBehavior == MonsterBehavior.MoveAround || monsterBehavior == MonsterBehavior.MoveToTarget) {
         if (seaMonsterData.roleType != RoleType.Minion && (isEnemyWithinRangedAttackDistance() || (!isEnemyWithinTerritory() && monsterBehavior == MonsterBehavior.MoveToTarget))) {
            stopMoving();
         }

         if (_currentPathIndex < _currentPath.Count) {
            // Move towards our current waypoint
            Vector2 waypointDirection = ((Vector2)_currentPath[_currentPathIndex] - (Vector2)transform.position).normalized;

            _body.AddForce(waypointDirection * getMoveSpeed());
            _lastMoveChangeTime = Time.time;

            // Clears a node as the unit passes by
            float distanceToWaypoint = Vector2.Distance(_currentPath[_currentPathIndex], transform.position);
            if (distanceToWaypoint < .1f) {
               commandMinions();
               ++_currentPathIndex;
            }
         } else {
            // Path complete, plan the next move
            monsterBehavior = MonsterBehavior.Idle;
            planNextMove();
         }
      }
   }

   #endregion

   #region External Entity Related Functions

   public override void noteAttacker (NetEntity attacker) {
      if (!(attacker is SeaMonsterEntity)) {
         base.noteAttacker(attacker);
         if (seaMonsterParentEntity != null) {
            seaMonsterParentEntity.noteAttacker(attacker);
         }
      }
   }

   protected void scanTargetsInArea () {
      if (isDead() || !isServer || !seaMonsterData.isAggressive) {
         return;
      }

      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, seaMonsterData.detectRadius);

      int currentCount = 0;
      if (hits.Length > 0) {
         foreach (Collider2D hit in hits) {
            if (currentCount > MAX_COLLISION_COUNT) {
               // Avoid stack overflow
               break;
            }
            currentCount++;
            if (hit == null) {
               continue;
            }

            ShipEntity ship = hit.GetComponent<ShipEntity>();
            if (ship != null) {
               if (!_attackers.ContainsKey(ship.netId) && !ship.isDead()) {
                  noteAttacker(ship);
               }
            }
         }
      }
   }

   protected void attackTarget () {
      if (isDead() || !isServer) {
         return;
      }

      // If we haven't reloaded, we can't attack
      if (!hasReloaded()) {
         planNextMove();
         return;
      }

      // Check if any of our attackers are within range
      if (targetEntity == null || targetEntity.isDead() || targetEntity == this || targetEntity is SeaMonsterEntity) {
         return;
      }

      // Check where the attacker currently is
      Vector2 spot = targetEntity.transform.position;

      // TODO: Setup a more efficient method for attack type setup
      if (seaMonsterData.skillIdList.Count > 0) {
         ShipAbilityData seaEntityAbilityData = ShipAbilityManager.self.getAbility(seaMonsterData.skillIdList[0]);
         if (seaEntityAbilityData != null) {
            seaMonsterData.attackType = seaEntityAbilityData.selectedAttackType;
         }
      }

      if (seaMonsterData.attackType != Attack.Type.None) {
         if (seaMonsterData.isMelee && isEnemyWithinMeleeAttackDistance()) {
            meleeAtSpot(spot, seaMonsterData.attackType);
            monsterBehavior = MonsterBehavior.AttackTarget;

            planNextMove();
         } else {
            if (seaMonsterData.isRanged) {
               int abilityId = -1;
               if (seaMonsterData.skillIdList.Count > 0) {
                  ShipAbilityData shipAbility = ShipAbilityManager.self.getAbility(seaMonsterData.skillIdList[0]);
                  abilityId = shipAbility.abilityId;
               }

               launchProjectile(spot, targetEntity.GetComponent<SeaEntity>(), abilityId, .2f, .4f);
               monsterBehavior = MonsterBehavior.AttackTarget;
            }
         }
      } else {
         isEngaging = true;
      }

      planNextMove();
   }

   protected NetEntity getNearestTarget () {
      NetEntity nearestEntity = null;
      float oldDistanceGap = 100;

      foreach (uint attackerId in _attackers.Keys) {
         NetEntity attacker = MyNetworkManager.fetchNetEntityTypeFromNetIdentity<NetEntity>(attackerId);
         if (attacker == null) {
            continue;
         }

         if (attacker.isDead() || (attacker is SeaMonsterEntity)) {
            continue;
         }

         float newDistanceGap = Vector2.Distance(attacker.transform.position, transform.position);

         if (newDistanceGap < oldDistanceGap) {
            oldDistanceGap = newDistanceGap;
            nearestEntity = attacker;
         }
      }

      return nearestEntity;
   }

   #endregion

   #region Minion / Master Related Functions

   public void moveToParentDestination (Vector3 newTarget) {
      if (!isServer) {
         return;
      }

      if (seaMonsterParentEntity != null && seaMonsterData != null) {
         if (seaMonsterData.roleType == RoleType.Minion) {
            findAndSetPath_Asynchronous(newTarget);
         }
      }
   }

   private void commandMinions () {
      if (seaMonsterData.roleType == RoleType.Master && seaMonsterChildrenList.Count > 0 && _currentPathIndex < _currentPath.Count) {
         foreach (SeaMonsterEntity childEntity in seaMonsterChildrenList) {
            if (!childEntity.isDead()) {
               childEntity.stopMoving();
               childEntity.moveToParentDestination(_currentPath[_currentPath.Count - 1]);
            }
         }
      }
   }

   #endregion

   public void stopMoving () {
      if (seaMonsterData == null) {
         return;
      }

      // Stops current route and plans a new move
      if (monsterBehavior == MonsterBehavior.MoveAround || monsterBehavior == MonsterBehavior.MoveToTarget) {
         _currentPath.Clear();

         monsterBehavior = MonsterBehavior.Idle;

         planNextMove();
      }
   }

   private void planNextMove () {
      // Minions cant decide for themselves
      if (!isServer) {
         return;
      }

      if (_analysisCoroutine != null) {
         StopCoroutine(_analysisCoroutine);
      }
      _analysisCoroutine = StartCoroutine(CO_ProcessNextMove());
   }

   private IEnumerator CO_ProcessNextMove () {
      yield return new WaitForSeconds(.1f);

      // Checks if there are enemies nearby
      scanTargetsInArea();

      // Gets the nearest target if there is
      yield return targetEntity = getNearestTarget();

      if (seaMonsterData.roleType == RoleType.Minion) {
         if (targetEntity != null) {
            if (isEnemyWithinRangedAttackDistance() && hasReloaded()) {
               attackTarget();
            } else {
               isMinionPlanning = false;
            }
         } else {
            isMinionPlanning = false;
         }
      } else {
         if (targetEntity != null && isEnemyWithinTerritory()) {
            // If there is a target, calculate if
            if (isEnemyWithinMoveDistance()) {
               if (isEnemyWithinRangedAttackDistance() && seaMonsterData.roleType != RoleType.Master) {
                  if (hasReloaded()) {
                     attackTarget();
                  } else {
                     yield return new WaitForSeconds(seaMonsterData.attackFrequency);
                     planNextMove();
                  }
               } else {
                  yield return new WaitForSeconds(seaMonsterData.moveFrequency * .5f);
                  if (targetEntity != null) {
                     findAndSetPath_Asynchronous(targetEntity.transform.position + Random.insideUnitCircle.ToVector3() * 0.5f);
                     monsterBehavior = MonsterBehavior.MoveToTarget;
                  } else {
                     findAndSetPath_Asynchronous(transform.position + Random.insideUnitCircle.ToVector3() * 0.5f);
                     monsterBehavior = MonsterBehavior.MoveAround;
                  }
               }
            } else {
               // Enemy is too far, keep moving around
               yield return new WaitForSeconds(seaMonsterData.moveFrequency);
               findAndSetPath_Asynchronous(transform.position + Random.insideUnitCircle.ToVector3() * 0.5f);
               monsterBehavior = MonsterBehavior.MoveAround;
            }
         } else {
            // No enemy, keep moving around
            yield return new WaitForSeconds(seaMonsterData.moveFrequency);
            findAndSetPath_Asynchronous(transform.position + Random.insideUnitCircle.ToVector3() * 0.5f);
            monsterBehavior = MonsterBehavior.MoveAround;
         }
      }
   }

   private void handleAnimations () {
      if (hasDied) {
         _simpleAnim.playAnimation(Anim.Type.Death_East);
         return;
      }

      if (isAttacking) {
         switch (this.facing) {
            case Direction.North:
               _simpleAnim.playAnimation(Anim.Type.Attack_North);
               break;
            case Direction.South:
               _simpleAnim.playAnimation(Anim.Type.Attack_South);
               break;
            default:
               _simpleAnim.playAnimation(Anim.Type.Attack_East);
               break;
         }
      } else {
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

   private void handleFaceDirection () {
      if (getVelocity().magnitude < MIN_MOVEMENT_MAGNITUDE && targetEntity != null) {
         if (Vector2.Distance(transform.position, _spawnPos) > seaMonsterData.territoryRadius) {
            // Forget target because it is beyond territory radius
            targetEntity = null;
            isEngaging = false;
         }

         if (isEngaging) {
            // Look at target entity
            this.facing = (Direction) SeaMonsterUtility.getDirectionToFace(targetEntity, transform.position);
         }
      } else {
         if (getVelocity().magnitude > MIN_MOVEMENT_MAGNITUDE) {
            // Update our facing direction
            faceVelocityDirection();
         }
      }
   }

   protected virtual void killUnit () {
      hasDied = true;
      handleAnimations();

      _clickableBox.gameObject.SetActive(false);
      seaMonsterBars.gameObject.SetActive(false);

      // Reduces the life of the parent entity if there is one
      if (seaMonsterData.roleType == RoleType.Minion && seaMonsterParentEntity != null) {
         seaMonsterParentEntity.currentHealth -= 1;
      }

      if (seaMonsterData.roleType == RoleType.Master) {
         foreach (SeaMonsterEntity childEntity in seaMonsterChildrenList) {
            NetworkServer.Destroy(childEntity.gameObject);
         }
      }

      // Drops the treasure bag if this entity can spawn one
      if (seaMonsterData.shouldDropTreasure) {
         spawnChest();
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
   protected void spawnChest () {
      Instance currentInstance = InstanceManager.self.getInstance(this.instanceId);
      TreasureManager.self.createSeaMonsterChest(currentInstance, transform.position, seaMonsterData.seaMonsterType);
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
      this.facing = (Direction) SeaMonsterUtility.getDirectionToFace(attacker, transform.position);

      int accuracy = Random.Range(1, 4);
      Vector2 targetLoc = new Vector2(0, 0);
      if (accuracy == 1) {
         targetLoc = spot + (attacker.getVelocity());
      } else {
         targetLoc = spot;
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
      fireAtSpot(targetLoc, abilityId, attackDelay, launchDelay, spawnPosition);

      isEngaging = true;

      monsterBehavior = MonsterBehavior.Idle;

      planNextMove();
   }

   private void findAndSetPath_Asynchronous (Vector3 targetPosition) {
      if (!_seeker.IsDone()) {
         _seeker.CancelCurrentPathRequest();
      }
      _seeker.StartPath(transform.position, targetPosition);
   }

   private void setPath_Asynchronous (Path newPath) {
      _currentPath = newPath.vectorPath;
      _currentPathIndex = 0;
      _seeker.CancelCurrentPathRequest(true);

      commandMinions();
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
         Gizmos.DrawWireSphere(transform.position, sizex);

         // Draws the range of the monsters search radius
         Gizmos.color = Color.red;
         sizex = seaMonsterData.maxMeleeDistanceGap;
         Gizmos.DrawWireSphere(transform.position, sizex);

         // Draws the range of the monsters follow radius
         Gizmos.color = Color.blue;
         sizex = seaMonsterData.maxDistanceGap;
         Gizmos.DrawWireSphere(transform.position, sizex);

         // Draws the range of the monsters attack radius
         Gizmos.color = Color.black;
         sizex = seaMonsterData.maxProjectileDistanceGap;
         Gizmos.DrawWireSphere(transform.position, sizex);
      }
   }

   protected bool isEnemyWithinMoveDistance () {
      if (targetEntity != null) {
         float distanceGap = Vector2.Distance(targetEntity.transform.position, transform.position);
         if (distanceGap < seaMonsterData.maxDistanceGap) {
            return true;
         }
      }
      return false;
   }

   protected bool isEnemyWithinTerritory () {
      float distanceGap = Vector2.Distance(_spawnPos, transform.position);
      if (distanceGap < seaMonsterData.territoryRadius) {
         return true;
      }
      return false;
   }

   protected bool isEnemyWithinRangedAttackDistance () {
      if (targetEntity != null) {
         float distanceGap = Vector2.Distance(targetEntity.transform.position, transform.position);
         if (distanceGap < seaMonsterData.maxProjectileDistanceGap) {
            return true;
         }
      }
      return false;
   }

   protected bool isEnemyWithinMeleeAttackDistance () {
      if (targetEntity != null) {
         float distanceGap = Vector2.Distance(targetEntity.transform.position, transform.position);
         if (distanceGap < seaMonsterData.maxMeleeDistanceGap) {
            return true;
         }
      }
      return false;
   }

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

   // Keeps reference to the recent coroutine so that it can be manually stopped
   private Coroutine _analysisCoroutine = null;

   #endregion
}
