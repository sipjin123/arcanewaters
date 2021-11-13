using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class NPCQuestManager : MonoBehaviour {
   #region Public Variables

   // List of quest data
   public List<QuestData> questDataList = new List<QuestData>();

   // List of quest timer data
   public List<QuestTimer> questTimerData = new List<QuestTimer>();

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
         questTimerData = DB_Main.getQuestTimers();

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

            initializeQuestRepeatRate();
         });
      });
   }

   private void initializeQuestRepeatRate () {
      // Generate the dictionary for quest timers
      foreach (QuestTimer timerData in questTimerData) {
         if (!_questResetCollection.ContainsKey(timerData.timerId)) {
            _questResetCollection.Add(timerData.timerId, new List<QuestResetClass>());
            timerData.timeDeployed = NetworkTime.time;
            InvokeRepeating(nameof(checkRepeatQuestStats), 1, timerData.repeatRateInMins * 60);
         }
      }

      // Update quest status for every quest duration
      foreach (QuestTimer questTimeData in questTimerData) {
         foreach (QuestData questData in questDataList) {
            // Find all quest nodes that references the quest timer id
            List<QuestDataNode> filteredQuestData = questData.questDataNodes.ToList().FindAll(_ => _.questTimerIdReference == questTimeData.timerId && _.isRepeatable);

            // Add the quest nodes to the new quest reset collection dictionary
            foreach (QuestDataNode newQuestData in filteredQuestData) {
               _questResetCollection[questTimeData.timerId].Add(new QuestResetClass {
                  questId = questData.questId,
                  questNodeId = newQuestData.questDataNodeId
               });
            }
         }
      }
   }

   private void checkRepeatQuestStats () {
      double currentTime = NetworkTime.time;
      foreach (QuestTimer questTimeData in questTimerData) {
         double timeLapsed = currentTime - questTimeData.timeDeployed;

         // Check if the quest timer has triggered its repeat rate target
         if (timeLapsed > questTimeData.repeatRateInMins * 60) {
            // Update database to reset all quests with quest id and quest node id
            if (_questResetCollection.ContainsKey(questTimeData.timerId)) {
               foreach (QuestResetClass resetClass in _questResetCollection[questTimeData.timerId]) {
                  UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
                     DB_Main.resetQuestsWithNodeId(resetClass.questId, resetClass.questNodeId);
                  });
               }
            }
            questTimeData.timeDeployed = currentTime;
         }
      }
   }

   public QuestData getQuestData (int questId) {
      QuestData newQuestData = questDataList.Find(_ => _.questId == questId);
      return newQuestData;
   }

   #region Private Variables

   // Class cached for reseting quests
   private class QuestResetClass {
      // The quest id
      public int questId;

      // The quest node id
      public int questNodeId;
   }

   // The cached quest reset collection
   private Dictionary<int, List<QuestResetClass>> _questResetCollection = new Dictionary<int, List<QuestResetClass>>();

   #endregion
}
