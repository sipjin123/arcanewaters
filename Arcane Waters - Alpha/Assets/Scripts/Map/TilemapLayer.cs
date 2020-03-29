﻿using UnityEngine.Tilemaps;
using MapCreationTool;

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

   #region Private Variables

   #endregion
}
