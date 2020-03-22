using UnityEngine;

namespace MapCreationTool
{
   public class WallGroupConfig : MonoBehaviour
   {
      public BoundsInt tileBounds = new BoundsInt();

      private void OnDrawGizmosSelected () {
         Gizmos.color = new Color(1, 0, 0, 0.4f);
         Gizmos.DrawCube(transform.position + tileBounds.center, tileBounds.size);
      }
   }
}