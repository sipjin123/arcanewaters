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

   // References to the triggers that allow the player to jump up / down this web
   public SpiderWebTrigger jumpUpTrigger, jumpDownTrigger;

   #endregion

   protected void Awake () {
      jumpUpTrigger.web = this;
      jumpDownTrigger.web = this;
      jumpUpTrigger.bounceDirection = Direction.North;
      jumpDownTrigger.bounceDirection = Direction.South;
   }

   private void Start () {
      checkForOtherWebs();
      StartCoroutine(CO_PlaceJumpDownTrigger());
   }

   private void checkForOtherWebs () {
      // Half width of the spider web, for determining whether they are over one another
      float hWidth = 0.15f;
      float maxDistance = 1.0f;

      SpiderWeb[] webs = FindObjectsOfType<SpiderWeb>();

      // Check above
      foreach (SpiderWeb web in webs) {
         // Ignore this web
         if (web == this) {
            continue;
         }

         // Ignore webs not higher than us, or out of range
         float yDiff = web.transform.position.y - transform.position.y;
         if (yDiff <= 0.0f || Mathf.Abs(yDiff) > maxDistance) {
            continue;
         }

         // Check if it  is horizontally aligned above us
         float xDiff = web.transform.position.x - transform.position.x;
         if (Mathf.Abs(xDiff) > hWidth) {
            continue;
         }

         // Found a web above
         _webAbove = web;
         jumpDownTrigger.gameObject.SetActive(false);
         break;
      }

      // Check  below
      foreach (SpiderWeb web in webs) {
         // Ignore this web
         if (web == this) {
            continue;
         }

         // Ignore webs not lower than us, or out of range
         float yDiff = web.transform.position.y - transform.position.y;
         if (yDiff >= 0.0f || Mathf.Abs(yDiff) > maxDistance) {
            continue;
         }

         // Check if it  is horizontally aligned above us
         float xDiff = web.transform.position.x - transform.position.x;
         if (Mathf.Abs(xDiff) > hWidth) {
            continue;
         }

         // Found a web below
         _webBelow = web;
         jumpUpTrigger.gameObject.SetActive(false);
         break;
      }
   }

   private IEnumerator CO_PlaceJumpDownTrigger () {
      if (_webAbove) {
         yield break;
      }
      
      // Wait for colliders to be setup
      yield return new WaitForSeconds(2.0f);

      int numAttempts = 10;

      while (numAttempts > 0) {
         tryPlaceJumpDownTrigger();

         // Successfully placed
         if (!checkForColliders(jumpDownTrigger.transform.position, PLAYER_COLLIDER_RADIUS)) {
            break;
         }

         numAttempts--;
         // Wait a second and try again
         yield return new WaitForSeconds(1.0f);
      }
      
      if (numAttempts == 0) {
         Debug.LogWarning("Spider Web couldn't find a location for its Jump Down trigger");
      }
   }

   private void tryPlaceJumpDownTrigger () {
      float minimumDistance = 0.2f;
      int maxChecks = 50;
      float distancePerCheck = 0.05f;
      Vector3 placeLocation = Vector3.zero;
      bool foundLocation = false;

      // Check a number of different places above us, to find a location for the player to land
      for (int i = 0; i < maxChecks; i++) {
         Vector3 checkLocation = transform.position + (Vector3.up * (i * distancePerCheck + minimumDistance));

         if (checkForColliders(checkLocation, PLAYER_COLLIDER_RADIUS)) {
            continue;
         } else {
            placeLocation = checkLocation;
            foundLocation = true;
            break;
         }
      }

      if (foundLocation) {
         jumpDownTrigger.transform.position = placeLocation;
      } else {
         Debug.LogWarning("Spider Web couldn't find a location for the player to land");
      }
   }

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.SPIDER_WEB_HEIGHT_KEY) == 0) {
            if (field.tryGetFloatValue(out float h)) {
               jumpHeight = h + CONSTANT_HEIGHT;
            }
         }
      }
   }

   protected override void startControl (ControlData puppet) {
      // Registers the bounce pad action status to the achievement data for recording
      if (puppet.entity.isServer) {
         AchievementManager.registerUserAchievement(puppet.entity, ActionType.JumpOnBouncePad);
      }

      puppet.endPos = calculateEndPos(puppet.startPos);

      // Instantiate the bounce effect
      SimpleAnimation anim = Instantiate(webBouncePrefab, transform.position, Quaternion.identity).GetComponent<SimpleAnimation>();

      if (isDroppingPuppet(puppet)) {
         anim.delayStart = true;
         anim.initialDelay = 0.5f;
      }

      // Determine what direction the player should be facing in while they bounce / fall
      Direction fallDir = isDroppingPuppet(puppet) ? Direction.South : Direction.North;
      puppet.entity.fallDirection = (int) fallDir;
      puppet.entity.facing = fallDir;

      if (puppet.entity.isLocalPlayer) {
         getPlayer(puppet).noteWebBounce(this);
      }
   }

   protected override void controlUpdate (ControlData puppet) {
      if (puppet.entity.isLocalPlayer) {
         updateLocalPlayerPosition(puppet);
      }

      AnimationCurve moveCurve = (isContinuingWebBounce(puppet)) ? movementCurveHalf : movementCurve;

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
      if (isDroppingPuppet(puppet) && _webBelow) {
         // Give control to lower web
         _webBelow.tryBouncePlayer(puppet.entity.GetComponent<PlayerBodyEntity>());
      } else if (!isDroppingPuppet(puppet) && _webAbove) {
         // Give control to upper web
         _webAbove.tryBouncePlayer(puppet.entity.GetComponent<PlayerBodyEntity>());
      }
   }

   private void updateLocalPlayerPosition (ControlData puppet) {
      float timeSinceBounce = puppet.time;

      // If player is falling, reverse curve
      if (isDroppingPuppet(puppet)) {
         float bounceTime = (isContinuingWebBounce(puppet)) ? getBounceDuration() / 2.0f : getBounceDuration();
         timeSinceBounce = bounceTime - timeSinceBounce;
      }

      AnimationCurve moveCurve = (isContinuingWebBounce(puppet)) ? movementCurveHalf : movementCurve;

      // Move the player according to animation curves
      float t = moveCurve.Evaluate(timeSinceBounce);

      if (isDroppingPuppet(puppet)) {
         t = 1.0f - t;
      }

      PlayerBodyEntity player = getPlayer(puppet);
      puppet.entity.getRigidbody().MovePosition(Vector3.LerpUnclamped(puppet.startPos, puppet.endPos, t));
   }

   private bool isContinuingWebBounce (ControlData puppet) {
      return (isDroppingPuppet(puppet) && _webAbove) || (!isDroppingPuppet(puppet) && _webBelow);
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

      bool bouncingToWebBelow = isAboveWeb(player.transform) && _webBelow;
      bool bouncingToWebAbove = !isAboveWeb(player.transform) && _webAbove;
      
      // Only check for colliders if  we're not bouncing to another web
      if (!bouncingToWebAbove && !bouncingToWebBelow) {
         // Check that there are no colliders at the arriving position
         if (checkForColliders(calculateEndPos(player.transform.position), player.getMainCollider().radius)) {
            return;
         }
      }

      tryTriggerController(player);
   }

   private bool isAboveWeb (Transform entity) {
      return (entity.position.y > transform.position.y);
   }

   private bool isDroppingPuppet (ControlData puppet) {
      return (puppet.endPos.y < puppet.startPos.y);
   }

   private Vector2 calculateEndPos (Vector2 startPosition) {
      Vector2 endPos;

      Vector3 webOffset = Vector3.up * 0.1f;

      if (Vector2.Distance(startPosition, jumpUpTrigger.transform.position) < Vector2.Distance(startPosition, jumpDownTrigger.transform.position)) {
         // Player is jumping up
         if (_webAbove) {
            endPos = _webAbove.transform.position - webOffset;
         } else {
            endPos = jumpDownTrigger.transform.position;
         }

      } else {
         // Player is jumping down
         if (_webBelow) {
            endPos = _webBelow.transform.position + webOffset;
         } else {
            endPos = jumpUpTrigger.transform.position;
         }
      }

      return endPos;
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

   public bool hasWebBelow () {
      return _webBelow != null;
   }

   public bool hasWebAbove () {
      return _webAbove != null;
   }

   public float getBounceDuration () {
      return movementCurve.keys.Last().time;
   }

   private bool checkForColliders (Vector2 checkPosition, float checkRadius) {
      // Find any colliders at the location
      int colCount = Physics2D.OverlapCircle(checkPosition, checkRadius, new ContactFilter2D { useTriggers = false }, _colliderBuffer);
      
      // Ignore enemies
      foreach (Collider2D collider in _colliderBuffer) {
         if (collider.GetComponent<Enemy>()) {
            colCount--;
         }
      }

      // Return true if we found any colliders
      return colCount > 0;
   }

   #region Private Variables

   // The radius of the player's main collider
   private const float PLAYER_COLLIDER_RADIUS = 0.06f;

   // A dictionary of player IDs, with their PlayerBodyEntities
   private Dictionary<int, PlayerBodyEntity> _playerIDEntityPairs = new Dictionary<int, PlayerBodyEntity>();

   // References to webs above or below this web, to link up with, if any
   private SpiderWeb _webAbove = null, _webBelow = null;

   #endregion
}
