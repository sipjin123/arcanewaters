using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

namespace BackgroundTool
{
   [Serializable]
   public class SpriteTemplateData
   {
      // Determines the layer of the sprite
      public int layerIndex = 3;

      // Determines the z axis of the sprite
      public int zAxisOffset = 0;

      // Determines if the sprite can be moved or not
      public bool isLocked = false;

      // The path of the sprite
      public string spritePath;

      // Position of the obj locally
      public Vector2 localPosition;
   }
}