using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ArmorLayer : SpriteLayer {
   #region Public Variables

   // The equipment id of the armor
   public int equipmentId = 0;

   #endregion

   public void setType (Gender.Type gender, int newType, bool immediate = true) {
      _type = newType;

      // Update our Animated Sprite
      string path = (newType == 0) ? "Empty_Layer" : "Armor/" + gender + "/" + gender + "_armor_" + (int)newType;
      Texture2D result = ImageManager.getTexture(path);

      if (immediate) {
         setTexture(result);
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

   #endregion
}
