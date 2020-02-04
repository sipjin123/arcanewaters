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
      // Scale ratio
      public float scaleAlteration = 1;

      // Rotation ratio
      public float rotationAlteration = 0;

      // Determines the layer of the sprite
      public int layerIndex = 0;

      // Determines if the sprite can be moved or not
      public bool isLocked = false;

      // The path of the sprite
      public string spritePath;

      // Position of the obj locally
      public Vector2 localPosition;
   }
}