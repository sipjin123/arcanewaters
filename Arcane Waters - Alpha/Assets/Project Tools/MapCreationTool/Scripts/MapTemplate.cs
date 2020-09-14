using UnityEngine;
using Cinemachine;

namespace MapCreationTool
{
   public class MapTemplate : MonoBehaviour
   {
      public Area area;
      public Transform tilemapParent;
      public Transform collisionTilemapParent;
      public Transform prefabParent;
      public Transform npcParent;
      public Transform effectorContainer;
      public PolygonCollider2D camBounds;
      public CinemachineConfiner confiner;
      public Transform rugMarkerParent;
      public FlockManager flockManager;
      public Transform rightBorder, leftBorder, topBorder, bottomBorder;
   }
}

