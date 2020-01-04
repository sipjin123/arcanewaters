﻿using UnityEngine;
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
      public PolygonCollider2D camBounds;
      public CinemachineConfiner confiner;
   }
}

