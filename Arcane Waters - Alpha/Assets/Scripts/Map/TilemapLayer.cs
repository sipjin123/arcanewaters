using UnityEngine.Tilemaps;
using MapCreationTool;
using UnityEngine;

[System.Serializable]
public class TilemapLayer
{
   #region Public Variables

   // The tilemap component of the layer
   public Tilemap tilemap;

   // The full name of the layer containing number of layer
   public string fullName;

   // The name of the layer
   public string name;

   // The type of the layer
   public LayerType type;

   #endregion

   public bool coversAllObjects () {
      // Return true, if this layer is placed above player and prefabs
      return Mathf.FloorToInt(tilemap.transform.position.z) <= 0;
   }

   #region Private Variables

   #endregion
}
