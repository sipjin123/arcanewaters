namespace Steam
{
   public class SteamStatics
   {
      #region Public Variables

      #if IS_SERVER_BUILD 
      
      // Steam key parameters for web api request
      public const string STEAM_WEB_USER_API_KEY = "1EB0664926636257A9861504BE93721B";
      public const string STEAM_WEB_PUBLISHER_API_KEY = "16FBA4602CFF4C139DC40E01D58F8869";
      // https://api.steampowered.com/ISteamUserStats/GetUserStatsForGame/v2/?appid1266340=&key=16FBA4602CFF4C139DC40E01D58F8869&steamid=
      // https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v2/?key=1EB0664926636257A9861504BE93721B&steamids=76561198067124199

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

      // The web API post command for altering and retrieving DATA
      public const string STEAMWEBAPI_SET_USER_STATS = "https://partner.steam-api.com/ISteamUserStats/SetUserStatsForGame/v1/";
      public const string REQUEST_ACHIEVEMENTS = "https://api.steampowered.com/ISteamUserStats/GetPlayerAchievements/v1/";
      public const string REQUEST_STATSFORGAME = "https://api.steampowered.com/ISteamUserStats/GetUserStatsForGame/v2/";

      #endregion
   }
}
