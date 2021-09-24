using System;
#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class PenaltyQueueItem {
   #region Public Variables

   // The penalties queue item SQL id
   public int id;

   // The target account's id
   public int targetAccId;

   // This item penalty type
   public PenaltyType penaltyType;

   #endregion

   public PenaltyQueueItem () { }

   #if IS_SERVER_BUILD

   public PenaltyQueueItem (MySqlDataReader dataReader) {
      this.id = dataReader.GetInt32("id");
      this.targetAccId = dataReader.GetInt32("targetAccId");
      this.penaltyType = (PenaltyType) dataReader.GetInt32("penaltyType");
   }

   #endif

   #region Private Variables

   #endregion
}

