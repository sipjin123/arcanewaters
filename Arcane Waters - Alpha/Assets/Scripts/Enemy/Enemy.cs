using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using Pathfinding;

public class Enemy : NetEntity, IMapEditorDataReceiver {
   #region Public Variables

   // The Type of Enemy
   public enum Type {
      None = 0,
      Plant = 100, Golem = 101, Slime = 102, Golem_Boss = 103, Lizard_King = 104,
      Coralbow = 200, Entarcher = 201, Flower = 202, Muckspirit = 203, Treeman = 204,
      Lizard = 205, Shroom = 206, Wisp = 207, Lizard_Armored = 208, Lizard_Shaman = 209, Lizard_Wizard = 210,
      Lizard_Sword = 211, Lizard_Captain = 212, Lizard_Champion = 213, 
      Wisp_Armored = 214, Wisp_Healer = 215, Wisp_Inferno = 216, Wisp_Purple = 217, Wisp_Yokai = 218,
      Snake_Base = 219, Snake_Assassin = 220, Snake_Healer = 221, Snake_Ranged = 222, Snake_Tank = 223,
      Shroom_Luminous = 224, Shroom_Old = 225, Shroom_Toxic = 226, Shroom_Warrior = 227,
      Pirate_Base = 228, Pirate_Healer = 229, Pirate_Shooter = 230, Pirate_Tank = 231, Pirate_Wisp = 232, Pirate_Berzerker = 238,
      Elemental_Base = 233, Elemental_Assassin = 234, Elemental_Healer = 235, Elemental_Ranged = 236, Elemental_Tank = 237,
      PlayerBattler = 305,
   }

   // The Type of animation the Enemy is associated with
   [SyncVar]
   public Anim.Group animGroupType;

   // The Type of Enemy
   [SyncVar]
   public Type enemyType;

   // Gets set to true after we've been defeated
   [SyncVar]
   public bool isDefeated;

   // Determines if this battler is a boss
   [SyncVar]
   public bool isBossType;

   // Our body animator
   public SimpleAnimation bodyAnim;

   // The position we want to move to, set on the server
   [SyncVar]
   public Vector2 desiredPosition;

   // A convenient reference to our collider
   public CircleCollider2D circleCollider;

   // A collider that toggles On if the unit is a boss (to avoid player rendering over a boss)
   public GameObject bossCollider;

   #endregion

   protected override void Awake () {
      base.Awake();

      // Look up components
      _zSnap = GetComponent<ZSnap>();

      // We can enable the Circle collider on clients
      if (NetworkClient.active) {
         circleCollider.enabled = true;
      }
   }

   protected override void Start () {
      base.Start();

      // Set our name to something meaningful
      this.name = "Enemy - " + this.enemyType;

      // Make note of where we start at
      _startPos = this.sortPoint.transform.position;

      // Update our sprite
      if (this.enemyType != Type.None && ImageManager.self.imageDataList.Count > 0) {
         string enemySpriteName = System.Enum.GetName(typeof(Enemy.Type), (int) this.enemyType).ToLower();
         bodyAnim.GetComponent<SpriteSwap>().newTexture = ImageManager.getTexture("Enemies/LandMonsters/" + enemySpriteName);
      }
      bodyAnim.group = animGroupType;

      if (isBossType) {
         bossCollider.SetActive(true);
         GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
      }

      if (isServer) {
         _seeker = GetComponent<Seeker>();
         if (_seeker == null) {
            D.error("There has to be a Seeker Script attached to the Enemy Prefab");
         }

         // Only use the graph in this area to calculate paths
         GridGraph graph = AreaManager.self.getArea(areaKey).getGraph();
         _seeker.graphMask = GraphMask.FromGraph(graph);

         _seeker.pathCallback = setPath_Asynchronous;

         InvokeRepeating(nameof(landMonsterBehavior), Random.Range(0f, 1f), 1f);
      }
   }

   protected override void Update () {
      base.Update();

      // Handle animating the enemy
      if (animGroupType != Anim.Group.None) {
         handleAnimations();
      }

      // Some enemies should stop blocking player movement after they die
      if (isDefeated) {
         _body.mass = 9999;

         if (shouldDisableColliderOnDeath()) {
            circleCollider.enabled = false;
         }
         if (MonsterManager.self.getBattler(this.enemyType).disableOnDeath) {
            bodyAnim.gameObject.SetActive(false);
         }
      }

      if (!this.isServer || isDefeated) {
         return;
      }

      // Calculate the new facing direction based on our velocity direction vector
      Direction newFacingDirection = DirectionUtil.getBodyDirectionForVelocity(this);

      // Only touch the Sync Var if it's actually changed
      if (this.facing != newFacingDirection) {
         this.facing = newFacingDirection;
      }
   }

   protected override void FixedUpdate () {
      // Only the Server moves Enemies
      if (!this.isServer || isDefeated) {
         return;
      }

      // If we're in a battle, don't move / Boss entities dont move
      if (isInBattle() || isBossType) {
         return;
      }

      // Only change our movement if enough time has passed
      if (Time.time - _lastMoveChangeTime < MOVE_CHANGE_INTERVAL) {
         return;
      }

      if (_currentPathIndex < _currentPath.Count) {
         // Move towards our current waypoint
         Vector2 waypointDirection = ((Vector2) _currentPath[_currentPathIndex] - (Vector2) sortPoint.transform.position).normalized;

         _body.AddForce(waypointDirection * 50f);
         _lastMoveChangeTime = Time.time;

         // Clears a node as the unit passes by
         float sqrDistanceToWaypoint = Vector2.SqrMagnitude(_currentPath[_currentPathIndex] - sortPoint.transform.position);
         if (sqrDistanceToWaypoint < .01f) {
            ++_currentPathIndex;
         }

         // Make note of the time
         _lastMoveChangeTime = Time.time;
      }
   }

