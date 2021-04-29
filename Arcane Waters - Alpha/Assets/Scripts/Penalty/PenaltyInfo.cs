using System;
#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class PenaltyInfo
{
   #region Public Variables
   // The accId who is banning or muting
   public int sourceAccId;

   // The usrId who is banning or muting
   public int sourceUsrId;

   // The usrName who is banning or muting
   public string sourceUsrName;

   // The accId being banned or muted
   public int targetAccId;

   // The usrId being banned or muted
   public int targetUsrId;

   // The usrName being banned or muted
   public string targetUsrName;

   // The reason for the penalty
   public string penaltyReason;

   // The type of the penalty applied to the account
   public PenaltyType penaltyType = PenaltyType.None;

   // When the account was penalized
   public DateTime penaltyStart;

   // When the account's penalty is over
   public DateTime penaltyEnd;

   // Is this a permanent penalty?
   public bool isPermanent;

   #endregion

   public PenaltyInfo () { }

#if IS_SERVER_BUILD

   public PenaltyInfo (MySqlDataReader dataReader) {
      try {
         sourceAccId = DataUtil.getInt(dataReader, "sourceAccId");
         sourceUsrId = DataUtil.getInt(dataReader, "sourceUsrId");
         sourceUsrName = DataUtil.getString(dataReader, "sourceUsrName");
         targetAccId = DataUtil.getInt(dataReader, "targetAccId");
         targetUsrId = DataUtil.getInt(dataReader, "targetUsrId");
         targetUsrName = DataUtil.getString(dataReader, "targetUsrName");
         penaltyReason = DataUtil.getString(dataReader, "penaltyReason");
         penaltyType = (PenaltyType) DataUtil.getInt(dataReader, "penaltyType");
         penaltyStart = DataUtil.getDateTime(dataReader, "penaltyStart");
         penaltyEnd = DataUtil.getDateTime(dataReader, "penaltyEnd");

         isPermanent = penaltyEnd == DateTime.MinValue;
      } catch (Exception ex) {
         D.debug("Error in parsing MySqlData for PenaltyInfo " + ex.ToString());
      }
   }

#endif

   public PenaltyInfo (int sourceAccId, int sourceUsrId, string sourceUsrName, PenaltyType penaltyType, string penaltyReason, int penaltyTime, bool isPermanent) {
      this.sourceAccId = sourceAccId;
      this.sourceUsrId = sourceUsrId;
      this.sourceUsrName = sourceUsrName;
      this.penaltyType = penaltyType;
      this.penaltyReason = penaltyReason;
      this.isPermanent = isPermanent;

      penaltyEnd = DateTime.UtcNow.AddMinutes(penaltyTime);
   }

   public bool hasPenaltyExpired () {
      if (penaltyEnd == DateTime.MinValue) {
         return false;
      } else {
         return DateTime.Compare(DateTime.UtcNow, penaltyEnd) > 0;
      }
   }

   public string getAction () {
      if (penaltyType == PenaltyType.Ban) {
         return "ban";
      } else if (penaltyType == PenaltyType.Mute || penaltyType == PenaltyType.StealthMute) {
         return "mute";
      } else {
         return "";
      }
   }

   public string getPastAction () {
      if (penaltyType == PenaltyType.Ban) {
         return "banned";
      } else if (penaltyType == PenaltyType.Mute || penaltyType == PenaltyType.StealthMute) {
         return "muted";
      } else {
         return "";
      }
   }

   public PenaltyStatus getAlreadyAppliedError () {
      if (penaltyType == PenaltyType.Ban) {
         return PenaltyStatus.AlreadyBanned;
      } else if (penaltyType == PenaltyType.Mute || penaltyType == PenaltyType.StealthMute) {
         return PenaltyStatus.AlreadyMuted;
      } else {
         return PenaltyStatus.None;
      }
   }

   public bool isTemporary () {
      return penaltyEnd > DateTime.MinValue;
   }

   #region Private Variables

   #endregion
}

public enum PenaltyStatus
{
   None = 0,
   AlreadyMuted = 1,
   AlreadyBanned = 2,
   Error = 3
}

public enum PenaltyType
{
   None = 0,
   Mute = 1,
   StealthMute = 2,
   Ban = 3,
   Kick = 4
}
