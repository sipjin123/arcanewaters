using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Tilemaps;

namespace AStar
{
   public class AStarGrid : MonoBehaviour
   {
      #region Public Variables

      //The array of nodes that the A Star algorithm uses
      public ANode[,] nodeArray;

      //The List of nodes that the A Star algorithm uses
      public List<ANode> nodeList;

      //The completed path that the red line will be drawn along
      public List<ANode> finalPath;

      // Reference to the current grid size of the map
      public float iGridSizeY, iGridSizeX;

      //A vector2 to store the width and height of the graph in world units.
      public Vector2 vGridWorldSize;

      // Fixed grid size of all maps
      public const int GRID_SIZE_X = 42;
      public const int GRID_SIZE_Y = 42;

      #endregion

      public void displayGrid (Vector3 pos, Area.Type areaType) {
         Area area = AreaManager.self.getArea(areaType);
         Grid grid = area.GetComponentInChildren<Grid>();

         List<ANode> nodeList = new List<ANode>();

         for (int q = 0; q < 42; q++) {
            for (int i = 0; i < 42; i++) {
               Vector3 alteredPos = new Vector3(grid.transform.position.x + (i * .25f), grid.transform.position.y - (q * .25f), pos.z);
               foreach (Tilemap tilemap in area.GetComponentsInChildren<Tilemap>()) {
                  if (tilemap.name.StartsWith("Land")) {
                     Vector3Int cellPos = grid.WorldToCell(alteredPos);
                     TileBase tile = tilemap.GetTile(cellPos);

                     if (tile != null) {
                        nodeList.Add(new ANode(true, alteredPos, i, q));
                     } else {
                        nodeList.Add(new ANode(false, alteredPos, i, q));
                     }
                     break;
                  }
               }
            }
         }

         setupGrid(pos, GRID_SIZE_X, GRID_SIZE_Y, nodeList);
      }

      private void setupGrid (Vector3 startPos, int iGridSizeX, int iGridSizeY, List<ANode> nodeList) {
         //Declare the array of nodes. 
         nodeArray = new ANode[iGridSizeX, iGridSizeY];
         this.nodeList = new List<ANode>();
         this.nodeList = nodeList;

         // Referencing grid size
         vGridWorldSize = new Vector2(iGridSizeX,iGridSizeY);
         this.iGridSizeY = iGridSizeY;
         this.iGridSizeX = iGridSizeX;

         int i = 0;
         //Loop through the array of nodes.
         for (int x = 0; x < iGridSizeX; x++)
         {
            //Loop through the array of nodes
            for (int y = 0; y < iGridSizeY; y++)
            {
               ANode currNode = nodeList[i];

               //Create a new node in the array.
               nodeArray[x, y] = new ANode(currNode.bIsWall, currNode.vPosition, x, y);
               this.nodeList[i].iGridX = x;
               nodeList[i].iGridY = y;
               i++;
            }
         }
      }

      //Function that gets the neighboring nodes of the given node.
      public List<ANode> getNeighboringNodes (ANode a_NeighborNode) {
         //Make a new list of all available neighbors.
         List<ANode> neighborList = new List<ANode>();
         int icheckX;//Variable to check if the XPosition is within range of the node array to avoid out of range errors.
         int icheckY;//Variable to check if the YPosition is within range of the node array to avoid out of range errors.

         //Check the right side of the current node.
         icheckX = a_NeighborNode.iGridX + 1;
         icheckY = a_NeighborNode.iGridY;

         //If the XPosition is in range of the array
         if (icheckX >= 0 && icheckX < iGridSizeX)
         {
            //If the YPosition is in range of the array
            if (icheckY >= 0 && icheckY < iGridSizeY)
            {
               //Add the grid to the available neighbors list
               neighborList.Add(nodeArray[icheckX, icheckY]);
            }
         }
         //Check the Left side of the current node.
         icheckX = a_NeighborNode.iGridX - 1;
         icheckY = a_NeighborNode.iGridY;

         //If the XPosition is in range of the array
         if (icheckX >= 0 && icheckX < iGridSizeX)
         {
            //If the YPosition is in range of the array
            if (icheckY >= 0 && icheckY < iGridSizeY)
            {
               //Add the grid to the available neighbors list
               neighborList.Add(nodeArray[icheckX, icheckY]);
            }
         }
         //Check the Top side of the current node.
         icheckX = a_NeighborNode.iGridX;
         icheckY = a_NeighborNode.iGridY + 1;

         //If the XPosition is in range of the array
         if (icheckX >= 0 && icheckX < iGridSizeX)
         {
            //If the YPosition is in range of the array
            if (icheckY >= 0 && icheckY < iGridSizeY)
            {
               //Add the grid to the available neighbors list
               neighborList.Add(nodeArray[icheckX, icheckY]);
            }
         }
         //Check the Bottom side of the current node.
         icheckX = a_NeighborNode.iGridX;
         icheckY = a_NeighborNode.iGridY - 1;

         //If the XPosition is in range of the array
         if (icheckX >= 0 && icheckX < iGridSizeX)
         {
            //If the YPosition is in range of the array
            if (icheckY >= 0 && icheckY < iGridSizeY)
            {
               //Add the grid to the available neighbors list
               neighborList.Add(nodeArray[icheckX, icheckY]);
            }
         }

         //Return the neighbors list.
         return neighborList;
      }

      //Gets the closest node to the given world position.
      public ANode nodeFromWorldPoint (Vector3 a_vWorldPos) {
         float computedX = a_vWorldPos.x - (a_vWorldPos.x % .25f);
         float computedY = a_vWorldPos.y - (a_vWorldPos.y % .25f);

         // Offset setup
         computedX += .25f;
         computedY -= .25f;

         if (nodeList == null) {
            return null;
         }

         ANode currNode = nodeList.Find(_ => _.vPosition.x == computedX && _.vPosition.y == computedY);

         try {
            return nodeArray[currNode.iGridX, currNode.iGridY];
         } catch {
            return null;
         }
      }

      private void OnDrawGizmos () {
         if (nodeArray != null)//If the grid is not empty
         {
            foreach (ANode n in nodeArray)//Loop through every node in the grid
            {
               if (n.bIsWall)//If the current node is a wall node
                {
                  Gizmos.color = Color.white;//Set the color of the node
                  continue;
               } else {
                  Gizmos.color = Color.blue;//Set the color of the node
               }

               if (finalPath != null)//If the final path is not empty
               {
                  if (finalPath.Contains(n))//If the current node is in the final path
                  {
                     Gizmos.color = Color.red;//Set the color of that node
                     float sizex = .1f;
                     Gizmos.DrawWireSphere(n.vPosition, sizex);//Draw the node at the position of the node.
                  }
               }

               float size = .05f;
               Gizmos.DrawCube(n.vPosition, new Vector3(size, size, size));//Draw the node at the position of the node.
            }
         }
      }
   }
}