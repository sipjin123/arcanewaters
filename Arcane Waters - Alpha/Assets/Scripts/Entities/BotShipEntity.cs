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

   // The radius of which we'll pick new points to patrol to
   public float patrolingWaypointsRadius = 1.0f;

   // The radius of which we'll pick new points to attack from
   public float attackingWaypointsRadius = 1.0f;

   // The seconds to patrol the current TreasureSite before choosing another one
   public float secondsPatrolingUntilChoosingNewTreasureSite;

   // The seconds spent idling between finding patrol routes
   public float secondsBetweenFindingPatrolRoutes;

   // The seconds spent idling between finding attack routes
   public float secondsBetweenFindingAttackRoutes;

   // How big the aggro cone is in degrees
   public float aggroConeDegrees;

   // How far the cone extends ahead of the ship
   public float aggroConeRadius;

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

         _treasureSitesInArea = new List<TreasureSite>(AreaManager.self.getArea(areaKey).GetComponentsInChildren<TreasureSite>());

         _currentSite = _treasureSitesInArea[Random.Range(0, _treasureSitesInArea.Count)];
      }

      // Check if we can shoot at any of our attackers
      InvokeRepeating("checkForAttackers", 1f, .5f);

      if (Application.isEditor) {
         editorGenerateAggroCone();
      }
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

      _attackingCurrentFrame = _attackers != null && _attackers.Count > 0;

      List<ANode> currentPath;
      if (_attackingCurrentFrame) {
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
         if (_attackingCurrentFrame) {
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
            _currentPatrolPath = _pathfindingReference.findPathNowInit(transform.position, _currentSite.transform.position + Random.insideUnitCircle.ToVector3() * patrolingWaypointsRadius);
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

               _currentPatrolPath = _pathfindingReference.findPathNowInit(transform.position, _currentSite.transform.position + Random.insideUnitCircle.ToVector3() * patrolingWaypointsRadius);
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
            _currentAttackingPath = _pathfindingReference.findPathNowInit(transform.position, _lastAttacker.transform.position + Random.insideUnitCircle.ToVector3() * attackingWaypointsRadius);
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
               _currentAttackingPath = _pathfindingReference.findPathNowInit(transform.position, _lastAttacker.transform.position + Random.insideUnitCircle.ToVector3() * attackingWaypointsRadius);
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

      Instance instance = InstanceManager.self.getInstance(instanceId);
      if (instance != null) {
         float degreesToDot = aggroConeDegrees / 90.0f;
         foreach (NetworkBehaviour iEntity in instance.getEntities()) {
            SeaEntity entity = iEntity.GetComponent<SeaEntity>();

            // If the attacker has already been added, early out
            bool earlyOut = false;
            foreach (SeaEntity attacker in _attackers.Keys) {
               if (attacker == entity) {
                  earlyOut = true;
               }
            }
            if (earlyOut) {
               continue;
            }

            if (entity != null && entity.faction != faction) {
               // If enemy isn't within radius, early out
               if ((transform.position - entity.transform.position).magnitude > aggroConeRadius)
                  continue;

               // If enemy isn't within cone aggro range, early out
               if (Vector2.Dot(Util.getDirectionFromFacing(facing), (entity.transform.position - transform.position).normalized) < 1.0f - degreesToDot) {
                  continue;
               }

               // We can see the enemy, attack it
               _attackers[entity] = TimeManager.self.getSyncedTime();
            }
         }
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
               if (shipData.spritePath != "") {
                  spritesContainer.GetComponent<SpriteSwap>().newTexture = ImageManager.getTexture(shipData.spritePath);
               }
            }
            Area area = GetComponentInParent<Area>();
            areaKey = area.areaKey;
            //ShipDataManager.self.storeShip(this);

            speed = Ship.getBaseSpeed(shipType);
            attackRangeModifier = Ship.getBaseAttackRange(shipType);

            // Assign ripple sprites
            _ripplesStillSprites = ImageManager.getTexture(Ship.getRipplesPath(shipType));
            _ripplesMovingSprites = ImageManager.getTexture(Ship.getRipplesMovingPath(shipType));
            if (shipData.rippleSpritePath != "") {
               ripplesContainer.GetComponent<SpriteSwap>().newTexture = _ripplesStillSprites;
            }
         }
      }
   }

   private void editorGenerateAggroCone () {
      float coneRadians = aggroConeDegrees * Mathf.Deg2Rad;
      float singleDegreeInRadian = 1 * Mathf.Deg2Rad;

      _editorConeAggroGizmoMesh = new Mesh();
      List<Vector3> positions = new List<Vector3>();
      List<Vector3> normals = new List<Vector3>();

      // Center of the cone fan
      positions.Add(Vector3.zero);
      normals.Add(Vector3.back);

      for (float iPoint = -coneRadians; iPoint < coneRadians; iPoint += singleDegreeInRadian) {
         positions.Add(new Vector3(Mathf.Cos(iPoint), Mathf.Sin(iPoint), 0.0f) * aggroConeRadius);

         // Point normals towards the camera
         normals.Add(Vector3.back);
      }

      List<int> triangles = new List<int>(positions.Count * 3);
      for (int iVertex = 2; iVertex < positions.Count; iVertex += 1) {
         triangles.Add(0);
         triangles.Add(iVertex);
         triangles.Add(iVertex - 1);
      }

      _editorConeAggroGizmoMesh.SetVertices(positions);
      _editorConeAggroGizmoMesh.SetNormals(normals);
      _editorConeAggroGizmoMesh.SetIndices(triangles, MeshTopology.Triangles, 0);
   }

   private void OnDrawGizmosSelected () {
      List<ANode> pathToDraw;
      if (_attackingCurrentFrame) {
         pathToDraw = _currentAttackingPath;
      } else {
         pathToDraw = _currentPatrolPath;
      }

      if (Application.isPlaying) {
         foreach (ANode node in pathToDraw) {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(node.vPosition, 0.1f);
         }
      }

      Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 0.5f);
      Gizmos.DrawMesh(_editorConeAggroGizmoMesh, 0, transform.position, Quaternion.Euler(0.0f, 0.0f, -Vector2.SignedAngle(Util.getDirectionFromFacing(facing), Vector2.right)), Vector3.one);
      Gizmos.color = Color.white;
   }

   #region Private Variables

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

   // The last attacker to have damaged our vessel
   private SeaEntity _lastAttacker;

   // The generated mesh for showing the cone of aggro in the Editor
   private Mesh _editorConeAggroGizmoMesh;

   // Are we attacking in the current frame?
   private bool _attackingCurrentFrame;

   #endregion
}
