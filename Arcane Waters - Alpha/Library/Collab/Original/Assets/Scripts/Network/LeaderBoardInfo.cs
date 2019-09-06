using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class LeaderBoardInfo
{
   #region Public Variables

   // The rank of the user
   public int rank;

   // The job type
   public Jobs.Type jobType;

   // The period of time of the leader board (day, week, month)
   public LeaderBoardsManager.Period period;

   // The user ID
   public int userId;

   // The user name
   public string userName;

   // The score
   public int score;

   #endregion

   public LeaderBoardInfo () { }

#if IS_SERVER_BUILD

   public LeaderBoardInfo (MySqlDataReader dataReader) {
      this.rank = DataUtil.getInt(dataReader, "rank");
      this.jobType = (Jobs.Type) DataUtil.getInt(dataReader, "jobType");
      this.period = (LeaderBoardsManager.Period) DataUtil.getInt(dataReader, "period");
      this.userId = DataUtil.getInt(dataReader, "usrId");
      this.userName = DataUtil.getString(dataReader, "usrName");
      this.score = DataUtil.getInt(dataReader, "score");
   }

#endif

   public LeaderBoardInfo (int rank, Jobs.Type jobType, LeaderBoardsManager.Period period, int userId, int score) {
      this.rank= rank;
      this.jobType = jobType;
      this.period = period;
      this.userId = userId;
      this.userName = "";
      this.score = score;
   }

   public override bool Equals (object rhs) {
      if (rhs is LeaderBoardInfo) {
         var other = rhs as LeaderBoardInfo;
         return (rank == other.rank && jobType == other.jobType && period == other.period);
      }
      return false;
   }

   public override int GetHashCode () {
      return 17 + 31 * rank.GetHashCode()
         + 883 * jobType.GetHashCode()
         + 9719 * period.GetHashCode();
   }

   #region Private Variables

   #endregion
}
