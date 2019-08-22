using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Biome {
   #region Public Variables

   // The Type of Biome
   public enum Type {  None = 0, Tropical = 1, Desert = 2, Pine = 3, Snow = 4, Lava = 5, Mushroom = 6 }

   #endregion

   public static string getName (Type biomeType) {
      switch (biomeType) {
         case Type.Tropical:
            return "Tropical Shores";
         case Type.Desert:
            return "Desert Isles";
         case Type.Pine:
            return "Misty Forests";
         case Type.Snow:
            return "Snowy Peaks";
         case Type.Lava:
            return "Lava Lands";
         case Type.Mushroom:
            return "Shroom Seas";
         default:
            return "Unknown";
      }
   }

   public static List<Type> getAllTypes () {
      List<Type> list = Util.getAllEnumValues<Type>();
      list.Remove(Type.None);
      return list;
   }

   #region Private Variables

   #endregion
}
