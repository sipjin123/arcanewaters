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

   // The type of ban applied to this account
   public Type banType = Type.None;

   // When this player was banned
   public DateTime banStartDate;

   // When this player's ban is over
   public DateTime banEndDate;

   // The reason for the ban
   public string reason;

   #endregion

#if IS_SERVER_BUILD

   public BanInfo (MySqlDataReader dataReader) {
      try {
         this.banType = (Type)DataUtil.getInt(dataReader, "banType");
         this.banStartDate = DataUtil.getDateTime(dataReader, "banDate");
         this.banEndDate = DataUtil.getDateTime(dataReader, "banEndDate");
         this.reason = DataUtil.getString(dataReader, "banReason");
      } catch (Exception e) {
         D.debug("Error in parsing MySqlData for BanInfo " + e.ToString());
      }
   }

#endif

   public BanInfo () { }

   public BanInfo (Type banType, DateTime banEndDate, string reason) {
      this.banType = banType;
      this.banEndDate = banEndDate;
      this.reason = reason;
   }

   public bool isBanned () {
      return banType != Type.None;
   }

   public bool hasBanExpired () {
      if (banType == Type.Indefinite) {
         return false;
      }

      int isEarlier = DateTime.Compare(DateTime.Now, banEndDate);
      return isEarlier > 0;
   }

   #region Private Variables

   #endregion
}
