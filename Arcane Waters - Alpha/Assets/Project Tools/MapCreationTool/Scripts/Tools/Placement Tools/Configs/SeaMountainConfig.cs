using UnityEngine;

namespace MapCreationTool
{
   public class SeaMountainConfig : MonoBehaviour
   {
      public BoundsInt mainBounds = new BoundsInt(0, 0, 0, 3, 3, 0);

      private void OnDrawGizmosSelected () {
         Gizmos.color = new Color(1, 0, 0, 0.4f);
         Gizmos.DrawCube(transform.localPosition + mainBounds.center, mainBounds.size);
      }
   }
}
