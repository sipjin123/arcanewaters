﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Rendering;

public class HatLayer : SpriteLayer
{
   #region Public Variables

   // The equipment id of the hat
   public int equipmentId = 0;

   #endregion

   public void setType (Gender.Type gender, int newType, bool immediate = true) {
      _type = newType;

      // Update our Animated Sprite
      string path = (newType == 0) ? "Empty_Layer" : "Hats/" + "hat_" + (int) newType;
      Texture2D result = ImageManager.getTexture(path);

      if (immediate) {
         GetComponent<SpriteSwap>().newTexture = result;
      } else {
         StartCoroutine(CO_SwapTexture(result));
      }
   }

   public int getType () {
      return _type;
   }

   #region Private Variables

   // Our current type
   protected int _type;

   // The material used by this layer
   protected Material _material;

   #endregion
}
