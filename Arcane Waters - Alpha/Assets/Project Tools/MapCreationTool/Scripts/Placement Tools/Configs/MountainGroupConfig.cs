using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class MountainGroupConfig : MonoBehaviour
   {
      public BiomeTilemaps biomeTileMaps = new BiomeTilemaps();
      public Vector2Int innerSize = Vector2Int.zero;
      public Vector2Int outerSize = Vector2Int.zero;

      [System.Serializable]
      public class BiomeTilemaps
      {
         public Biome.Type biome;
         public Tilemap outerTilemap;
         public Tilemap innerTilemap;
      }
   }
}