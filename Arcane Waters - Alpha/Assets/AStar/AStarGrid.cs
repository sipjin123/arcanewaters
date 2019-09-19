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

      // The array of nodes that the A Star algorithm uses
      public ANode[,] nodeArray;

      // The List of nodes that the A Star algorithm uses
      public List<ANode> nodeList;

      // The completed path that the red line will be drawn along
      public List<ANode> finalPath;

      // Reference to the current grid size of the map
      public float iGridSizeY, iGridSizeX;

      // A vector2 to store the width and height of the graph in world units.
      public Vector2 vGridWorldSize;

      // Fixed grid size of all maps
      public const int GRID_SIZE_X = 42;
      public const int GRID_SIZE_Y = 42;

      #endregion

      public void displayGrid (Vector3 pos, Area.Type areaType) {
         Area area = AreaManager.self.getArea(areaType);
         Grid grid = area.GetComponentInChildren<Grid>();

         List<ANode> nodeList = new List<ANode>();

         for (int col = 0; col < 42; col++) {
            for (int row = 0; row < 42; row++) {
               Vector3 alteredPos = new Vector3(grid.transform.position.x + (row * .25f), grid.transform.position.y - (col * .25f), pos.z);
               foreach (Tilemap tilemap in area.GetComponentsInChildren<Tilemap>()) {
                  if (tilemap.name.StartsWith("Land")) {
                     Vector3Int cellPos = grid.WorldToCell(alteredPos);
                     TileBase tile = tilemap.GetTile(cellPos);

                     if (tile != null) {
                        nodeList.Add(new ANode(true, alteredPos, row, col));
                     } else {
                        nodeList.Add(new ANode(false, alteredPos, row, col));
                     }
                     break;
                  }
               }
            }
         }

         setupGrid(pos, GRID_SIZE_X, GRID_SIZE_Y, nodeList);
      }

      private void setupGrid (Vector3 startPos, int iGridSizeX, int iGridSizeY, List<ANode> nodeList) {
         // Declare the array of nodes. 
         nodeArray = new ANode[iGridSizeX, iGridSizeY];
         this.nodeList = new List<ANode>();
         this.nodeList = nodeList;

         // Referencing grid size
         vGridWorldSize = new Vector2(iGridSizeX,iGridSizeY);
         this.iGridSizeY = iGridSizeY;
         this.iGridSizeX = iGridSizeX;

         int i = 0;
         // Loop through the array of nodes.
         for (int x = 0; x < iGridSizeX; x++) {
            // Loop through the array of nodes
            for (int y = 0; y < iGridSizeY; y++) {
               ANode currNode = nodeList[i];

               // Create a new node in the array.
               nodeArray[x, y] = new ANode(currNode.bIsWall, currNode.vPosition, x, y);
               this.nodeList[i].iGridX = x;
               nodeList[i].iGridY = y;
               i++;
            }
         }
      }

      // Function that gets the neighboring nodes of the given node.
      public List<ANode> getNeighboringNodes (ANode a_NeighborNode) {
         // Make a new list of all available neighbors.
         List<ANode> neighborList = new List<ANode>();

         for (int row = -1; row <= 1; row++) {
            for (int col = -1; col <= 1; col++) {
               // if we are on the node that was passed in, skip this iteration.
               if (row == 0 && col == 0) {
                  continue;
               }

               int checkX = a_NeighborNode.iGridX + row;
               int checkY = a_NeighborNode.iGridY + col;

               // Make sure the node is within the grid.
               if (checkX >= 0 && checkX < iGridSizeX && checkY >= 0 && checkY < iGridSizeY) {
                  // Adds to the neighbours list.
                  neighborList.Add(nodeArray[checkX, checkY]); 
               }
            }
         }

         return neighborList;
      }

      // Gets the closest node to the given world position.
      public ANode nodeFromWorldPoint (Vector3 a_vWorldPos) {
         // Snaps the position into a grid node
         float computedX = a_vWorldPos.x - (a_vWorldPos.x % .25f);
         float computedY = a_vWorldPos.y - (a_vWorldPos.y % .25f);

         // Offset setup
         computedX += .25f;
         computedY -= .25f;

         if (nodeList == null) {
            return null;
         }

         // Find the node in the array with the coordinates
         ANode currNode = nodeList.Find(_ => _.vPosition.x == computedX && _.vPosition.y == computedY);

         try {
            return nodeArray[currNode.iGridX, currNode.iGridY];
         } catch {
            return null;
         }
      }

      private void OnDrawGizmos () {
         // If the grid is not empty
         if (nodeArray != null) {
            // Loop through every node in the grid
            foreach (ANode n in nodeArray) {
               // If the current node is a wall node
               if (n.bIsWall) {
                  Gizmos.color = Color.white;
                  continue;
               } else {
                  Gizmos.color = Color.white;
               }

               // If the final path is not empty
               if (finalPath != null) {
                  // If the current node is in the final path
                  if (finalPath.Contains(n)) {
                     Gizmos.color = Color.red;
                     float sizex = .1f;
                     Gizmos.DrawWireSphere(n.vPosition, sizex);
                  }
               }

               float size = .05f;
               Gizmos.DrawCube(n.vPosition, new Vector3(size, size, size));
            }
         }
      }
   }
}