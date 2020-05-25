using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PaletteDef : MonoBehaviour {
   #region Public Variables
      
   public class Eyes
   {
      public static string Blue = "eyes_blue";
      public static string Brown = "eyes_brown";
      public static string Green = "eyes_green";
      public static string Purple = "eyes_purple";
      public static string Black = "eyes_black";
   }

   public class Hair
   {
      public static string Brown = "hair_brown";
      public static string Red = "hair_red";
      public static string Black = "hair_black";
      public static string Yellow = "hair_yellow";
      public static string White = "hair_white";
      public static string Blue = "hair_blue";
   }

   public class Armor1
   {
      public static string Brown = "armor_one_brown";
      public static string White = "armor_one_white";
      public static string Blue = "armor_one_blue";
      public static string Red = "armor_one_red";
      public static string Green = "armor_one_green";
      public static string Yellow = "armor_one_yellow";
      public static string Teal = "armor_one_teal";
   }

   public class Armor2
   {
      public static string Brown = "armor_two_brown";
      public static string White = "armor_two_white";
      public static string Blue = "armor_two_blue";
      public static string Red = "armor_two_red";
      public static string Green = "armor_two_green";
      public static string Yellow = "armor_two_yellow";
      public static string Teal = "armor_two_teal";
   }

   // Is there any point of using male/female classes?
   public class Armor
   {
      public static string Black = "";
      public static string White = "";
   }

   public class Flag
   {
      public static string White = "";
      public static string Black = "";
      public static string Blue = "";
      public static string Yellow = "";
      public static string DarkGreen = "";
      public static string PureBlack = "";
   }

   public class ShipHull
   {
      public static string Brown = "";
   }

   public class ShipSail
   {
      public static string White = "";
   }

   public static string WHITE_WEAPON_COMPLETED_TUTORIAL_STEP = "";

   #endregion

   #region Private Variables

   #endregion
}
