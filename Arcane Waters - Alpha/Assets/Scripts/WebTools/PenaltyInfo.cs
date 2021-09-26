using System;
#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class PenaltyInfo
{
   #region Public Variables

   // The penalties queue item SQL id
   public int id;

   // The source account's id
   public int sourceAccId;

   // The source user's id
   public int sourceUsrId;

   // The source user's name
   public string sourceUsrName;

   // The target account's id
   public int targetAccId;

   // The target user's id
   public int targetUsrId;

   // The target user's name
   public string targetUsrName;

   // This item penalty type
   public PenaltyActionType penaltyType;

   // The reason for this penalty
   public string penaltyReason;

   // The time for the penalty
   public int penaltyTime;

   // The source of this penalty
   public PenaltySource penaltySource;

   // If this is a Force Single Player action, then we use this value
   public bool forceSinglePlayer;

   // The expiration date for the penalty (mute & ban)
   public long expiresAt = DateTime.MinValue.Ticks;

   #endregion

   public PenaltyInfo () { }

   #if IS_SERVER_BUILD

   public PenaltyInfo (MySqlDataReader dataReader) {
      this.id = DataUtil.getInt(dataReader, "id");
      this.sourceAccId = DataUtil.getInt(dataReader, "sourceAccId");
      this.sourceUsrId = DataUtil.getInt(dataReader, "sourceUsrId");
      this.sourceUsrName = DataUtil.getString(dataReader, "sourceUsrName");
      this.targetAccId = DataUtil.getInt(dataReader, "targetAccId");
      this.targetUsrId = DataUtil.getInt(dataReader, "targetUsrId");
      this.targetUsrName = DataUtil.getString(dataReader, "targetUsrName");
      this.penaltyType = (PenaltyActionType) DataUtil.getInt(dataReader, "penaltyType");
      this.penaltyReason = DataUtil.getString(dataReader, "penaltyReason");
      this.penaltySource = (PenaltySource) DataUtil.getInt(dataReader, "penaltySource");
      this.penaltyTime = DataUtil.getInt(dataReader, "penaltyTime");
      this.forceSinglePlayer = DataUtil.getInt(dataReader, "forceSinglePlayer") == 1;
      this.expiresAt = DataUtil.getDateTime(dataReader, "expiresAt").Ticks;
   }

   #endif

   #region Private Variables

   #endregion
}

public enum PenaltyActionType
{
   None = 0,
   Mute = 1,
   StealthMute = 2,
   Ban = 3,
   PermanentBan = 4,
   Kick = 5,
   ForceSinglePlayer = 6,
   LiftMute = 7,
   LiftBan = 8
}

public enum PenaltySource
{
   None = 0,
   Game = 1,
   WebTools = 2
}
