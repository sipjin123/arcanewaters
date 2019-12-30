using UnityEngine;
using UnityEngine.Tilemaps;

namespace MapCreationTool
{
   public class InteriorWallConfig : MonoBehaviour
   {
      public Tilemap tilemap;
      public Vector2Int size = new Vector2Int(5, 9);
   }
}
