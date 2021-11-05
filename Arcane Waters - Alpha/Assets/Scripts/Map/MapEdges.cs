using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Map
{
   public class MapEdges : MonoBehaviour
   {
      #region Public Variables

      // Top edge
      public EdgeCollider2D top;

      // Right edge
      public EdgeCollider2D right;

      // Bottom edge
      public EdgeCollider2D bottom;

      // Left edge
      public EdgeCollider2D left;

      #endregion

      public Direction computeDirectionFromEdge(EdgeCollider2D edge) {
         if (top == edge) {
            return Direction.North;
         } else if (right == edge) {
            return Direction.East;
         } else if (bottom == edge) {
            return Direction.South;
         } else if (left == edge) {
            return Direction.West;
         }

         return Direction.North;
      }

      #region Private Variables

      #endregion
   }
}
