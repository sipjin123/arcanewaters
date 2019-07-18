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

   // NPC and Player Dialogue Sequence
   public List<QuestDialogue> questDialogueList;
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

public enum QuestState
{
   Ready,
   Initialized,
   Pending,
   Claim,
   Completed,
   None
}

public enum QuestType
{
   Hunt,
   Slay,
   Deliver,
}