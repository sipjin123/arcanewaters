using UnityEngine;
using MapCreationTool.Serialization;

public class SpiderWeb : MonoBehaviour, IMapEditorDataReceiver
{
   #region Public Variables

   #endregion

   public void receiveData (DataField[] dataFields) {
      foreach (DataField field in dataFields) {
         if (field.k.CompareTo(DataField.SPIDER_WEB_HEIGHT_KEY) == 0) {
            float height = field.floatValue;
            Ledge ledge = GetComponentInChildren<Ledge>();
            ledge.transform.localPosition = new Vector3(0, (height + 0.5f) * 0.16f);
            ledge.setSize(new Vector2(2, height + 2f));
         }
      }
   }

   #region Private Variables

   #endregion
}
