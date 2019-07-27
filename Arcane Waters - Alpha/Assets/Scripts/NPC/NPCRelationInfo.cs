using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class NPCRelationInfo {
   #region Public Variables

   // User ID
   public int userID;

   // NPD ID
   public int npcID;

   // NPC Name
   public string npcName;

   // NPC Type of Quest
   public QuestType npcQuestType;

   // Friendship level 0 - 100
   public int npcRelationLevel;

   // Quest state iteration
   public int npcQuestIndex;

   // Quest state such as Init Pending and Complete
   public int npcQuestProgress;

   #endregion

#if IS_SERVER_BUILD

   public NPCRelationInfo (MySqlDataReader dataReader) {
      this.userID = DataUtil.getInt(dataReader, "user_id");
      this.npcID = DataUtil.getInt(dataReader, "npc_id");
      this.npcName = DataUtil.getString(dataReader, "npc_name");
      this.npcRelationLevel = DataUtil.getInt(dataReader, "npc_relation_level");
      this.npcQuestIndex = DataUtil.getInt(dataReader, "npc_quest_index");
      this.npcQuestProgress = DataUtil.getInt(dataReader, "npc_quest_progress");
      this.npcQuestType = (QuestType) Enum.Parse(typeof(QuestType), DataUtil.getString(dataReader, "npc_quest_type"), true);
   }

#endif

   public NPCRelationInfo (int userID, int npcID, string npcName, string npcQuestType,int npcRelationLevel, int npcQuestIndex, int npcQuestProgress) {
      this.userID = userID;
      this.npcID = npcID;
      this.npcName = npcName;
      this.npcRelationLevel = npcRelationLevel;
      this.npcQuestIndex = npcQuestIndex;
      this.npcQuestProgress = npcQuestProgress;
      this.npcQuestType = (QuestType) Enum.Parse(typeof(QuestType), npcQuestType, true);
   }
}
