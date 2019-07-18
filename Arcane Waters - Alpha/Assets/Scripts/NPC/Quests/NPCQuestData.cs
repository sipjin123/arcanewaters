using System;
using System.Collections.Generic;
using UnityEngine;

public class NPCQuestData : ScriptableObject
{
   #region Public Variables

   // Indicates the type of quest
   public QuestType questType;

   // List of all delivery quests
   public List<DeliveryQuestPair> deliveryQuestList;
   // List of all hunting quests
   public List<HuntQuestPair> huntQuestList;

   // NPC Initial message before any quests is taken
   public string initQuestMessage;
   // NPC Message if there are no available quests
   public string questCompletedMessage;

   // Player reply to accepting quest
   public ClickableText.Type initQuestAnswer;

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
   // State of quest if its complete or pending
   public QuestState questState;

   // Player response accepting the quest
   public ClickableText.Type introAnswer;

   // Player response if quest requirements are met
   public ClickableText.Type successAnswer;

   // Player response if quest requirements are not met
   public ClickableText.Type failAnswer;

   // NPC response after quest completion
   public string unlockableDialogue;

   // NPC Quest details
   public string introDialogue;
}

public enum QuestState
{
   None,
   Initialized,
   Pending,
   Completed
}

public enum QuestType
{
   Hunt,
   Slay,
   Deliver,
}