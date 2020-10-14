using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class NPCQuestManager : MonoBehaviour {
   #region Public Variables

   // List of quest data
   public List<QuestData> questDataList = new List<QuestData>();

   // Self
   public static NPCQuestManager self;

   // Id of the blank quest for referencing
   public const int BLANK_QUEST_ID = 75;

   #endregion

   private void Awake () {
      self = this;
   }

   public void receiveListFromZipData (Dictionary<int, QuestData> zipData) {
      questDataList = new List<QuestData>();
      foreach (KeyValuePair<int, QuestData> questData in zipData) {
         QuestData newQuestData = questDataList.Find(_ => _.questId == questData.Key);
         if (newQuestData == null) {
            questDataList.Add(questData.Value);
         }
      }
   }

   public void initializeServerDataCache () {
      questDataList = new List<QuestData>();
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> xmlPairList = DB_Main.getNPCQuestXML();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair xmlPair in xmlPairList) {
               TextAsset newTextAsset = new TextAsset(xmlPair.rawXmlData);
               QuestData newQuestData = Util.xmlLoad<QuestData>(newTextAsset);
               int uniqueKey = xmlPair.xmlId;
               newQuestData.questId = xmlPair.xmlId;

               if (!questDataList.Exists(_=>_.questId == newQuestData.questId)) {
                  questDataList.Add(newQuestData);
               } else {
                  D.editorLog("Duplicate data!: " + uniqueKey, Color.red);
               }
            }
         });
      });
   }

   public QuestData getQuestData (int questId) {
      QuestData newQuestData = questDataList.Find(_ => _.questId == questId);
      return newQuestData;
   }

   #region Private Variables

   #endregion
}
