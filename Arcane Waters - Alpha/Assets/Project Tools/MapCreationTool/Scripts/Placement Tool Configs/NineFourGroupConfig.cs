using UnityEngine;

namespace MapCreationTool
{
   public class NineFourGroupConfig : MonoBehaviour
   {
      public BoundsInt mainBounds = new BoundsInt(0, 0, 0, 3, 3, 0);
      public BoundsInt cornerBounds = new BoundsInt(0, 0, 0, 2, 2, 0);

      public bool singleLayer = false;

      private void OnDrawGizmosSelected () {
         Gizmos.color = new Color(1, 0, 0, 0.4f);
         Gizmos.DrawCube(transform.position + mainBounds.center, mainBounds.size);
         Gizmos.color = new Color(0, 0, 1, 0.4f);
         Gizmos.DrawCube(transform.position + cornerBounds.center, cornerBounds.size);
      }
   }
}