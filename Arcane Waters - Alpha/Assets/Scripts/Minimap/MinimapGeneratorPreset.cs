using SubjectNerd.Utilities;

using UnityEngine;

namespace MinimapGeneration
{
   [CreateAssetMenu(fileName = "newMinimapGeneratorPreset", menuName = "Minimap Generator Preset")]
   public class MinimapGeneratorPreset : ScriptableObject
   {
      // Determine if preset is dedicated to special usage (single map - based on area key) and which type of map should be used
      public enum SpecialType
      {
         NotSpecial = 0,
         Land = 1,
         Sea = 2,
         Interior = 3
      }

      [Header("Images settings")]
      public string _minimapsPath = "Resources/Sprites/Minimaps/";
      public string imagePrefixName = "Editor/";
      public string imageSuffixName = "";
      public Vector2Int _textureSize = new Vector2Int(1024, 1024);

      [Header("Background")]
      [Tooltip("Will create a layer with background color")]
      public bool useBackground;
      public Color backgroundColor;

      [Header("Outline")]
      [Tooltip("Will create a outline")]
      public bool useOutline;
      public Color outlineColor;

      [Header("Special Type")]
      public SpecialType specialType = SpecialType.NotSpecial;
      public string specialTypeAreaKey = "";

      [Reorderable]
      [Header("Layers")]
      public TileLayer[] _tileLayer = new TileLayer[0];

      [Reorderable]
      public TileIcon[] _tileIconLayers = new TileIcon[0];


      [Space(10)]
      [Header("V2 Sea Minimap Generation properties")]

      [Tooltip("Color of generic land that has nothing else on it")]
      public Color baseLandColor;
      [Tooltip("Color of generic land on the border of something")]
      public Color landBorderColor;
      [Tooltip("Color of outline around land mass")]
      public Color landOutlineColor;
      [Tooltip("Color off the lower pixel of cliffs")]
      public Color landCliffLowerColor;
      [Tooltip("Color of the upper pixel of cliffs")]
      public Color landCliffUpperColor;
      [Space(5)]
      [Tooltip("Color of the base water")]
      public Color waterColor;
      [Tooltip("Color of the border of water where it touches land")]
      public Color waterBorderColor;
      [Tooltip("Color of the deeper water")]
      public Color deeper1WaterColor;
      [Tooltip("Color of the even deeper water")]
      public Color deeper2WaterColor;
      [Tooltip("Color of the even waterfalls")]
      public Color waterfallColor;
      [Space(5)]
      [Tooltip("Color of pathways")]
      public Color pathwayColor;
      [Tooltip("Color of pathway borders")]
      public Color pathwayBorderColor;
      [Space(5)]
      [Tooltip("Icon of mountains")]
      public TileIcon mountainIcon;
      [Tooltip("Icon of dock, which is facing north")]
      public TileIcon dockNorthIcon;
      [Tooltip("Icon of dock, which is facing south")]
      public TileIcon dockSouthIcon;
      [Tooltip("Icon of dock, which is facing east")]
      public TileIcon dockEastIcon;
      [Tooltip("Icon of dock, which is facing west")]
      public TileIcon dockWestIcon;
      [Tooltip("Icon of houses")]
      public TileIcon houseIcon;
      [Tooltip("Icon of trees")]
      public TileIcon tree1Icon;
      [Tooltip("Icon of trees")]
      public TileIcon tree2Icon;
   }
}