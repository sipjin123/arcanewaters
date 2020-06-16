using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using Pathfinding;

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
         _seeker = GetComponent<Seeker>();
         if (_seeker == null) {
            D.error("There has to be a Seeker Script attached to the BotShipEntity Prefab");
         }

         // Only use the graph in this area to calculate paths
         GridGraph graph = AreaManager.self.getArea(areaKey).getGraph();
         _seeker.graphMask = GraphMask.FromGraph(graph);

         _seeker.pathCallback = setPath_Asynchronous;

         _treasureSitesInArea = new List<TreasureSite>(AreaManager.self.getArea(areaKey).GetComponentsInChildren<TreasureSite>());

         if (_treasureSitesInArea.Count > 0) {
            _currentSite = _treasureSitesInArea[Random.Range(0, _treasureSitesInArea.Count)];
         }

         _originalPosition = transform.position;

         InvokeRepeating(nameof(checkEnemiesToAggro), 0.0f, 0.5f);
         StartCoroutine(CO_attackEnemiesInRange(0.25f));
      }

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

      moveAlongCurrentPath();

      if (_attackers != null && _attackers.Count > 0) {
         if (!_wasAttackedLastFrame) {
            _currentSecondsBetweenAttackRoutes = 0.0f;
            _attackingWaypointState = WaypointState.FINDING_PATH;
            if (_currentPath != null) {
               _currentPath.Clear();
            }
            findAndSetPath_Asynchronous(findAttackerVicinityPosition(true));
         }

         // Update attacking state with only Minor updates
         float tempMajorRef = 0.0f;
         updateState(ref _attackingWaypointState, secondsBetweenFindingAttackRoutes, 9001.0f, ref _currentSecondsBetweenAttackRoutes, ref tempMajorRef, findAttackerVicinityPosition);
      } else {
         if (_wasAttackedLastFrame) {
            _currentSecondsBetweenPatrolRoutes = 0.0f;
            _currentSecondsPatroling = 0.0f;
            _patrolingWaypointState = WaypointState.FINDING_PATH;
            if (_currentPath != null) {
               _currentPath.Clear();
            }
            findAndSetPath_Asynchronous(findTreasureSiteVicinityPosition(true));
         }
         updateState(ref _patrolingWaypointState, secondsBetweenFindingPatrolRoutes, secondsPatrolingUntilChoosingNewTreasureSite, ref _currentSecondsBetweenPatrolRoutes, ref _currentSecondsPatroling, findTreasureSiteVicinityPosition);
      }

      _wasAttackedLastFrame = _attackers != null && _attackers.Count > 0;

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
   private void updateState (ref WaypointState state, float secondsBetweenMinorSearch, float secondsBetweenMajorSearch, ref float currentSecondsBetweenMinorSearch, ref float currentSecondsBetweenMajorSearch, System.Func<bool, Vector3> targetPositionFunction) {
      switch (state) {
         case WaypointState.FINDING_PATH:
            if (_seeker.IsDone() && (_currentPath == null || _currentPath.Count <= 0)) {
               // Try to find another path as the previous one wasn't valid
               findAndSetPath_Asynchronous(targetPositionFunction(false));
            }

            if (_currentPath != null && _currentPath.Count > 0) {
               // If we have a valid path, start moving to the target
               state = WaypointState.MOVING_TO;
            }
            break;

         case WaypointState.MOVING_TO:
            // If there's no nodes left in the path, continue into a patroling state
            if (_currentPathIndex >= _currentPath.Count) {
               state = WaypointState.PATROLING;
               currentSecondsBetweenMinorSearch = 0.0f;
               currentSecondsBetweenMajorSearch = 0.0f;
            }
            break;

         case WaypointState.PATROLING:
            currentSecondsBetweenMajorSearch += Time.fixedDeltaTime;

            // If we've still got a path to complete, don't try to find a new one
            if (_currentPath != null && _currentPathIndex < _currentPath.Count) {
               break;
            }

            // Find a new Major target
            if (currentSecondsBetweenMajorSearch >= secondsBetweenMajorSearch) {
               findAndSetPath_Asynchronous(targetPositionFunction(true));
               state = WaypointState.FINDING_PATH;
               break;
            }

            currentSecondsBetweenMinorSearch += Time.fixedDeltaTime;

            // Find a new Minor target
            if (currentSecondsBetweenMinorSearch >= secondsBetweenMinorSearch) {
               _currentSecondsBetweenPatrolRoutes = 0.0f;
               findAndSetPath_Asynchronous(targetPositionFunction(false));
            }
            break;
      }
   }

   [Server]
   private void moveAlongCurrentPath () {
      if (_currentPath != null && _currentPathIndex < _currentPath.Count) {
         // Only change our movement if enough time has passed
         float moveTime = Time.time - _lastMoveChangeTime;
         if (moveTime >= MOVE_CHANGE_INTERVAL) {
            _body.AddForce(((Vector2) _currentPath[_currentPathIndex] - (Vector2) transform.position).normalized * getMoveSpeed());
            _lastMoveChangeTime = Time.time;
         }

         // Advance along the path as the unit comes close enough to the current waypoint
         float distanceToWaypoint = Vector2.Distance(_currentPath[_currentPathIndex], transform.position);
         if (distanceToWaypoint < .1f) {
            ++_currentPathIndex;
         }
      }
   }

   [Server]
   private Vector3 findTreasureSiteVicinityPosition (bool newSite) {
      // If we should choose a new site, and we have a selection available, pick a unique one randomly, favoring a new target if possible
      if (newSite && _treasureSitesInArea != null && _treasureSitesInArea.Count > 0) {
         int foundSiteIndex = Random.Range(0, _treasureSitesInArea.Count);
         TreasureSite foundSite = _treasureSitesInArea[foundSiteIndex];
         if (foundSite == _currentSite) {
            foundSite = _treasureSitesInArea[++foundSiteIndex % _treasureSitesInArea.Count];
         }
         _currentSite = foundSite;
      }

      if (_currentSite != null) {
         return findPositionAroundPosition(_currentSite.transform.position, patrolingWaypointsRadius);
      } else {
         return findPositionAroundPosition(_originalPosition, patrolingWaypointsRadius);
      }
   }

   [Server]
   private Vector3 findAttackerVicinityPosition (bool newAttacker) {
      bool hasAttacker = _attackers.ContainsKey(_currentAttacker);

      // If we should choose a new attacker, and we have a selection available, pick a unique one randomly, which could be the same as the previous
      if ((!hasAttacker || (hasAttacker && newAttacker)) && _attackers.Count > 0) {
         _currentAttacker = _attackers.RandomKey();
         hasAttacker = true;
      }

      // If we fail to get a non-null attacker, return somewhere around yourself
      if (!hasAttacker) {
         return findPositionAroundPosition(transform.position, attackingWaypointsRadius);
      }

      // If we have a registered attacker but it's not in the scene list of spawned NetworkIdentities, then return somewhere around yourself
      if (!NetworkIdentity.spawned.ContainsKey(_currentAttacker)) {
         return findPositionAroundPosition(transform.position, attackingWaypointsRadius);
      }

      // If we have gotten a NetworkIdentity but it's by some chance null, then return somewhere around yourself
      NetworkIdentity attackerIdentity = NetworkIdentity.spawned[_currentAttacker];
      if (attackerIdentity == null) {
         return findPositionAroundPosition(transform.position, attackingWaypointsRadius);
      }

      // If we have an attacker, find new position around it
      return findPositionAroundPosition(attackerIdentity.transform.position, attackingWaypointsRadius);
   }

   [Server]
   private Vector3 findPositionAroundPosition(Vector3 position, float radius) {
      return position + Random.insideUnitCircle.ToVector3() * radius;
   }

   [Server]
   private void findAndSetPath_Asynchronous (Vector3 targetPosition) {
      if (!_seeker.IsDone()) {
         _seeker.CancelCurrentPathRequest();
      }
      _seeker.StartPath(transform.position, targetPosition);
   }

   [Server]
   private void setPath_Asynchronous (Path newPath) {
      _currentPath = newPath.vectorPath;
      _currentPathIndex = 0;
      _seeker.CancelCurrentPathRequest(true);
   }

   [Server]
   private void checkEnemiesToAggro () {
      if (instanceId <= 0) {
         D.log("BotshipEntity needs to be placed in an instance");
         return;
      }

      Instance instance = InstanceManager.self.getInstance(instanceId);
      float degreesToDot = aggroConeDegrees / 90.0f;

      foreach (NetworkBehaviour iBehaviour in instance.getEntities()) {
         NetEntity iEntity = iBehaviour as NetEntity;

         // If the entity is a fellow bot ship, ignore it
         if (iEntity == null || iEntity.isBotShip())
            continue;

         // If enemy isn't within radius, early out
         if ((transform.position - iEntity.transform.position).sqrMagnitude > aggroConeRadius * aggroConeRadius)
            continue;

         // If enemy isn't within cone aggro range, early out
         if (Vector2.Dot(Util.getDirectionFromFacing(facing), (iEntity.transform.position - transform.position).normalized) < 1.0f - degreesToDot) {
            continue;
         }

         // We can see the enemy, attack it
         _attackers[iEntity.netId] = TimeManager.self.getSyncedTime();
      }
   }

   [Server]
   private IEnumerator CO_attackEnemiesInRange (float delayInSecondsWhenNotReloading) {
      while (!isDead()) {
         bool attacked = false;

         // Check if any of our attackers are within range
         foreach (uint attackerId in _attackers.Keys) {
            NetEntity attacker = MyNetworkManager.fetchEntityFromNetId<NetEntity>(attackerId);
            if (attacker == null || attacker.isDead()) {
               continue;
            }

            Vector2 attackerPosition = attacker.transform.position;
            if (isInRange(attackerPosition)) {
               ShipAbilityData shipAbilityData = ShipAbilityManager.self.getAbility(Attack.Type.Cannon);
               fireAtSpot(attackerPosition, shipAbilityData.abilityId, 0, 0, transform.position);
               attacked = true;
               break;
            }
         }

         float waitTimeToUse = delayInSecondsWhenNotReloading;
         if(attacked) {
            waitTimeToUse = reloadDelay;
         }
         yield return new WaitForSeconds(waitTimeToUse);
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

            initialize(shipData);

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
      if (Application.isPlaying) {
         Gizmos.color = new Color(1.0f, 0.0f, 0.0f, 0.5f);
         Gizmos.DrawMesh(_editorConeAggroGizmoMesh, 0, transform.position, Quaternion.Euler(0.0f, 0.0f, -Vector2.SignedAngle(Util.getDirectionFromFacing(facing), Vector2.right)), Vector3.one);
         Gizmos.color = Color.white;
      }
   }

   public override bool isBotShip () { return true; }

   public override void setAreaParent (Area area, bool worldPositionStays) {
      this.transform.SetParent(area.botShipParent, worldPositionStays);
   }

   #region Private Variables

   // The Seeker that handles Pathfinding
   protected Seeker _seeker;

   // How many seconds have passed since we've started patrolling the current TreasureSite
   private float _currentSecondsPatroling;

   // How many seconds have passed since we last stopped on a patrol route
   private float _currentSecondsBetweenPatrolRoutes;

   // How many seconds have passed since we last stopped on an attack route
   private float _currentSecondsBetweenAttackRoutes;

   private enum WaypointState
   {
      NONE = 0,
      FINDING_PATH = 1,
      MOVING_TO = 2,
      PATROLING = 3,
   }

   // In what state the Patrol Waypoint traversing is in
   private WaypointState _patrolingWaypointState = WaypointState.FINDING_PATH;

   // In what state the Attack Waypoint traversing is in
   private WaypointState _attackingWaypointState = WaypointState.FINDING_PATH;

   // Were we attacked last frame?
   private bool _wasAttackedLastFrame;

   // The TreasureSite Objects that are present in the area
   private List<TreasureSite> _treasureSitesInArea;

   // The current targeted TreasureSite
   private TreasureSite _currentSite;

   // The current path to the destination
   private List<Vector3> _currentPath;

   // The first and closest attacker that we've aggroed
   private uint _currentAttacker;

   // The generated mesh for showing the cone of aggro in the Editor
   private Mesh _editorConeAggroGizmoMesh;

   // In case there's no TreasureSites to pursue, use this to patrol the vicinity
   private Vector3 _originalPosition;

   // The current Point Index of the path
   private int _currentPathIndex;

   #endregion
}
