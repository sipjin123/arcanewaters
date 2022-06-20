using System;
#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class PenaltyInfo
{
   #region Public Variables

   // The penalty SQL id
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
   public ActionType penaltyType;

   // The reason for this penalty
   public string penaltyReason;

   // The time for the penalty
   public int penaltyTime;

   #endregion

   public PenaltyInfo () { }

   public PenaltyInfo (NetEntity source, UserAccountInfo target, ActionType penaltyType, string reason, int seconds, int penaltyId = 0) {
      this.id = penaltyId;

      this.sourceAccId = source.accountId;
      this.sourceUsrId = source.userId;
      this.sourceUsrName = source.entityName;

      this.targetAccId = target.accountId;
      this.targetUsrId = target.userId;
      this.targetUsrName = target.username;

      this.penaltyReason = reason;
      this.penaltyTime = seconds;

      this.penaltyType = penaltyType;
   }

   public PenaltyInfo (int targetAccId, ActionType penaltyType, string penaltyReason, int penaltyTime) {
      this.targetAccId = targetAccId;
      this.penaltyType = penaltyType;
      this.penaltyTime = penaltyTime;

      if (!string.IsNullOrEmpty(penaltyReason)) {
         this.penaltyReason = penaltyReason;
      }
   }

   #if IS_SERVER_BUILD

   public PenaltyInfo (MySqlDataReader dataReader) {
      this.id = DataUtil.getInt(dataReader, "id");
      this.sourceAccId = DataUtil.getInt(dataReader, "sourceAccId");
      this.sourceUsrId = DataUtil.getInt(dataReader, "sourceUsrId");
      this.sourceUsrName = DataUtil.getString(dataReader, "sourceUsrName");
      this.targetAccId = DataUtil.getInt(dataReader, "targetAccId");
      this.targetUsrId = DataUtil.getInt(dataReader, "targetUsrId");
      this.targetUsrName = DataUtil.getString(dataReader, "targetUsrName");
      this.penaltyType = (ActionType) DataUtil.getInt(dataReader, "penaltyType");
      this.penaltyReason = DataUtil.getString(dataReader, "penaltyReason");
      this.penaltyTime = DataUtil.getInt(dataReader, "penaltyTime");
   }

   #endif

   public bool IsMute () {
      return this.penaltyType == ActionType.Mute || this.penaltyType == ActionType.StealthMute;
   }

   public bool IsBan () {
      return this.penaltyType == ActionType.SoloBan || this.penaltyType == ActionType.SoloPermanentBan;
   }

   public bool IsLiftType () {
      return this.penaltyType == ActionType.LiftMute || this.penaltyType == ActionType.LiftBan;
   }

   #region Private Variables

   #endregion

   public enum ActionType
   {
      None = 0,
      Mute = 1,
      StealthMute = 2,
      SoloBan = 3,
      SoloPermanentBan = 4,
      Kick = 5,
      ForceSinglePlayer = 6,
      LiftMute = 7,
      LiftBan = 8,
      LiftForceSinglePlayer = 9,
      StealthSoloBan = 10,
      FullPermanentBan = 11
   }
}

