using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class BanInfo
{
   #region Public Variables

   // The different types of ban
   public enum Type
   {
      None = 0,
      Indefinite = 1,
      Temporary = 2
   }

   // The different types of ban status
   public enum BanStatus
   {
      None = 0,
      AlreadyBanned = 1,
      BanError = 2
   }

   // The accId who is banning
   public int sourceAccId;

   // The accId being banned
   public int targetAccId;

   // The type of ban applied to this account
   public Type banType = Type.None;

   // Ban time in minutes
   public int banTime;

   // When this player was banned
   public DateTime banStart;

   // When this player's ban is over
   public DateTime banEnd;

   // When this player's ban was lifted
   public DateTime banLift;

   // The reason for the ban
   public string reason;

   #endregion

#if IS_SERVER_BUILD

   public BanInfo (MySqlDataReader dataReader) {
      try {
         sourceAccId = DataUtil.getInt(dataReader, "sourceAccId");
         targetAccId = DataUtil.getInt(dataReader, "targetAccId");
         banType = (Type) DataUtil.getInt(dataReader, "banType");
         banTime = DataUtil.getInt(dataReader, "banTime");
         banStart = DataUtil.getDateTime(dataReader, "banStart");
         banEnd = DataUtil.getDateTime(dataReader, "banEnd");
         banLift = DataUtil.getDateTime(dataReader, "banLift");
         reason = DataUtil.getString(dataReader, "banReason");
      } catch (Exception e) {
         D.debug("Error in parsing MySqlData for BanInfo " + e.ToString());
      }
   }

#endif

   public BanInfo () { }

   public BanInfo (int sourceAccId, Type banType, DateTime banEndDate, int minutes, string reason) {
      this.sourceAccId = sourceAccId;
      this.banType = banType;
      this.banEnd = banEndDate;
      this.reason = reason;
      this.banTime = minutes;
   }

   public bool hasBanExpired () {
      bool expired = false;

      // If the ban is indefinite, we check for a liftDate
      if (banType == Type.Indefinite) {
         if (banLift != null) {
            expired = true;
         }
         return expired;
      } else {
         int isEarlier = DateTime.Compare(DateTime.UtcNow, banEnd);
         return isEarlier > 0;
      }
   }

   #region Private Variables

   #endregion
}
