using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class FriendshipInfo
{
   #region Public Variables

   // The user ID
   public int userId;

   // The user ID of the friend
   public int friendUserId;

   // The status of the friendship
   public Friendship.Status friendshipStatus;

   // The date of last interaction with the friend
   public long lastContactDate;

   // The name of the friend
   public string friendName;

   // The area key where the friend is located
   public string friendAreaKey;

   #endregion

   public FriendshipInfo () { }

#if IS_SERVER_BUILD

   public FriendshipInfo (MySqlDataReader dataReader) {
      this.userId = DataUtil.getInt(dataReader, "usrId");
      this.friendUserId = DataUtil.getInt(dataReader, "friendUsrId");
      this.friendshipStatus = (Friendship.Status) DataUtil.getInt(dataReader, "friendshipStatus");
      this.lastContactDate = DataUtil.getDateTime(dataReader, "lastContactDate").ToBinary();
      this.friendName = DataUtil.getString(dataReader, "usrName");
      this.friendAreaKey = DataUtil.getString(dataReader, "areaKey");
   }

#endif

   public FriendshipInfo (int userId, int friendUserId, Friendship.Status friendshipStatus, DateTime lastContactDate) {
      this.userId = userId;
      this.friendUserId = friendUserId;
      this.friendshipStatus = friendshipStatus;
      this.lastContactDate = lastContactDate.ToBinary();
   }

   public override bool Equals (object rhs) {
      if (rhs is FriendshipInfo) {
         var other = rhs as FriendshipInfo;
         return (userId == other.userId && friendUserId == other.friendUserId);
      }
      return false;
   }

   public override int GetHashCode () {
      return 17 + 31 * userId.GetHashCode()
         + 883 * friendUserId.GetHashCode();
   }

   #region Private Variables

   #endregion
}
