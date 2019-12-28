using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AStar
{
   [Serializable]
   public class ANode
   {
      // X and Y Position in the Node Array
      public int iGridX;
      public int iGridY;

      // Tells the program if this node is being obstructed.
      public bool bIsWall;

      // The world position of the node.
      public Vector3 vPosition;

      // For the AStar algoritm, will store what node it previously came from so it cn trace the shortest path.
      public ANode parentNode;

      // The cost of moving to the next square.
      public int igCost;

      // The distance to the goal from this node.
      public int ihCost;

      // Quick get function to add G cost and H Cost, and since we'll never need to edit FCost, we dont need a set function.
      public int fCost { get { return igCost + ihCost; } }

      public ANode (bool a_bIsWall, Vector3 a_vPos, int a_igridX, int a_igridY) {
         bIsWall = a_bIsWall;
         vPosition = a_vPos;
         iGridX = a_igridX;
         iGridY = a_igridY;
      }
   }
}