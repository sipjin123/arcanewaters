using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Specialty : MonoBehaviour {
   #region Public Variables

   // The Types of specialties
   public enum Type {
      None = 0, Treasure = 1, Adventurer = 2, Merchant = 3, Farmer = 4, Crafter = 5,
      Sharpshooter = 6, Fencer = 7, Brawler = 8, Sailor = 9, Cannoneer = 10, TestEntry = 11
   }

   #endregion

   public static string toString (Type type) {
      switch (type) {
         case Type.Treasure:
            return "Treasure Hunter";
         default:
            return type + "";
      }
   }

   public static string getDescription (Type type) {
      switch (type) {
         case Type.Treasure:
            return "+50 luck";
         case Type.Adventurer:
            return "+10% dmg, +10% health";
         case Type.Merchant:
            return "+20% trade profit";
         case Type.Farmer:
            return "+20% crop growth rate";
         case Type.Crafter:
            return "+15% craft quality";
         case Type.Sharpshooter:
            return "+20% ranged dmg";
         case Type.Fencer:
            return "+20% sword dmg";
         case Type.Brawler:
            return "+25% health";
         case Type.Sailor:
            return "+15% ship speed";
         case Type.Cannoneer:
            return "+20% cannon dmg";
         default:
            return "";
      }
   }

   public static Sprite getIcon (Type type) {
      return ImageManager.getSprite("Icons/Specialties/specialty_" + type);
   }

   #region Private Variables

   #endregion
}
