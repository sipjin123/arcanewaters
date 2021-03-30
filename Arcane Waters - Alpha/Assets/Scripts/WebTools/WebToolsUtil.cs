﻿public static class WebToolsUtil
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
   public const string BUG_REPORT_SUBMIT = "https://tools.arcanewaters.com/api/tasks/submit";
   public const string IS_SERVER_ONLINE = "https://tools.arcanewaters.com/api/game/{db}/isServerOnline";
   public const string GET_SERVER_HISTORY = "https://tools.arcanewaters.com/api/game/{db}/getServerHistory";
}

public enum TicketSourceType
{
   Game = 1,
   Server = 2,
   Email = 3
}
