using System.Collections;
using System.Collections.Generic;
using AStar;
using Mirror;
using UnityEngine;

public class SeaMonsterEntity : SeaEntity
{
   #region Public Variables

   // List of children dependencies
   public List<SeaMonsterEntity> seaMonsterChildrenList;

   // The parent entity of this sea monster
   public SeaMonsterEntity seaMonsterParentEntity;

   // Determines the current behavior of the monster
   public MonsterBehavior monsterBehavior;

   // Holds the reference to the grid generator of the area
   public AStarGrid gridReference;

   // Holds the script responsible for generating the route using the grid data
   public Pathfinding pathFindingReference;

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
   public Enemy.Type monsterType = 0;

   // Determines the location of this unit in relation to its spawn point
   public Vector2 distanceFromSpawnPoint;

   // Holds the info of the seamonster health Bars
   public SeaMonsterBars seaMonsterBars;

   // The time an attack animation plays
   public const float ATTACK_DURATION = .3f;

   // Snaps the minion to its parents position while moving
   public bool snapToParent = false;

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
      ripplesContainer.GetComponent<SpriteSwap>().newTexture = seaMonsterData.defaultRippleSprite;
      
      ripplesContainer.transform.localScale = new Vector3(seaMonsterData.rippleScaleOverride, seaMonsterData.rippleScaleOverride, seaMonsterData.rippleScaleOverride);
      spritesContainer.transform.localScale = new Vector3(seaMonsterData.scaleOverride, seaMonsterData.scaleOverride, seaMonsterData.scaleOverride);
      spritesContainer.transform.GetChild(0).localScale = new Vector3(seaMonsterData.outlineScaleOverride, seaMonsterData.outlineScaleOverride, seaMonsterData.outlineScaleOverride);

      _simpleAnim = spritesContainer.GetComponent<SimpleAnimation>();
      _simpleAnim.group = seaMonsterData.animGroup;
      _simpleAnim.frameLengthOverride = seaMonsterData.animationSpeedOverride;
      _simpleAnim.enabled = true;

      reloadDelay = seaMonsterData.reloadDelay;
      currentHealth = seaMonsterData.maxHealth;
      maxHealth = seaMonsterData.maxHealth;
      invulnerable = seaMonsterData.isInvulnerable;

      if (seaMonsterData.isInvulnerable) {
         seaMonsterBars.gameObject.SetActive(false);
         spritesContainer.transform.GetChild(0).gameObject.SetActive(false);
      }

