#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif
using System;


namespace Steam
{
   public class SteamOrder
   {
      #region Public Variables

      // The id of the order
      public ulong orderId;

      // The id of the user who owns the order
      public int userId;

      // The creation date of the order
      public DateTime creationDate;

      // The time of the last update to the order
      public DateTime timestamp;

      // The status of the order
      public string status;

      // Is the order closed?
      public bool closed;

      // The serialized content of the order
      public string content;

      #endregion

      public SteamOrder () {

      }

      public SteamOrder (ulong orderId, int userId, DateTime creationDate, DateTime updateTime, string status, bool closed, string content) {
         this.orderId = orderId;
         this.userId = userId;
         this.creationDate = creationDate;
         this.timestamp = updateTime;
         this.status = status;
         this.closed = closed;
         this.content = content;
      }

      #if IS_SERVER_BUILD

      public static SteamOrder create (MySqlDataReader reader) {
         SteamOrder order = new SteamOrder();
         order.orderId = DataUtil.getUInt64(reader, "soOrderId");
         order.userId = DataUtil.getInt(reader, "soUserId");
         order.creationDate = DataUtil.getDateTime(reader, "soCreationDate");
         order.timestamp = DataUtil.getDateTime(reader, "soTimestamp");
         order.status = DataUtil.getString(reader, "soStatus");
         order.closed = DataUtil.getBoolean(reader, "soClosed");
         order.content = DataUtil.getString(reader, "soOrderContent");
         return order;
      }

      #endif
   }
}