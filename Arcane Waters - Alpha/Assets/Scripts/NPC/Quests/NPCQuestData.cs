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

   // Retrieves all the quests
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

   // Retrieves all the quest specified
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
   public CraftingIngredients.Type rewardType;
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
   // Current progress state of the quest
   public QuestState questState;

   // Dialogue for the npc 
   public string npcDialogue;

   // Checks if player can have positive or negative reply
   public bool checkCondition;

   // Reply if conditions are met for quest
   public ClickableText.Type playerReply;

   // Reply if conditions are not met for quest
   public ClickableText.Type playerNegativeReply;

   // Next state for quest progression
   public QuestState nextState;
}

public class QuestInfoData
{
   // List of quest per quest type
   public List<QuestInfo> questList;

   // Represents the rank of the quest in the list
   public int index;

   // Represents the type of quest
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
}

public enum QuestType
{
   None = 0,
   Hunt = 1,
   Slay = 2,
   Deliver = 3,
}