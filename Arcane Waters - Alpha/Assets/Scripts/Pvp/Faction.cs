using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Faction {
   #region Public Variables
      
   public enum Type
   {
      None = 0,
      Privateers = 1,
      Naturalists = 2,
      Builders = 3,
      Cartographers = 4,
      Pirates = 5,
      Pillagers = 6,
      Merchants = 7,
      COUNT = 8 ,
   }

   #endregion

   public static List<Type> getAllValidTypes () {
      List<Type> validTypes = new List<Type>();
      
      for (int i = 1; i < (int)Type.COUNT; i++) {
         validTypes.Add((Type) i);
      }

      return validTypes;
   }

   public static string getShipSpritePath (Type factionType) {
      return "Sprites/Ships/type_1_" + factionType.ToString().ToLower();
   }

   #region Private Variables
      
   #endregion
}
