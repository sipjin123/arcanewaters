using System;

public struct TrackedUserAction
{
   #region Public Variables

   // The type of action user can perform that we track
   public enum Type : uint
   {
      None = 0,
      OpenedFriendList = 1
   }

   // Unique identifier
   public int id;

   // User that performed the action
   public int userId;

   // Account that performed the action
   public int accId;

   // The type of action that was performed
   public Type type;

   // When the action was performed
   public DateTime time;

   #endregion

   #region Private Variables

   #endregion
}
