using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class SeaMonsterEntity : SeaEntity
{
   #region Public Variables

   // List of children dependencies
   public List<SeaMonsterEntity> seaMonsterChildrenList;

   // The parent entity of this sea monster
   public SeaMonsterEntity seaMonsterParentEntity;

   // Incase this unit has projectile attack, this variable determines if the target is near enough
   public bool withinProjectileDistance = false;

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

   // The Route that this Bot should follow
   public Route route;

   // Randomizes behavior before moving
   public float randomizedTimer = 1;

   [SyncVar]
   // Determines the variety of the monster sprite if there is one
   public int variety = 0;

   [SyncVar]
   // Determines the monster type index of this unit
   public int monsterType = 0;

   // Determines the location of this unit in relation to its spawn point
   public Vector2 locationSetup;

   // Holds the info of the seamonster health Bars
   public SeaMonsterBars seaMonsterBars;

   // Determines if the monster is approaching a target ship
   public bool isApproachingTarget = true;

   // The time an attack animation plays
   public const float ATTACK_DURATION = .3f;

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

      currentHealth = seaMonsterData.maxHealth;
      maxHealth = seaMonsterData.maxHealth;
      invulnerable = seaMonsterData.isInvulnerable;

      if (seaMonsterData.isInvulnerable) {
         seaMonsterBars.gameObject.SetActive(false);

         spritesContainer.transform.GetChild(0).gameObject.SetActive(false);
      }
   }

   protected override void Start () {
      base.Start();
      if (!isServer) {
         initData(EnemyManager.self.SeaMonsterEntityData.Find(_ => _.seaMonsterType == (Enemy.Type)monsterType).seaMonsterData);
      }

      if (variety != 0 && seaMonsterData.secondarySprite != null) {
         spritesContainer.GetComponent<SpriteRenderer>().sprite = seaMonsterData.secondarySprite;
      } else {
         spritesContainer.GetComponent<SpriteRenderer>().sprite = seaMonsterData.defaultSprite;
      }

      // Note our spawn position
      _spawnPos = this.transform.position;

      // Check if we can shoot at any of our attackers
      InvokeRepeating("checkForAttackers", 2f, seaMonsterData.attackFrequency);

      if (seaMonsterData.isAggressive) {
         // Check if there are players to attack nearby
         InvokeRepeating("checkForTargets", 1f, seaMonsterData.findTargetsFrequency);
      }

      if (seaMonsterData.seaMonsterDependencyType == RoleType.Minion) {
         // Calls functions that randomizes and calls the coroutine that handles movement
         initializeBehavior();
      } else {
         // Sometimes we want to generate random waypoints
         InvokeRepeating("handleAutoMove", 1f, seaMonsterData.moveFrequency);
      }
   }

   protected override void Update () {
      base.Update();

      // If we're dead and have finished sinking, remove the ship
      if (isServer && isDead() && spritesContainer.transform.localPosition.y < -.25f) {
         InstanceManager.self.removeEntityFromInstance(this);

         // Destroy the object
         NetworkServer.Destroy(this.gameObject);
      }

      if (hasDied == false && isDead()) {
         killUnit();
      }

      handleAnimations();

      if (Util.netTime() > _attackStartAnimateTime && !_hasAttackAnimTriggered) {
         isAttacking = true;
         _hasAttackAnimTriggered = true;
         _attackEndAnimateTime = Util.netTime() + ATTACK_DURATION;
      } else {
         if (isAttacking && (Util.netTime() > _attackEndAnimateTime)) {//|| getVelocity().magnitude > MIN_MOVEMENT_MAGNITUDE)) {
            isAttacking = false;
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

      handleFaceDirection();

      handleWaypoints();

      // If we don't have a waypoint, we're done
      if (_waypoint == null || Vector2.Distance(this.transform.position, _waypoint.transform.position) < .08f) {
         if (seaMonsterData.seaMonsterDependencyType == RoleType.Master && isApproachingTarget) {
            isApproachingTarget = false;
            foreach (SeaMonsterEntity childEntities in seaMonsterChildrenList) {
               childEntities.initializeBehavior();
            }
         }
         return;
      }

      // Move towards our current waypoint
      Vector2 waypointDirection = _waypoint.transform.position - this.transform.position;
      waypointDirection = waypointDirection.normalized;
      _body.AddForce(waypointDirection.normalized * getMoveSpeed());

      // Make note of the time
      _lastMoveChangeTime = Time.time;
   }

   #endregion

   #region Invoke Functions

   protected virtual void handleAutoMove () {
      if (!seaMonsterData.autoMove || !isServer || isAttacking) {
         return;
      }

      // Remove our current waypoint
      if (_waypoint != null) {
         Destroy(_waypoint.gameObject);
      }

      // This handles the waypoint spawn toward the target enemy
      if (canMoveTowardEnemy()) {
         setWaypoint(targetEntity.transform);
         return;
      } else {
         // Forget about target
         targetEntity = null;
         isEngaging = false;
      }

      setWaypoint(null);
   }

   protected void checkForTargets () {
      if (isDead() || !isServer) {
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

   public override void noteAttacker (NetEntity entity) {
      base.noteAttacker(entity);
      if (seaMonsterParentEntity != null) {
         seaMonsterParentEntity.noteAttacker(entity);
      }
   }

   protected void checkForAttackers () {
      if (isDead() || !isServer) {
         return;
      }

      // If we haven't reloaded, we can't attack
      if (!hasReloaded()) {
         return;
      }

      // Check if any of our attackers are within range
      foreach (SeaEntity attacker in _attackers) {
         if (attacker == null || attacker.isDead() || attacker == this || attacker.GetComponent<SeaMonsterEntity>() != null) {
            continue;
         }

         // Check where the attacker currently is
         Vector2 spot = attacker.transform.position;

         // If the requested spot is not in the allowed area, reject the request
         if (leftAttackBox.OverlapPoint(spot) || rightAttackBox.OverlapPoint(spot)) {
            if (seaMonsterData.attackType != Attack.Type.None) {
               if (seaMonsterData.isMelee) {
                  if (Vector2.Distance(transform.position, spot) < seaMonsterData.maxProjectileDistanceGap) {
                     meleeAtSpot(spot, seaMonsterData.attackType);
                  }
               } else {
                  launchProjectile(spot, attacker, seaMonsterData.attackType, .2f, .4f);
               }
            } else {
               isEngaging = true;
               targetEntity = attacker;
            }
            return;
         }
      }
   }

   #endregion

   #region MINION SPECIFIC FUNCTIONS

   [Server]
   public void initializeBehavior () {
      if (!isDead()) {
         randomizedTimer = Random.Range(1.0f, 2.5f);
         _movementCoroutine = StartCoroutine(CO_HandleAutoMove());
      }
   }

   public void moveToParentDestination (Vector2 newPos) {
      StopCoroutine(_movementCoroutine);
      float delayTime = .1f;

      Vector2 areaAroundParent = getAreaAroundParent();
      if (Vector2.Distance(transform.position, areaAroundParent) > 1f) {
         transform.position = areaAroundParent;
      }

      StartCoroutine(CO_HandleBossMovement(newPos, delayTime));
   }

   private Vector2 getAreaAroundParent () {
      float randomizedX = (locationSetup.x != 0 && locationSetup.y != 0) ? Random.Range(.4f, .6f) : Random.Range(.6f, .8f);
      float randomizedY = (locationSetup.x != 0 && locationSetup.y != 0) ? Random.Range(.4f, .6f) : Random.Range(.6f, .8f);

      randomizedX *= locationSetup.x;
      randomizedY *= locationSetup.y;

      Vector2 newSpot = new Vector2(seaMonsterParentEntity.transform.position.x, seaMonsterParentEntity.transform.position.y) + new Vector2(randomizedX, randomizedY);
      return newSpot;
   }

   private IEnumerator CO_HandleBossMovement (Vector2 newPos, float delay) {
      yield return new WaitForSeconds(delay);

      if (!seaMonsterData.autoMove || !isServer) {
         yield return null;
      }

      // Remove our current waypoint
      if (_waypoint != null) {
         Destroy(_waypoint.gameObject);
      }

      float randomizedX = Random.Range(.4f, .8f);
      float randomizedY = Random.Range(.4f, .8f);

      randomizedX *= locationSetup.x;
      randomizedY *= locationSetup.y;
      _cachedCoordinates = new Vector2(randomizedX, randomizedY);

      // Pick a new spot around our spawn position
      Vector2 newLoc = new Vector2(newPos.x + randomizedX, newPos.y + randomizedY);

      Waypoint newWaypoint = Instantiate(PrefabsManager.self.waypointPrefab);
      newWaypoint.transform.position = newLoc;
      _waypoint = newWaypoint;
   }

   public IEnumerator CO_HandleAutoMove () {
      if (!seaMonsterData.autoMove || !isServer) {
         yield return null;
      }

      // Remove our current waypoint
      if (_waypoint != null) {
         Destroy(_waypoint.gameObject);
      }

      Vector2 areaAroundParent = getAreaAroundParent();

      // Pick a new spot around the Parent Entity if this unit is a minion
      if (seaMonsterParentEntity != null) {
         Vector2 newSpot = areaAroundParent;

         Waypoint newWaypoint = Instantiate(PrefabsManager.self.waypointPrefab);
         newWaypoint.transform.position = newSpot;
         _waypoint = newWaypoint;
      }

      yield return new WaitForSeconds(randomizedTimer);

      StopCoroutine(_movementCoroutine);
      initializeBehavior();
   }

   #endregion

   protected void handleFaceDirection () {
      if (getVelocity().magnitude < MIN_MOVEMENT_MAGNITUDE && targetEntity != null) {
         float distanceGap = Vector2.Distance(targetEntity.transform.position, transform.position);
         if (distanceGap < seaMonsterData.maxProjectileDistanceGap) {
            withinProjectileDistance = true;
         } else {
            withinProjectileDistance = false;
         }

         if (Vector2.Distance(transform.position, _spawnPos) > seaMonsterData.territoryRadius) {
            targetEntity = null;
            isEngaging = false;
         }

         if (isEngaging) {
            this.facing = (Direction) lockToTarget(targetEntity);
         }
      } else {
         if (getVelocity().magnitude > MIN_MOVEMENT_MAGNITUDE) {
            // Update our facing direction
            lookAtTarget();
         }
      }
   }

   protected void handleWaypoints () {
      // If we've been assigned a Route, get our waypoint from that
      if (route != null) {
         List<Waypoint> waypoints = route.getWaypoints();

         // If we haven't picked a waypoint yet, start with the first one
         if (_waypoint == null) {
            _waypoint = route.getClosest(this.transform.position);
         }

         // Check if we're close enough to update our waypoint
         if (Vector2.Distance(this.transform.position, _waypoint.transform.position) < .16f) {
            int index = waypoints.IndexOf(_waypoint);
            index++;
            index %= waypoints.Count;
            _waypoint = waypoints[index];
         }
      }
   }

   protected void setWaypoint (Transform target) {
      if (target == null) {
         // Pick a new spot around our spawn position
         Vector2 newSpot = _spawnPos + new Vector2(Random.Range(-.5f, .5f), Random.Range(-.5f, .5f));
         Waypoint newWaypoint = Instantiate(PrefabsManager.self.waypointPrefab);
         newWaypoint.transform.position = newSpot;
         _waypoint = newWaypoint;
      } else {
         // Pick a new spot around target object position
         Vector2 newSpot1 = targetEntity.transform.position;
         Waypoint newWaypoint1 = Instantiate(PrefabsManager.self.waypointPrefab);
         newWaypoint1.transform.position = newSpot1;
         _waypoint = newWaypoint1;
      }

      if (seaMonsterData.seaMonsterDependencyType == RoleType.Master) {
         if (seaMonsterChildrenList.Count > 0) {
            foreach (SeaMonsterEntity childEntity in seaMonsterChildrenList) {
               if (!childEntity.isDead()) {
                  childEntity.moveToParentDestination(_waypoint.transform.position);
               }
            }
         }
      }
   }

   protected virtual void killUnit () {
      hasDied = true;
      handleAnimations();

      if (seaMonsterData.seaMonsterDependencyType == RoleType.Minion) {
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
   protected void lookAtTarget () {
      Direction newFacingDirection = DirectionUtil.getDirectionForVelocity(_body.velocity);
      if (newFacingDirection != this.facing) {
         this.facing = newFacingDirection;
      }
   }

   [Server]
   protected void launchProjectile (Vector2 spot, SeaEntity attacker, Attack.Type attackType, float attackDelay, float launchDelay) {
      if (getVelocity().magnitude > MIN_MOVEMENT_MAGNITUDE) {
         return;
      }
      this.facing = (Direction) lockToTarget(attacker);

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
   }

   protected bool canMoveTowardEnemy () {
      if (targetEntity != null && isEngaging) {
         float distanceGap = Vector2.Distance(targetEntity.transform.position, transform.position);
         if (distanceGap > 1 && distanceGap < seaMonsterData.maxDistanceGap) {
            return true;
         } else if (distanceGap >= seaMonsterData.maxDistanceGap) {
            return false;
         }
      }
      return false;
   }
   
   protected int lockToTarget (NetEntity attacker) {
      int horizontalDirection = 0;
      int verticalDirection = 0;

      float offset = .1f;

      Vector2 spot = attacker.transform.position;
      if (spot.x > transform.position.x + offset) {
         horizontalDirection = (int) Direction.East;
      } else if (spot.x < transform.position.x - offset) {
         horizontalDirection = (int) Direction.West;
      } else {
         horizontalDirection = 0;
      }

      if (spot.y > transform.position.y + offset) {
         verticalDirection = (int) Direction.North;
      } else if (spot.y < transform.position.y - offset) {
         verticalDirection = (int) Direction.South;
      } else {
         verticalDirection = 0;
      }

      int finalDirection = 0;
      if (horizontalDirection == (int) Direction.East) {
         if (verticalDirection == (int) Direction.North) {
            finalDirection = (int) Direction.NorthEast;
         } else if (verticalDirection == (int) Direction.South) {
            finalDirection = (int) Direction.SouthEast;
         }

         if (verticalDirection == 0) {
            finalDirection = (int) Direction.East;
         }
      } else if (horizontalDirection == (int) Direction.West) {
         if (verticalDirection == (int) Direction.North) {
            finalDirection = (int) Direction.NorthWest;
         } else if (verticalDirection == (int) Direction.South) {
            finalDirection = (int) Direction.SouthWest;
         }

         if (verticalDirection == 0) {
            finalDirection = (int) Direction.West;
         }
      } else {
         if (verticalDirection == (int) Direction.North) {
            finalDirection = (int) Direction.North;
         } else if (verticalDirection == (int) Direction.South) {
            finalDirection = (int) Direction.South;
         }
      }

      return finalDirection;
   }

   #region Private Variables

   // The handling for sprite animation
   protected SimpleAnimation _simpleAnim;

   // The position we spawned at
   protected Vector2 _spawnPos;

   // The current waypoint
   protected Waypoint _waypoint;

   // Keeps reference to the recent coroutine so that it can be manually stopped
   private Coroutine _movementCoroutine = null;

   // The target location of this unit
   private Vector3 _cachedCoordinates;

   #endregion
}
