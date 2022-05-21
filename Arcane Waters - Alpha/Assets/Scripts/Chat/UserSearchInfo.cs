public class UserSearchInfo {
   #region Public Variables

   // The input used to do the search
   public string input;

   // The filtering mode to be used during the search
   public FilteringMode filter;

   // Type of search
   public enum FilteringMode
   {
      // None
      None = 0,

      // By Name
      Name = 1,

      // By Biome
      Biome = 2,

      // By Level
      Level = 3,

      // By account's steam id
      SteamId = 4
   }

   // The page to return
   public int page = 0;

   // The number of results per page requested
   public int resultsPerPage = 1;

   public static bool tryParseFilteringMode(string text, out FilteringMode filter) {
      if (Util.areStringsEqual(text, "is")) {
         filter = FilteringMode.Name;
         return true;
      }

      if (Util.areStringsEqual(text, "in")) {
         filter = FilteringMode.Biome;
         return true;
      }

      if (Util.areStringsEqual(text, "lv") || Util.areStringsEqual(text, "level")) {
         filter = FilteringMode.Level;
         return true;
      }

      filter = FilteringMode.None;
      return false;
   }

   #endregion
}
