using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public struct TutorialInfo {
   #region Public Variables

   // The user 
   public int userId;

   // The step number
   public int stepNumber;

   // The time the user started this step
   public long finishTime;

   #endregion

#if IS_SERVER_BUILD

   public TutorialInfo (MySqlDataReader dataReader) {
      this.userId = DataUtil.getInt(dataReader, "usrId");
      this.stepNumber = DataUtil.getInt(dataReader, "stepNumber");
      this.finishTime = DataUtil.getDateTime(dataReader, "finishTime").ToBinary();
   }

#endif

   public TutorialInfo (int userId, int stepNumber, long startTime, long finishTime) {
      this.userId = userId;
      this.stepNumber = stepNumber;
      this.finishTime = finishTime;
   }

   public override bool Equals (object obj) {
      if (!(obj is TutorialInfo))
         return false;

      TutorialInfo other = (TutorialInfo) obj;
      return userId == other.userId && stepNumber == other.stepNumber && finishTime == other.finishTime;
   }

   public override int GetHashCode () {
      unchecked // Overflow is fine, just wrap
      {
         int hash = 17;
         hash = hash * 23 + userId.GetHashCode();
         hash = hash * 23 + stepNumber.GetHashCode();
         hash = hash * 23 + finishTime.GetHashCode();
         return hash;
      }
   }

   public override string ToString () {
      return "TutorialInfo: user: " + userId + ", stepNumber: " + stepNumber + ", finishTime: " + finishTime;
   }

   #region Private Variables

   #endregion
}
