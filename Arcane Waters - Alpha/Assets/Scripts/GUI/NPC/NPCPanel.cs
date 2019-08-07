using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Mirror;

public class NPCPanel : Panel {
   #region Public Variables

   // A class utilized by the server side that caches the player and npc response
   public class DialogueData
   {
      // List of response for the player during npc dialogue
      public List<ClickableText.Type> answerList;

      // The npc quest dialogue 
      public string npcDialogue;
   }

   // Data cache for current hunting quest
   public HuntQuestPair currentHuntQuest;

   // Data cache for current delivery quest
   public DeliveryQuestPair currentDeliveryQuest;

   // Data cache for current quest type
   public QuestType currentQuestType;

   // Data cache for current quest index
   public int currentQuestIndex;

   // The NPC associated with this panel
   public NPC npc;

   // The text we want to type out
   public TextMeshProUGUI greetingText;

   // Our head animation
   public SimpleAnimation headAnim;

   // Our Faction Icon
   public Image factionIcon;

   // Our Specialty Icon
   public Image specialtyIcon;

   // The Text that shows the NPC name
   public Text nameText;

   // The Text that shows our friendship number
   public Text friendshipText;

   // The level of relationship between the player and npc
   public int friendshipLevel;

   // The container of our clickable rows
   public GameObject clickableRowContainer;

   // The prefab we use for creating NPC text rows
   public ClickableText clickableRowPrefab;

   // Self
   public static NPCPanel self;

   // Loader Indicators
   public List<GameObject> loadingIndicators;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;

