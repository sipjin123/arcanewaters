using UnityEngine;
using MapCreationTool.Serialization;

public class Ledge : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   // The direction associated with this ledge
   public Direction direction = Direction.South;

   // How much force to apply to player when falling off of ledge
   public float forceMagnitude;

   #endregion

   private void Start () {
      // The server doesn't need to both with this
      if (Util.isBatch()) {
         this.gameObject.SetActive(false);
      }
   }

   public void triggerEnter (BodyEntity player, BoxCollider2D collider) {
      if (collider == _fallStartCollider) {
         // Triggers the player to start falling
         player.fallDirection = (int) this.direction;
         triggerStay(player, collider);
      }
   }

   public void triggerStay (BodyEntity player, BoxCollider2D collider) {
      if (player.fallDirection == (int) direction) {
         player.getRigidbody().AddForce(Util.getDirectionFromFacing(direction).normalized * forceMagnitude);
      }
   }

   public void triggerExit (BodyEntity player, BoxCollider2D collider) {
      if (collider == _applyForceCollider) {
         player.fallDirection = 0;
      }
   }

   public void setSize (Vector2 size) {
      // Set the collider sizes
      foreach (BoxCollider2D col in new BoxCollider2D[] { _fallStartCollider, _oneWayCollider, _applyForceCollider }) {
         col.size = new Vector2(size.x * 0.16f, size.y * 0.16f);
         col.offset = new Vector2(0, -size.y * 0.08f + 0.08f);
      }

      // Offset the launch collider away from the end of the ledge and from the sides
      _fallStartCollider.offset -= Util.getDirectionFromFacing(direction).normalized * 0.02f;
      _fallStartCollider.size -= new Vector2(0.14f, 0.04f);

      // Offset the force collider towards the exit, to ensure player exits the ledge completely
      _applyForceCollider.offset += Util.getDirectionFromFacing(direction).normalized * 0.04f;
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

   [SerializeField, Tooltip("Collider, which applies fall force to the player")]
   private BoxCollider2D _applyForceCollider;

   [SerializeField, Tooltip("Collider, which prevents the player from entering the ledge from the other side")]
   private BoxCollider2D _oneWayCollider;

   #endregion
}
