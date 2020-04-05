using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MapCreationTool.Serialization;

public class Enemy : NetEntity, IMapEditorDataReceiver {
   #region Public Variables

   // The Type of Enemy
   public enum Type {
      None = 0,
      Plant = 100, Golem = 101, Slime = 102, Golem_Boss = 103, Lizard_King = 104,
      Coralbow = 200, Entarcher = 201, Flower = 202, Muckspirit = 203, Treeman = 204,
      Lizard = 205, Shroom = 206, Wisp = 207, Lizard_Armored = 208, Lizard_Shaman = 209, Lizard_Wizard = 210,
      Lizard_Sword = 211, Lizard_Captain = 212, Lizard_Champion = 213, PlayerBattler = 305
   }

   // The Type of animation the Enemy is associated with
   [SyncVar]
   public Anim.Group animGroupType;

   // The Type of Enemy
   [SyncVar]
   public Type enemyType;

   // Our current target
   [SyncVar]
   public int targetUserId;

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
      _startPos = this.transform.position;

      // Update our sprite
      if (this.enemyType != Type.None) {
         string enemySpriteName = System.Enum.GetName(typeof(Enemy.Type), (int) this.enemyType).ToLower();
         bodyAnim.GetComponent<SpriteSwap>().newTexture = ImageManager.getTexture("Enemies/LandMonsters/" + enemySpriteName);
      }
      bodyAnim.group = animGroupType;

      if (isBossType) {
         bossCollider.SetActive(true);
         GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
      }

      // Choose a random desired position every few seconds
      if (isServer) {
         InvokeRepeating("chooseRandomDesiredPosition", 3f, 3f);
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

      // If we're close to our move target, we don't need to do anything
      if (Vector2.Distance(this.transform.position, desiredPosition) < .1f) {
         return;
      }

      // Figure out the direction of our movement
      Vector2 direction = this.desiredPosition - (Vector2) this.transform.position;
      _body.AddForce(direction.normalized * 50f);

      // Calculate the new facing direction based on our velocity direction vector
      Direction newFacingDirection = DirectionUtil.getBodyDirectionForVelocity(this);

      // Only touch the Sync Var if it's actually changed
      if (this.facing != newFacingDirection) {
         this.facing = newFacingDirection;
      }

      // Make note of the time
      _lastMoveChangeTime = Time.time;
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

   protected void chooseRandomDesiredPosition () {
      // Only the Server does this
      if (!this.isServer) {
         return;
      }

      // Don't do this while we have a target or we're in a battle
      if (targetUserId > 0 || isInBattle() || isDefeated || isBossType) {
         return;
      }

      // Have a chance of not moving
      if (Random.Range(0f, 1f) <= .60f) {
         return;
      }

      // Set a new desired position within +- 1 units
      this.desiredPosition = _startPos + new Vector2(Random.Range(-.5f, .5f), Random.Range(-.5f, .5f));
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

   #endregion
}
