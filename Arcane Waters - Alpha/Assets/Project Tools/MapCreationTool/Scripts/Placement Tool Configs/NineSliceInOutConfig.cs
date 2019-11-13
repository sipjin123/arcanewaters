using UnityEngine;

namespace MapCreationTool
{
    public class NineSliceInOutConfig : MonoBehaviour
    {
        public BoundsInt outerBounds;
        public BoundsInt innerBounds;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, 0, 0, 0.4f);
            Gizmos.DrawCube(transform.position + innerBounds.center, innerBounds.size);
            Gizmos.color = new Color(0, 0, 1, 0.4f);
            Gizmos.DrawCube(transform.position + outerBounds.center, outerBounds.size);
        }
    }
}
