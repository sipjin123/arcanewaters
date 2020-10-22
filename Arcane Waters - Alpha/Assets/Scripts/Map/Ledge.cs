using System.Collections.Generic;
using System.Linq;
using MapCreationTool.Serialization;
using UnityEngine;

public class Ledge : TemporaryController, IMapEditorDataReceiver
{
   #region Public Variables

   // The direction associated with this ledge
   public Direction direction = Direction.South;

   // Animation of the movement, representing the duration and the easing
   public AnimationCurve movementCurve;

   #endregion

   private void Start () {
      // The server doesn't need to both with this
      if (Util.isBatch()) {
         this.gameObject.SetActive(false);
      }
   }

   private void OnTriggerStay2D (Collider2D collision) {
      BodyEntity player = collision.transform.GetComponent<BodyEntity>();

      if (player == null) {
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

      if (!player.hasScheduledController(this)) {
         player.requestControl(this);
      }
   }

   private Vector2 calculateEndPos (Rigidbody2D puppetBody, Collider2D puppetCollider) {
      return new Vector2(
         puppetBody.position.x,
         transform.position.y - _fallStartCollider.offset.y - puppetCollider.offset.y);
   }

   protected override void startControl (ControlData puppet) {
      puppet.entity.fallDirection = (int) direction;
      puppet.entity.facing = direction;

      // Disable the collider for the entity
      if (puppet.entity.getMainCollider() != null) {
         puppet.entity.getMainCollider().isTrigger = true;
      }

      // Calculate global target spot
      puppet.endPos = calculateEndPos(puppet.entity.getRigidbody(), puppet.entity.getMainCollider());
   }

   protected override void controlUpdate (ControlData puppet) {
      // Move the player according to animation curve
      float t = movementCurve.Evaluate(puppet.time);
      puppet.entity.getRigidbody().MovePosition(Vector3.LerpUnclamped(puppet.startPos, puppet.endPos, t));

      // End control if time has run out
      if (puppet.time >= movementCurve.keys.Last().time) {
         puppet.entity.fallDirection = 0;
         puppet.entity.getRigidbody().MovePosition(puppet.endPos);
         if (puppet.entity.getMainCollider() != null) {
            puppet.entity.getMainCollider().isTrigger = false;
         }
         endControl(puppet);
      }
   }

   protected override void onForceFastForward (ControlData puppet) {
      puppet.entity.fallDirection = 0;
      puppet.entity.transform.position = puppet.endPos;
      if (puppet.entity.getMainCollider() != null) {
         puppet.entity.getMainCollider().isTrigger = false;
      }
   }

   public void setSize (Vector2 size) {
      _fallStartCollider.offset = new Vector2(_fallStartCollider.offset.x, size.y * 0.16f * 0.5f);
      _fallStartCollider.size = new Vector2(size.x * 0.16f, _fallStartCollider.size.y);
   }

   public void receiveData (DataField[] dataFields) {
      int w = 1;
      int h = 1;

      foreach (DataField field in dataFields) {
         switch (field.k.ToLower()) {
            case DataField.LEDGE_WIDTH_KEY:
               if (field.tryGetIntValue(out int width)) {
                  w = width;
               }
               break;
            case DataField.LEDGE_HEIGHT_KEY:
               if (field.tryGetIntValue(out int height)) {
                  h = height;
               }
               break;
         }
      }

      setSize(new Vector2(w, h));
   }

   #region Private Variables

   [SerializeField, Tooltip("Collider, which triggers the ledge to start falling the player")]
   private BoxCollider2D _fallStartCollider;

   #endregion
}
