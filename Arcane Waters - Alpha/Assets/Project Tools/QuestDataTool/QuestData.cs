using System;

[Serializable]
public class QuestData {
   // Name of the quest group
   public string questGroupName;

   // The details of the quest group
   public string questGroupDetails;

   // Id of the quest, a primary key to be referenced
   public int questId;

   // An icon representing the quest
   public string questGroupIcon;

   // Array of quest data
   public QuestDataNode[] questDataNodes;

   // If the quest is active
   public bool isActive;

   // The icon path
   public string iconPath;
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
   // The dialogue info
   public string npcDialogue;
   public string playerDialogue;

   // The index of the dialogue
   public int dialogueIdIndex;

   // Rewarded friendship pts after completing the dialogue
   public int friendshipRewardPts;

   // Item requirements
   public Item[] itemRequirements;

   // Item rewards
   public Item[] itemRewards;

   // Rewarded ability
   public int abilityIdReward = -1;

   // The required job type to proceed with dialogue
   public int jobTypeRequirement = 0;

   // The level of the job required to proceed with the dialogue
   public int jobLevelRequirement = 0;
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
