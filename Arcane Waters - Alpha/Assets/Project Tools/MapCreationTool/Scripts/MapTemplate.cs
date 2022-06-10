using UnityEngine;
using Cinemachine;

namespace MapCreationTool
{
   public class MapTemplate : MonoBehaviour
   {
      public Area area;
      public Transform tilemapParent;
      public Transform collisionTilemapParent;
      public Transform staticColliderParent;
      public Transform prefabParent;
      public Transform npcParent;
      public Transform effectorContainer;
      public CinemachineConfiner confiner;
      public MapCameraBounds mapCameraBounds;
      public Transform rugMarkerParent;
      public FlockManager flockManager;
      public Transform rightBorder, leftBorder, topBorder, bottomBorder;
      public Transform bottomLeftCorner, bottomRightCorner, topLeftCorner, topRightCorner;
      public SpriteRenderer backgroundRenderer;
   }
}

