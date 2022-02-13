using System.Collections.Generic;
using System.Linq;

public class EmoteManager {
   #region Public Variables

   // The types of emotes
   public enum EmoteTypes
   {
      // None
      None = 0,

      // Dance
      Dance = 1,

      // Kneel
      Kneel = 2,

      // Greet
      Greet = 3,

      // Point
      Point = 4
   }

   #endregion

   public static List<string> getSupportedEmoteNames (bool lowerCase = true) {
      return System.Enum.GetNames(typeof(EmoteTypes))
         .Where(_ => !Util.areStringsEqual(_, "none"))
         .Select(_ => lowerCase ? _.ToLower() : _)
         .ToList();
   }

   public static EmoteTypes parse(string source) {
      if (Util.isEmpty(source)) {
         return EmoteTypes.None;
      }

      if (Util.areStringsEqual(source, "dance")) {
         return EmoteTypes.Dance;
      }

      if (Util.areStringsEqual(source, "kneel")) {
         return EmoteTypes.Kneel;
      }

      if (Util.areStringsEqual(source, "greet")) {
         return EmoteTypes.Greet;
      }

      if (Util.areStringsEqual(source, "point")) {
         return EmoteTypes.Point;
      }

      return EmoteTypes.None;
   }
}