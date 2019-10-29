using SubjectNerd.Utilities;

using UnityEngine;

namespace MinimapGeneration
{
   [CreateAssetMenu(fileName = "newMinimapGeneratorPreset", menuName = "Minimap Generator Preset")]
   public class MinimapGeneratorPreset : ScriptableObject
   {
      [Header("Images settings")]
      public string _minimapsPath = "/Sprites/Minimaps/";
      public string imagePrefixName = "";
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

      [Header("Map Types")]
      public string[] mapTypeNames;
      public Area.Type[] mapTypes;

      [Tooltip("it is just checked if have something written")]
      public string biome = "";

      [Reorderable]
      [Header("Layers")]
      public TileLayer[] _tileLayer = new TileLayer[0];

      [Reorderable]
      public TileIcon[] _tileIconLayers = new TileIcon[0];

   }
}