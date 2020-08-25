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
      public static string OnlyPalette = "ship_flag_nation";
   }

   public class ShipHull
   {
      public static string Brown = "";
   }

   public class ShipSail
   {
      public static string White = "";
   }

   // The guild icon palette names and their representing color
   public static Dictionary<string, Color> guildIcon1 = new Dictionary<string, Color>() {
      {"guild_icon_one_orange_2", PaletteSwapManager.intToColor (236, 134, 13) },
      {"guild_icon_one_yellow", PaletteSwapManager.intToColor (243, 235, 21) },
      {"guild_icon_one_green_1", PaletteSwapManager.intToColor (130, 232, 137) },
      {"guild_icon_one_green_2", PaletteSwapManager.intToColor (13, 236, 29) },
      {"guild_icon_one_green_3", PaletteSwapManager.intToColor (20, 172, 30) },
      {"guild_icon_one_cyan", PaletteSwapManager.intToColor (13, 233, 236) },
      {"guild_icon_one_blue", PaletteSwapManager.intToColor (19, 79, 118) },
      {"guild_icon_one_blue_2", PaletteSwapManager.intToColor (13, 107, 236) },
      {"guild_icon_one_blue_3", PaletteSwapManager.intToColor (60, 141, 193) },
      {"guild_icon_one_purple", PaletteSwapManager.intToColor (149, 15, 193) },
      {"guild_icon_one_pink", PaletteSwapManager.intToColor (236, 15, 228) },
      {"guild_icon_one_red", PaletteSwapManager.intToColor (246, 18, 18) },
      {"guild_icon_one_orange", PaletteSwapManager.intToColor (224, 170, 154) },
      {"guild_icon_one_black", PaletteSwapManager.intToColor (39, 39, 39) },
      {"guild_icon_one_white", PaletteSwapManager.intToColor (255, 255, 255) },
   };

   public static Dictionary<string, Color> guildIcon2 = new Dictionary<string, Color>() {
      {"guild_icon_two_orange_2", PaletteSwapManager.intToColor (236, 134, 13) },
      {"guild_icon_two_yellow", PaletteSwapManager.intToColor (243, 235, 21) },
      {"guild_icon_two_green_1", PaletteSwapManager.intToColor (130, 232, 137) },
      {"guild_icon_two_green_2", PaletteSwapManager.intToColor (13, 236, 29) },
      {"guild_icon_two_green_3", PaletteSwapManager.intToColor (20, 172, 30) },
      {"guild_icon_two_cyan", PaletteSwapManager.intToColor (13, 233, 236) },
      {"guild_icon_two_blue", PaletteSwapManager.intToColor (19, 79, 118) },
      {"guild_icon_two_blue_2", PaletteSwapManager.intToColor (13, 107, 236) },
      {"guild_icon_two_blue_3", PaletteSwapManager.intToColor (60, 141, 193) },
      {"guild_icon_two_purple", PaletteSwapManager.intToColor (149, 15, 193) },
      {"guild_icon_two_pink", PaletteSwapManager.intToColor (236, 15, 228) },
      {"guild_icon_two_red", PaletteSwapManager.intToColor (246, 18, 18) },
      {"guild_icon_two_orange", PaletteSwapManager.intToColor (224, 170, 154) },
      {"guild_icon_two_black", PaletteSwapManager.intToColor (39, 39, 39) },
      {"guild_icon_two_white", PaletteSwapManager.intToColor (255, 255, 255) },
   };

   #endregion

   #region Private Variables

   #endregion
}
