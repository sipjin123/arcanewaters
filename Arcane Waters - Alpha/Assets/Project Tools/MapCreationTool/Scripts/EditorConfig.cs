using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using System;
using UnityEngine.Assertions;
using System.Collections.Generic;

namespace MapCreationTool
{
   [CreateAssetMenu(fileName = "New config", menuName = "Data/Editor config")]
   public class EditorConfig : ScriptableObject
   {
      public Tile[] layerTiles;
      public Tile[] clusterTiles;
      public TileBase transparentTile;

      public LayerConfig[] areaLayerIndexes;
      public LayerConfig[] seaLayerIndexes;
      public LayerConfig[] interiorLayerIndexes;

      public MinimapGeneration.MinimapGeneratorPreset[] areaPresets;
      public MinimapGeneration.MinimapGeneratorPreset[] seaPresets;
      public MinimapGeneration.MinimapGeneratorPreset interiorPreset;

      public Color[] rugLookup;

      public string[] areaLayerNames
      {
         get { return areaLayerIndexes.OrderBy(l => l.index).Select(l => l.layer).ToArray(); }
      }

      public string[] seaLayerNames
      {
         get { return seaLayerIndexes.OrderBy(l => l.index).Select(l => l.layer).ToArray(); }
      }

      public string[] interiorLayerNames
      {
         get { return interiorLayerIndexes.OrderBy(l => l.index).Select(l => l.layer).ToArray(); }
      }

      public int getIndex (string layer, EditorType editorType) {
         foreach (LayerConfig lc in getLayers(editorType)) {
            if (lc.layer.CompareTo(layer) == 0)
               return lc.index;
         }
         throw new Exception($"Undefined layer {layer}.");
      }

      public LayerConfig[] getLayers (EditorType editorType) {
         switch (editorType) {
            case EditorType.Area:
               return areaLayerIndexes;
            case EditorType.Sea:
               return seaLayerIndexes;
            case EditorType.Interior:
               return interiorLayerIndexes;
            default:
               throw new Exception("Undefined editor type.");
         }
      }

      public string[] getLayerNames (EditorType editorType) {
         switch (editorType) {
            case EditorType.Area:
               return areaLayerNames;
            case EditorType.Sea:
               return seaLayerNames;
            case EditorType.Interior:
               return interiorLayerNames;
            default:
               throw new Exception("Undefined editor type.");
         }
      }

      public LayerConfig getLayerConfig (EditorType editorType, string layerName) {
         return getLayers(editorType).First(l => l.layer.CompareTo(layerName) == 0);
      }

      [System.Serializable]
      public class LayerConfig
      {
         public string layer;
         public int index;
         public float zOffset;
         public LayerType layerType = LayerType.Regular;
      }

      public void testConfigCorrectness () {
         testPresetsCount();
         testEmptyPresets();
         testDuplicatePresets();
         testMissingLayersInPresets();
      }

      private void testPresetsCount () {
         // Test biome counts
         int biomeCount = Biome.getAllTypes().Count;
         Assert.IsTrue(areaPresets.Length == biomeCount, "Please provide area presets for all biomes. Missing " + (biomeCount - areaPresets.Length).ToString() + " biome presets");
         Assert.IsTrue(seaPresets.Length == biomeCount, "Please provide sea presets for all biomes. Missing " + (biomeCount - areaPresets.Length).ToString() + " biome presets");
      }

      private void testEmptyPresets () {
         // Test for empty biomes
         for (int i = 0; i < areaPresets.Length; i++) {
            Assert.IsTrue(areaPresets[i], "Area preset #" + i.ToString() + " is empty. Please provide correct preset");
         }
         for (int i = 0; i < seaPresets.Length; i++) {
            Assert.IsTrue(seaPresets[i], "Sea preset #" + i.ToString() + " is empty. Please provide correct preset");
         }
         Assert.IsNotNull(interiorPreset, "Interior preset is empty. Please provide correct preset");
      }

      private void testDuplicatePresets () {
         // Test that all presets provided are different
         for (int i = 0; i < areaPresets.Length; i++) {
            for (int j = i + 1; j < areaPresets.Length; j++) {
               Assert.IsFalse(areaPresets[i] == areaPresets[j], "Area preset #" + i.ToString() + " is same as area preset #" + j.ToString() + ". Please provide unique presets");
            }
         }
         for (int i = 0; i < seaPresets.Length; i++) {
            for (int j = i + 1; j < seaPresets.Length; j++) {
               Assert.IsFalse(seaPresets[i] == seaPresets[j], "Sea preset #" + i.ToString() + " is same as sea preset #" + j.ToString() + ". Please provide unique presets");
            }
         }
      }

      private void testMissingLayersInPresets () {
         testMissingLayersInPresets(areaPresets, areaLayerIndexes);
         testMissingLayersInPresets(seaPresets, seaLayerIndexes);
         testMissingLayersInPresets(interiorPreset, interiorLayerIndexes);
      }

      private void testMissingLayersInPresets (MinimapGeneration.MinimapGeneratorPreset preset, LayerConfig[] layerIndexes) {
         MinimapGeneration.MinimapGeneratorPreset[] presets = { preset };
         testMissingLayersInPresets(presets, layerIndexes);
      }

      private void testMissingLayersInPresets (MinimapGeneration.MinimapGeneratorPreset[] presets, LayerConfig[] layerIndexes) {
         List<string> missingLayerNames = new List<string>();
         // Check if all layers were provided by presets
         for (int i = 0; i < presets.Length; i++) {
            foreach (LayerConfig layerConfig in layerIndexes) {
               bool isLayerCorrect = false;
               foreach (MinimapGeneration.TileLayer layer in presets[i]._tileLayer) {
                  if (layer.Name == layerConfig.layer) {
                     isLayerCorrect = true;
                     break;
                  }
               }

               if (isLayerCorrect) {
                  continue;
               } else {
                  missingLayerNames.Add(layerConfig.layer);
               }
            }

            if (missingLayerNames.Count > 0) {
               string missingLayersSingleString = "";
               for (int s = 0; s < missingLayerNames.Count; s++) {
                  missingLayersSingleString += missingLayerNames[s];
                  if (s < missingLayerNames.Count - 1) {
                     missingLayersSingleString += ", ";
                  }
               }
               Assert.IsTrue(false, "Preset: " + presets[i].name + " is missing layers: " + missingLayersSingleString);
            }
         }
      }
   }
}


