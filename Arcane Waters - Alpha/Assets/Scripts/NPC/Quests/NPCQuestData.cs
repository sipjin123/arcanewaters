using System.Collections.Generic;
using UnityEngine;

public class NPCQuestData : ScriptableObject
{
   #region Public Variables
   public QuestType questType;
   public ClickableText.Type UnlockableDialogue;

   public List<DeliverQuest> deliveryQuests;
   #endregion
}

public interface IQuest
{
}

public enum QuestType
{
   Hunt,
   Slay,
   Deliver,
}