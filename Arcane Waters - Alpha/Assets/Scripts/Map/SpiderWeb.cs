using UnityEngine;
using MapCreationTool.Serialization;

public class SpiderWeb : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   // Collider, used to set the player's fall direction
   public BoxCollider2D fallDirectonCollider;

   // One way collider, blocking objects from the top
   public BoxCollider2D oneWayCollider;

   #endregion

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.SPIDER_WEB_HEIGHT_KEY) == 0) {
            float height = field.floatValue;
            oneWayCollider.offset = new Vector2(oneWayCollider.offset.x, 0.15f + height * 0.08f);
            oneWayCollider.size = new Vector2(oneWayCollider.size.x, height * 0.16f);

            fallDirectonCollider.offset = new Vector2(fallDirectonCollider.offset.x, height * 0.08f);
            fallDirectonCollider.size = new Vector2(fallDirectonCollider.size.x, (height + 1) * 0.16f);
         }
      }
   }

   #region Private Variables

   #endregion
}
