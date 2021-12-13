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

   // Endpoints
   //public const string BaseUrl = "https://localhost:5001/api";
   public const string BaseUrl = "https://tools.arcanewaters.com/api";

   public const string BUG_REPORT_SUBMIT = BaseUrl + "/tasks/submit";
   public const string BUG_REPORT_SERVER_LOG_SUBMIT = BaseUrl + "/tasks/submitServerLog";
   public const string SubmitTicket = BaseUrl + "/supportTickets/submit";

   public const string SubmitTicketLogs = BaseUrl + "/supportTickets/submitLogs";
   public const string SubmitTicketScreenshot = BaseUrl + "/supportTickets/submitScreenshot";

   // Subject lengths for bug reports and support tickets (complaints)
   public const int MinSubjectLength = 3;
   public const int MaxSubjectLength = 256;

   // Minimum interval between reports (bugs & tickets)
   public const float ReportInterval = 5f;

   public enum ResponseCode
   {
      None = 0,
      Success = 200,
      BadRequest = 400
   }

   public enum ActionSource
   {
      None = 0,
      Game = 1,
      WebTools = 2,
      Email = 3
   }

   public enum HistoryActionType
   {
      None = 0,
      Create = 1,
      ReOpen = 2,
      Close = 3,
      ModifyRelated = 4
   }

   public enum ModifyRelatedType
   {
      None = 0,
      Add = 1,
      Remove = 2
   }

   public enum RelatedRole
   {
      None = 0,
      Assignee = 1,
      Watcher = 2
   }

   public enum Status
   {
      None = 0,
      Unassigned = 1,
      Open = 2,
      Closed = 3
   }

   public enum SupportTicketType
   {
      None = 0,
      Complaint = 1,
      Help = 2,
      SuspiciousActivity = 3
   }

   public static string formatAreaPosition (Area area, float posX, float posY) {
      return $"{area}: ({posX}; {posY})";
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
