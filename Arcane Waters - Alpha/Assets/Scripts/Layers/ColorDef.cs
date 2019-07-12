using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public enum ColorType {
   None = 0,

   Red = 100, Green = 101, Blue = 102, White = 103, Black = 104, Yellow = 105,
   Teal = 110, Orange = 111, Pink = 112, Brown = 113,

   LightGreen = 200, LightPurple = 201, LightRed = 202, LightBlue = 203, LightYellow = 204,
   LightPink = 210, LightGray = 211,

   DarkGreen = 300, DarkPurple = 301, DarkRed = 302, DarkBlue = 303, DarkYellow = 304,
   DarkPink = 310, DarkGray = 311,

   SteelBlue = 500, SteelGrey = 501,

   HullBrown = 600, SourceRed = 601, SourceGreen = 602, SourceBlue = 603,
   SailWhite = 700,
   FlagWhite = 800, FlagBlack = 801, FlagBlue = 802, FlagYellow = 803, FlagBrown2 = 805,

   GreenEyes = 900, PurpleEyes = 901, BlackEyes = 902,

   PureBlack = 1000,
}

public class ColorDef {
   #region Public Variables

   // The Type of Color
   public ColorType colorType;

   // The displayed name of the color
   public string colorName;

   // The color associated with this color definition
   public Color color;

   #endregion

   public ColorDef(string colorName, ColorType colorType, Color color) {
      this.colorName = colorName;
      this.colorType = colorType;
      this.color = color;
   }

   public static ColorDef get (ColorType colorType) {
      switch (colorType) {
         case ColorType.SourceRed:
            return new ColorDef("Source Red", colorType, Util.getColor(255, 0, 0));
         case ColorType.SourceGreen:
            return new ColorDef("Source Green", colorType, Util.getColor(0, 255, 0));
         case ColorType.SourceBlue:
            return new ColorDef("Source Blue", colorType, Util.getColor(0, 0, 255));

         case ColorType.HullBrown:
            return new ColorDef("Hull Brown", colorType, Util.getColor(220, 130, 110));

         case ColorType.SailWhite:
            return new ColorDef("Sail White", colorType, Util.getColor(215, 255, 255));

         case ColorType.FlagWhite:
            return new ColorDef("Flag White", colorType, Util.getColor(255, 225, 225));
         case ColorType.FlagBlack:
            return new ColorDef("Flag Black", colorType, Util.getColor(40, 40, 40));
         case ColorType.FlagBlue:
            return new ColorDef("Flag Blue", colorType, Util.getColor(80, 80, 210));
         case ColorType.FlagYellow:
            return new ColorDef("Flag Yellow", colorType, Util.getColor(250, 230, 50));
         case ColorType.FlagBrown2:
            return new ColorDef("Flag Brown 2", colorType, Util.getColor(150, 100, 100));

         case ColorType.Red:
            return new ColorDef("Red", colorType, Util.getColor(206, 83, 83));
         case ColorType.Green:
            return new ColorDef("Green", colorType, Util.getColor(118, 238, 109));
         case ColorType.Blue:
            return new ColorDef("Blue", colorType, Util.getColor(0, 180, 255));
         case ColorType.White:
            return new ColorDef("White", colorType, Util.getColor(215, 255, 255));
         case ColorType.Black:
            return new ColorDef("Black", colorType, Util.getColor(55, 70, 70));
         case ColorType.Yellow:
            return new ColorDef("Yellow", colorType, Util.getColor(229, 244, 95));

         case ColorType.Teal:
            return new ColorDef("Teal", colorType, Util.getColor(124, 250, 255));
         case ColorType.Orange:
            return new ColorDef("Orange", colorType, Util.getColor(255, 190, 96));
         case ColorType.Pink:
            return new ColorDef("Pink", colorType, Util.getColor(255, 124, 215));
         case ColorType.Brown:
            return new ColorDef("Brown", colorType, Util.getColor(140, 100, 60));

         case ColorType.LightGreen:
            return new ColorDef("Light Green", colorType, Util.getColor(85, 221, 131));
         case ColorType.LightPurple:
            return new ColorDef("Light Purple", colorType, Util.getColor(218, 135, 255));
         case ColorType.LightRed:
            return new ColorDef("Light Red", colorType, Util.getColor(225, 120, 120));
         case ColorType.LightBlue:
            return new ColorDef("Light Blue", colorType, Util.getColor(100, 100, 255));
         case ColorType.LightYellow:
            return new ColorDef("Light Yellow", colorType, Util.getColor(214, 214, 126));

         case ColorType.LightPink:
            return new ColorDef("Light Pink", colorType, Util.getColor(255, 193, 230));
         case ColorType.LightGray:
            return new ColorDef("Light Gray", colorType, Util.getColor(192, 192, 192));

         case ColorType.DarkGreen:
            return new ColorDef("Dark Green", colorType, Util.getColor(80, 160, 100));
         case ColorType.DarkPurple:
            return new ColorDef("Dark Purple", colorType, Util.getColor(168, 86, 221));
         case ColorType.DarkRed:
            return new ColorDef("Dark Red", colorType, Util.getColor(96, 36, 36));
         case ColorType.DarkBlue:
            return new ColorDef("Dark Blue", colorType, Util.getColor(30, 30, 75));
         case ColorType.DarkYellow:
            return new ColorDef("Dark Yellow", colorType, Util.getColor(128, 128, 63));

         case ColorType.DarkPink:
            return new ColorDef("Dark Pink", colorType, Util.getColor(150, 0, 88));
         case ColorType.DarkGray:
            return new ColorDef("Dark Gray", colorType, Util.getColor(64, 64, 64));

         case ColorType.SteelBlue:
            return new ColorDef("Steel Blue", colorType, Util.getColor(116, 213, 238));
         case ColorType.SteelGrey:
            return new ColorDef("Steel Grey", colorType, Util.getColor(200, 225, 225));

         case ColorType.GreenEyes:
            return new ColorDef("Jade", colorType, Util.getColor(80, 160, 100));
         case ColorType.PurpleEyes:
            return new ColorDef("Violet", colorType, Util.getColor(168, 86, 221));
         case ColorType.BlackEyes:
            return new ColorDef("Dark", colorType, Util.getColor(55, 70, 70));

         case ColorType.PureBlack:
            return new ColorDef("Pure Black", colorType, Util.getColor(0, 0, 0));


         case ColorType.None:
            return new ColorDef("None", colorType, Util.getColor(255, 255, 255));

         default:
            return new ColorDef("Unknown", colorType, Util.getColor(0, 0, 0));
      }
   }

   public static List<ColorType> getColors () {
      List<ColorType> colors = new List<ColorType>();

      foreach (ColorType colorType in Enum.GetValues(typeof(ColorType))) {
         colors.Add(colorType);
      }

      return colors;
   }

   #region Private Variables

   #endregion
}
