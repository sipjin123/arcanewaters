using System.Collections;
using System.Collections.Generic;

using SubjectNerd.Utilities;

using UnityEngine;

namespace ProceduralMap
{
   [CreateAssetMenu(fileName = "new Procedural Map Preset", menuName = "Procedural Map Preset")]
   public class MapGeneratorPreset : ScriptableObject
   {
      [Header("Map Settings")]
      public string mapPath = "Assets/Prefabs/Maps/";
      public string MapPrefixName = "Type ";
      public string MapName = "Map";
      public string MapSuffixName = " Biome";
      public Vector2Int mapSize = new Vector2Int(64, 64);
      public float replaceWaterHeight = 0.6f;

      [Header("Noise")]
      public float noiseScale = 27.6f;
      public int octaves = 4;

      [Range(0, 1)]
      public float persistance = .5f;
      public float lacunarity = 2;
      public int seed;
      public Vector2 offset;

      [Header("Layers")]
      [Tooltip("If it not active, will set layer height automatic, the first layer will be the lower and the last layer will be the higher.")]
      public bool usePresetLayerHeight;
      public bool updateTilesFromProject = true;

      public LayerType[] layers;

      [Header("River")]
      public River river;

      /// <summary>
      /// Called when the script is loaded or a value is changed in the
      /// inspector (Called in the editor only).
      /// </summary>
      void OnValidate () {
         if (mapSize.x < 1) {
            mapSize.x = 1;
         }

         if (mapSize.y < 1) {
            mapSize.y = 1;
         }

         if (lacunarity < 1) {
            lacunarity = 1;
         }

         if (octaves < 0) {
            octaves = 0;
         }

         if (noiseScale < 1) {
            noiseScale = 1;
         }

         if (layers.Length > 1) {
            layers[0].height = 1;
         }

      }
   }
}