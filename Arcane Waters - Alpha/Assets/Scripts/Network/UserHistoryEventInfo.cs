using System;
#if IS_SERVER_BUILD
#endif

public class UserHistoryEventInfo
{
   #region Public Variables

   // Event type
   public enum EventType
   {
      // None
      None = 0,

      // Created
      Create = 1,

      // Temporarily deleted
      Delete = 2,

      // Restored
      Restore = 3,

      // Deleted permanently
      DeletePermanent = 4
   }

   // Event Type
   public EventType eventType;

   // User Name
   public string userName;

   // User Id
   public int userId;

   // Account Id
   public int accountId;

   // Timestamp
   public DateTime timestamp;

   #endregion

   public UserHistoryEventInfo (int accountId, int userId, string userName, EventType eventType, DateTime timestamp) {
      this.accountId = accountId;
      this.userId = userId;
      this.userName = userName;
      this.eventType = eventType;
      this.timestamp = timestamp;
   }

   #region Private Variables

   #endregion
}
