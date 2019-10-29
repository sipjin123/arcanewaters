using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class BotShipEntity : ShipEntity {
   #region Public Variables

   // The Type of NPC that is sailing this ship
   [SyncVar]
   public NPC.Type npcType;

   // The Name of the NPC that is sailing this ship
   [SyncVar]
   public string npcName;

   // The Route that this Bot should follow
   public Route route;

   // The current waypoint
   public Waypoint waypoint;

   // A custom max force that we can optionally specify
   public float maxForceOverride = 0f;

   // When set to true, we pick random waypoints
   public bool autoMove = false;

   #endregion

   protected override void Start () {
      base.Start();

      // Note our spawn position
      _spawnPos = this.transform.position;

      // Set our name
      this.nameText.text = "[" + getNameForFaction() + "]";
      NPC.setNameColor(nameText, npcType);

      // Sometimes we want to generate random waypoints
      InvokeRepeating("handleAutoMove", 1f, 2f);

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

   protected void handleAutoMove () {
      if (!autoMove || !isServer) {
         return;
      }

      // Remove our current waypoint
      if (this.waypoint != null) {
         Destroy(this.waypoint.gameObject);
      }

      // Pick a new spot around our spawn position
      Vector2 newSpot = _spawnPos + new Vector2(Random.Range(-.5f, .5f), Random.Range(-.5f, .5f));
      Waypoint newWaypoint = Instantiate(PrefabsManager.self.waypointPrefab);
      newWaypoint.transform.position = newSpot;
      this.waypoint = newWaypoint;
   }

   //protected void checkForAttackers () {
   //   if (isDead() || !isServer) {
   //      return;
   //   }

   //   // If we haven't reloaded, we can't attack
   //   if (!hasReloaded()) {
   //      return;
   //   }

   //   // Check if any of our attackers are within range
   //   foreach (SeaEntity attacker in _attackers) {
   //      if (attacker == null || attacker.isDead()) {
   //         continue;
   //      }

   //      // Check where the attacker currently is
   //      Vector2 spot = attacker.transform.position;

   //      // If the requested spot is not in the allowed area, reject the request
   //      if (isAttackableSpot(spot)) {
   //         fireAtSpot(spot, Attack.Type.Cannon, 0, 0, transform.position);

   //         return;
   //      }
   //   }
   //}

   #region Private Variables

   // The position we spawned at
   protected Vector2 _spawnPos;

   #endregion
}
