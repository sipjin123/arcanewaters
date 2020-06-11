using UnityEngine;

public class LedgeTrigger : MonoBehaviour
{
   #region Public Variables

   #endregion

   private void Start () {
      _ledge = GetComponentInParent<Ledge>();
      _col = GetComponent<BoxCollider2D>();
   }

   private void OnTriggerEnter2D (Collider2D other) {
      BodyEntity player = other.transform.GetComponent<BodyEntity>();

      if (player == null) {
         return;
      }

      _ledge.triggerEnter(player, _col);
   }

   private void OnTriggerStay2D (Collider2D other) {
      BodyEntity player = other.transform.GetComponent<BodyEntity>();

      if (player == null) {
         return;
      }

      _ledge.triggerStay(player, _col);
   }

   private void OnTriggerExit2D (Collider2D other) {
      BodyEntity player = other.transform.GetComponent<BodyEntity>();

      if (player == null) {
         return;
      }

      _ledge.triggerExit(player, _col);
   }

   #region Private Variables

   // Ledge that this trigger is for
   private Ledge _ledge;

   // Collider, that is controlling this trigger
   private BoxCollider2D _col;

   #endregion
}
