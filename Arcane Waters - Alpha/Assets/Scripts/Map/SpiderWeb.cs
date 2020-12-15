using UnityEngine;
using MapCreationTool.Serialization;
using System.Linq;

public class SpiderWeb : TemporaryController, IMapEditorDataReceiver
{
   #region Public Variables

   // The constant part of the height that doesn't change and is added to the variable height
   public const float CONSTANT_HEIGHT = 0.5f;

   // The height of the jump, set in map editor
   public float jumpHeight;

   // Animation of the movement, representing the duration and the easing
   public AnimationCurve movementCurve;

   // The prefab we use for the bouncing spider web effect
   public GameObject webBouncePrefab;

   #endregion

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
      // Instantiate the bounce effect
      Instantiate(webBouncePrefab, transform.position, Quaternion.identity);

      // Registers the bounce pad action status to the achievement data for recording
      if (puppet.entity.isServer) {
         AchievementManager.registerUserAchievement(puppet.entity, ActionType.JumpOnBouncePad);
      }

      puppet.entity.fallDirection = (int) Direction.North;
      puppet.entity.facing = Direction.North;

      // Calculate global target spot
      puppet.endPos = calculateEndPos(puppet.entity.getRigidbody(), puppet.entity.getMainCollider());
   }

   protected override void controlUpdate (ControlData puppet) {
      if (puppet.entity.isLocalPlayer) {
         // Move the player according to animation curve
         float t = movementCurve.Evaluate(puppet.time);
         puppet.entity.getRigidbody().MovePosition(Vector3.LerpUnclamped(puppet.startPos, puppet.endPos, t));
      }

      // End control if time has run out
      if (puppet.time >= movementCurve.keys.Last().time) {
         if (puppet.entity.isLocalPlayer) {
            puppet.entity.getRigidbody().MovePosition(puppet.endPos);
         }
         puppet.entity.fallDirection = 0;
         endControl(puppet);
      }
   }

   protected override void onForceFastForward (ControlData puppet) {
      if (puppet.entity.isLocalPlayer) {
         puppet.entity.transform.position = puppet.endPos;
      }
      puppet.entity.fallDirection = 0;
   }

   private void OnTriggerStay2D (Collider2D collision) {
      BodyEntity player = collision.transform.GetComponent<BodyEntity>();

      if (player == null || !player.isLocalPlayer) {
         return;
      }

      // Check that there's no colliders at the arriving position
      int colCount = Physics2D.OverlapCircle(
         calculateEndPos(player.getRigidbody(), player.getMainCollider()) + player.getMainCollider().offset,
         player.getMainCollider().radius,
         new ContactFilter2D { useTriggers = false },
         _colliderBuffer);
      if (colCount > 0) {
         return;
      }

      tryTriggerController(player);
   }

   private Vector2 calculateEndPos (Rigidbody2D puppetBody, Collider2D puppetCollider) {
      return new Vector2(puppetBody.position.x, transform.position.y + jumpHeight * 0.16f - puppetCollider.offset.y);
   }

   #region Private Variables

   #endregion
}
