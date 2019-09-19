using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AStar
{
   public class Pathfinding : MonoBehaviour
   {
      #region Public Variables

      // For referencing the grid class
      public AStarGrid gridReference;

      // Starting position to pathfind from
      public Transform startPosition;

      // Starting position to pathfind to
      public Transform targetPosition;

      #endregion

      public List<ANode> findPathNowInit (Vector3 pos, Vector3 endPos) {
         startPosition.transform.position = pos;
         targetPosition.transform.position = endPos;

         findPath(startPosition.position, targetPosition.position);

         return gridReference.finalPath;
      }

      private void findPath (Vector3 a_StartPos, Vector3 a_TargetPos) {
         // Gets the node closest to the starting position
         ANode startNode = gridReference.nodeFromWorldPoint(a_StartPos);

         // Gets the node closest to the target position
         ANode targetNode = gridReference.nodeFromWorldPoint(a_TargetPos);

         if (startNode == null || targetNode == null) {
            Debug.LogError("Invalid Path");
            return;
         }

         // List of nodes for the open list
         List<ANode> openList = new List<ANode>();

         // Hashset of nodes for the closed list
         HashSet<ANode> closedList = new HashSet<ANode>();

         // Add the starting node to the open list to begin the program
         openList.Add(startNode);

         // Whilst there is something in the open list
         while (openList.Count > 0) {
            // Create a node and set it to the first item in the open list
            ANode currentNode = openList[0];

            // Loop through the open list starting from the second object
            for (int i = 1; i < openList.Count; i++) {
               // If the f cost of that object is less than or equal to the f cost of the current node
               if (openList[i].fCost < currentNode.fCost || openList[i].fCost == currentNode.fCost && openList[i].ihCost < currentNode.ihCost) {
                  // Set the current node to that object
                  currentNode = openList[i];
               }
            }

            // Remove that from the open list
            openList.Remove(currentNode);
            // And add it to the closed list
            closedList.Add(currentNode);

            // If the current node is the same as the target node
            if (currentNode == targetNode) {
               // Then Calculate the final path
               getFinalPath(startNode, targetNode);
            }

            // Loop through each neighbor of the current node
            foreach (ANode NeighborNode in gridReference.getNeighboringNodes(currentNode)) {
               // If the neighbor is a wall or has already been checked
               if (NeighborNode.bIsWall || closedList.Contains(NeighborNode)) {
                  // Skip it
                  continue;
               }

               // Get the F cost of that neighbor
               int MoveCost = currentNode.igCost + getManhattenDistance(currentNode, NeighborNode);

               // If the f cost is greater than the g cost or it is not in the open list
               if (MoveCost < NeighborNode.igCost || !openList.Contains(NeighborNode)) {
                  // Set the g cost to the f cost
                  NeighborNode.igCost = MoveCost;

                  // Set the h cost
                  NeighborNode.ihCost = getManhattenDistance(NeighborNode, targetNode);

                  // Set the parent of the node for retracing steps
                  NeighborNode.parentNode = currentNode;

                  // If the neighbor is not in the openlist
                  if (!openList.Contains(NeighborNode)) {
                     // Add it to the list
                     openList.Add(NeighborNode);
                  }
               }
            }
         }
      }

      private void getFinalPath (ANode a_StartingNode, ANode a_EndNode) {
         // List to hold the path sequentially 
         List<ANode> FinalPath = new List<ANode>();

         // Node to store the current node being checked
         ANode CurrentNode = a_EndNode;

         // While loop to work through each node going through the parents to the beginning of the path
         while (CurrentNode != a_StartingNode) {
            // Add that node to the final path
            FinalPath.Add(CurrentNode);

            // Move onto its parent node
            CurrentNode = CurrentNode.parentNode;
         }

         // Reverse the path to get the correct order
         FinalPath.Reverse();

         // Set the final path
         gridReference.finalPath = FinalPath;
      }

      private int getManhattenDistance (ANode a_nodeA, ANode a_nodeB) {
         // x1-x2 and y1-y2
         int ix = Mathf.Abs(a_nodeA.iGridX - a_nodeB.iGridX);
         int iy = Mathf.Abs(a_nodeA.iGridY - a_nodeB.iGridY);

         return ix + iy;
      }
   }
}