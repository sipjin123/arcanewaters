using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Text;

public class NPCPanel : Panel {
   #region Public Variables

   // The mode of the panel
   public enum Mode { QuestList = 1, QuestNode = 2, GiftOffer = 3 }

   // The Text element showing the current NPC dialogue line
   public TextMeshProUGUI npcDialogueText;

   // Our head animation
   public SimpleAnimation headAnim;

   // Our Faction Icon
   public Image factionIcon;

   // Our Specialty Icon
   public Image specialtyIcon;

   // The Text that shows the NPC name
   public Text nameText;

   // The Text that shows our friendship level
   public Text friendshipLevelText;

   // The Text that shows our friendship rank
   public Text friendshipRankText;

   // The different sections that each mode uses
   public GameObject questListSection;
   public GameObject questNodeSection;
   public GameObject giftOfferSection;

   // The container for the dialogue options, quest list mode
   public GameObject dialogueOptionRowContainerForQuestList;

   // The container for the dialogue options, quest node mode
   public GameObject dialogueOptionRowContainerForQuestNode;

   // The root of the quest objectives section
   public GameObject questObjectivesGO;

   // The title of the quest objectives section
   public Text questObjectiveTitle;

   // The container for the quest objective cells
   public GameObject questObjectivesContainer;

   // The prefab we use for creating quest objective cells
   public NPCPanelQuestObjectiveCell questObjectiveCellPrefab;

   // The container for the gifted item cell
   public GameObject itemCellContainer;

   // The button used to confirm the gifting of an item
   public Button confirmOfferGiftButton;

   // The prefab we use for creating dialogue rows
   public ClickableText dialogueOptionRowPrefab;

   // The prefab we use for creating item cells
   public ItemCell itemCellPrefab;

   // The animator to trigger when the friendship increases
   public Animator friendshipIncreaseAnimator;

   // The color for dialogue options that cannot be clicked
   public Color disabledClickableRowColor;

   // The default texture if there is an issue with sql texture loading
   public Texture2D defaultTexture;

   // A notife that is enabled if the npc is hireable
   public GameObject isHireableNotification;

   // Sends a command to hire the npc as a companion
   public Button hireButton;

