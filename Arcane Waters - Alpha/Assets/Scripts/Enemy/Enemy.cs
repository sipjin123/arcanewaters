using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;
using Pathfinding;
using TMPro;

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
      Pirate_Base = 228, Pirate_Healer = 229, Pirate_Shooter = 230, Pirate_Tank = 231, Pirate_Wisp = 232, 
      Elemental_Base = 233, Elemental_Assassin = 234, Elemental_Healer = 235, Elemental_Ranged = 236, Elemental_Tank = 237, Pirate_Berzerker = 238,
      Skelly_Captain = 239, Skelly_Captain_Tutorial = 240, Skelly_Healer = 241, Skelly_Shooter = 242, Skelly_Tank = 243, Skelly_Assassin = 244, Skelly_Tutorial_Leader = 245,
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

   // If this is a mini boss
   public bool isMiniBoss;

   // Determines if this battler is a support
   [SyncVar]
   public bool isSupportType;

   // Determines if this battler can walk aroynd
   [SyncVar]
   public bool isStationary;

   // Our body animator
   public SimpleAnimation bodyAnim;

   // Our shadow animator
   public SimpleAnimation shadowAnim;

   // Static shadow object beneath enemy character
   public GameObject shadowObject;

   // The shadow renderer
   public SpriteRenderer shadowRenderer;

   // The shadow to use for large enemies
   public Sprite largeShadowSprite;

   // A convenient reference to our collider
   public CircleCollider2D circleCollider;

   // A collider that toggles On if the unit is a boss (to avoid player rendering over a boss)
   public GameObject bossCollider;

   // Reference to the collider that triggers combat
   public EnemyBattleCollider enemyBattleCollider;

   // The canvas that allows user to hover over the enemy
   public Canvas highlightCanvas;

   // The text ui for the enemy name
   public TextMeshProUGUI displayNameText;

   // The possible spawn positions for the loot bags
   public Transform[] lootSpawnPositions;

   // Sound when character starts moving around
   public SoundManager.Type walkSound = SoundManager.Type.None;

   // The z snap offset of boss monsters which have larger sprites
   public const float BOSS_Z_OFFSET = -0.48f;

   // Prefab of special collider, used after golem boss death
   public GameObject bossGolemPolygonCollider;

   // Prefab of special collider, used after lizard boss death
   public GameObject bossLizardPolygonCollider;

   // The combat collider of boss type monsters
   public const float BOSS_COMBAT_COLLIDER = 0.45f;

   // If this unit is respawning
   public bool isRespawning = false;

   // The respawn timer
   public float respawnTimer = -1;

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

      // Debug logs to catch a null reference haunting the server logs
      if (this.sortPoint == null) {
         Debug.LogError("The sort point is not defined in the enemy " + this.name + " in area " + areaKey + ". This should not happen and must be investigated.");
      }

      // Set our name to something meaningful
      this.name = "Enemy - " + this.enemyType;

      // Make note of where we start at
      _startPos = this.sortPoint.transform.position;

      // Update our sprite
      if (this.enemyType != Type.None && !Util.isBatch()) {
         if (!System.Enum.IsDefined(typeof(Enemy.Type), (int) this.enemyType)) {
            Debug.LogError("The enemy type " + this.enemyType + " is not defined in the Enemy.Type enum (area " + areaKey + "). This should not happen and must be investigated.");
         }

         if (bodyAnim == null) {
            Debug.LogError("The bodyAnim is not defined in the enemy " + this.name + " in area " + areaKey + ". This should not happen and must be investigated.");
         }

         if (bodyAnim.GetComponent<SpriteSwap>() == null) {
            Debug.LogError("Could not find the SpriteSwap component in the enemy " + this.name + " in area " + areaKey + ". This should not happen and must be investigated.");
         }

         string enemySpriteName = System.Enum.GetName(typeof(Enemy.Type), (int) this.enemyType).ToLower();
         bodyAnim.GetComponent<SpriteSwap>().newTexture = ImageManager.getTexture("Enemies/LandMonsters/" + enemySpriteName);

         BattlerData battlerData = MonsterManager.self.getBattlerData(this.enemyType);

         shadowObject.transform.localScale = new Vector2(battlerData.shadowScale, battlerData.shadowScale);
         shadowObject.transform.localPosition = new Vector3(battlerData.shadowOffset.x, battlerData.shadowOffset.y, shadowObject.transform.localPosition.z);

         if (isBossType) {
            shadowRenderer.sprite = largeShadowSprite;
         }
      }
      bodyAnim.group = animGroupType;
      
      if (isBossType) {
         if (bossCollider == null) {
            Debug.LogError("The bossCollider is not defined in the enemy " + this.name + " in area " + areaKey + ". This should not happen and must be investigated.");
         }

         if (GetComponent<Rigidbody2D>() == null) {
            Debug.LogError("Could not find the Rigidbody2D component in the enemy " + this.name + " in area " + areaKey + ". This should not happen and must be investigated.");
         }

         bossCollider.SetActive(true);
         enemyBattleCollider.battleCollider.radius = BOSS_COMBAT_COLLIDER;
         GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
         _zSnap.sortPoint.transform.localPosition = new Vector3(0, BOSS_Z_OFFSET, 0);
         _zSnap.initialize();
      }

      if (isServer) {
         _seeker = GetComponent<Seeker>();

         if (_seeker == null) {
            Debug.LogError("Could not find the Seeker component in the enemy " + this.name + " in area " + areaKey + ". This should not happen and must be investigated.");
         }

         if (AreaManager.self.getArea(areaKey) == null) {
            Debug.LogError("The area was null when initializing the enemy " + this.name + " in area " + areaKey + ". This should not happen and must be investigated.");
         }

         isMiniBoss = MonsterManager.self.getBattlerData(enemyType).isMiniBoss;

         // Only use the graph in this area to calculate paths
         GridGraph graph = AreaManager.self.getArea(areaKey).getGraph();
         _seeker.graphMask = GraphMask.FromGraph(graph);
         _seeker.pathCallback = setPath_Asynchronous;

         InvokeRepeating(nameof(landMonsterBehavior), Random.Range(0f, 1f), 1f);
      } else {
         displayNameText.text = MonsterManager.self.getBattlerData(enemyType).enemyName;
      }
   }

   private void OnEnable () {
      // Don't display enemy name to start
      displayNameText.enabled = false;
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
         _outline.setVisibility(false);
         displayNameText.enabled = false;
         highlightCanvas.enabled = false;
         if (respawnTimer > 0 && !isRespawning) {
            isRespawning = true;
            Invoke(nameof(destroyCorpse), respawnTimer);
         }
         enemyBattleCollider.gameObject.SetActive(false);

         if (shouldDisableColliderOnDeath()) {
            circleCollider.enabled = false;

            if (this.enemyType == Type.Golem_Boss && _bossGolemColliderRef == null) {
               _bossGolemColliderRef = Instantiate(bossGolemPolygonCollider);
               _bossGolemColliderRef.transform.SetParent(this.transform);
               _bossGolemColliderRef.transform.localPosition = Vector3.zero;
               bossCollider.SetActive(false);

               if (this.facing == Direction.West) {
                  _bossGolemColliderRef.transform.localScale = new Vector3(-1f, 1f, 1f);
               }
            }
            else if (this.enemyType == Type.Lizard_King && _bossLizardColliderRef == null) {
               _bossLizardColliderRef = Instantiate(bossLizardPolygonCollider);
               _bossLizardColliderRef.transform.SetParent(this.transform);
               _bossLizardColliderRef.transform.localPosition = new Vector3(0.0f, -0.185f, 0.0f);
               bossCollider.SetActive(false);

               if (this.facing == Direction.East) {
                  _bossLizardColliderRef.transform.localScale = new Vector3(-1f, 1f, 1f);
               }
            }
         }
         if (MonsterManager.self.getBattlerData(this.enemyType).disableOnDeath) {
            bodyAnim.gameObject.SetActive(false);
         }
      }

      if (!this.isServer || isDefeated) {
         return;
      }

      if (!isInBattle()) {
         // Calculate the new facing direction based on our velocity direction vector
         Direction newFacingDirection = DirectionUtil.getBodyDirectionForVelocity(this);

         // Only touch the Sync Var if it's actually changed
         if (this.facing != newFacingDirection) {
            this.facing = newFacingDirection;
         }
      }
   }

   private void destroyCorpse () {
      bool isSpawnPointVacant = false;
      float vacancyDistance = .85f;
      Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, vacancyDistance);
      foreach (Collider2D collidedEntity in hits) {
         if (collidedEntity != null) {
            if (collidedEntity.GetComponent<PlayerBodyEntity>() != null) {
               isSpawnPointVacant = true;
               break;
            }
         }
      }

      if (isSpawnPointVacant) {
         Invoke(nameof(destroyCorpse), respawnTimer);
      } else {
         Instance currInstance = InstanceManager.self.getInstance(instanceId);
         if (currInstance != null) {
            currInstance.removeEntityFromInstance(this);
         }
         EnemyManager.self.spawnEnemyAtLocation(enemyType, InstanceManager.self.getInstance(instanceId), transform.localPosition, respawnTimer);
         NetworkServer.Destroy(gameObject);
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

      // Do not process path finding if stationary
      if (isStationary) {
         return;
      }

      // Only change our movement if enough time has passed
      if (NetworkTime.time - _lastMoveChangeTime < MOVE_CHANGE_INTERVAL) {
         return;
      }

      if (_currentPathIndex < _currentPath.Count) {
         // Move towards our current waypoint
         Vector2 waypointDirection = ((Vector2) _currentPath[_currentPathIndex] - (Vector2) sortPoint.transform.position).normalized;

         _body.AddForce(waypointDirection * 50f);
         _lastMoveChangeTime = NetworkTime.time;

         // Clears a node as the unit passes by
         float sqrDistanceToWaypoint = Vector2.SqrMagnitude(_currentPath[_currentPathIndex] - sortPoint.transform.position);
         if (sqrDistanceToWaypoint < .01f) {
            ++_currentPathIndex;
         }

         // Make note of the time
         _lastMoveChangeTime = NetworkTime.time;
      }
   }

   public bool isPlayerClose () {
      if (Global.player == null) {
         return false;
      }

      return (Vector2.Distance(this.sortPoint.transform.position, Global.player.sortPoint.transform.position) < NPC.TALK_DISTANCE);
   }

   protected void handleAnimations () {
      Anim.Type newAnimType = bodyAnim.currentAnimation;

      // Check if we're moving
      if (isDefeated) {
         newAnimType = Anim.Type.Death_East;
      } else if (isInBattle() && newAnimType != Anim.Type.Idle_East) {
         newAnimType = Anim.Type.Idle_East;
      } else if (getVelocity().magnitude > .01f) {
         // Check our facing direction
         if (this.facing == Direction.North) {
            newAnimType = Anim.Type.Run_North;
         } else if (this.facing == Direction.East || this.facing == Direction.West) {
            newAnimType = Anim.Type.Run_East;
         } else if (this.facing == Direction.South) {
            newAnimType = Anim.Type.Run_South;
         }

         // If enemy starts moving around - play sound
         if (_isIdle) {
            _isIdle = false;
            if (getWalkingSound() != SoundManager.Type.None) {
               //SoundManager.playAttachedClip(getWalkingSound(), transform);
               SoundEffectManager.self.playAttachedWithType(getWalkingSound(), this.gameObject);
            }
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
         _isIdle = true;
      }

      // Play the new animation
      if (newAnimType != Anim.Type.Death_East || (newAnimType == Anim.Type.Death_East && bodyAnim.currentAnimation != Anim.Type.Death_East)) {
         bodyAnim.playAnimation(newAnimType);
      }

      if (newAnimType == Anim.Type.Death_East && !_shadowAnimDataCopied) {
         // Use only once to setup shadowAnim
         _shadowAnimDataCopied = true;

         // Use reflection to copy data from bodyAnim to shadowAnim component
         System.Type type = bodyAnim.GetType();
         System.Reflection.FieldInfo[] fields = type.GetFields();
         foreach (System.Reflection.FieldInfo field in fields) {
            field.SetValue(shadowAnim, field.GetValue(bodyAnim));
         }

         // Assign death animation spritesheet to the shadow
         assignDeathShadowSprite(enemyType, bodyAnim, shadowAnim, shadowObject);

         // Play animation, leave the scope and do not use it again
         shadowAnim.playAnimation(newAnimType);
      }
   }

   public static void assignDeathShadowSprite (Enemy.Type enemyType, SimpleAnimation bodyAnim, SimpleAnimation shadowAnim, GameObject shadowObject) {
      // Use animated shadow during death animation instead of static one
      Sprite sprite = null;
      if (enemyType.ToString().StartsWith("Skelly")) {
         string name = bodyAnim.GetComponent<SpriteRenderer>().sprite.name;
         if (name.Contains("skelly")) {
            sprite = ImageManager.getSprite("Sprites/EnemyShadows/SkellyShadow.png");
         }
      }
      // Standard case - use group type name
      else {
         sprite = ImageManager.getSprite("Sprites/EnemyShadows/" + shadowAnim.group.ToString().Replace("_", "") + "Shadow.png");
      }

      // Assign shadow sprite and use it during death animation instead of static shadow sprite
      if (sprite) {
         shadowAnim.GetComponent<SpriteRenderer>().sprite = sprite;
         shadowAnim.transform.localScale = new Vector3(1f, 1f, 1f);
         shadowObject.transform.localScale = new Vector3(0f, 0f, 0f);
      } else {
         shadowAnim.gameObject.SetActive(false);
      }
   }
   
   public void assignBattleId (int newBattleId, NetEntity aggressor) {
      // Assign the Sync Var
      this.battleId = newBattleId;
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
      _seeker.StartPath(sortPoint.transform.position, targetPosition);

      while (!_seeker.IsDone()) {
         yield return null;
      }
   }

   protected bool shouldDisableColliderOnDeath () {
      // Some enemies should stop blocking player movement after they die
      switch (this.enemyType) {
         case Type.Slime:
         case Type.Golem_Boss:
         case Type.Lizard_King:
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

   public override void setAreaParent (Area area, bool worldPositionStays) {
      this.transform.SetParent(area.enemyParent, worldPositionStays);
   }

   private SoundManager.Type getWalkingSound () {
      switch (enemyType) {
         case Type.Skelly_Captain_Tutorial:
         case Type.Skelly_Captain:
            return SoundManager.Type.Skeleton_Walk;
      }

      return SoundManager.Type.None;
   }

   public override bool isDead () {
      return isDefeated;
   }

   public void modifyEnemyType (Enemy.Type newEnemyType) {
      this.enemyType = newEnemyType;
   }

   public override bool isLandEnemy () { return true; }

   #region Private Variables

   // Our zSnap component
   protected ZSnap _zSnap = default;

   // The position we were created at
   protected Vector2 _startPos = Vector2.negativeInfinity;

   // The Seeker that handles Pathfinding
   protected Seeker _seeker = default;

   // The current waypoint List
   protected List<Vector3> _currentPath = new List<Vector3>();

   // Store velocity magnitude of enemy character from previous Update() frame
   protected bool _isIdle = true;

   // The current Point Index of the path
   private int _currentPathIndex;

   // Gets set to true when the moving around behavior is running
   private bool _isMovingAround = false;

   // Checks whether data has already been copied to shadow simple animation when playing dead animation
   private bool _shadowAnimDataCopied = false;

   // Reference which stores special collider for boss golem after death
   private GameObject _bossGolemColliderRef = null;

   // Reference which stores special collider for boss lizard after death
   private GameObject _bossLizardColliderRef = null;

   #endregion
}
