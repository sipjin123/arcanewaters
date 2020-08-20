using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using Pathfinding;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Seeker))]
public class OfflineNPC : MonoBehaviour {
   #region Public Variables

   // The direction we're facing
   public Direction facing = Direction.East;

   // Whether or not this Entity has sprites for diagonal directions
   public bool hasDiagonals;

   // The area containing this NPC
   public OfflineArea offlineArea;

   // The movement speed of this NPC
   public float moveSpeed = 10;

   #endregion

   private void Awake () {
      _animators.AddRange(GetComponentsInChildren<Animator>());
      _renderers.AddRange(GetComponentsInChildren<SpriteRenderer>());
      _body = GetComponent<Rigidbody2D>();
      _seeker = GetComponent<Seeker>();

      if (_seeker == null) {
         _seeker = gameObject.AddComponent<Seeker>();
      }
   }

   private void Start () {
      foreach (Animator animator in _animators) {
         animator.SetInteger("facing", (int) facing);
      }

      // Only use the graph in this area to calculate paths
      GridGraph graph = offlineArea.getGraph();
      _seeker.graphMask = GraphMask.FromGraph(graph);
      _seeker.pathCallback = setPath_Asynchronous;
      _startPosition = transform.position;

      generateNewWaypoints();
   }

   private void Update () {
      Vector2 direction;
      if (_currentPathIndex < _currentPath.Count) {
         direction = (Vector2) _currentPath[_currentPathIndex] - (Vector2) transform.position;
      } else {
         direction = Util.getDirectionFromFacing(facing);
      }
      
      // Calculate an angle for that direction
      float angle = Util.angle(direction);

      // Set our facing direction based on that angle
      facing = hasDiagonals ? Util.getFacingWithDiagonals(angle) : Util.getFacing(angle);

      // Pass our angle and velocity on to the Animator
      foreach (Animator animator in _animators) {
         animator.SetFloat("velocityX", _body.velocity.x);
         animator.SetFloat("velocityY", _body.velocity.y);
         animator.SetBool("isMoving", _body.velocity.magnitude > .01f);
         animator.SetInteger("facing", (int) facing);
      }

      // Check if we're showing a West sprite
      bool isFacingWest = facing == Direction.West || facing == Direction.NorthWest || facing == Direction.SouthWest;

      // Flip our sprite renderer if we're going west
      foreach (SpriteRenderer renderer in _renderers) {
         renderer.flipX = isFacingWest;
      }
   }

   private void FixedUpdate () {
      if (_currentPathIndex < _currentPath.Count) {
         // Move towards our current waypoint
         // Only change our movement if enough time has passed
         float moveTime = Time.time - _lastMoveChangeTime;
         if (moveTime >= MOVE_CHANGE_INTERVAL) {            
            _body.AddForce(((Vector2) _currentPath[_currentPathIndex] - (Vector2) transform.position).normalized * moveSpeed);
            _lastMoveChangeTime = Time.time;
         }

         // Clears a node as the unit passes by
         float distanceToWaypoint = Vector2.Distance(_currentPath[_currentPathIndex], transform.position);
         if (distanceToWaypoint < .1f) {
            ++_currentPathIndex;
         }
      } else if (_seeker.IsDone() && _moving) {
         _moving = false;
         // Generate a new path
         Invoke("generateNewWaypoints", PAUSE_BETWEEN_PATHS);
      }
   }

   protected void generateNewWaypoints () {
      findAndSetPath_Asynchronous(_startPosition + Random.insideUnitCircle * MAX_MOVE_DISTANCE);
   }

   private void findAndSetPath_Asynchronous (Vector3 targetPosition) {
      if (!_seeker.IsDone()) {
         _seeker.CancelCurrentPathRequest();
      }
      _seeker.StartPath(transform.position, targetPosition);
   }

   private void setPath_Asynchronous (Path newPath) {
      _currentPath = newPath.vectorPath;
      _currentPathIndex = 0;
      _moving = true;
      _seeker.CancelCurrentPathRequest(true);
   }

   #region Private Variables

   // The Seeker that handles Pathfinding
   protected Seeker _seeker;

   // The current Path
   protected List<Vector3> _currentPath = new List<Vector3>();

   // The current Point Index of the Path
   private int _currentPathIndex;

   // Whether this NPC is moving
   private bool _moving;

   //The initial position of this NPC
   private Vector2 _startPosition;

   // Our various component references
   protected List<Animator> _animators = new List<Animator>();
   protected List<SpriteRenderer> _renderers = new List<SpriteRenderer>();
   protected Rigidbody2D _body;

   // How far the NPC will be able to move from it's starting position
   protected const float MAX_MOVE_DISTANCE = 0.5f;

   // How long, in seconds, the NPC should pause between finding new paths to walk
   protected const float PAUSE_BETWEEN_PATHS = 3.0f;

   // The amount of time that must pass between movement changes
   protected const float MOVE_CHANGE_INTERVAL = .05f;

   // The last time this NPC moved
   private float _lastMoveChangeTime;

   #endregion
}
