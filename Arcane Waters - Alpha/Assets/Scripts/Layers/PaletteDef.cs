using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class PaletteDef : MonoBehaviour
{
   #region Public Variables

   public struct Subcategory
   {
      // Name of recolor
      public string name;

      // Type assosiated with this recolor
      public PaletteToolManager.PaletteImageType type;

      // Source colors stored in hex format
      public string[] colorsHex;

      // Constructor requiring all of above variables
      public Subcategory (string name_, PaletteToolManager.PaletteImageType type_, string[] colorsHex_) {
         name = name_;
         type = type_;
         colorsHex = colorsHex_;
      }
   }

   // Tags used to mark different palette types
   public class Tags
   {
      public static int STARTER = 4;
   }

   public class Eyes
   {
      public static string Blue = "eyes_blue";
      public static string Brown = "eyes_brown";
      public static string Green = "eyes_green";
      public static string Purple = "eyes_purple";
      public static string Black = "eyes_black";

      public static Subcategory primary = new Subcategory("primary", PaletteToolManager.PaletteImageType.Eyes, new string[1] { "#00FF00" });
   }

   public class Hair
   {
      public static string Brown = "hair_brown";
      public static string Red = "hair_red";
      public static string Black = "hair_black";
      public static string Yellow = "hair_yellow";
      public static string White = "hair_white";
      public static string Blue = "hair_blue";

      public static Subcategory primary = new Subcategory("primary", PaletteToolManager.PaletteImageType.Hair, new string[4] { "#B15D23", "#85461A", "#673412", "#50280A" });
      public static Subcategory secondary = new Subcategory("secondary", PaletteToolManager.PaletteImageType.Hair, new string[4] { "#F82727", "#E21414", "#B71212", "#900202" });
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

      public static Subcategory primary = new Subcategory("primary", PaletteToolManager.PaletteImageType.Armor, new string[4] { "#D9E9FB", "#A1B7CC", "#748EA7", "#4F6276" });
      public static Subcategory secondary = new Subcategory("secondary", PaletteToolManager.PaletteImageType.Armor, new string[4] { "#70B9FF", "#3284D2", "#075CA7", "#083B6B" });
      public static Subcategory accent = new Subcategory("accent", PaletteToolManager.PaletteImageType.Armor, new string[4] { "#F82727", "#E21414", "#B71212", "#900202" });
   }

   public class Weapon
   {
      public static Subcategory primary = new Subcategory("primary", PaletteToolManager.PaletteImageType.Weapon, new string[4] { "#E9EEF2", "#91A7B6", "#687E8D", "#465B69" });
      public static Subcategory secondary = new Subcategory("secondary", PaletteToolManager.PaletteImageType.Weapon, new string[4] { "#E7A38C", "#B5654B", "#7B3C27", "#442116" });
      public static Subcategory power = new Subcategory("power", PaletteToolManager.PaletteImageType.Weapon, new string[4] { "#A8FCFF", "#68FEFF", "#00FEFF", "#00CFD0" });
   }

   public class Flag
   {
      public static string OnlyPalette = "ship_flag_nation";
   }

   public class Ship
   {
      public static Subcategory hull = new Subcategory("hull", PaletteToolManager.PaletteImageType.Ship, new string[4] { "#CC4435", "#A7322E", "#822028", "#5B111C" });
      public static Subcategory sail = new Subcategory("sail", PaletteToolManager.PaletteImageType.Armor, new string[3] { "#E1DCFF", "#B6ABED", "#6B60A2" });
      public static Subcategory flag = new Subcategory("flag", PaletteToolManager.PaletteImageType.Armor, new string[2] { "#91BDFF", "#3888FF" });
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

   public class GuildIcon
   {
      public static Subcategory primary = new Subcategory("primary", PaletteToolManager.PaletteImageType.GuildIconBackground, new string[4] { "#00FF00", "#00C400", "#FF0000", "#AC0000" });
   }

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