      // Keep track of what our intro text is
      _greetingText = greetingText.text;
   }

   public void SetMessage (string text) {
      greetingText.text = text;
      _greetingText = text;
   }

   public override void show () {
      base.show();

      // Fill in the details for this NPC
      factionIcon.sprite = Faction.getIcon(npc.faction);
      specialtyIcon.sprite = Specialty.getIcon(npc.specialty);
      nameText.text = npc.npcName;

      // Update the head image based on the type of NPC this is
      string path = "Faces/" + npc.GetComponent<SpriteSwap>().newTexture.name;
      Texture2D newTexture = ImageManager.getTexture(path);
      headAnim.GetComponent<SimpleAnimation>().setNewTexture(newTexture);

      // Start typing out our intro text
      AutoTyper.SlowlyRevealText(greetingText, _greetingText);

      // Toggle loading indicators
      for (int i = 0; i < loadingIndicators.Count; i++) {
         loadingIndicators[i].SetActive(true);
      }
   }

   public void receiveIndividualNPCQuestData (QuestType questType, int npcQuestIndex, int npcQuestProgress) {
      switch (questType) {
         case QuestType.Deliver:
            npc.npcData.npcQuestList[0].deliveryQuestList[npcQuestIndex].questState = (QuestState) npcQuestProgress;
            break;
         case QuestType.Hunt:
            npc.npcData.npcQuestList[0].huntQuestList[npcQuestIndex].questState = (QuestState) npcQuestProgress;
            break;
      }
   }

   public void readyNPCPanel (int friendshipLevel) {
      // Displays initial friendship level
      this.friendshipLevel = friendshipLevel;
      friendshipText.text = friendshipLevel.ToString();

      // Toggle loading indicators
      for (int i = 0; i < loadingIndicators.Count; i++) {
         loadingIndicators[i].SetActive(false);
      }
   }

   public void receiveNPCRelationDataFromServer (int friendshipLevel) {
      // Updates friendship level
      this.friendshipLevel = friendshipLevel;
      friendshipText.text = friendshipText.ToString();
   }

   public void setClickableQuestOptions () {
      // Clear out any old stuff
      clickableRowContainer.DestroyChildren();

      // Sets the index of the dialogue
      _questOptionIndex = 1;

      // Sets npc quest selection greeting
      SetMessage("What can I do for ya?");

      // Sets quest data such as dialogues and index
      QuestInfoData deliveryQuests = processQuestInfo(QuestType.Deliver);
      QuestInfoData huntQuest = processQuestInfo(QuestType.Hunt);

      // Sets functionality to ClickableText
      setQuestClicker(deliveryQuests);
      setQuestClicker(huntQuest);
   }

   public void setClickableRows (List<ClickableText.Type> options) {
      // Clear out any old stuff
      clickableRowContainer.DestroyChildren();

      // Sets the index of the dialogue
      int index = 1;

      // Create a clickable text row for each option in the list
      foreach (ClickableText.Type option in options) {
         ClickableText row = Instantiate(clickableRowPrefab);
         row.transform.SetParent(clickableRowContainer.transform);

         // Set the type
         row.textType = option;
         row.initData(index);
         index++;

         // Set up the click function
         row.clickedEvent.AddListener(() => rowClickedOn(row, this.npc));
      }
   }

   public void questCategoryRowClickedOn (ClickableText row, NPC npc, QuestType questType, int questIndex) {
      currentQuestType = questType;
      currentQuestIndex = questIndex;
      switch (questType) {
         case QuestType.Hunt:
            currentHuntQuest = npc.npcData.npcQuestList[0].huntQuestList[questIndex];
            break;
         case QuestType.Deliver:
            currentDeliveryQuest = npc.npcData.npcQuestList[0].deliveryQuestList[questIndex];
            break;
      }
      checkQuest(currentDeliveryQuest);
   }

   public void rowClickedOn (ClickableText row, NPC npc) {
      QuestState currentQuestState = currentDeliveryQuest.questState;
      QuestDialogue currentDialogue = currentDeliveryQuest.dialogueData.questDialogueList.Find(_ => _.questState == currentQuestState);

      if (row.textType == ClickableText.Type.TradeDeliveryFail) {
         // Change reply of NPC
         string reply = "go on and get my stuff then";
         npc.npcReply = reply;
         SetMessage(reply);
      } else if (row.textType == ClickableText.Type.TradeDeliveryComplete) {
         // CloseNPCPanel and Call Reward Panel
         PanelManager.self.popPanel();
         Global.player.rpc.Cmd_FinishedQuest(npc.npcId, currentQuestIndex);

         // Update quest State
         currentDeliveryQuest.questState = currentDialogue.nextState;
         Global.player.rpc.Cmd_UpdateNPCQuestProgress(npc.npcId, (int) QuestState.Completed, currentQuestIndex, currentQuestType.ToString());

         // Setup dialogue of player
         npc.currentAnswerDialogue.Clear();
         npc.currentAnswerDialogue.Add(ClickableText.Type.None);

         int updatedRelationship = (int.Parse(friendshipText.text) + 20);
         Global.player.rpc.Cmd_UpdateNPCRelation(npc.npcId, updatedRelationship);
      } else {
         Global.player.rpc.Cmd_UpdateNPCQuestProgress(npc.npcId, (int) currentDialogue.nextState, currentQuestIndex, currentQuestType.ToString());
         currentDeliveryQuest.questState = currentDialogue.nextState;
         checkQuest(currentDeliveryQuest);
      }
   }

   public void checkQuest (DeliveryQuestPair deliveryQuestPair) {
      QuestState currentQuestState = deliveryQuestPair.questState;
      QuestDialogue currentDialogue = deliveryQuestPair.dialogueData.questDialogueList.Find(_ => _.questState == currentQuestState);

      // Sets npc response
      npc.npcReply = currentDialogue.npcDialogue;
      SetMessage(npc.npcReply);
      npc.currentAnswerDialogue.Clear();

      if (currentDialogue.checkCondition) {
         List<Item> itemList = InventoryCacheManager.self.itemList;
         DeliverQuest deliveryQuest = deliveryQuestPair.deliveryQuest;
         Item findingItemList = itemList.Find(_ => (CraftingIngredients.Type) _.itemTypeId == (CraftingIngredients.Type) deliveryQuest.itemToDeliver.itemTypeId
         && _.category == Item.Category.CraftingIngredients);

         if (findingItemList != null) {
            if (findingItemList.count >= deliveryQuest.quantity) {
               // Sets the player to a positive response if Requirements are met
               npc.currentAnswerDialogue.Add(currentDialogue.playerReply);
               Global.player.rpc.Cmd_GetClickableRows(npc.npcId, (int) currentQuestType, (int) currentQuestState, currentQuestIndex);
               return;
            }
         } else {
            // Sets the player to a negative response if Requirements are met
            npc.currentAnswerDialogue.Add(currentDialogue.playerNegativeReply);
            Global.player.rpc.Cmd_GetClickableRows(npc.npcId, (int) currentQuestType, (int) currentQuestState, currentQuestIndex);
            return;
         }
      }

      // Sets the player to a positive response if Requirements are met
      npc.currentAnswerDialogue.Add(currentDialogue.playerReply);

      // Send a request to the server to get the clickable text options
      Global.player.rpc.Cmd_GetClickableRows(this.npc.npcId, (int) currentQuestType, (int) currentQuestState, currentQuestIndex);
   }

   private void setQuestClicker (QuestInfoData data) {
      for (int i = 0; i < data.questList.Count; i++) {
         ClickableText row = Instantiate(clickableRowPrefab);
         row.transform.SetParent(clickableRowContainer.transform);

         // Handles visual update if quest is finished
         string clickableTextName = data.questList[i].questType.ToString() + " (" + data.questList[i].questTitle + ")";
         if (data.questList[i].questState == QuestState.Completed) {
            row.SetColor(Color.black);
            clickableTextName += "(Finished)";
         }

         // Handles visual update if quest is locked
         if (data.questList[i].relationRequirement > friendshipLevel) {
            row.SetColor(Color.gray);
            clickableTextName += "(Requires level [" + data.questList[i].relationRequirement + "])";
         }

         // Updates row data
         row.customText(clickableTextName, _questOptionIndex);
         int newIndex = data.index;
         row.clickedEvent.AddListener(() => questCategoryRowClickedOn(row, this.npc, data.questType, newIndex));

         // Iterates indexes
         data.index++;
         _questOptionIndex++;
      }
   }

   private QuestInfoData processQuestInfo (QuestType type) {
      QuestInfoData newInfoData = new QuestInfoData();
      newInfoData.questList = npc.npcData.npcQuestList[0].getAllQuestSpecific(type);
      newInfoData.index = 0;
      newInfoData.questType = type;

      return newInfoData;
   }

   public static DialogueData getDialogueInfo (int questState, DeliveryQuestPair deliveryQuestPair, bool hasMaterials) {
      DialogueData newDialogueData = new DialogueData();
      List<ClickableText.Type> newList = new List<ClickableText.Type>();
      QuestDialogue currentDialogue = deliveryQuestPair.dialogueData.questDialogueList.Find(_ => (int) _.questState == questState);

      // Determines if the dialogue needs a condition check
      if (currentDialogue.checkCondition) {
         List<Item> itemList = InventoryCacheManager.self.itemList;
         DeliverQuest deliveryQuest = deliveryQuestPair.deliveryQuest;

         if (hasMaterials) {
            // Sets the player to a positive response if Requirements are met
            newList.Add(currentDialogue.playerReply);
         } else {
            // Sets the player to a negative response if Requirements are met
            newList.Add(currentDialogue.playerNegativeReply);
         }
      } else {
         // Returns default positive reply
         newList.Add(currentDialogue.playerReply);
      }

      newDialogueData.answerList = newList;
      newDialogueData.npcDialogue = currentDialogue.npcDialogue;
      return newDialogueData;
   }

   #region Private Variables

   // Keeps track of what our starting text is
   protected string _greetingText = "";

   // Used for quest category index
   protected int _questOptionIndex = 0;

   #endregion
}