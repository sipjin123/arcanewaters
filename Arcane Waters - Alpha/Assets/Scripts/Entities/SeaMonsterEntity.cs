﻿using UnityEngine;
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

   // When set to true, we pick random waypoints
   public bool autoMove = false;

   // Animator
   public Animator animator;

   // A flag to determine if the object has died
   public bool hasDied = false;

   // The max gap distance between target and this unity
   public const float DISTANCE_GAP = 3;

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

   protected override void Update () {
      base.Update();

      if (hasDied == false && isDead()) {
         hasDied = true;
         animator.Play("Die");
         spawnChest();
      }

      if (Util.netTime() > _attackStartAnimateTime && !_hasAttackAnimTriggered) {
         animator.SetBool("attacking", true);
         _hasAttackAnimTriggered = true;
         _attackEndAnimateTime = Time.time + .2f;
      } else {
         if (Util.netTime() > _attackEndAnimateTime || getVelocity().magnitude > .05f) {
            animator.SetBool("attacking", false);
         }
      }
   }

   [Server]
   protected void spawnChest () {
      Instance currentInstance = InstanceManager.self.getInstance(this.instanceId);
      TreasureManager.self.createSeaTreasure(currentInstance, transform.position);
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
      if (getVelocity().magnitude > .05f) {
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
         if (distanceGap > 1 && distanceGap < DISTANCE_GAP) {
            return true;
         } else if (distanceGap >= DISTANCE_GAP) {
            return false;
         }
      }
      return false;
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

   // The radius that defines how far the monster will chase before it retreats
   protected float _territoryRadius = 4.5f;

#pragma warning disable 1234
   // The radius that defines how near the player ships are before this unit chases it
   protected float _detectRadius = 4;
#pragma warning restore 1234

   #endregion
}
