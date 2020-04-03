﻿using UnityEngine;
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

      // Displays the gizmo on editor
      public bool showGizmo;

      // The array of nodes that the A Star algorithm uses
      public ANode[,] nodeArray;

      // The List of nodes that the A Star algorithm uses
      public List<ANode> nodeList;

      // The completed path that the red line will be drawn along
      public List<ANode> finalPath;

      // The number of tiles in the grid
      public Vector2Int gridTiles;

      // The Tile Bounds of the Grid
      public BoundsInt gridBounds;

      // The cell size as told by the Grid
      public Vector2 cellSize;

      #endregion

      public void displayGrid (Vector3 pos, string areaKey) {
         Area area = AreaManager.self.getArea(areaKey);
         displayGrid(pos, area);
      }

      public void displayGrid (Vector3 pos, Area area) {
         Grid grid = area.GetComponentInChildren<Grid>();
         Tilemap firstTilemap = area.GetComponentInChildren<Tilemap>();

         // Gather number of tiles in the map
         gridTiles.x = firstTilemap.size.x;
         gridTiles.y = firstTilemap.size.y;

         gridBounds = firstTilemap.cellBounds;

         // Gather how large the tiles are
         cellSize.x = grid.cellSize.x * grid.transform.localScale.x;
         cellSize.y = grid.cellSize.y * grid.transform.localScale.y;

         float gridYOffset = gridTiles.y * cellSize.y;
         float gridYPosition = grid.transform.position.y;

         // Determines if the map is autogenerated, the pivots of the starting points will differ. Fixed Maps starting pivot starts at the top, while autogenerated maps start from the bottom
         if (Vector2.Distance(area.transform.position, grid.transform.position) > 5) {
            gridYPosition += gridYOffset;
         }
         gridYPosition = (int) gridYPosition;

         float halfGridSizeX = gridTiles.x * cellSize.x * 0.5f;
         float halfGridSizeY = gridTiles.y * cellSize.y * 0.5f;

         // Extract the Collider Tilemaps only and not the visual tilemaps
         TilemapCollider2D[] tilemapColliders = area.GetComponentsInChildren<TilemapCollider2D>(true);
         Tilemap[] tilemaps = new Tilemap[tilemapColliders.Length];
         for (int collider = 0; collider < tilemapColliders.Length; ++collider) {
            tilemaps[collider] = tilemapColliders[collider].GetComponent<Tilemap>();
         }

         List<ANode> nodeList = new List<ANode>();
         for (int col = 0; col < gridTiles.x; col++) {
            for (int row = 0; row < gridTiles.y; row++) {
               Vector3 alteredPos = new Vector3(-halfGridSizeX + grid.transform.position.x + col * cellSize.x + cellSize.x * 0.5f, halfGridSizeY + gridYPosition - row * cellSize.y - cellSize.y * 0.5f, pos.z);
               Vector3Int cellPos = grid.WorldToCell(alteredPos);

               bool isWall = false;
               foreach (Tilemap tilemap in tilemaps) {
                  TileBase tile = tilemap.GetTile(cellPos);
                  if (tile == null) {
                     continue;
                  }

                  foreach (string collisionName in COLLIDING_TILEMAPS) {
                     if (tilemap.name.StartsWith(collisionName)) {
                        isWall = true;
                        break;
                     }
                  }

                  if (isWall) {
                     break;
                  }
               }

               nodeList.Add(new ANode(isWall, alteredPos, col, row));
            }
         }

         setupGrid(pos, nodeList);
      }

      private void setupGrid (Vector3 startPos, List<ANode> nodeList) {
         // Declare the array of nodes. 
         nodeArray = new ANode[gridTiles.x, gridTiles.y];
         this.nodeList = new List<ANode>();
         this.nodeList = nodeList;
         int i = 0;
         // Loop through the array of nodes.
         for (int x = 0; x < gridTiles.x; x++) {
            // Loop through the array of nodes
            for (int y = 0; y < gridTiles.y; y++) {
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
               if (checkX >= 0 && checkX < gridBounds.size.x && checkY >= 0 && checkY < gridBounds.size.y) {
                  // Adds to the neighbours list.
                  neighborList.Add(nodeArray[checkX, checkY]);
               }
            }
         }

         return neighborList;
      }

      // Gets the closest node to the given world position.
      public ANode nodeFromWorldPoint (Vector3 a_vWorldPos) {
         // If there's no nodes, early exit as there's nothing to do
         if (nodeList == null) {
            return null;
         }

         // Snaps the position into the local grid
         a_vWorldPos -= transform.position;

         // Since the grid is visually centered, we also need to pull the position into the local array space
         Vector3 scaledGridSize = gridBounds.size.ToFloatVector();
         scaledGridSize.Scale(cellSize.ToVector3());
         scaledGridSize *= 0.5f;

         // Reverse the Y component to align with how the grid is visually downward structured(top -> bottom)
         scaledGridSize.y *= -1.0f;

         a_vWorldPos += scaledGridSize;

         // Now snap the position into a cell
         int cellIndexX = Mathf.FloorToInt(a_vWorldPos.x / cellSize.x);
         int cellIndexY = Mathf.CeilToInt(a_vWorldPos.y / cellSize.y);

         // Reverse the y index to correspond to the array
         cellIndexY *= -1;

         // If the snapped cell coordinates are within the grid bounds, return node
         if (cellIndexX >= 0 && cellIndexX < gridBounds.size.x && cellIndexY >= 0 && cellIndexY < gridBounds.size.y) {
            return nodeArray[cellIndexX, cellIndexY];
         }
         return null;
      }

      private void OnDrawGizmosSelected () {
         if (!showGizmo) {
            return;
         }

         // If the grid is not empty
         if (nodeArray != null) {
            // Loop through every node in the grid
            foreach (ANode n in nodeArray) {
               // If the current node is a wall node
               Color gizmoColor;
               if (n.bIsWall) {
                  gizmoColor = Color.red;
               } else {
                  gizmoColor = Color.green;
               }
               gizmoColor.a = 0.5f;
               Gizmos.color = gizmoColor;

               // If the final path is not empty
               if (finalPath != null) {
                  // If the current node is in the final path
                  if (finalPath.Contains(n)) {
                     Gizmos.color = Color.red;
                     float sizex = .1f;
                     Gizmos.DrawWireSphere(n.vPosition, sizex);
                  }
               }

               float size = 0.1f;
               Gizmos.DrawCube(n.vPosition, new Vector3(size, size, size));
            }
         }
      }

      #region Private Variables

      // The starting names of the tilemaps that will be considered for blocking Nodes
      private static readonly string[] COLLIDING_TILEMAPS = new string[] {
            "mountain", "shrub", "water", "fence", "bush", "stump", "stair", "prop"
         };

      #endregion
   }
}