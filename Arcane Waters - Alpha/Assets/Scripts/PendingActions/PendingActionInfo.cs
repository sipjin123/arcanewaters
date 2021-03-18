using System;
#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class PendingActionInfo
{
   #region Public Variables

   // The pending action's id from the database
   public int id;

   // The pending action purpose
   public PendingActionType actionType;

   // The pending action content, serialized as JSON
   public string actionJson;

   #endregion

   public PendingActionInfo () {

   }

   #if IS_SERVER_BUILD

   public PendingActionInfo (MySqlDataReader dataReader) {
      try {
         id = DataUtil.getInt(dataReader, "id");
         actionType = (PendingActionType) DataUtil.getInt(dataReader, "actionType");
         actionJson = DataUtil.getString(dataReader, "actionJson");
      } catch (Exception ex) {
         D.debug("Error in parsing MySqlData for PendingActionInfo " + ex.ToString());
      }
   }

   #endif

   #region Private Variables

   #endregion
}

public enum PendingActionType
{
   None = 0,
   NotifyMute = 1,
   NotifyBan = 2,
   LiftMute = 3,
   LiftBan = 4
}

public enum PendingActionServerType
{
   Localhost = 0,
   Development = 1,
   Production = 2
}
