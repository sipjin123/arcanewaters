#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class PenaltiesQueueItem {
   #region Public Variables

   // Queue item's id from the database
   public int id;

   // Penalty's target account Id
   public int targetAccId;

   // Penalty's source account Id
   public int sourceAccId;

   // Penalty type
   public PenaltyInfo.ActionType penaltyType;

   // Penalty reason
   public string penaltyReason;

   // Penalty time, in seconds
   public int penaltyTime;

   #endregion

   #if IS_SERVER_BUILD

   public PenaltiesQueueItem (MySqlDataReader dataReader) {
      this.id = DataUtil.getInt(dataReader, "id");
      this.targetAccId = DataUtil.getInt(dataReader, "targetAccId");
      this.sourceAccId = DataUtil.getInt(dataReader, "sourceAccId");
      this.penaltyType = (PenaltyInfo.ActionType) DataUtil.getInt(dataReader, "penaltyType");
      this.penaltyReason = DataUtil.getString(dataReader, "penaltyReason");
      this.penaltyTime = DataUtil.getInt(dataReader, "penaltyTime");
   }

   #endif

   #region Private Variables

   #endregion
}
