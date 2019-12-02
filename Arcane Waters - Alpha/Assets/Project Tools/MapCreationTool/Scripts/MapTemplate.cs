using UnityEngine;
using Cinemachine;

namespace MapCreationTool
{
   public class MapTemplate : MonoBehaviour
   {
      public Area area;
      public Transform tilemapParent;
      public Transform prefabParent;
      public PolygonCollider2D camBounds;
      public CinemachineConfiner confiner;
   }
}

