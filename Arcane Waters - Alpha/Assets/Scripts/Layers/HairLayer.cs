using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.Rendering;

public class HairLayer : SpriteLayer {
   #region Public Variables

   // Whether this is the front or back hair layer
   public bool isFront = true;

   // The Type
   public enum Type {
      Male_Hair_1 = 101, Male_Hair_2 = 102, Male_Hair_3 = 103, Male_Hair_4 = 104, Male_Hair_5 = 105,
      Male_Hair_6 = 106, Male_Hair_7 = 107, Male_Hair_8 = 108, Male_Hair_9 = 109,

      Female_Hair_1 = 201, Female_Hair_2 = 202, Female_Hair_3 = 203, Female_Hair_4 = 204, Female_Hair_5 = 205,
      Female_Hair_6 = 206, Female_Hair_7 = 207, Female_Hair_8 = 208, Female_Hair_9 = 209, Female_Hair_10 = 210,

   }

   #endregion

   public static string getSheetName (Type newType, bool isFront) {
      // Insert "Front" or "Back" into the string name
      string[] split = newType.ToString().Split('_');
      string adjustedName = split[0] + "_" + split[1] + (isFront ? "_Front" : "_Back") + "_" + split[3];

      return adjustedName;
   }

   public void setType (Type newType) {
      _type = newType;

      // Update our Animated Sprite
      getSpriteSwap().newTexture = getTexture(newType, isFront);

      Material mat = getMaterial();
      mat.SetInt("_StencilPass", (int)StencilOp.Keep);
      mat.SetInt("_StencilCompare", (int) CompareFunction.NotEqual);
      mat.SetInt("_StencilRef", HAT_STENCIL_ID);
   }

   public static Texture2D getTexture (Type hairType, bool isFront) {
      Gender.Type gender = hairType.ToString().StartsWith("Male") ? Gender.Type.Male : Gender.Type.Female;
      string subfolder = isFront ? "Front" : "Back";
      string typeString = hairType.ToString().Replace("Hair", "Hair_" + subfolder);
      string path = "Hair/" + gender + "/" + subfolder + "/" + typeString;
      Texture2D result = ImageManager.getTexture(path, false);
      if (result == null) {
         result = ImageManager.getTexture("Empty_Layer");
      }

      return result;
   }

   public static List<Type> getList (Gender.Type gender) {
      List<Type> list = new List<Type>();

      foreach (Type hairType in Enum.GetValues(typeof(Type))) {
         if (hairType.ToString().Contains(gender.ToString())) {
            list.Add(hairType);
         }
      }

      return list;
   }

   public Type getType () {
      return _type;
   }

   #region Private Variables

   // Our current type
   protected Type _type;

   #endregion
}
