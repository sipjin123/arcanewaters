using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;
using System;

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

      [System.Serializable]
      public class LayerConfig
      {
         public string layer;
         public int index;
         public float zOffset;
         public LayerType layerType = LayerType.Regular;
      }
   }
}


