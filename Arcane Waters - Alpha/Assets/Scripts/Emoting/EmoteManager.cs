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
      Greet = 3
   }

   #endregion

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

      return EmoteTypes.None;
   }
}