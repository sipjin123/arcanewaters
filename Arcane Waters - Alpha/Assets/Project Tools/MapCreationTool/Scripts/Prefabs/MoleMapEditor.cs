using UnityEngine;


namespace MapCreationTool
{
   public class MoleMapEditor : MonoBehaviour, IPrefabDataListener
   {
      public void dataFieldChanged (string key, string value) {
         if (key.Trim(' ').CompareTo("run direction") == 0) {
            if (value.CompareTo("left") == 0) {
               transform.localScale = new Vector3(-Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            } else if (value.CompareTo("right") == 0) {
               transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x), transform.localScale.y, transform.localScale.z);
            }
         }
      }
   }
}
