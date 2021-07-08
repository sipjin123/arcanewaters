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
        public Subcategory(string name_, PaletteToolManager.PaletteImageType type_, string[] colorsHex_)
        {
            name = name_;
            type = type_;
            colorsHex = colorsHex_;
        }
    }

    // Tags used to mark different palette types
    public class Tags
    {
        public static int STARTER = 4;
        public static int PvP = 5;
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
        public static Subcategory flag = new Subcategory("flag", PaletteToolManager.PaletteImageType.Flag, new string[1] { "#FF00FF" });
    }

    public class Ship
    {
        public static Subcategory hull = new Subcategory("hull", PaletteToolManager.PaletteImageType.Ship, new string[4] { "#CC4435", "#A7322E", "#822028", "#5B111C" });
        public static Subcategory sail = new Subcategory("sail", PaletteToolManager.PaletteImageType.Ship, new string[3] { "#E1DCFF", "#B6ABED", "#6B60A2" });
        public static Subcategory flag = new Subcategory("flag", PaletteToolManager.PaletteImageType.Ship, new string[2] { "#91BDFF", "#3888FF" });
    }

    public class SeaStructure
    {
      public static Subcategory fill = new Subcategory("fill", PaletteToolManager.PaletteImageType.SeaStructure, new string[3] { "F230DF", "830269", "#FF00FF" });
      public static Subcategory outline = new Subcategory("outline", PaletteToolManager.PaletteImageType.SeaStructure, new string[3] { "D100A7", "5C0854", "410B36" });
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

    public class GuildIconBackground
    {
        public static Subcategory primary = new Subcategory("primary", PaletteToolManager.PaletteImageType.GuildIconBackground, new string[2] { "#FF0000", "#AC0000" });
        public static Subcategory secondary = new Subcategory("secondary", PaletteToolManager.PaletteImageType.GuildIconBackground, new string[2] { "#00FF00", "#00C400" });
    }

    public class GuildIconSigil
    {
        public static Subcategory primary = new Subcategory("primary", PaletteToolManager.PaletteImageType.GuildIconSigil, new string[2] { "#FF0000", "#AC0000" });
        public static Subcategory secondary = new Subcategory("secondary", PaletteToolManager.PaletteImageType.GuildIconSigil, new string[2] { "#00FF00", "#00C400" });
    }

    public class Guild
    {
        public static Subcategory primary = new Subcategory("primary", PaletteToolManager.PaletteImageType.GuildIconBackground, new string[2] { "#FF0000", "#AC0000" });
        public static Subcategory secondary = new Subcategory("secondary", PaletteToolManager.PaletteImageType.GuildIconBackground, new string[2] { "#00FF00", "#00C400" });
    }

    #endregion

    #region Private Variables

    #endregion
}
