using UnityEngine;
using MapCreationTool.Serialization;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public class SpiderWeb : TemporaryController, IMapEditorDataReceiver
{
   #region Public Variables

   // The constant part of the height that doesn't change and is added to the variable height
   public const float CONSTANT_HEIGHT = 0.5f;

   // The height of the jump, set in map editor
   public float jumpHeight;

   // Controls how parts of the player move while they're being bounced
   public AnimationCurve spriteHeightCurve, movementCurve, shadowCurve;

   // Controls how parts of the player move while they're doing a half bounce
   public AnimationCurve spriteHeightCurveHalf, movementCurveHalf, shadowCurveHalf;

   // The prefab we use for the bouncing spider web effect
   public GameObject webBouncePrefab;

   // The (X,Y) coordinates of the destination for this spider web
   public float destinationX, destinationY;

   // The trigger that will detect if the player jumps
   public SpiderWebTrigger jumpTrigger;

   // The type of biome this prefab is in
   public Biome.Type biomeType;

   // Reference to the 2d asset
   public Texture2D webIdleReference, webBounceReference;

   // Reference to the simple anim component
   public SimpleAnimation simpleAnim;

   // Bounce web frame indexes
   public int bounceWebMin, bounceWebMax;

   // Returns whether this web has a linked web
   public bool hasLinkedWeb => _linkedWeb != null;

   #endregion

   private void Awake () {
      jumpTrigger.web = this;
   }

   private void Start () {
      checkForOtherWebs();
   }

   private void checkForOtherWebs () {
      // Do an overlap circle at the destination point, if a web is found, link to it
      int layerMask = LayerMask.GetMask(LayerUtil.DEFAULT);

      Collider2D[] hits = Physics2D.OverlapCircleAll(getDestination(), 0.15f, layerMask);

      foreach (Collider2D hit in hits) {
         SpiderWeb web = hit.GetComponent<SpiderWeb>();
         if (web && web != this) {
            _linkedWeb = web;
            break;
         }
      }
   }

   public void initializeBiome (Biome.Type biome) {
      simpleAnim = GetComponent<SimpleAnimation>();
      biomeType = biome;
      switch (biome) {
         case Biome.Type.Forest:
            simpleAnim.updateIndexMinMax(0, 1);

            bounceWebMin = 0;
            bounceWebMax = 5;
            break;
         case Biome.Type.Desert:
            simpleAnim.updateIndexMinMax(2, 3);

            bounceWebMin = 6;
            bounceWebMax = 11;
            break;
         case Biome.Type.Pine:
            simpleAnim.updateIndexMinMax(6, 7);

            bounceWebMin = 18;
            bounceWebMax = 23;
            break;
         case Biome.Type.Snow:
            simpleAnim.updateIndexMinMax(4, 5);

            bounceWebMin = 12;
            bounceWebMax = 17;
            break;
         case Biome.Type.Lava:
            simpleAnim.updateIndexMinMax(8, 9);

            bounceWebMin = 24;
            bounceWebMax = 29;
            break;
         case Biome.Type.Mushroom:
            simpleAnim.updateIndexMinMax(10, 11);

            bounceWebMin = 30;
            bounceWebMax = 35;
            break;
      }
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.SPIDER_WEB_X_KEY) == 0) {
            if (field.tryGetFloatValue(out float value)) {
               destinationX = value * MapCreationTool.SpiderWebMapEditor.EDITOR_SCALING_FACTOR;
            }
         }

         if (field.k.CompareTo(DataField.SPIDER_WEB_Y_KEY) == 0) {
            if (field.tryGetFloatValue(out float value)) {
               destinationY = value * MapCreationTool.SpiderWebMapEditor.EDITOR_SCALING_FACTOR;
            }
         }
      }
   }

   protected override void startControl (ControlData puppet) {
      // Registers the bounce pad action status to the achievement data for recording
      if (puppet.entity.isServer) {
         AchievementManager.registerUserAchievement(puppet.entity, ActionType.JumpOnBouncePad);
      }

      puppet.endPos = getDestination();

      // Run only in client
      if (puppet.entity.isClient) {
         // Instantiate the bounce effect
         SimpleAnimation anim = Instantiate(webBouncePrefab, transform.position, Quaternion.identity).GetComponent<SimpleAnimation>();
         anim.updateIndexMinMax(bounceWebMin, bounceWebMax);

         SoundEffectManager.self.playFmodSfx(SoundEffectManager.WEB_JUMP, transform.position);

         // Command to play the sound effect for other clients, using a RPC
         puppet.entity.Cmd_PlayWebSound(transform.position);
      }

      // Determine what direction the player should be facing in while they bounce / fall
      float bounceAngle = Util.angle(getDestination() - (Vector2) puppet.entity.transform.position);
      Direction bounceDir = Util.getFacing(bounceAngle);

      puppet.entity.fallDirection = (int) bounceDir;
      puppet.entity.facing = bounceDir;

      if (puppet.entity.isLocalPlayer) {
         getPlayer(puppet).noteWebBounce(this);
      }
   }

   protected override void controlUpdate (ControlData puppet) {
      if (puppet.entity.isLocalPlayer) {
         updateLocalPlayerPosition(puppet);
      }

      AnimationCurve moveCurve = (puppet.entity.passedOnTemporaryControl) ? movementCurveHalf : movementCurve;

      // End control if time has run out
      if (puppet.time >= moveCurve.keys.Last().time) {
         if (puppet.entity.isLocalPlayer) {
            puppet.entity.getRigidbody().MovePosition(puppet.endPos);
         }
         puppet.entity.fallDirection = 0;
         endControl(puppet);
         onControlEnded(puppet);
      }
   }

   private void onControlEnded (ControlData puppet) {
      if (_linkedWeb) {
         // Give control to linked web
         puppet.entity.passedOnTemporaryControl = true;
         _linkedWeb.tryBouncePlayer(puppet.entity.getPlayerBodyEntity());
      } else {
         puppet.entity.passedOnTemporaryControl = false;
      }
   }

   private void updateLocalPlayerPosition (ControlData puppet) {
      float timeSinceBounce = puppet.time;

      bool continuedBounce = puppet.entity.passedOnTemporaryControl;

      AnimationCurve moveCurve = (continuedBounce) ? movementCurveHalf : movementCurve;

      // Move the player according to animation curves
      float t = moveCurve.Evaluate(timeSinceBounce);

      PlayerBodyEntity player = getPlayer(puppet);

      // If the player is jumping onto a web, we need to scale the animation
      if (!continuedBounce) {
         // Up until the player bounces, we want them to jump at a normal speed
         if (t <= BOUNCE_POINT) {
            t /= BOUNCE_POINT;
            puppet.entity.getRigidbody().MovePosition(Vector3.LerpUnclamped(puppet.startPos, transform.position, t));

            // Over the rest of the bounce, the player needs to make it to the end point
         } else {
            t = (t - BOUNCE_POINT) / (1.0f - BOUNCE_POINT);
            puppet.entity.getRigidbody().MovePosition(Vector3.LerpUnclamped(transform.position, puppet.endPos, t));
         }
      } else {
         puppet.entity.getRigidbody().MovePosition(Vector3.LerpUnclamped(transform.position, puppet.endPos, t));
      }
   }

   protected override void onForceFastForward (ControlData puppet) {
      if (puppet.entity.isLocalPlayer) {
         puppet.entity.transform.position = puppet.endPos;
      }
      puppet.entity.fallDirection = 0;
   }

   public void tryBouncePlayer (PlayerBodyEntity player) {
      if (player == null || !player.isLocalPlayer) {
         return;
      }

      // Only check for colliders if  we're not bouncing to another web
      if (!_linkedWeb) {
         // Check that there are no colliders at the arriving position
         if (checkForColliders(getDestination(), player.getMainCollider().radius)) {
            return;
         }
      }

      tryTriggerController(player);
   }

   private PlayerBodyEntity getPlayer (ControlData puppet) {
      int playerID = puppet.entity.userId;
      if (_playerIDEntityPairs.ContainsKey(playerID)) {
         return _playerIDEntityPairs[playerID];
      } else {
         PlayerBodyEntity newPlayer = puppet.entity.GetComponent<PlayerBodyEntity>();
         _playerIDEntityPairs[playerID] = newPlayer;
         return newPlayer;
      }
   }

   public AnimationCurve getSpriteHeightCurve (bool isHalfBounce) {
      return (isHalfBounce) ? spriteHeightCurveHalf : spriteHeightCurve;
   }

   public AnimationCurve getShadowCurve (bool isHalfBounce) {
      return (isHalfBounce) ? shadowCurveHalf : shadowCurve;
   }

   public float getBounceDuration () {
      return movementCurve.keys.Last().time;
   }

   private bool checkForColliders (Vector2 checkPosition, float checkRadius) {
      // Find any colliders at the location
      int colCount = Physics2D.OverlapCircle(checkPosition, checkRadius, new ContactFilter2D { useTriggers = false }, _colliderBuffer);

      // Ignore enemies and players
      foreach (Collider2D collider in _colliderBuffer) {
         if (collider != null && (collider.gameObject.layer == LayerMask.NameToLayer(LayerUtil.PLAYER_BIPEDS) || collider.GetComponent<Enemy>() || collider.GetComponent<PlayerBodyEntity>())) {
            colCount--;
         }
      }

      // Return true if we found any colliders
      return colCount > 0;
   }

   private Vector2 getDestination () {
      return new Vector2(destinationX, destinationY) + (Vector2) transform.position;
   }

   private void OnDrawGizmosSelected () {
      Gizmos.color = Color.red;
      Gizmos.DrawWireSphere(transform.position + new Vector3(destinationX, destinationY, 0.0f), 0.15f);
   }

   #region Private Variables

   // The radius of the player's main collider
   private const float PLAYER_COLLIDER_RADIUS = 0.06f;

   // A dictionary of player IDs, with their PlayerBodyEntities
   private Dictionary<int, PlayerBodyEntity> _playerIDEntityPairs = new Dictionary<int, PlayerBodyEntity>();

   // A reference to the web we are linked to, if we are linked to one.
   private SpiderWeb _linkedWeb = null;

   // How far through the animation the player 'bounces'
   private const float BOUNCE_POINT = 0.25f;

   #endregion
}
