using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using AStar;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class BotShipEntity : ShipEntity, IMapEditorDataReceiver
{
   #region Public Variables

   // A custom max force that we can optionally specify
   public float maxForceOverride = 0f;

   // Determines if this ship is spawned at debug mode
   public bool isDebug = false;

   // The seconds to patrol the current TreasureSite before choosing another one
   public float secondsPatrolingUntilChoosingNewTreasureSite;

   // The seconds spent idling between finding patrol routes
   public float secondsBetweenFindingPatrolRoutes;

   // The seconds spent idling between finding attack routes
   public float secondsBetweenFindingAttackRoutes;

   #endregion

   protected override void Start () {
      base.Start();

      // Set our name
      nameText.text = "[" + getNameForFaction() + "]";

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

         // Add some move targets
         _moveTargetOffsets.Add(new Vector2(-1.0f, 0f).ToVector3());
         _moveTargetOffsets.Add(new Vector2(1.0f, 0f).ToVector3());
         _moveTargetOffsets.Add(new Vector2(0f, -1.0f).ToVector3());
         _moveTargetOffsets.Add(new Vector2(0f, 1.0f).ToVector3());
         _moveTargetOffsets.Add(new Vector2(-1.0f, -1.0f).ToVector3());
         _moveTargetOffsets.Add(new Vector2(-1.0f, 1.0f).ToVector3());
         _moveTargetOffsets.Add(new Vector2(1.0f, -1.0f).ToVector3());
         _moveTargetOffsets.Add(new Vector2(1.0f, 1.0f).ToVector3());

         _treasureSitesInArea = new List<TreasureSite>(AreaManager.self.getArea(areaKey).GetComponentsInChildren<TreasureSite>());

         _currentSite = _treasureSitesInArea[Random.Range(0, _treasureSitesInArea.Count)];
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
         NetworkServer.Destroy(gameObject);
      }
   }

   protected override void FixedUpdate () {
      base.FixedUpdate();

      // Only the server updates waypoints and movement forces
      if (!isServer || isDead()) {
         return;
      }

      bool attacking = _attackers != null && _attackers.Count > 0;

      List<ANode> currentPath;
      if (attacking) {
         currentPath = _currentAttackingPath;
      } else {
         currentPath = _currentPatrolPath;
      }

      if (currentPath != null && currentPath.Count > 0) {
         // Move towards our current waypoint
         Vector2 waypointDirection = currentPath[0].vPosition - transform.position;
         // Only change our movement if enough time has passed
         float moveTime = Time.time - _lastMoveChangeTime;
         if (moveTime >= MOVE_CHANGE_INTERVAL) {
            _body.AddForce(waypointDirection.normalized * getMoveSpeed());
            _lastMoveChangeTime = Time.time;
         }

         // Clears a node as the unit passes by
         float distanceToWaypoint = Vector2.Distance(currentPath[0].vPosition, transform.position);
         if (distanceToWaypoint < .1f) {
            currentPath.RemoveAt(0);
         }
      } else {
         if (attacking) {
            generateNewAttackWaypoints();
         } else {
            generateNewPatrolWaypoints();
         }
      }

      // Update our facing direction
      if (_body.velocity.magnitude > 0.0f) {
         Direction newFacingDirection = DirectionUtil.getDirectionForVelocity(_body.velocity);
         if (newFacingDirection != facing) {
            facing = newFacingDirection;
         }
      }
   }

   protected string getNameForFaction () {
      switch (faction) {
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
   private void generateNewPatrolWaypoints () {
      switch (_patrolingWaypointState) {
         case WaypointState.FINDING_PATH:
            _currentPatrolPath = _pathfindingReference.findPathNowInit(transform.position, _currentSite.transform.position + _moveTargetOffsets.ChooseRandom().ToVector3());
            if (_currentPatrolPath != null && _currentPatrolPath.Count > 0) {
               _patrolingWaypointState = WaypointState.MOVING_TO;
            }
            break;

         case WaypointState.MOVING_TO:
            if (_currentPatrolPath.Count <= 0) {
               _patrolingWaypointState = WaypointState.PATROLING;
               _secondsPatroling = 0.0f;
               _secondsBetweenPatrolRoutes = 0.0f;
            }

            break;

         case WaypointState.PATROLING:
            _secondsPatroling += Time.fixedDeltaTime;
            _secondsBetweenPatrolRoutes += Time.fixedDeltaTime;

            if (_secondsPatroling >= secondsPatrolingUntilChoosingNewTreasureSite) {
               _currentSite = _treasureSitesInArea[Random.Range(0, _treasureSitesInArea.Count)];
               _patrolingWaypointState = WaypointState.FINDING_PATH;
            } else if (_secondsBetweenPatrolRoutes >= secondsBetweenFindingPatrolRoutes) {
               _secondsBetweenPatrolRoutes = 0.0f;

               _currentPatrolPath = _pathfindingReference.findPathNowInit(transform.position, _currentSite.transform.position + _moveTargetOffsets.ChooseRandom().ToVector3());
            }
            break;
      }
   }

   [Server]
   private void generateNewAttackWaypoints () {
      if (_lastAttacker == null) {
         return;
      }

      switch (_attackingWaypointState) {
         case WaypointState.FINDING_PATH:
            _currentAttackingPath = _pathfindingReference.findPathNowInit(transform.position, _lastAttacker.transform.position + _moveTargetOffsets.ChooseRandom().ToVector3());
            if (_currentAttackingPath != null && _currentAttackingPath.Count > 0) {
               _attackingWaypointState = WaypointState.MOVING_TO;
            }
            break;

         case WaypointState.MOVING_TO:
            if (_currentAttackingPath.Count <= 0) {
               _attackingWaypointState = WaypointState.PATROLING;
               _secondsBetweenAttackRoutes = 0.0f;
            }

            break;

         case WaypointState.PATROLING:
            _secondsBetweenAttackRoutes += Time.fixedDeltaTime;

            if (_secondsBetweenAttackRoutes >= secondsBetweenFindingAttackRoutes) {
               _secondsBetweenAttackRoutes = 0.0f;

               _attackingWaypointState = WaypointState.FINDING_PATH;
               _currentAttackingPath = _pathfindingReference.findPathNowInit(transform.position, _lastAttacker.transform.position + _moveTargetOffsets.ChooseRandom().ToVector3());
            }
            break;
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

         _lastAttacker = attacker;

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

#if UNITY_EDITOR
   private void OnDrawGizmosSelected () {
      // Only force draw the grid Gizmos when the NPC is selected and not the Grid itself
      // (or they will double draw as is the parent/child relationship in terms of gizmos)
      if (!Selection.Contains(_gridReference.gameObject)) {
         if (_attackers != null && _attackers.Count > 0) {
            _gridReference.pathToDraw = _currentAttackingPath;
         } else {
            _gridReference.pathToDraw = _currentPatrolPath;
         }
         _gridReference.OnDrawGizmosSelected();
      }
   }
#endif

   #region Private Variables

   // The available positions to move to
   protected List<Vector2> _moveTargetOffsets = new List<Vector2>();

   // The AStarGrid Reference fetched from the Main Scene
   protected AStarGrid _gridReference;

   // The Pathfinding Reference fetched from the Main Scene
   protected Pathfinding _pathfindingReference;

   // How many seconds have passed since we've started patrolling the current TreasureSite
   private float _secondsPatroling;

   // How many seconds have passed since we last stopped on a patrol route
   private float _secondsBetweenPatrolRoutes;

   // How many seconds have passed since we last stopped on an attack route
   private float _secondsBetweenAttackRoutes;

   private enum WaypointState
   {
      FINDING_PATH = 1,
      MOVING_TO = 2,
      PATROLING = 3,
   }

   // In what state the Patrol Waypoint traversing is in
   private WaypointState _patrolingWaypointState = WaypointState.FINDING_PATH;

   // In what state the Attack Waypoint traversing is in
   private WaypointState _attackingWaypointState = WaypointState.FINDING_PATH;

   // The TreasureSite Objects that are present in the area
   private List<TreasureSite> _treasureSitesInArea;

   // The current targeted TreasureSite
   private TreasureSite _currentSite;

   // The current path to the patrol destination
   private List<ANode> _currentPatrolPath;

   // The current path when attacking something
   private List<ANode> _currentAttackingPath;

   // The last attecker to have damaged our vessel
   private SeaEntity _lastAttacker;

   #endregion
}
