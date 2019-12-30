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

      public TitleIndex[] areaLayerIndexes;
      public TitleIndex[] seaLayerIndexes;
      public TitleIndex[] interiorLayerIndexes;

      public string[] areaLayerNames
      {
         get { return areaLayerIndexes.Select(l => l.layer).ToArray(); }
      }

      public string[] seaLayerNames
      {
         get { return seaLayerIndexes.Select(l => l.layer).ToArray(); }
      }

      public string[] interiorLayerNames
      {
         get { return interiorLayerIndexes.Select(l => l.layer).ToArray(); }
      }

      public int getIndex(string layer, EditorType editorType) {
         foreach(TitleIndex ti in getLayers(editorType)) {
            if (ti.layer.CompareTo(layer) == 0)
               return ti.index;
         }
         throw new Exception($"Undefined layer {layer}.");
      }

      public TitleIndex[] getLayers (EditorType editorType) {
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
      public class TitleIndex
      {
         public string layer;
         public int index;
      }
   }
}


