using UnityEngine;

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

      [Header("Only border")]
      public bool isOnlyBorder;

      [Header("Full name")]
      public bool useFullName;

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
      public int topPixelCount;

      [Header("Down Border")]
      public bool useDownBorder;
      public Color downBorderColor;
      public int downPixelCount;

      [Header("Another Down Border")]
      public bool useAnotherDownBorder;
      public Color anotherDownBorderColor;
      public int anotherDownPixelCount;

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