   public void clientClickedMe () {
      if (Global.player == null || isDefeated) {
         return;
      }

      // Only works when the player is close enough
      if (!isPlayerClose()) {
         Instantiate(PrefabsManager.self.tooFarPrefab, this.transform.position + new Vector3(0f, .24f), Quaternion.identity);
         return;
      }

      // For now, require a sword to be equipped
      PlayerBodyEntity body = (PlayerBodyEntity) Global.player;
      if (!body.weaponManager.isHoldingWeapon()) {
         PanelManager.self.noticeScreen.show("You need to equip a weapon to attack this enemy!");
         return;
      }

      Global.player.rpc.Cmd_StartNewBattle(this.netId, Battle.TeamType.Attackers);
   }

   public bool isPlayerClose () {
      if (Global.player == null) {
         return false;
      }

      return (Vector2.Distance(this.transform.position, Global.player.transform.position) < NPC.TALK_DISTANCE);
   }

   protected void handleAnimations () {
      Anim.Type newAnimType = bodyAnim.currentAnimation;

      // Check if we're moving
      if (isDefeated) {
         newAnimType = Anim.Type.Death_East;
      } else if (getVelocity().magnitude > .01f) {
         // Check our facing direction
         if (this.facing == Direction.North) {
            newAnimType = Anim.Type.Run_North;
         } else if (this.facing == Direction.East || this.facing == Direction.West) {
            newAnimType = Anim.Type.Run_East;
         } else if (this.facing == Direction.South) {
            newAnimType = Anim.Type.Run_South;
         }
      } else {
         // Check our facing direction
         if (this.facing == Direction.North) {
            newAnimType = Anim.Type.Idle_North;
         } else if (this.facing == Direction.East || this.facing == Direction.West) {
            newAnimType = Anim.Type.Idle_East;
         } else if (this.facing == Direction.South) {
            newAnimType = Anim.Type.Idle_South;
         }
      }

      // Play the new animation
      bodyAnim.playAnimation(newAnimType);
   }

   public void assignBattleId (int newBattleId, NetEntity aggressor) {
      // Assign the Sync Var
      this.battleId = newBattleId;

      // Make sure we immediately stop any movement
      this.desiredPosition = this.transform.position;

      // Make us face toward the player who initiated the battle
      Vector2 vec = aggressor.transform.position - this.transform.position;
      Direction directionToFace = DirectionUtil.getBodyDirectionForVector(vec);
      this.facing = directionToFace;
   }

   [Server]
   private void setPath_Asynchronous (Path newPath) {
      _currentPath = newPath.vectorPath;
      _currentPathIndex = 0;
      _seeker.CancelCurrentPathRequest(true);
   }

   [Server]
   private void landMonsterBehavior () {
      // Skip in certain situations
      if (isInBattle() || isDefeated || isBossType) {
         return;
      }

      // Wait until the previous action finishes
      if (_isMovingAround) {
         return;
      }

      // Choose a new random position and move towards it
      StartCoroutine(CO_MoveToPosition(
         _startPos + Random.insideUnitCircle * 0.5f, Random.Range(3f, 9f)));
   }

   [Server]
   private IEnumerator CO_MoveToPosition (Vector2 pos, float endDelay) {
      _isMovingAround = true;

      yield return findAndSetPath_Asynchronous(pos);

      // The movement is performed in FixedUpdate
      while (_currentPathIndex < _currentPath.Count) {
         yield return new WaitForSeconds(0.1f);
      }

      if (endDelay >= 0) {
         yield return new WaitForSeconds(endDelay);
      }

      _isMovingAround = false;
   }

   [Server]
   private IEnumerator findAndSetPath_Asynchronous (Vector3 targetPosition) {
      if (!_seeker.IsDone()) {
         _seeker.CancelCurrentPathRequest();
      }
      _seeker.StartPath(transform.position, targetPosition);

      while (!_seeker.IsDone()) {
         yield return null;
      }
   }

   protected bool shouldDisableColliderOnDeath () {
      // Some enemies should stop blocking player movement after they die
      switch (this.enemyType) {
         case Type.Slime:
            return true;
         default:
            return false;
      }
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.LAND_ENEMY_DATA_KEY) == 0) {
            // Get ID from npc data field
            // Field arrives in format <npc id>: <npc name>
            int id = int.Parse(field.v.Split(':')[0]);
         }
      }
   }

   public static int fetchReceivedData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.LAND_ENEMY_DATA_KEY) == 0) {
            // Get ID from npc data field
            if (field.tryGetIntValue(out int id)) {
               return id;
            }
         }
      }
      return 0;
   }

   #region Private Variables

   // Our zSnap component
   protected ZSnap _zSnap;

   // The position we were created at
   protected Vector2 _startPos = Vector2.negativeInfinity;

   // The Seeker that handles Pathfinding
   protected Seeker _seeker;

   // The current waypoint List
   protected List<Vector3> _currentPath = new List<Vector3>();

   // The current Point Index of the path
   private int _currentPathIndex;

   // Gets set to true when the moving around behavior is running
   private bool _isMovingAround = false;

   #endregion
}
