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

   // The data edited in the sea monster tool
   public SeaMonsterEntityData seaEntityData;

   // The guild ids for the bot ship guilds
   public static int PRIVATEERS_GUILD_ID = 1;
   public static int PIRATES_GUILD_ID = 2;

   // A custom max force that we can optionally specify
   public float maxForceOverride = 0f;

   // Determines if this ship is spawned at debug mode
   public bool isDebug = false;

   // The radius of which we'll pick new points to patrol to, when there is no treasure sites in the map
   public float newWaypointsRadius = 10.0f;

   // The radius of which we'll pick new points to patrol to
   public float patrolingWaypointsRadius = 1.0f;

   // The radius of which we'll pick new points to attack from
   public float attackingWaypointsRadius = 0.5f;

   // The seconds to patrol the current TreasureSite before choosing another one
   public float secondsPatrolingUntilChoosingNewTreasureSite;

   // The seconds spent idling between finding patrol routes
   public float secondsBetweenFindingPatrolRoutes;

   // The seconds spent idling between finding attack routes
   public float secondsBetweenFindingAttackRoutes;

   // The maximum amount of seconds chasing a target before finding a new path
   public float maxSecondsOnChasePath = 2.0f;

   // How big the aggro cone is in degrees
   public float aggroConeDegrees;

   // How far the cone extends ahead of the ship
   public float aggroConeRadius;

   #endregion

   protected override void Start () {
      base.Start();

      // Continually pick new move targets
      if (isServer) {
         _seeker = GetComponent<Seeker>();
         if (_seeker == null) {
            D.debug("There has to be a Seeker Script attached to the BotShipEntity Prefab");
         }

         Area area = AreaManager.self.getArea(areaKey);

         // Only use the graph in this area to calculate paths
         GridGraph graph = area.getGraph();
         _seeker.graphMask = GraphMask.FromGraph(graph);

         _seeker.pathCallback = setPath_Asynchronous;

         _originalPosition = transform.position;

         List<WarpTreasureSite> treasureSites = new List<WarpTreasureSite>(AreaManager.self.getArea(areaKey).GetComponentsInChildren<WarpTreasureSite>());

         Spawn[] playerSpawns = area.GetComponentsInChildren<Spawn>();
         foreach (Spawn spawn in playerSpawns) {
            bool add = true;
            foreach (WarpTreasureSite treasureSite in treasureSites) {
               if (Vector2.Distance(spawn.transform.position, treasureSite.transform.position) < 1.0f) {
                  add = false;
               }
            }
            if (add) {
               _playerSpawnPoints.Add(spawn.transform.position);
            }
         }
         _minDistanceToSpawn = area.getAreaSizeWorld().x * MIN_DISTANCE_TO_SPAWN_PERCENT;
         _minDistanceToSpawnPath = area.getAreaSizeWorld().x * MIN_DISTANCE_TO_SPAWN_PATH_PERCENT;

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

      // If the tutorial is waiting for a bot ship defeat, test if the conditions are met
      if (isClient && isDead() && TutorialManager3.self.getCurrentTrigger() == TutorialTrigger.DefeatPirateShip) {
         if (Global.player != null && hasBeenAttackedBy(Global.player)) {
            TutorialManager3.self.tryCompletingStep(TutorialTrigger.DefeatPirateShip);
         }
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
         double chaseDuration = NetworkTime.time - _chaseStartTime;
         if (!_isChasingEnemy || (chaseDuration > maxSecondsOnChasePath)) {
            _currentSecondsBetweenAttackRoutes = 0.0f;
            _attackingWaypointState = WaypointState.FINDING_PATH;
            if (_currentPath != null) {
               _currentPath.Clear();
            }
            findAndSetPath_Asynchronous(findAttackerVicinityPosition(true));
            _chaseStartTime = NetworkTime.time;
         }

         // Update attacking state with only Minor updates
         float tempMajorRef = 0.0f;
         updateState(ref _attackingWaypointState, secondsBetweenFindingAttackRoutes, 9001.0f, ref _currentSecondsBetweenAttackRoutes, ref tempMajorRef, findAttackerVicinityPosition);
      } else {
         // Use treasure site only in maps with more than two treasure sites
         System.Func<bool, Vector3> findingFunction = findRandomVicinityPosition;

         if (_isChasingEnemy) {
            _currentSecondsBetweenPatrolRoutes = 0.0f;
            _currentSecondsPatroling = 0.0f;
            _patrolingWaypointState = WaypointState.FINDING_PATH;
            if (_currentPath != null) {
               _currentPath.Clear();
            }
            findAndSetPath_Asynchronous(findingFunction(true));
         }
         updateState(ref _patrolingWaypointState, secondsBetweenFindingPatrolRoutes, secondsPatrolingUntilChoosingNewTreasureSite, ref _currentSecondsBetweenPatrolRoutes, ref _currentSecondsPatroling, findingFunction);
      }

      bool wasChasingLastFrame = _isChasingEnemy;
      _isChasingEnemy = _attackers != null && _attackers.Count > 0;

      if (!wasChasingLastFrame && _isChasingEnemy) {
         _chaseStartTime = NetworkTime.time;
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
            // If there are no nodes left in the path, change into a patrolling state
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
         double moveTime = NetworkTime.time - _lastMoveChangeTime;
         if (moveTime >= MOVE_CHANGE_INTERVAL) {
            Vector2 direction = (Vector2) _currentPath[_currentPathIndex] - (Vector2) transform.position;
            
            // Update our facing direction
            Direction newFacingDirection = DirectionUtil.getDirectionForVelocity(direction);
            if (newFacingDirection != facing) {
               facing = newFacingDirection;
            }

            _body.AddForce(direction.normalized * getMoveSpeed());
            _lastMoveChangeTime = NetworkTime.time;
         }

         // If ship got too close to spawn point - change path
         if (!_isChasingEnemy && !_disableSpawnDistanceTmp) {
            foreach (Vector3 spawn in _playerSpawnPoints) {
               float dist = Vector2.Distance(spawn, _currentPath[_currentPathIndex]);
               if (dist < _minDistanceToSpawn) {
                  _currentPathIndex = int.MaxValue - 1;
                  _disableSpawnDistanceTmp = true;
                  _lastSpawnPosition = spawn;
                  return;
               }
            }
         }

         // Advance along the path as the unit comes close enough to the current waypoint
         float distanceToWaypoint = Vector2.Distance(_currentPath[_currentPathIndex], transform.position);
         if (distanceToWaypoint < .1f) {
            ++_currentPathIndex;
         }
      }
   }

   [Server]
   private Vector3 findRandomVicinityPosition (bool placeholder) {
      Vector3 start = _body.transform.position;

      // If ship is near original position - try to find new distant location to move to
      if (Vector2.Distance(start, _originalPosition) < 1.0f) {
         const int MAX_RETRIES = 50;
         int retries = 0;
         bool retry = true;

         Vector3 dir = start - _lastSpawnPosition;
         dir.Normalize();
         while (retry && retries < MAX_RETRIES) {
            retry = false;
            Vector3 end = _disableSpawnDistanceTmp ? findPositionAroundPosition(start + dir * newWaypointsRadius, newWaypointsRadius) : findPositionAroundPosition(start, newWaypointsRadius);
            foreach (Vector3 spawn in _playerSpawnPoints) {
               if (Vector2.Distance(spawn, end) < _minDistanceToSpawnPath) {
                  retry = true;
                  retries++;
                  break;
               }
            }

            if (!retry) {
               return end;
            }
         }

         return findPositionAroundPosition(start, newWaypointsRadius);
      }

      // Otherwise - go back to original location of the ship
      return findPositionAroundPosition(_originalPosition, patrolingWaypointsRadius);
   }

   private void enableSpawnDistance () {
      _disableSpawnDistanceTmp = false;
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

      NetEntity enemyEntity = attackerIdentity.GetComponent<NetEntity>();
      Vector3 projectedPosition = attackerIdentity.transform.position;

      if (enemyEntity) {
         // Find a point ahead of the target to path find to
         projectedPosition = enemyEntity.getProjectedPosition(maxSecondsOnChasePath / 2.0f);
      }

      // If we have an attacker, find new position around it
      return findPositionAroundPosition(projectedPosition, attackingWaypointsRadius);
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

      if (_disableSpawnDistanceTmp) {
         Invoke("enableSpawnDistance", 3.5f);
      }
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
      if (instance == null) {
         return;
      }

      float degreesToDot = aggroConeDegrees / 90.0f;

      foreach (NetworkBehaviour iBehaviour in instance.getEntities()) {
         NetEntity iEntity = iBehaviour as NetEntity;

         // If the entity is a fellow bot ship with same guild, ignore it
         if (iEntity == null || (iEntity.isBotShip() && iEntity.guildId == guildId)) {
            continue;
         }

         // Reset the z axis value to ensure the square magnitude is not compromised by the z axis
         Vector3 currentPosition = transform.position;
         currentPosition.z = 0;
         Vector3 targetPosition = iEntity.transform.position;
         targetPosition.z = 0;

         // If enemy isn't within radius, early out
         if ((currentPosition - targetPosition).sqrMagnitude > aggroConeRadius * aggroConeRadius) {
            continue;
         }

         // If enemy isn't within cone aggro range, early out
         if (Vector2.Dot(Util.getDirectionFromFacing(facing), (targetPosition - currentPosition).normalized) < 1.0f - degreesToDot) {
            continue;
         }
       
         // We can see the enemy, attack it
         _attackers[iEntity.netId] = NetworkTime.time;
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
               if (primaryAbilityId > 0) {
                  ShipAbilityData shipAbilityData = ShipAbilityManager.self.getAbility(primaryAbilityId);
                  fireAtSpot(attackerPosition, shipAbilityData.abilityId, 0, 0, transform.position);
               } else {
                  ShipAbilityData shipAbilityData = ShipAbilityManager.self.getAbility(Attack.Type.Cannon);
                  fireAtSpot(attackerPosition, shipAbilityData.abilityId, 0, 0, transform.position);
               }
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
            Area area = GetComponentInParent<Area>();
            areaKey = area.areaKey;

         } else if (field.k.CompareTo(DataField.SHIP_GUILD_ID) == 0) {
            int id = int.Parse(field.v.Split(':')[0]);
            guildId = id;
         }
      }
   }

   public void setShipData (int enemyXmlData, Ship.Type shipType, int instanceDifficulty) {
      ShipData shipData = ShipDataManager.self.getShipData(shipType);
      if (shipData != null && (int) shipType != -1) {
         if (shipData.spritePath != "") {
            spritesContainer.GetComponent<SpriteSwap>().newTexture = ImageManager.getSprite(shipData.spritePath).texture;
         }
      } else {
         shipData = ShipDataManager.self.shipDataList[0];
         D.debug("Cant get ship data for: {" + shipType + "}");
      }
      SeaMonsterEntityData seaEnemyData = SeaMonsterManager.self.getMonster(enemyXmlData);
      if (seaEnemyData == null) {
         D.debug("Failed to get sea monster data");
      }

      initializeAsSeaEnemy(seaEnemyData, shipData, instanceDifficulty);

      // Assign ripple sprites
      _ripplesStillSprites = ImageManager.getTexture(Ship.getRipplesPath(shipType));
      _ripplesMovingSprites = ImageManager.getTexture(Ship.getRipplesMovingPath(shipType));
      if (shipData.rippleSpritePath != "") {
         ripplesContainer.GetComponent<SpriteSwap>().newTexture = _ripplesStillSprites;
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

   // The time at which we started chasing an enemy, on this path
   private double _chaseStartTime = 0.0f;

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

   // Are we currently chasing an enemy
   private bool _isChasingEnemy;

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

   // Spawn points placed in the area that bot ships should avoid
   private List<Vector3> _playerSpawnPoints = new List<Vector3>();

   // Const values to calculate distance to spawn points
   private static float MIN_DISTANCE_TO_SPAWN_PERCENT = 0.3f;
   private static float MIN_DISTANCE_TO_SPAWN_PATH_PERCENT = 0.4f;

   // Distance values that bot ship should keep from the spawn points, calculated for current area
   private float _minDistanceToSpawn;
   private float _minDistanceToSpawnPath;

   // The flag which temporarily disables avoiding spawn points
   private bool _disableSpawnDistanceTmp = false;

   // The last spawn point that bot ship was nearby and had to change its path
   private Vector3 _lastSpawnPosition = Vector3.zero;

   #endregion
}
