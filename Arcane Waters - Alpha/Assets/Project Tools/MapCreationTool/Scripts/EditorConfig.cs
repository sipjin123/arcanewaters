using UnityEngine;
using UnityEngine.Tilemaps;
using System.Linq;

namespace MapCreationTool
{
   [CreateAssetMenu(fileName = "New config", menuName = "Data/Editor config")]
   public class EditorConfig : ScriptableObject
   {
      public Tile[] layerTiles;
      public Tile[] clusterTiles;
      public TileBase transparentTile;

      public TitleIndex[] areaLayerTitleIndexes;

      public string[] areaLayerMap
      {
         get
         {
            string[] result = new string[areaLayerTitleIndexes.Max(l => l.index+1)];
            foreach (TitleIndex tl in areaLayerTitleIndexes) {
               result[tl.index] = tl.layer;
            }

            return result;
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


