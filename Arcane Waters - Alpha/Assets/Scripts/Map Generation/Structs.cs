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

namespace MinimapGeneration
{
   [System.Serializable]
   public struct TileLayer
   {
      [Header("Base")]
      public string Name;
      public Color color;

      [Tooltip("it's just to remember, do not affect the code")]
      [Multiline()]
      public string description;

      [Header("Sub Layer")]
      public bool isSubLayer;

      [Tooltip("it is just checked if have something written")]
      public string[] subLayerSpriteSuffixNames;
      public Sprite[] sprites;

      [Header("Random color")]
      public bool useRandomColor;
      public Color randomColor;

      [Header("Top Border")]
      public bool useTopBorder;
      public Color topBorderColor;
      [Header("Down Border")]
      public bool useDownBorder;
      public Color downBorderColor;

      [Header("Top Down Border")]
      public bool useTopDownBorder;
      public Color topDownBorderColor;

      [Header("Lateral Border")]
      public bool useLateralBorder;
      public Color lateralColor;

      [Header("Border")]
      public bool useBorder;
      public Color borderColor;

      [Header("Alternating Color")]
      public bool useAlternatingColor;
      public Color alternatingColor;

      [Header("Vertical Alternating Color")]
      public bool useVerticalAlternatingColor;
      public Color verticalAlternatingColor;

      [Header("Horizontal Alternating Color")]
      public bool useHorizontalAlternatingColor;
      public Color horizontalAlternatingColor;

      [Header("Outline Color")]
      public bool useOutline;
      public Color outlineColor;
   }

   [System.Serializable]
   public struct TileIcon
   {
      [Header("Base")]
      public string iconLayerName;
      public Sprite spriteIcon;

      public Vector2Int offset;
      [Tooltip("put the icon where has Area Effector 2D, will ignore sublayer")]
      public bool useAreaEffector2D;
      [Tooltip("put the icon where has Collider 2D, will ignore sublayer")]
      public bool useCollider2D;
      [Header("Sub Layer")]
      public bool isSubLayer;

      [Tooltip("it is just checked if have something written")]
      public string[] subLayerSpriteSuffixNames;
      public Sprite[] subLayerSprites;
   }
}