      if (variety != 0 && seaMonsterData.secondarySprite != null) {
         spritesContainer.GetComponent<SpriteRenderer>().sprite = seaMonsterData.secondarySprite;
      } else {
         spritesContainer.GetComponent<SpriteRenderer>().sprite = seaMonsterData.defaultSprite;
      }
   }

   protected override void Start () {
      base.Start();

      // Initializes the data from the scriptable object
      initData(EnemyManager.self.seaMonsterDataList.Find(_ => _.seaMonsterType == monsterType).seaMonsterData);

      if (isServer && seaMonsterData.roleType != RoleType.Minion) {
         gridReference.displayGrid(transform.position, this.areaType);
         planNextMove();
      }

      // Note our spawn position
      _spawnPos = this.transform.position;
   }

   protected override void Update () {
      base.Update();

      // If we're dead and have finished sinking, remove the ship
      if (isServer && isDead() && spritesContainer.transform.localPosition.y < -.25f) {
         InstanceManager.self.removeEntityFromInstance(this);

         // Destroy the object
         NetworkServer.Destroy(this.gameObject);
      }

      // Alters the simple animation data
      handleAnimations();

      // Kills the unit
      if (hasDied == false && isDead()) {
         killUnit();
      }

      // Handles attack animations
      if (Util.netTime() > _attackStartAnimateTime && !_hasAttackAnimTriggered) {
         isAttacking = true;
         _hasAttackAnimTriggered = true;
         _attackEndAnimateTime = Util.netTime() + ATTACK_DURATION;
      } else {
         if (isAttacking && (Util.netTime() > _attackEndAnimateTime)) {
            isAttacking = false;
         }
      }
   }

   protected override void FixedUpdate () {
      base.FixedUpdate();

      // Only the server updates waypoints and movement forces
      if (!isServer || isDead()) {
         return;
      }

      // Only change our movement if enough time has passed
      if (Time.time - _lastMoveChangeTime < MOVE_CHANGE_INTERVAL) {
         return;
      }

      // Sets up where this unit is facing
      handleFaceDirection();

      // If this entity is a Minion, snap to its parent
      if (seaMonsterData.roleType == RoleType.Minion && snapToParent) {
         if (seaMonsterParentEntity != null) {
            Vector2 targetLocation = SeaMonsterUtility.GetRandomPositionAroundPosition(seaMonsterParentEntity.transform.position, distanceFromSpawnPoint);
            Vector2 waypointDirection = targetLocation - (Vector2) this.transform.position;
            waypointDirection = waypointDirection.normalized;

            // Teleports the Minions if too far away from Parent
            if (Vector3.Distance(transform.position, seaMonsterParentEntity.transform.position) > 1) {
               transform.position = targetLocation;
            }

            _body.AddForce(waypointDirection.normalized * (getMoveSpeed()/2));

            float distanceToWaypoint = Vector2.Distance(targetLocation, this.transform.position);
            if (distanceToWaypoint < .05f) {
               planNextMove();
               snapToParent = false;
            }
         }
         return;
      }

      // Process movement towards route
      if (monsterBehavior == MonsterBehavior.MoveAround || monsterBehavior == MonsterBehavior.MoveToTarget) {
         if (seaMonsterData.roleType != RoleType.Minion && (isEnemyWithinAttackDistance() || (!isEnemyWithinTerritory() && monsterBehavior == MonsterBehavior.MoveToTarget))) {
            stopMoving();
         }

         if (_waypointList.Count > 0) {
            // Move towards our current waypoint
            Vector2 waypointDirection = _waypointList[0].transform.position - this.transform.position;
            waypointDirection = waypointDirection.normalized;

            _body.AddForce(waypointDirection.normalized * getMoveSpeed());
            _lastMoveChangeTime = Time.time;

            // Clears a node as the unit passes by
            float distanceToWaypoint = Vector2.Distance(_waypointList[0].transform.position, this.transform.position);
            if (distanceToWaypoint < .1f) {
               commandMinions();
               Destroy(_waypointList[0].gameObject);
               _waypointList.RemoveAt(0);
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

   public override void noteAttacker (NetEntity entity) {
      base.noteAttacker(entity);
      if (seaMonsterParentEntity != null) {
         seaMonsterParentEntity.noteAttacker(entity);
      }
   }

   protected void scanTargetsInArea () {
      if (isDead() || !isServer || !seaMonsterData.isAggressive) {
         return;
      }

      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, seaMonsterData.detectRadius);
      if (hits.Length > 0) {
         foreach (Collider2D hit in hits) {
            if (hit.GetComponent<PlayerShipEntity>() != null) {
               if (!_attackers.Contains(hit.GetComponent<NetEntity>())) {
                  noteAttacker(hit.GetComponent<PlayerShipEntity>());
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
      foreach (SeaEntity attacker in _attackers) {
         if (attacker == null || attacker.isDead() || attacker == this || attacker.GetComponent<SeaMonsterEntity>() != null) {
            continue;
         }

         // Check where the attacker currently is
         Vector2 spot = attacker.transform.position;

         if (seaMonsterData.attackType != Attack.Type.None) {
            if (seaMonsterData.isMelee) {
               if (Vector2.Distance(transform.position, spot) < seaMonsterData.maxProjectileDistanceGap) {
                  meleeAtSpot(spot, seaMonsterData.attackType);
                  monsterBehavior = MonsterBehavior.AttackTarget;
                  planNextMove();
               }
            } else {
               launchProjectile(spot, attacker, seaMonsterData.attackType, .2f, .4f);
               monsterBehavior = MonsterBehavior.AttackTarget;
            }
         } else {
            isEngaging = true;
            targetEntity = attacker;
         }
         return;
      }
      planNextMove();
   }

   protected NetEntity getNearestTarget () {
      NetEntity nearestEntity = null;
      foreach (NetEntity entity in _attackers) {
         if (entity == null) {
            continue;
         }

         if (nearestEntity == null) {
            nearestEntity = entity;
         }

         float oldDistanceGap = Vector2.Distance(nearestEntity.transform.position, transform.position);
         float newDistanceGap = Vector2.Distance(entity.transform.position, transform.position);

         if (newDistanceGap < oldDistanceGap) {
            nearestEntity = entity;
         }
      }

      return nearestEntity;
   }

   #endregion

   #region Minion / Master Related Functions

   public void moveToParentDestination (Transform newTarget) {
      if (!isServer) {
         return;
      }

      if (seaMonsterParentEntity != null && seaMonsterData != null) {
         if (seaMonsterData.roleType == RoleType.Minion) {
            setWaypoint(newTarget);
         }
      }
   }

   private void commandMinions () {
      if (seaMonsterData.roleType == RoleType.Master && seaMonsterChildrenList.Count > 0 && _waypointList.Count > 0) {
         foreach (SeaMonsterEntity childEntity in seaMonsterChildrenList) {
            if (!childEntity.isDead()) {
               childEntity.stopMoving();
               childEntity.moveToParentDestination(_waypointList[_waypointList.Count - 1].transform);
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
         while (_waypointList.Count > 0) {
            Destroy(_waypointList[0].gameObject);
            _waypointList.RemoveAt(0);
         }

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
      // Checks if there are enemies nearby
      scanTargetsInArea();

      // Gets the nearest target if there is
      targetEntity = getNearestTarget();

      if (seaMonsterData.roleType == RoleType.Minion && targetEntity != null) {
         if (isEnemyWithinAttackDistance() && hasReloaded()) {
            attackTarget();
         }
      } else {
         if (targetEntity != null && isEnemyWithinTerritory()) {
            // If there is a target, calculate if
            if (isEnemyWithinMoveDistance()) {
               if (isEnemyWithinAttackDistance() && seaMonsterData.roleType != RoleType.Master) {
                  if (hasReloaded()) {
                     attackTarget();
                  } else {
                     yield return new WaitForSeconds(seaMonsterData.attackFrequency);
                     planNextMove();
                  }
               } else {
                  yield return new WaitForSeconds(seaMonsterData.moveFrequency * .5f);
                  setWaypoint(targetEntity.transform);
               }
            } else {
               // Enemy is too far, keep moving around
               yield return new WaitForSeconds(seaMonsterData.moveFrequency);
               setWaypoint(null);
            }
         } else {
            // No enemy, keep moving around
            yield return new WaitForSeconds(seaMonsterData.moveFrequency);
            setWaypoint(null);
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

   private void setWaypoint (Transform target) {
      if (seaMonsterData.roleType == RoleType.Minion) {
         if (target != null) {
            snapToParent = true;
         } else {
            snapToParent = false;
         }
      } else {
         if (target == null) {
            // Pick a new spot around our spawn position
            float moveDistance = .5f;
            Vector2 newTargetPos = _spawnPos + new Vector2(Random.Range(-moveDistance, moveDistance), Random.Range(-moveDistance, moveDistance));

            _waypointList = new List<Waypoint>();
            List<ANode> gridPath = pathFindingReference.findPathNowInit(transform.position, newTargetPos);
            if (gridPath == null) {
               // Invalid Path, attempt again
               planNextMove();
               return;
            }

            // Register Route
            foreach (ANode node in gridPath) {
               Waypoint newWaypointPath = Instantiate(PrefabsManager.self.waypointPrefab);
               newWaypointPath.transform.position = node.vPosition;
               _waypointList.Add(newWaypointPath);
            }

            monsterBehavior = MonsterBehavior.MoveAround;
         } else {
            // Pick a new spot around the target opponent
            Vector2 newTargetPos = targetEntity.transform.position;

            // Register Route
            _waypointList = new List<Waypoint>();
            foreach (ANode node in pathFindingReference.findPathNowInit(transform.position, newTargetPos)) {
               Waypoint newWaypointPath = Instantiate(PrefabsManager.self.waypointPrefab);
               newWaypointPath.transform.position = node.vPosition;
               _waypointList.Add(newWaypointPath);
            }

            monsterBehavior = MonsterBehavior.MoveToTarget;
         }
         // If has minions, command them to follow
         commandMinions();
      }
   }

   protected virtual void killUnit () {
      hasDied = true;
      handleAnimations();

      if (seaMonsterData.roleType == RoleType.Minion && seaMonsterParentEntity != null) {
         seaMonsterParentEntity.currentHealth -= 1;
      }

      if (seaMonsterData.shouldDropTreasure) {
         spawnChest();
      }
   }

   [Server]
   protected void spawnChest () {
      Instance currentInstance = InstanceManager.self.getInstance(this.instanceId);
      TreasureManager.self.createMonsterChest(currentInstance, transform.position, seaMonsterData.seaMonsterType, false);
   }

   [Server]
   protected void faceVelocityDirection () {
      Direction newFacingDirection = DirectionUtil.getDirectionForVelocity(_body.velocity);
      if (newFacingDirection != this.facing) {
         this.facing = newFacingDirection;
      }
   }

   [Server]
   protected void launchProjectile (Vector2 spot, SeaEntity attacker, Attack.Type attackType, float attackDelay, float launchDelay) {
      this.facing = (Direction) SeaMonsterUtility.getDirectionToFace(attacker,transform.position);

      int accuracy = Random.Range(1, 4);
      Vector2 targetLoc = new Vector2(0, 0);
      if (accuracy == 1) {
         targetLoc = spot + (attacker.getVelocity());
      } else {
         targetLoc = spot;
      }

      fireAtSpot(targetLoc, attackType, attackDelay, launchDelay);

      targetEntity = attacker;
      isEngaging = true;

      monsterBehavior = MonsterBehavior.Idle;
      planNextMove();
   }

   #region Utilities

   private void OnDrawGizmos () {
      if (seaMonsterData != null) {
         if (!seaMonsterData.showDebugGizmo) {
            return;
         }

         // Draws the range of the monster territory
         Gizmos.color = Color.yellow;
         float sizex = seaMonsterData.territoryRadius;
         Gizmos.DrawWireSphere(_spawnPos, sizex);

         // Draws the range of the monsters search radius
         Gizmos.color = Color.red;
         sizex = seaMonsterData.detectRadius;
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

   protected bool isEnemyWithinAttackDistance () {
      if (targetEntity != null) {
         float distanceGap = Vector2.Distance(targetEntity.transform.position, transform.position);
         if (distanceGap < seaMonsterData.maxProjectileDistanceGap) {
            return true;
         }
      }
      return false;
   }

   #endregion

   #region Private Variables

   // The handling for sprite animation
   protected SimpleAnimation _simpleAnim;

   // The position we spawned at
   protected Vector2 _spawnPos;

   // The current waypoint List
   protected List<Waypoint> _waypointList;

   // Keeps reference to the recent coroutine so that it can be manually stopped
   private Coroutine _analysisCoroutine = null;

   #endregion
}
