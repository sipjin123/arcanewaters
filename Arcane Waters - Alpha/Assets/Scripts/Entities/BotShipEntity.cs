using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using AStar;

public class BotShipEntity : ShipEntity, IMapEditorDataReceiver
{
   #region Public Variables

   // A custom max force that we can optionally specify
   public float maxForceOverride = 0f;

   // Determines if this ship is spawned at debug mode
   public bool isDebug = false;

   #endregion

   protected override void Start () {
      base.Start();

      // Set our name
      this.nameText.text = "[" + getNameForFaction() + "]";

      // Add some move targets
      _moveTargets.Add(transform.position);
      _moveTargets.Add(transform.position + new Vector2(-1.0f, 0f).ToVector3());
      _moveTargets.Add(transform.position + new Vector2(1.0f, 0f).ToVector3());
      _moveTargets.Add(transform.position + new Vector2(0f, -1.0f).ToVector3());
      _moveTargets.Add(transform.position + new Vector2(0f, 1.0f).ToVector3());
      _moveTargets.Add(transform.position + new Vector2(-1.0f, -1.0f).ToVector3());
      _moveTargets.Add(transform.position + new Vector2(-1.0f, 1.0f).ToVector3());
      _moveTargets.Add(transform.position + new Vector2(1.0f, -1.0f).ToVector3());
      _moveTargets.Add(transform.position + new Vector2(1.0f, 1.0f).ToVector3());

      // Continually pick new move targets
      if (isServer) {
         _gridReference = AreaManager.self.getArea(areaKey).pathfindingGrid;
         if (_gridReference == null) {
            D.error("There has to be an AStarGrid Script attached to the MapTemplate Prefab");
         }

         _pathfindingReference = GetComponent<Pathfinding>();
         if (_pathfindingReference == null) {
            D.error("There has to be a Pathfinding Script attached to the NPC Prefab");
         }
         _pathfindingReference.gridReference = _gridReference;

         StartCoroutine(generateNewWaypoints(6f + Random.Range(-1f, 1f)));
      }

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

      if (_waypointList.Count > 0) {
         // Move towards our current waypoint
         Vector2 waypointDirection = _waypointList[0].transform.position - transform.position;
         _body.AddForce(waypointDirection.normalized * getMoveSpeed());
         _lastMoveChangeTime = Time.time;

         // Clears a node as the unit passes by
         float distanceToWaypoint = Vector2.Distance(_waypointList[0].transform.position, transform.position);
         if (distanceToWaypoint < .1f) {
            Destroy(_waypointList[0].gameObject);
            _waypointList.RemoveAt(0);
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
   protected IEnumerator generateNewWaypoints (float moveAgainDelay) {
      // Initial delay
      float waitTime = 1.0f;
      while (true) {
         yield return new WaitForSeconds(waitTime);

         if (_waypointList.Count > 0) {
            // There's still points left of the old path, wait 1 second and check again if there's no more waypoints
            waitTime = 1.0f;
            continue;
         }

         List<ANode> gridPath = _pathfindingReference.findPathNowInit(transform.position, _moveTargets.ChooseRandom());
         if (gridPath == null || gridPath.Count <= 0) {
            // Invalid Path, attempt again after 1 second
            waitTime = 1.0f;
            continue;
         }

         // We have a new path, set to normal delay
         waitTime = moveAgainDelay;

         // Register Route
         foreach (ANode node in gridPath) {
            Waypoint newWaypointPath = Instantiate(PrefabsManager.self.waypointPrefab);
            newWaypointPath.transform.position = node.vPosition;
            _waypointList.Add(newWaypointPath);
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
      foreach (SeaEntity attacker in _attackers.Keys) {
         if (attacker == null || attacker.isDead()) {
            continue;
         }

         // Check where the attacker currently is
         Vector2 spot = attacker.transform.position;

         // If the requested spot is not in the allowed area, reject the request
         if (isInRange(spot)) {
            fireAtSpot(spot, Attack.Type.Cannon, 0, 0, transform.position);

            return;
         }
      }
   }

   public static int fetchDataFieldID (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.SHIP_DATA_KEY) == 0) {
            // Get Type from ship data field
            if (int.TryParse(field.v, out int shipId)) {
               return shipId;
            }
         }
      }
      return 0;
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.SHIP_DATA_KEY) == 0) {
            // Get ID from ship data field
            // Field arrives in format <ship type>: <ship name>
            int type = int.Parse(field.v.Split(':')[0]);
            isDebug = true;

            ShipData shipData = ShipDataManager.self.getShipData((Ship.Type) type);
            shipType = shipData.shipType;
            if (shipData != null) {
               string spritePath = shipData.spritePath;
               if (spritePath != "") {
                  spritesContainer.GetComponent<SpriteSwap>().newTexture = ImageManager.getTexture(spritePath);
               }
            }
            Area area = GetComponentInParent<Area>();
            areaKey = area.areaKey;
            //ShipDataManager.self.storeShip(this);

            speed = Ship.getBaseSpeed(shipType);
            attackRangeModifier = Ship.getBaseAttackRange(shipType);

            // extract Route
         }
      }
   }

   #region Private Variables

   // The available positions to move to
   protected List<Vector2> _moveTargets = new List<Vector2>();

   // The AStarGrid Reference fetched from the Main Scene
   protected AStarGrid _gridReference;

   // The Pathfinding Reference fetched from the Main Scene
   protected Pathfinding _pathfindingReference;

   // The current waypoint List
   protected List<Waypoint> _waypointList = new List<Waypoint>();

   #endregion
}
