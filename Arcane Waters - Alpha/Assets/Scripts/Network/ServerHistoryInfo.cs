﻿#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class ServerHistoryInfo
{
   #region Public Variables

   public enum EventType
   {
      None = 0,
      ServerStart = 1,
      ServerStop = 2,
      RestartRequested = 3,
      RestartCanceled = 4
   }

   // The date of the event
   public long eventDate;

   // The event type
   public EventType eventType;

   // The server version
   public int serverVersion;

   #endregion

   public ServerHistoryInfo () { }

#if IS_SERVER_BUILD

   public ServerHistoryInfo (MySqlDataReader dataReader) {
      this.eventDate = DataUtil.getDateTime(dataReader, "eventDate").ToBinary();
      this.eventType = (EventType) DataUtil.getInt(dataReader, "eventType");
      this.serverVersion = DataUtil.getInt(dataReader, "serverVersion");
   }

#endif

   #region Private Variables

   #endregion
}
