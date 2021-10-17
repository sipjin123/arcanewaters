#if IS_SERVER_BUILD
using System;
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

   // The event date in eastern standard time
   public long eventDateEST;

   // The event type
   public EventType eventType;

   // The server version
   public int serverVersion;

   // The server port
   public int serverPort;

   #endregion

   public ServerHistoryInfo () { }

#if IS_SERVER_BUILD

   public ServerHistoryInfo (MySqlDataReader dataReader) {
      this.eventDate = DataUtil.getDateTime(dataReader, "eventDate").ToBinary();
      this.eventDateEST = Util.getTimeInEST(DateTime.FromBinary(eventDate)).ToBinary();
      this.eventType = (EventType) DataUtil.getInt(dataReader, "eventType");
      this.serverVersion = DataUtil.getInt(dataReader, "serverVersion");
      this.serverPort = DataUtil.getInt(dataReader, "serverPort");
   }

#endif

   #region Private Variables

   #endregion
}
