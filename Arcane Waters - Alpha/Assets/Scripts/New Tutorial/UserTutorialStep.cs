using System;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class UserTutorialStep {

   #region Public Variables

   // The step id
   public int stepId;

   // The completion time of this step for a given user
   public DateTime? completedTimestamp;

   #endregion

   public UserTutorialStep () { }

#if IS_SERVER_BUILD
   public UserTutorialStep (MySqlDataReader reader) {
      stepId = reader.GetInt32("stepId");
      completedTimestamp = reader.IsDBNull(reader.GetOrdinal("completedTimestamp")) ? (DateTime?) null : reader.GetDateTime("completedTimestamp");
   }
#endif

   #region Private Variables

   #endregion
}
