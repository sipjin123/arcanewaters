using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ArmorLayer : SpriteLayer {
   #region Public Variables

   #endregion

   public void setType (Gender.Type gender, Armor.Type newType, bool immediate = false) {
      _type = newType;

      // Update our Animated Sprite
      string path = (newType == Armor.Type.None) ? "Empty_Layer" : "Armor/" + gender + "/" + gender + "_armor_" + (int)newType;
      Texture2D result = ImageManager.getTexture(path);

      if (immediate) {
         GetComponent<SpriteSwap>().newTexture = result;
      } else {
         StartCoroutine(CO_SwapTexture(result));
      }
   }

   public Armor.Type getType () {
      return _type;
   }

   #region Private Variables

   // Our current type
   protected Armor.Type _type;

   #endregion
}
