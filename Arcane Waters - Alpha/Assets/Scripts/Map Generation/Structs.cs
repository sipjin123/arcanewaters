using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Tilemaps;

namespace ProceduralMap
{
   [System.Serializable]
   public class LayerType
   {
      public string name;
      public bool isLand;

      [Range(0, 1)]
      public float height;
      public bool useCollider;
      public Biome.Type biome;

      [HideInInspector]
      public Tile tile;
      public string tilePrefix;
      public string tileSuffix;

      [Header("Borders")]
      public bool useBorderOnDifferentLayer;
      public string BorderLayerName;
      public Border[] borders;
      [Header("Corners")]
      public Corner[] corners;

      [Header("Objects")]
      public ObjectLayer[] objectLayers;
   }

   [System.Serializable]
   public class ObjectLayer
   {
      public string name;
      public int maxNumberOfObjects;
      [Range(0, 100)]
      public int percentageOfRejection;

      [Header("Tile")]
      public string tilePrefix;
      public string tileSuffix;

      [HideInInspector]
      public Tile tile;
   }

   [System.Serializable]
   public class Border
   {
      public BorderDirection borderDirection;

      [Header("Tile")]
      public string tilePrefix;
      public string tileSuffix;

      [HideInInspector]
      public Tile borderTile;
   }

   [System.Serializable]
   public class Corner
   {
      public CornerDirection cornerDirection;

      [Header("Tile")]
      public string tilePrefix;
      public string tileSuffix;

      [HideInInspector]
      public Tile cornerTile;
   }

   [System.Serializable]
   public class River
   {
      public string layerToPlaceRiver;
      public int numberOfAttempts;
      public Biome.Type biome;

      [Header("Tile")]
      public string tilePrefix;
      public string tileSuffix;

      [HideInInspector]
      public Tile riverTile;

      [Header("Borders")]
      public RiverBorder[] riverBorders;
   }

   [System.Serializable]
   public class RiverBorder
   {

      public RiverDirection riverDirection;

      [Header("Tile")]
      public string tilePrefix;
      public string tileSuffix;

      [HideInInspector]
      public Tile borderTile;
   }

   /// <summary>
   /// Node for the a star algorithm
   /// </summary>
   public class Node
   {
      /// <summary>
      /// X position in the node array
      /// </summary>
      public int gridX;

      /// <summary>
      /// Y position in the node array
      /// </summary>
      public int gridY;

      /// <summary>
      /// type of the current node
      /// </summary>
      public NodeType nodeType;

      /// <summary>
      /// world position of the node
      /// </summary>
      public Vector3 position;

      /// <summary>
      /// For the a star algorithm, will store what node it previously came from seealso it can trace the shortest path
      /// </summary>
      public Node parent;

      /// <summary>
      /// the cost of the moving to the next node
      /// </summary>
      public int gCost;

      /// <summary>
      /// the distance to the goal from this node
      /// </summary>
      public int hCost;

      public Node (int gridX, int gridY, Vector3 position, NodeType nodeType = NodeType.Wall) {
         this.gridX = gridX;
         this.gridY = gridY;
         this.position = position;
         this.nodeType = nodeType;
      }

      /// <summary>
      /// Quick access to gCost+hCost
      /// </summary>
      /// <value></value>
      public int fCost
      {
         get
         {
            return gCost + hCost;
         }
      }

   }
}