public static class WebToolsUtil
{
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
}

public enum TicketSourceType
{
   Game = 1,
   Server = 2,
   Email = 3
}
