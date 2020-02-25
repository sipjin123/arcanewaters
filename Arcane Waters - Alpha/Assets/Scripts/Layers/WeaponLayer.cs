using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class WeaponLayer : SpriteLayer {
   #region Public Variables

   // Whether this is the front or back hair layer
   public bool isFront = true;

   #endregion

   public static string getSheetName (Gender.Type gender, int newType, bool isFront) {
      // Insert "Back" if it's the Back layer
      string adjustedName = gender + "_" + newType + (isFront ? "" : "_Back");

      return adjustedName;
   }

   public void setType (Gender.Type gender, int newType) {
      _type = newType;

      // Update our Animated Sprite
      string suffix = (isFront ? "_Front" : "_Back");
      string path = (newType == 0) ? "Empty_Layer" : "Weapons/Female/weapon_" + newType + suffix;
      Texture2D result = ImageManager.getTexture(path);

      StartCoroutine(CO_SwapTexture(result));
   }

   public int getType () {
      return _type;
   }

   #region Private Variables

   // Our current type
   protected int _type;

   #endregion
}
