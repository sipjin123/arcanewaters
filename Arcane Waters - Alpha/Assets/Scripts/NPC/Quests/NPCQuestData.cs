using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class NPCQuestData : ScriptableObject
{

    public QuestType questType;
    public ClickableText.Type UnlockableDialogue;

    public List<DeliverQuest> deliveryQuests;
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