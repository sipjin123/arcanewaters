namespace Steam
{
   public class SteamStatics
   {
      #region Public Variables

      #if IS_SERVER_BUILD 
      
      // Steam key parameters for web api request
      public static string STEAM_WEB_USER_API_KEY = "1EB0664926636257A9861504BE93721B";
      public static string STEAM_WEB_PUBLISHER_API_KEY = "16FBA4602CFF4C139DC40E01D58F8869";

      #else

      // Steam key parameters for web api request
      public static string STEAM_WEB_USER_API_KEY = "";
      public static string STEAM_WEB_PUBLISHER_API_KEY = "";

      #endif

      // The arcane waters steam app id
      public const string GAMEPLAYTEST_APPID = "1489170";
      public const string GAME_APPID = "1266340";

      // The various parameters used for the web api requests
      public const string PARAM_STEAM_ID = "steamid=";
      public const string PARAM_KEY = "key=";
      public const string PARAM_APPID = "appid=";
      public const string PARAM_TICKET = "ticket=";

      #endregion
   }
}
