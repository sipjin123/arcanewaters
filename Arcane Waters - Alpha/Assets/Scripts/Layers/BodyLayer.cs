using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class BodyLayer : SpriteLayer {
   #region Public Variables

   // The Type of Body we have
   public enum Type {
      Male_Body_1 = 101, Male_Body_2 = 102, Male_Body_3 = 103, Male_Body_4 = 104,
      Female_Body_1 = 201, Female_Body_2 = 202, Female_Body_3 = 203, Female_Body_4 = 204,
   }

   #endregion

   public void setType (Type newBodyType) {
      _type = newBodyType;

      // Update our Animated Sprite
      Gender.Type gender = newBodyType.ToString().StartsWith("Male") ? Gender.Type.Male : Gender.Type.Female;
      string path = "Bodies/" + gender + "/" + newBodyType;
      Texture2D result = ImageManager.getTexture(path);
      setTexture(result);
   }

   public static List<Type> getList (Gender.Type gender) {
      List<Type> list = new List<Type>();

      foreach (Type bodyType in Enum.GetValues(typeof(Type))) {
         if (bodyType.ToString().Contains(gender.ToString())) {
            list.Add(bodyType);
         }
      }

      return list;
   }

   public Type getType () {
      return _type;
   }


   public static Type getRandomBodyTypeOfGender (Gender.Type gender) {
      List<Type> genderTypes = new List<Type>();
      Array bodyTypes = Enum.GetValues(typeof(Type));

      foreach (var item in bodyTypes) {
         var bodyType = (Type) item;

         if ((gender == Gender.Type.Male && bodyType.ToString().ToLower().StartsWith("male")) || (gender == Gender.Type.Female && bodyType.ToString().ToLower().StartsWith("female"))) {
            genderTypes.Add(bodyType);
         }
      }

      int index = Mathf.FloorToInt(UnityEngine.Random.Range(0, genderTypes.Count - 1));
      return genderTypes[index];
   }

   #region Private Variables

   // Our current type
   protected Type _type;

   #endregion
}
