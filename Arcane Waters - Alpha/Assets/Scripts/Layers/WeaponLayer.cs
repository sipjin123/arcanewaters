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

   public static string getSheetName (Gender.Type gender, Weapon.Type newType, bool isFront) {
      // Insert "Back" if it's the Back layer
      string adjustedName = gender + "_" + newType + (isFront ? "" : "_Back");

      return adjustedName;
   }

   public void setType (Gender.Type gender, Weapon.Type newType) {
      _type = newType;

      // Update our Animated Sprite
      string suffix = (isFront ? "_Front" : "_Back");
      string path = (newType == Weapon.Type.None) ? "Empty_Layer" : "Weapons/Female/" + newType + suffix;
      Texture2D result = ImageManager.getTexture(path);

      StartCoroutine(CO_SwapTexture(result));
   }

   public Weapon.Type getType () {
      return _type;
   }

   #region Private Variables

   // Our current type
   protected Weapon.Type _type;

   #endregion
}
