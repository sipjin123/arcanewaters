using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class EyesLayer : SpriteLayer {
   #region Public Variables

   // The Type
   public enum Type {
      Male_Eyes_1 = 101, Male_Eyes_2 = 102, Male_Eyes_3 = 103, Male_Eyes_4 = 104, Male_Eyes_5 = 105,
      Male_Eyes_6 = 106,

      Female_Eyes_1 = 201, Female_Eyes_2 = 202, Female_Eyes_3 = 203, Female_Eyes_4 = 204, Female_Eyes_5 = 205,
      Female_Eyes_6 = 206,
   }

   #endregion

   public void setType (Type newType) {
      _type = newType;

      // Update our Animated Sprite
      Gender.Type gender = newType.ToString().StartsWith("Male") ? Gender.Type.Male : Gender.Type.Female;
      string path = "Eyes/" + gender + "/" + newType;
      Texture2D result = ImageManager.getTexture(path);
      getSpriteSwap().newTexture = result;
   }

   public static List<Type> getList (Gender.Type gender) {
      List<Type> list = new List<Type>();

      foreach (Type eyeType in Enum.GetValues(typeof(Type))) {
         if (eyeType.ToString().Contains(gender.ToString())) {
            list.Add(eyeType);
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
