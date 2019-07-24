using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using MySql.Data.MySqlClient;

public class NPCRelationInfo : MonoBehaviour {
   #region Public Variables

   // NPD ID
   public int npcID;

   // NPC Name
   public string npcName;

   // Friendship level 0 - 100
   public int npcRelationLevel;

   // Quest state iteration
   public int npcQuestChapter;

   // Quest state such as Init Pending and Complete
   public int npcQuestProgress;

   #endregion

#if IS_SERVER_BUILD

   public NPCRelationInfo (MySqlDataReader dataReader) {
      this.npcID = DataUtil.getInt(dataReader, "npc_id");
      this.npcName = DataUtil.getString(dataReader, "npc_name");
      this.npcRelationLevel = DataUtil.getInt(dataReader, "npc_relation_level");
      this.npcQuestChapter = DataUtil.getInt(dataReader, "npc_quest_chapter");
      this.npcQuestProgress = DataUtil.getInt(dataReader, "npc_quest_progress");
   }

#endif

   public NPCRelationInfo (int npcID, string npcName,int npcRelationLevel, int npcQuestChapter, int npcQuestProgress) {
      this.npcID = npcID;
      this.npcName = npcName;
      this.npcRelationLevel = npcRelationLevel;
      this.npcQuestChapter = npcQuestChapter;
      this.npcQuestProgress = npcQuestProgress;
      D.error("This character does not exist");
   }
}
