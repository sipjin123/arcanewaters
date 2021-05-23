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

   }
}