   // Self
   public static NPCPanel self;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;
   }

   public override void show () {
      base.show();

      // Update the head image based on the type of NPC this is
      string spritePath = "Faces/" + _npc.GetComponent<SpriteSwap>().newTexture.name + "_1";
      Texture2D newTexture = ImageManager.getTexture(spritePath, false);
      if (newTexture == ImageManager.self.blankTexture) {
         newTexture = NPCManager.self.defaultNpcFaceSprite.texture;
      }
      headAnim.GetComponent<SimpleAnimation>().setNewTexture(newTexture);

      // Start typing out our intro text
      AutoTyper.SlowlyRevealText(npcDialogueText, _npcDialogueLine);
   }

   public void updatePanelWithQuestSelection (int npcId, string npcName, Faction.Type faction, Specialty.Type specialty,
      int friendshipLevel, string greetingText, bool canOfferGift, bool hasTradeGossipDialogue, bool hasGoodbyeDialogue,
      Quest[] quests, bool isHireable, int landMonsterId) {
      // Show the correct section
      configurePanelForMode(Mode.QuestList);

      // Initialize the NPC characteristics
      setNPC(npcId, npcName, faction, specialty, friendshipLevel);

      // Set the panel content common to the different modes
      setCommonPanelContent(greetingText, friendshipLevel);

      // Clear out the old clickable options
      clearDialogueOptions();

      isHireableNotification.SetActive(isHireable);
      hireButton.onClick.RemoveAllListeners();
      hireButton.onClick.AddListener(() => {
         Global.player.rpc.Cmd_HireCompanion(landMonsterId);
      });

      // Create a clickable text row for each quest in the list
      foreach (Quest quest in quests) {
         // Set the quest title
         string questTitle = quest.title;

         string questStatus = null;
         if (quest.questProgress > 0) {
            questStatus = "In Progress";
         }

         // Verifies if the user has enough friendship to start the quest
         bool canStartQuest = true;
         if (!NPCFriendship.isRankAboveOrEqual(friendshipLevel, quest.friendshipRankRequired)) {
            canStartQuest = false;
            questStatus = "Friendship too Low";
         }

         // Create a clickable text row for the quest
         addDialogueOptionRow(Mode.QuestList, ClickableText.Type.NPCDialogueOption,
            () => questSelectionRowClickedOn(quest.questId), canStartQuest, questTitle, questStatus);
      }
      
      // Create a clickable text row for the gift offering
      if (canOfferGift) {
         addDialogueOptionRow(Mode.QuestList, ClickableText.Type.Gift,
            () => giftRowClickedOn(), true);
      }

      // Create a clickable text row for the trade gossip
      if (hasTradeGossipDialogue) {
         addDialogueOptionRow(Mode.QuestList, ClickableText.Type.TradeGossip,
            () => gossipRowClickedOn(), true);
      }

      // Create a clickable text row for the 'Goodbye'
      if (hasGoodbyeDialogue) {
         addDialogueOptionRow(Mode.QuestList, ClickableText.Type.NPCDialogueEnd,
         () => dialogueEndClickedOn(), true);
      }
   }

   public void updatePanelWithQuestNode (int friendshipLevel, int questId, QuestNode node,
      bool areObjectivesCompleted, int[] objectivesProgress, bool isEnabled) {
      if (areObjectivesCompleted && !isEnabled) {
         areObjectivesCompleted = false;
      }

      // Show the correct section
      configurePanelForMode(Mode.QuestNode);

      // Set the panel content common to the different modes
      setCommonPanelContent(node.npcText, friendshipLevel, isEnabled, node.actionRequirements);

      // Clear out the old clickable options
      clearDialogueOptions();

      // Create a clickable text row with the user's answer
      addDialogueOptionRow(Mode.QuestNode, ClickableText.Type.NPCDialogueOption, 
         () => nextQuestNodeRowClickedOn(questId), areObjectivesCompleted, node.userText);

      // Retrieve the quest objectives
      List<QuestObjective> questObjectives = node.getAllQuestObjectives();
      
      // If there are quest objectives, display them in their section
      if (questObjectives != null && questObjectives.Count > 0) {
         // Enable the quest objectives section
         questObjectivesGO.SetActive(true);

         // Set the title of the section
         if (questObjectives.Count > 1) {
            questObjectiveTitle.text = "Quest Objectives";
         } else {
            questObjectiveTitle.text = "Quest Objective";
         }

         // Clear the quest objectives grid
         questObjectivesContainer.DestroyChildren();

         // Add each quest objective
         int k = 0;
         foreach(QuestObjective o in questObjectives) {
            // Create a quest objective cell
            NPCPanelQuestObjectiveCell cell = Instantiate(questObjectiveCellPrefab);
            cell.transform.SetParent(questObjectivesContainer.transform);

            // Test if we received the progress for this objective from the server
            if (k < objectivesProgress.Length) {
               cell.setCellForQuestObjective(o, objectivesProgress[k], isEnabled);
            } else {
               D.warning("The quest objectives progress received from the server is inconsistent with the client's quest data");
               cell.setCellForQuestObjective(o, -1, isEnabled);
            }
            k++;
         }
      }
   }

   public void updatePanelWithGiftOffer (string npcText) {
      // Show the correct section
      configurePanelForMode(Mode.GiftOffer);

      // Clear out the item cell
      itemCellContainer.DestroyChildren();

      // Disable the 'offer' button
      confirmOfferGiftButton.interactable = false;

      // Set the panel content common to the different modes
      setCommonPanelContent(npcText);
   }

   public void updatePanelWithCustomDialogue (int friendshipLevel, string npcText,
      ClickableText.Type userTextType, string userText, string questStatusText = null) {
      // Show the correct section
      configurePanelForMode(Mode.QuestNode);

      // Set the panel content common to the different modes
      setCommonPanelContent(npcText, friendshipLevel);

      // Clear out the old clickable options
      clearDialogueOptions();
      
      // Create a clickable text row with the user's answer
      if (userText == null) {
         addDialogueOptionRow(Mode.QuestNode, userTextType,
            () => backToQuestSelectionRowClickedOn(), true);
      } else {
         addDialogueOptionRow(Mode.QuestNode, ClickableText.Type.NPCDialogueOption,
            () => backToQuestSelectionRowClickedOn(), true, userText, questStatusText);
      }
   }

   public void questSelectionRowClickedOn (int questId) {
      Global.player.rpc.Cmd_SelectNPCQuest(_npc.npcId, questId);
   }

   public void nextQuestNodeRowClickedOn (int questId) {
      Global.player.rpc.Cmd_MoveToNextNPCQuestNode(_npc.npcId, questId);
   }

   public void gossipRowClickedOn () {
      Global.player.rpc.Cmd_RequestNPCTradeGossipFromServer(_npc.npcId);
   }

   public void dialogueEndClickedOn () {
      PanelManager.self.popPanel();
   }

   public void backToQuestSelectionRowClickedOn () {
      Global.player.rpc.Cmd_RequestNPCQuestSelectionListFromServer(_npc.npcId);
   }

   public void giftRowClickedOn () {
      Global.player.rpc.Cmd_RequestGiftOfferNPCTextFromServer(_npc.npcId);
   }

   public void selectGiftButtonClickedOn () {
      // Associate a new function with the select button
      PanelManager.self.itemSelectionScreen.selectButton.onClick.RemoveAllListeners();
      PanelManager.self.itemSelectionScreen.selectButton.onClick.AddListener(() => returnFromGiftSelection());

      // Associate a new function with the cancel button
      PanelManager.self.itemSelectionScreen.cancelButton.onClick.RemoveAllListeners();
      PanelManager.self.itemSelectionScreen.cancelButton.onClick.AddListener(() => hideItemSelectionScreen());

      // Show the item selection screen
      PanelManager.self.itemSelectionScreen.show(new List<int>(), Item.Category.None);
   }

   public void confirmOfferGiftButtonClickedOn () {
      Global.player.rpc.Cmd_GiftItemToNPC(_npc.npcId, _selectedGiftItem.id, _selectedGiftItemCount);
   }

   public void hideItemSelectionScreen () {
      PanelManager.self.itemSelectionScreen.hide();
   }

   public void returnFromGiftSelection () {
      // Hide item selection screen
      PanelManager.self.itemSelectionScreen.hide();

      // Save the selected item
      _selectedGiftItem = ItemSelectionScreen.selectedItem;

      // Save the number of items to gift
      _selectedGiftItemCount = ItemSelectionScreen.selectedItemCount;

      // Clear out the old item cell
      itemCellContainer.DestroyChildren();


      if (_selectedGiftItem != null) {
         // Instantiates the item cell
         ItemCell cell = Instantiate(itemCellPrefab, itemCellContainer.transform, false);
         cell.transform.SetParent(itemCellContainer.transform, false);

         // Initializes the cell
         cell.setCellForItem(_selectedGiftItem, _selectedGiftItemCount);

         // Disable the click event on the cell
         cell.disablePointerEvents();

         // Enables the 'confirm gift' button
         confirmOfferGiftButton.interactable = true;
      }
   }

   private void setCommonPanelContent(string npcText, int friendshipLevel, bool hasFinishedAchievements = true, QuestActionRequirement[] actionRequirements = null) {
      // If the friendship level changed, play an animation
      if (_friendshipLevel != -1 && _friendshipLevel != friendshipLevel) {
         friendshipIncreaseAnimator.SetTrigger("friendshipChanged");

         // If the friendship rank increased, notice the player
         if (_friendshipRank != NPCFriendship.Rank.None &&
            friendshipLevel > _friendshipLevel &&
            _friendshipRank != NPCFriendship.getRank(friendshipLevel))  {
            PanelManager.self.noticeScreen.show(string.Format("Your friendship with {0} raised to {1}", _npc.getName(), NPCFriendship.getRankName(friendshipLevel)));
         }
      }

      if (actionRequirements != null) {
         string requirementContent = "";
         foreach (QuestActionRequirement requirement in actionRequirements) {
            requirementContent += "\n" + requirement.actionTitle;
         }

         if (!hasFinishedAchievements) {
            PanelManager.self.noticeScreen.show(string.Format("This Quest requires you to finish {0}", requirementContent));
         }
      }

      // Set the friendship level
      _friendshipLevel = friendshipLevel;
      friendshipLevelText.text = friendshipLevel.ToString();

      // Set the friendship rank
      _friendshipRank = NPCFriendship.getRank(friendshipLevel);
      friendshipRankText.text = NPCFriendship.getRankName(_friendshipRank);

      setCommonPanelContent(npcText);
   }

   private void setCommonPanelContent (string npcText) {
      // Set the current npc text line
      _npcDialogueLine = npcText;

      // If the panel is already showing, start writing the new text
      if (isShowing()) {
         AutoTyper.SlowlyRevealText(npcDialogueText, _npcDialogueLine);
      }

      // By default, hide the quest objectives section
      questObjectivesGO.SetActive(false);
   }

   private void setNPC(int npcId, string npcName, Faction.Type faction, Specialty.Type specialty,
      int friendshipLevel) {
      _npc = NPCManager.self.getNPC(npcId);

      // Fill in the details for this NPC
      factionIcon.sprite = Faction.getIcon(faction);
      specialtyIcon.sprite = Specialty.getIcon(specialty);
      nameText.text = npcName;

      // Set the friendship level
      _friendshipLevel = friendshipLevel;
      friendshipLevelText.text = friendshipLevel.ToString();

      // Set the friendship rank
      _friendshipRank = NPCFriendship.getRank(friendshipLevel);
      friendshipRankText.text = NPCFriendship.getRankName(_friendshipRank);
   }

   private void configurePanelForMode(Mode mode) {
      switch (mode) {
         case Mode.QuestList:
            questListSection.SetActive(true);
            questNodeSection.SetActive(false);
            giftOfferSection.SetActive(false);
            break;
         case Mode.QuestNode:
            questListSection.SetActive(false);
            questNodeSection.SetActive(true);
            giftOfferSection.SetActive(false);
            break;
         case Mode.GiftOffer:
            questListSection.SetActive(false);
            questNodeSection.SetActive(false);
            giftOfferSection.SetActive(true);
            break;
         default:
            questListSection.SetActive(false);
            questNodeSection.SetActive(true);
            giftOfferSection.SetActive(false);
            break;
      }
   }

   private void clearDialogueOptions () {
      dialogueOptionRowContainerForQuestList.DestroyChildren();
      dialogueOptionRowContainerForQuestNode.DestroyChildren();
   }

   private void addDialogueOptionRow (Mode mode, ClickableText.Type clickableType,
      UnityEngine.Events.UnityAction functionToCall, bool isInteractive, string text = null, string statusText = null) {
      // Find the correct container that will hold the row
      GameObject container;
      switch (mode) {
         case Mode.QuestList:
            container = dialogueOptionRowContainerForQuestList;
            break;
         case Mode.QuestNode:
            container = dialogueOptionRowContainerForQuestNode;
            break;
         default:
            D.warning("The NPC Panel mode " + mode.ToString() + " does not handle dialogue options");
            return;
      }

      // Create a clickable text row
      ClickableText row = Instantiate(dialogueOptionRowPrefab);
      row.transform.SetParent(container.transform);

      if (statusText != null) {
         row.statusIndicator.SetActive(true);
         row.setBackground(isInteractive, statusText);
      }

      // Set the text
      if (text == null) {
         row.initData(clickableType);
      } else {
         row.initData(clickableType, text);
      }

      // Set up the click function
      row.clickedEvent.AddListener(functionToCall);
      row.gameObject.SetActive(true);

      // Disable the row if it is not interactive
      if (!isInteractive) {
         row.disablePointerEvents(disabledClickableRowColor);
      }
   }

   #region Private Variables

   // The NPC associated with this panel
   private NPC _npc;

   // Keeps track of what our starting text is
   protected string _npcDialogueLine = "";

   // Keep track of the currently displayed friendship level
   protected int _friendshipLevel = -1;

   // Keep track of the currently displayed friendship rank
   protected NPCFriendship.Rank _friendshipRank = NPCFriendship.Rank.None;

   // The selected gift item
   protected Item _selectedGiftItem;

   // The number of gift items
   protected int _selectedGiftItemCount = 1;

   #endregion
}
