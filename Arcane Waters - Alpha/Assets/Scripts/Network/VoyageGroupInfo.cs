using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class VoyageGroupInfo
{
   #region Public Variables

   // The group ID
   public int groupId;

   // The voyage ID
   public int voyageId;

   // The date at which the group was created
   public long creationDate;

   // Gets set to true when the group is waiting for more quickmatch members
   public bool isQuickmatchEnabled;

   // Gets set to true when the group is private
   public bool isPrivate;

   // The number of group members
   public int memberCount;

   #endregion

   public VoyageGroupInfo () { }

#if IS_SERVER_BUILD

   public VoyageGroupInfo (MySqlDataReader dataReader) {
      this.groupId = DataUtil.getInt(dataReader, "groupId");
      this.voyageId = DataUtil.getInt(dataReader, "voyageId");
      this.creationDate = DataUtil.getDateTime(dataReader, "creationDate").ToBinary();
      this.isQuickmatchEnabled = DataUtil.getBoolean(dataReader, "isQuickMatchEnabled");
      this.isPrivate = DataUtil.getBoolean(dataReader, "isPrivate");
      this.memberCount = DataUtil.getInt(dataReader, "memberCount");
   }

#endif

   public VoyageGroupInfo (int groupId, int voyageId, DateTime creationDate, bool isQuickmatchEnabled,
      bool isPrivate, int memberCount) {
      this.groupId = groupId;
      this.voyageId = voyageId;
      this.creationDate = creationDate.ToBinary();
      this.isQuickmatchEnabled = isQuickmatchEnabled;
      this.isPrivate = isPrivate;
      this.memberCount = memberCount;
   }

   public override bool Equals (object rhs) {
      if (rhs is VoyageGroupInfo) {
         var other = rhs as VoyageGroupInfo;
         return groupId == other.groupId;
      }
      return false;
   }

   public override int GetHashCode () {
      return groupId.GetHashCode();
   }

   #region Private Variables

   #endregion
}
