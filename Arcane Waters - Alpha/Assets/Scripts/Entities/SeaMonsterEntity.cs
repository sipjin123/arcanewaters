using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaMonsterEntity : SeaEntity
{
   #region Public Variables

   // Incase this unit has projectile attack, this variable determines if the target is near enough
   public bool withinProjectileDistance = false;

   // Determines if this monster is engaging a ship
   public bool isEngaging = false;

   // Current target entity
   public NetEntity targetEntity;

   // The Name of the NPC Seamonster
   [SyncVar]
   public string monsterName;

   // The Route that this Bot should follow
   public Route route;

   // The current waypoint
   public Waypoint waypoint;

   // Animator
   public Animator animator;

   // A flag to determine if the object has died
   public bool hasDied = false;

   // The unique data for each seamonster
   public SeaMonsterEntityData seaMonsterData;

   // The minimum magnitude to determine the movement of the unit
   public const float MIN_MOVEMENT_MAGNITUDE = .05f;

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

   protected override void Start () {
      base.Start();

      // Note our spawn position
      _spawnPos = this.transform.position;

      // Sometimes we want to generate random waypoints
      InvokeRepeating("handleAutoMove", 1f, seaMonsterData.moveFrequency);

      // Check if we can shoot at any of our attackers
      InvokeRepeating("checkForAttackers", 2f, seaMonsterData.attackFrequency);

      if (seaMonsterData.isAggressive) {
         // Check if there are players to attack nearby
         InvokeRepeating("checkForTargets", 1f, seaMonsterData.findTargetsFrequency);
      }
   }

   protected override void Update () {
      base.Update();

      animator.SetFloat("facingF", (float) this.facing);

      // If we're dead and have finished sinking, remove the ship
      if (isServer && isDead() && spritesContainer.transform.localPosition.y < -.25f) {
         InstanceManager.self.removeEntityFromInstance(this);

         // Destroy the object
         NetworkServer.Destroy(this.gameObject);
      }

      if (hasDied == false && isDead()) {
         killUnit();
      }

      if (Util.netTime() > _attackStartAnimateTime && !_hasAttackAnimTriggered) {
         animator.SetBool("attacking", true);
         _hasAttackAnimTriggered = true;
         _attackEndAnimateTime = Time.time + .2f;
      } else {
         if (animator.GetBool("attacking") && (Util.netTime() > _attackEndAnimateTime || getVelocity().magnitude > MIN_MOVEMENT_MAGNITUDE)) {
            animator.SetBool("attacking", false);
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
      if (this.waypoint == null || Vector2.Distance(this.transform.position, waypoint.transform.position) < .08f) {
         return;
      }

      // Move towards our current waypoint
      Vector2 waypointDirection = this.waypoint.transform.position - this.transform.position;
      waypointDirection = waypointDirection.normalized;
      _body.AddForce(waypointDirection.normalized * getMoveSpeed());

      // Make note of the time
      _lastMoveChangeTime = Time.time;
   }

   #endregion

   #region Invoke Functions

   protected virtual void handleAutoMove () {
      if (!seaMonsterData.autoMove || !isServer) {
         return;
      }

      // Remove our current waypoint
      if (this.waypoint != null) {
         Destroy(this.waypoint.gameObject);
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
            if (seaMonsterData.isMelee) {
               meleeAtSpot(spot, seaMonsterData.attackType);
            } else {
               launchProjectile(spot, attacker, seaMonsterData.attackType, .2f, .4f);
            }
            return;
         }
      }
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
            waypoint = null;
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
         if (waypoint == null) {
            waypoint = route.getClosest(this.transform.position);
         }

         // Check if we're close enough to update our waypoint
         if (Vector2.Distance(this.transform.position, waypoint.transform.position) < .16f) {
            int index = waypoints.IndexOf(waypoint);
            index++;
            index %= waypoints.Count;
            this.waypoint = waypoints[index];
         }
      }
   }

   protected void setWaypoint (Transform target) {
      if (target == null) {
         // Pick a new spot around our spawn position
         Vector2 newSpot = _spawnPos + new Vector2(Random.Range(-.5f, .5f), Random.Range(-.5f, .5f));
         Waypoint newWaypoint = Instantiate(PrefabsManager.self.waypointPrefab);
         newWaypoint.transform.position = newSpot;
         this.waypoint = newWaypoint;
      } else {
         // Pick a new spot around target object position
         Vector2 newSpot1 = targetEntity.transform.position;
         Waypoint newWaypoint1 = Instantiate(PrefabsManager.self.waypointPrefab);
         newWaypoint1.transform.position = newSpot1;
         this.waypoint = newWaypoint1;
      }
   }

   protected virtual void killUnit () {
      hasDied = true;
      animator.Play("Die");

      if (shouldDropTreasure()) {
         spawnChest();
      }
   }

   [Server]
   protected void spawnChest () {
      Instance currentInstance = InstanceManager.self.getInstance(this.instanceId);
      TreasureManager.self.createSeaTreasure(currentInstance, transform.position, seaMonsterData.seaMonsterType);
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

   protected virtual bool shouldDropTreasure () {
      return true;
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

   // The position we spawned at
   protected Vector2 _spawnPos;

   #endregion
}
