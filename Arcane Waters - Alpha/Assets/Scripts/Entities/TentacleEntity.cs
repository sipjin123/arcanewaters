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
   public int locationSide;

   // Determines if location is top or bottom side of the boss monster
   public int locationSideTopBot;

   // Randomizes behavior before moving
   public float randomizedTimer = 1;

   // Determines the variety of the tentacle
   [SyncVar]
   public int variety = 0;

   #endregion

   protected override void Start () {
      base.Start();

      // Note our spawn position
      _spawnPos = this.transform.position;

      // Set our name
      NPC.setNameColor(nameText, npcType);

      // Calls functions that randomizes and calls the coroutine that handles movement
      initializeBehavior();

      // Check if we can shoot at any of our attackers
      InvokeRepeating("checkForAttackers", 1f, .5f);

      animator.SetFloat("variety", variety);
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

      // Updates animation if monster is moving
      if (_body.velocity.magnitude < .05f) {
         callAnimation(TentacleAnimType.MoveStop);
      } else {
         callAnimation(TentacleAnimType.Move);
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
      Vector2 waypointDirection = waypointDirection = this.waypoint.transform.position - this.transform.position;
      waypointDirection = waypointDirection.normalized;
      _body.AddForce(waypointDirection.normalized * getMoveSpeed());

      Direction newFacingDirection = DirectionUtil.getDirectionForVelocity(_body.velocity);
      if (newFacingDirection != this.facing) {
         this.facing = newFacingDirection;
      }

      // Make note of the time
      _lastMoveChangeTime = Time.time;
   }

   public override void noteAttacker (NetEntity entity) {
      base.noteAttacker(entity);
      horrorEntity.noteAttacker(entity);
   }

   [Server]
   public void initializeBehavior () {
      randomizedTimer = Random.Range(2.0f, 4.5f);
      _movementCoroutine = StartCoroutine (CO_HandleAutoMove());
   }

   public IEnumerator CO_HandleAutoMove () {
      if (!autoMove || !isServer) {
         yield return null;
      }

      // Remove our current waypoint
      if (this.waypoint != null) {
         Destroy(this.waypoint.gameObject);
      }

      float randomizedX = (locationSide != 0 && locationSideTopBot != 0) ? Random.Range(.4f, .6f) : Random.Range(.6f, .8f);
      float randomizedY = (locationSide != 0 && locationSideTopBot != 0) ? Random.Range(.4f, .6f) : Random.Range(.6f, .8f);

      randomizedX *= locationSide;
      randomizedY *= locationSideTopBot;
      
      // Pick a new spot around the Horror monster
      Vector2 newSpot = new Vector2(horrorEntity.transform.position.x, horrorEntity.transform.position.y) + new Vector2(randomizedX, randomizedY);
      
      Waypoint newWaypoint = Instantiate(PrefabsManager.self.waypointPrefab);
      newWaypoint.transform.position = newSpot;
      this.waypoint = newWaypoint;

      yield return new WaitForSeconds(randomizedTimer);
      initializeBehavior();
   }

   public void moveToParentDestination (Vector2 newPos) {
      StopCoroutine(_movementCoroutine);
      float delayTime = .1f;
      StartCoroutine(CO_HandleBossMovement(newPos, delayTime));
   }

   private IEnumerator CO_HandleBossMovement (Vector2 newPos, float delay) {
      yield return new WaitForSeconds(delay);

      if (!autoMove || !isServer) {
         yield return null;
      }

      // Remove our current waypoint
      if (this.waypoint != null) {
         Destroy(this.waypoint.gameObject);
      }
 
      float randomizedX = Random.Range(.4f, .8f);
      float randomizedY = Random.Range(.4f, .8f);

      randomizedX *= locationSide;
      randomizedY *= locationSideTopBot;
      _cachedCoordinates = new Vector2(randomizedX, randomizedY);

      // Pick a new spot around our spawn position
      Vector2 newLoc = new Vector2(newPos.x + randomizedX, newPos.y + randomizedY);

      Waypoint newWaypoint = Instantiate(PrefabsManager.self.waypointPrefab);
      newWaypoint.transform.position = newLoc;
      this.waypoint = newWaypoint;
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
            horrorEntity.noteAttacker(shipEntity);
         }
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

   #region Private Variables

   // Keeps reference to the recent coroutine so that it can be manually stopped
   private Coroutine _movementCoroutine = null;

   // The target location of this unit
   private Vector3 _cachedCoordinates;

   #endregion
}
