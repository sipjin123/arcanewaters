﻿using UnityEngine;
using System.Collections.Generic;
using Mirror;

public class HorrorEntity : SeaMonsterEntity
{
   #region Public Variables

   // The total tentacles left before this unit dies
   [SyncVar]
   public int tentaclesLeft;

   // List of tentacle entities
   public List<TentacleEntity> tentacleList;

   // Checks nearest target
   public NetEntity nearestShipTarget;

   // Determines if the monster is approaching a target ship
   public bool approachShip;

   #endregion

   protected override void Start () {
      base.Start();

      // Note our spawn position
      _spawnPos = this.transform.position;

      // Set our name
      this.nameText.text = "[" + getNameForFaction() + "]";
      NPC.setNameColor(nameText, npcType);

      // Sometimes we want to generate random waypoints
      InvokeRepeating("handleAutoMove", 7f, 7f);

      // Check if theres a nearby enemy to go near to
      InvokeRepeating("checkForHostiles", .5f, 1f);
   }

   protected override void Update () {
      base.Update();

      // If we're dead and have finished sinking, remove the ship
      if (isServer && isDead() && spritesContainer.transform.localPosition.y < -.25f) {
         InstanceManager.self.removeEntityFromInstance(this);

         // Destroy the object
         NetworkServer.Destroy(this.gameObject);
      }
   }

   protected override void FixedUpdate () {
      base.FixedUpdate();

      // Only change our movement if enough time has passed
      if (Time.time - _lastMoveChangeTime < MOVE_CHANGE_INTERVAL) {
         return;
      }

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

      // If we don't have a waypoint, we're done
      if (this.waypoint == null || Vector2.Distance(this.transform.position, waypoint.transform.position) < .08f) {
         if(approachShip) {
            approachShip = false;
            foreach (TentacleEntity tentacles in tentacleList) {
               tentacles.initializeBehavior();
            }
         }

         return;
      }

      // Move towards waypoint
      Vector2 waypointDirection = this.waypoint.transform.position - this.transform.position;
      waypointDirection = waypointDirection.normalized;
      _body.AddForce(waypointDirection.normalized * getMoveSpeed());

      // Checks if the distance of the target ship is too far
      if (nearestShipTarget != null) {
         if (Vector2.Distance(_spawnPos, waypoint.transform.position) > _territoryRadius) {
            nearestShipTarget = null;
         }
      }

      // Update our facing direction
      Direction newFacingDirection = DirectionUtil.getDirectionForVelocity(_body.velocity);
      if (newFacingDirection != this.facing) {
         this.facing = newFacingDirection;
      }

      // Make note of the time
      _lastMoveChangeTime = Time.time;
   }

   protected void handleAutoMove () {
      if (!autoMove || !isServer) {
         return;
      }

      // Remove our current waypoint
      if (this.waypoint != null) {
         Destroy(this.waypoint.gameObject);
      }

      Vector2 newSpot = new Vector2(0,0); 
      if (nearestShipTarget != null) {
         // Go to the spot near the nearest target ship
         newSpot = new Vector2(nearestShipTarget.transform.position.x, nearestShipTarget.transform.position.y);
      } else {
         // Pick a new spot around our spawn position
         newSpot = _spawnPos + new Vector2(Random.Range(-.5f, .5f), Random.Range(-.5f, .5f));
         
      }
      approachShip = true;

      Waypoint newWaypoint = Instantiate(PrefabsManager.self.waypointPrefab);
      newWaypoint.transform.position = newSpot;
      this.waypoint = newWaypoint;

      foreach(TentacleEntity tentacles in tentacleList) {
         if (!tentacles.isDead()) {
            tentacles.moveToParentDestination(waypoint.transform.position);
         }
      }
   }

   protected void checkForHostiles () {
      float closestDistance = 100;
      NetEntity closestEntity = null;

      // Fetches the nearest ship
      foreach(NetEntity entity in _attackers) {
         if (!entity.isDead() && entity != null) {
            if (Vector2.Distance(_spawnPos, entity.transform.position) < closestDistance) {
               closestEntity = entity;
               closestDistance = Vector2.Distance(_spawnPos, entity.transform.position);
            }
         }
      }

      // Checks if nearest ship is valid to pursue
      if (closestEntity != null && closestDistance < _detectRadius) {
         nearestShipTarget = closestEntity;
      }
   }

   protected string getNameForFaction () {
      switch (this.faction) {
         case Faction.Type.Pirates:
            return "Pirate";
         case Faction.Type.Privateers:
            return "Privateer";
         case Faction.Type.Merchants:
            return "Merchant";
         case Faction.Type.Cartographers:
         case Faction.Type.Naturalists:
            return "Explorer";
         default:
            return "Sailor";
      }
   }

   [ClientRpc]
   public void Rpc_CallAnimation (TentacleAnimType tentacleAnim) {
      switch(tentacleAnim) {
         case TentacleAnimType.Die:
            animator.Play("Die");
            break;
      }
   }

   #region Private Variables

   // The position we spawned at
   protected Vector2 _spawnPos;

   // The radius that defines how far the monster will chase before it retreats
   private float _territoryRadius = 3.5f;

   // The radius that defines how near the player ships are before this unit chases it
   private float _detectRadius = 3;

   #endregion
}
