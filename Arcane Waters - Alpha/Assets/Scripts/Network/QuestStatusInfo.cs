using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class QuestStatusInfo
{
   #region Public Variables

   // The NPC ID
   public int npcId;

   // The user ID
   public int userId;

   // The quest ID
   public int questId;

   // The quest node ID - the current progress in the quest
   public int questNodeId;

   #endregion

   public QuestStatusInfo () { }

#if IS_SERVER_BUILD

   public QuestStatusInfo (MySqlDataReader dataReader) {
      this.npcId = DataUtil.getInt(dataReader, "npcId");
      this.userId = DataUtil.getInt(dataReader, "usrId");
      this.questId = DataUtil.getInt(dataReader, "questId");
      this.questNodeId = DataUtil.getInt(dataReader, "questNodeId");
   }

#endif

   public QuestStatusInfo (int npcId, int userId, int questId, int questNodeId) {
      this.npcId = npcId;
      this.userId = userId;
      this.questId = questId;
      this.questNodeId = questNodeId;
   }

   public override bool Equals (object rhs) {
      if (rhs is QuestStatusInfo) {
         var other = rhs as QuestStatusInfo;
         return (npcId == other.npcId && userId == other.userId && questId == other.questId);
      }
      return false;
   }

   public override int GetHashCode () {
      return 17 + 23 * npcId.GetHashCode()
         + 53 * userId.GetHashCode()
         + 97 * questId.GetHashCode();
   }

   #region Private Variables

   #endregion
}
