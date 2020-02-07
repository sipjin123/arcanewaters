using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using static BackgroundTool.ImageLoader;

namespace BackgroundTool
{
   [Serializable]
   public class SpriteTemplateData
   {
      // Determines the layer of the sprite
      public int layerIndex = 3;

      // Determines the z axis of the sprite
      public float zAxisOffset = 0;

      // Determines if the sprite can be moved or not
      public bool isLocked = false;

      // The path of the sprite
      public string spritePath;

      // Position of the obj locally
      public Vector2 localPositionData;

      // Determines the content category of the sprite if it is a bg sprite etc
      public BGContentCategory contentCategory;
   }
}