using System;

[Serializable]
public class QuestData {
   // Name of the quest group
   public string questGroupName;

   // Id of the quest, a primary key to be referenced
   public int questId;

   // An icon representing the quest
   public string questGroupIcon;

   // Array of quest data
   public QuestDataNode[] questDataNodes;

   // If the quest is active
   public bool isActive;
}

[Serializable]
public class QuestDataNode {
   // Title of the quest
   public string questNodeTitle;

   // Required friendship level to unlock the quest
   public int friendshipLevelRequirement;

   // The primary Id assigned automatically in order
   public int questDataNodeId;

   // The quest dialogue node array
   public QuestDialogueNode[] questDialogueNodes;
}

[Serializable]
public class QuestDialogueNode {
   public string npcDialogue;
   public string playerDialogue;

   // Rewarded friendship pts after completing the dialogue
   public int friendshipRewardPts;

   // Item requirements
   public Item[] itemRequirements;

   // Item rewards
   public Item[] itemRewards;

   // Rewarded ability
   public int abilityIdReward;
}

[Serializable]
public class QuestXMLContent {
   // Id of the xml entry
   public int xmlId;

   // Data of the quest content
   public QuestData xmlData;

   // Determines if the entry is enabled in the database
   public bool isEnabled;
}
