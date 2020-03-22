using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class InteriorWallConfig : MonoBehaviour
   {
      public BiomeTilemap biomeTilemap;
      public Vector2Int size = new Vector2Int(5, 9);

      [System.Serializable]
      public class BiomeTilemap
      {
         public Biome.Type biome;
         public Tilemap tilemap;
      }
   }
}
