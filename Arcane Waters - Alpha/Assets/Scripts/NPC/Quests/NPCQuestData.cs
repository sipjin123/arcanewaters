using System;
using System.Collections.Generic;
using UnityEngine;

public class NPCQuestData : ScriptableObject
{
   #region Public Variables
   public QuestType questType;

   public List<DeliveryQuestPair> deliveryQuestList;
   public List<HuntQuestPair> huntQuestList;

   public string initQuestMessage;
   public string questCompletedMessage;
   public ClickableText.Type initQuestAnswer;
   #endregion
}

[Serializable]
public class DeliveryQuestPair : QuestInfo
{
   public DeliverQuest DeliveryQuest;
}
[Serializable]
public class HuntQuestPair : QuestInfo
{
   public HuntQuest HuntyQuest;
}

public class QuestInfo
{
   public QuestState questState;
   public ClickableText.Type introAnswer;
   public ClickableText.Type successAnswer;
   public ClickableText.Type failAnswer;
   public string unlockableDialogue;
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