using UnityEngine;
using MapCreationTool.Serialization;
using System.Linq;

public class SpiderWeb : TemporaryController, IMapEditorDataReceiver
{
   #region Public Variables

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
               jumpHeight = h;
            }
         }
      }
   }

   protected override void startControl (ControlData puppet) {
      // Instantiate the bounce effect
      Instantiate(webBouncePrefab, this.transform.position, Quaternion.identity);

      // Registers the bounce pad action status to the achievement data for recording
      if (puppet.entity.isServer) {
         AchievementManager.registerUserAchievement(puppet.entity.userId, ActionType.JumpOnBouncePad);
      }

      puppet.entity.fallDirection = (int) Direction.North;
      puppet.entity.facing = Direction.North;

      // Disable the collider for the entity
      if (puppet.mainEntityCollider != null) {
         puppet.mainEntityCollider.isTrigger = true;
      }

      // Calculate global target spot
      puppet.endPos = new Vector2(puppet.entity.getRigidbody().position.x, transform.position.y + (jumpHeight + 2f) * 0.16f);
   }

   protected override void controlUpdate (ControlData puppet) {
      // Move the player according to animation curve
      float t = movementCurve.Evaluate(puppet.time);
      puppet.entity.getRigidbody().MovePosition(Vector3.LerpUnclamped(puppet.startPos, puppet.endPos, t));

      // End control if time has run out
      if (puppet.time >= movementCurve.keys.Last().time) {
         puppet.entity.fallDirection = 0;
         puppet.entity.getRigidbody().MovePosition(puppet.endPos);
         if (puppet.mainEntityCollider != null) {
            puppet.mainEntityCollider.isTrigger = false;
         }
         endControl(puppet);
      }
   }

   private void OnTriggerEnter2D (Collider2D collision) {
      BodyEntity player = collision.transform.GetComponent<BodyEntity>();

      if (player == null) {
         return;
      }

      if (!player.hasScheduledController(this)) {
         player.requestControl(this);
      }
   }

   #region Private Variables

   #endregion
}
