using UnityEngine;

namespace MapCreationTool
{
    public class NineFourGroupConfig : MonoBehaviour
    {
        public BoundsInt pathBounds;
        public BoundsInt cornerBounds;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, 0, 0, 0.4f);
            Gizmos.DrawCube(transform.position + pathBounds.center, pathBounds.size);
            Gizmos.color = new Color(0, 0, 1, 0.4f);
            Gizmos.DrawCube(transform.position + cornerBounds.center, cornerBounds.size);
        }
    }
}
