using UnityEngine;

namespace MapCreationTool
{
   public class LoadCanvas : MonoBehaviour
   {
      private void Awake () {
         GetComponentInChildren<Canvas>().enabled = true;
      }
   }
}
