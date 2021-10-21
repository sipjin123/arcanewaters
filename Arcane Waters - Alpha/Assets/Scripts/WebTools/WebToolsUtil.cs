public static class WebToolsUtil
{
   // Game request token
   public const string GAME_TOKEN_HEADER = "GameToken";
   public const string GAME_TOKEN = "arcane_game_vjk53fx";

   // Constants for Tasks / Bug Reports
   public const string UNASSIGNED = "Unassigned";
   public const string OPEN = "Open";
   public const string CLOSED = "Closed";

   public const string ASSIGN = "Assign";
   public const string WATCH = "Watch";
   public const string CREATE = "Create";
   public const string CLEAR = "Clear";
   public const string CLOSE = "Close";
   public const string REOPEN = "Re-Open";

   // Response Codes
   public const int SUCCESS = 200;
   public const int BAD_REQUEST = 400;

   // Endpoints
   public const string BASE_URL = "https://tools.arcanewaters.com/api";

   public const string BUG_REPORT_SUBMIT = BASE_URL + "/tasks/submit";
   public const string BUG_REPORT_SERVER_LOG_SUBMIT = BASE_URL + "/tasks/submitServerLog";
   public const string COMPLAINT_SUBMIT = BASE_URL + "/supportTickets/submit";

   public enum ActionSource
   {
      None = 0,
      Game = 1,
      WebTools = 2
   }
}

public enum TicketSourceType
{
   Game = 1,
   Server = 2,
   Email = 3
}

public enum SessionEvent
{
   GameAccountLogin = 1,
   GameUserCreate = 2,
   GameUserDestroy = 3,
   WebToolsLogin = 4
}
