using System;
using System.Collections.Generic;
using UnityEngine;

public class NPCQuestData : ScriptableObject
{
   #region Public Variables

   // List of all delivery quests
   public List<DeliveryQuestPair> deliveryQuestList;
   // List of all hunting quests
   public List<HuntQuestPair> huntQuestList;

   public List<QuestInfo> getAllQuests()
   {
      List<QuestInfo> allQuests = new List<QuestInfo>();
      for(int i = 0; i < deliveryQuestList.Count; i++) {
         allQuests.Add(deliveryQuestList[i]);
      }
      for (int i = 0; i < huntQuestList.Count; i++) {
         allQuests.Add(huntQuestList[i]);
      }

      return allQuests;
   }

   public List<QuestInfo> getAllQuestSpecific(QuestType questtype) {
      List<QuestInfo> allQuests = new List<QuestInfo>();

      switch(questtype) {
         case QuestType.Deliver:
            for (int i = 0; i < deliveryQuestList.Count; i++) {
               allQuests.Add(deliveryQuestList[i]);
            }
            break;
         case QuestType.Hunt:
            for (int i = 0; i < huntQuestList.Count; i++) {
               allQuests.Add(huntQuestList[i]);
            }
            break;
      }
      return allQuests;
   }

   #endregion
}

[Serializable]
public class DeliveryQuestPair : QuestInfo
{
   public DeliverQuest deliveryQuest;
}

[Serializable]
public class HuntQuestPair : QuestInfo
{
   public HuntQuest huntyQuest;
}

public class QuestInfo
{
   // Title of the quest
   public string questTitle;

   // Indicates the type of quest
   public QuestType questType;

   // State of quest if its complete or pending
   public QuestState questState;

   // The level of relationship that unlocks this dialogue
   public int relationRequirement;

   // NPC and Player Dialogue Sequence
   public QuestDialogueData dialogueData;
}

[Serializable]
public class QuestDialogue
{
   public QuestState questState;
   public string npcDialogue;
   public bool checkCondition;
   public ClickableText.Type playerReply;
   public ClickableText.Type playerNegativeReply;
   public QuestState nextState;
}

public class QuestRow
{
   public string questType;
   public int questIndex;
   public int questProgress;
}

public class QuestInfoData
{
   public List<QuestInfo> questList;
   public int index;
   public QuestType questType;
}


public enum QuestState
{
   None = 0,
   Ready = 1,
   Initialized = 2,
   Pending = 3,
   Claim = 4,
   Completed = 5,
   Done = 6,
   NoQuest = 7
}

public enum QuestType
{
   None = 0,
   Hunt = 1,
   Slay = 2,
   Deliver = 3,
}