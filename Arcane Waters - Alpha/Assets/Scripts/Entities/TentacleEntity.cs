using UnityEngine;
using System.Collections.Generic;
using Mirror;
using System.Collections;

public class TentacleEntity : SeaMonsterEntity
{
   #region Public Variables
   
   // Reference to the boss object
   public HorrorEntity horrorEntity;

   // Determines if location is left or right side of the boss monster
   [SyncVar]
   public int locationSide;

   // Determines if location is top or bottom side of the boss monster
   [SyncVar]
   public int locationSideTopBot;

   // Randomizes behavior before moving
   public float randomizedTimer = 1;

   #endregion

   protected override void Start () {
      base.Start();

      // Note our spawn position
      _spawnPos = this.transform.position;

      // Set our name
      this.nameText.text = "[" + getNameForFaction() + "]";
      NPC.setNameColor(nameText, npcType);

      // Calls functions that randomizes and calls the coroutine that handles movement
      initializeBehavior();

      // Check if we can shoot at any of our attackers
      InvokeRepeating("checkForAttackers", 1f, .5f);
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

      // Only the server updates waypoints and movement forces
      if (!isServer || isDead()) {
         if (hasDied == false && isDead()) {
            hasDied = true;
            tentacleDeath();
         }
         return;
      }

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
         return;
      }

      // Move towards our current waypoint
      Vector2 waypointDirection = this.waypoint.transform.position - this.transform.position;
      waypointDirection = waypointDirection.normalized;
      _body.AddForce(waypointDirection.normalized * getMoveSpeed());

      // Update our facing direction
      Direction newFacingDirection = DirectionUtil.getDirectionForVelocity(_body.velocity);
      if (newFacingDirection != this.facing) {
         this.facing = newFacingDirection;
      }

      // Make note of the time
      _lastMoveChangeTime = Time.time;
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

   [Server]
   public void initializeBehavior () {
      randomizedTimer = Random.Range(2.0f, 4.5f);
      StartCoroutine(CO_HandleAutoMove());
   }

   public IEnumerator CO_HandleAutoMove () {
      if (!autoMove || !isServer) {
         yield return null;
      }

      // Remove our current waypoint
      if (this.waypoint != null) {
         Destroy(this.waypoint.gameObject);
      }

      bool farFromBossMonster = Vector2.Distance(horrorEntity.transform.position, transform.position) > 1.5f;
      Vector2 newSpot = new Vector2(0, 0);
      if(recentAttacker != null) {
         if (farFromBossMonster) {
            recentAttacker = null;
         } else {
            newSpot = recentAttacker.transform.position;
         }
      } else {
         float randomizedX = Random.Range(.1f, .8f);
         float randomizedY = Random.Range(.15f, .75f);

         randomizedX *= locationSide;
         randomizedY *= locationSideTopBot;

         // Pick a new spot around our spawn position
         newSpot = new Vector2(horrorEntity.transform.position.x, horrorEntity.transform.position.y) + new Vector2(randomizedX, randomizedY);
      }

      Waypoint newWaypoint = Instantiate(PrefabsManager.self.waypointPrefab);
      newWaypoint.transform.position = newSpot;
      this.waypoint = newWaypoint;

      yield return new WaitForSeconds(randomizedTimer);
      initializeBehavior();
   }

   public void initializeDelayedMovement (Vector2 newPos) {
      float delayTime = Random.Range(.1f, .7f);
      handleAutoMove(newPos, delayTime);
   }

   private IEnumerator handleAutoMove (Vector2 newPos, float delay) {
      yield return new WaitForSeconds(delay);
      if (!autoMove || !isServer) {
         yield return null;
      }

      // Remove our current waypoint
      if (this.waypoint != null) {
         Destroy(this.waypoint.gameObject);
      }
 
      float randomizedX = Random.Range(.2f, .6f);
      float randomizedY = Random.Range(.15f, .45f);

      randomizedX *= locationSide;
      randomizedY *= locationSideTopBot;

      // Pick a new spot around our spawn position
      //Vector2 newSpot = new Vector2(horrorEntity.transform.position.x + randomizedX, horrorEntity.transform.position.y + randomizedY);
      Vector2 newSpot = new Vector2(newPos.x + randomizedX, newPos.y + randomizedY);

      Waypoint newWaypoint = Instantiate(PrefabsManager.self.waypointPrefab);
      newWaypoint.transform.position = newSpot;
      this.waypoint = newWaypoint;

      StopCoroutine(CO_HandleAutoMove());
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
         if (attacker == null || attacker.isDead()) {
            continue;
         }

         // Check where the attacker currently is
         Vector2 spot = attacker.transform.position;

         // If the requested spot is not in the allowed area, reject the request
         if (leftAttackBox.OverlapPoint(spot) || rightAttackBox.OverlapPoint(spot)) {
            meleeAtSpot(spot, Attack.Type.Tentacle);
            callAnimation(TentacleAnimType.Attack);
            return;
         }
      }
   }

   private void OnTriggerStay2D (Collider2D collision) {
      if (collision.GetComponent<PlayerShipEntity>() != null) {
         NetEntity shipEntity = collision.GetComponent<PlayerShipEntity>();
         if (!_attackers.Contains(shipEntity)) {
            _attackers.Add(shipEntity);
         }
         recentAttacker = shipEntity;
      }
   }

   [Server]
   public void tentacleDeath () {
      callAnimation(TentacleAnimType.Die);
      horrorEntity.tentaclesLeft -= 1;
      if (horrorEntity.tentaclesLeft <= 0) {
         horrorEntity.currentHealth = 0;
         horrorEntity.Rpc_CallAnimation(TentacleEntity.TentacleAnimType.Die);
      }
   }

   [Server]
   public void callAnimation (TentacleAnimType anim) {
      Rpc_CallAnimation(anim);
   }

   [ClientRpc]
   public void Rpc_CallAnimation (TentacleAnimType anim) {
      switch (anim) {
         case TentacleAnimType.Attack:
            animator.Play("Attack");
            break;
         case TentacleAnimType.Die:
            animator.SetTrigger("Dead");
            break;
      }
   }

   #region Private Variables

   // The position we spawned at
   protected Vector2 _spawnPos;

   #endregion
}
