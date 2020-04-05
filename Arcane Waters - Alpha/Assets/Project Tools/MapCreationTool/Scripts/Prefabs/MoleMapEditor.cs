using UnityEngine;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class MoleMapEditor : MonoBehaviour, IPrefabDataListener
   {
      public void dataFieldChanged (DataField field) {
         if (field.k.Trim(' ').CompareTo(DataField.CRITTER_RUN_DIRECTION_KEY) == 0) {
            if (field.v.CompareTo("left") == 0) {
               transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            } else if (field.v.CompareTo("right") == 0) {
               transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
         }
      }
   }
}
