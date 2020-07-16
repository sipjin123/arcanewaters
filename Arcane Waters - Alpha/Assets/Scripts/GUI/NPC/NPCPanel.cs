using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Text;
using System.Xml.XPath;

public class NPCPanel : Panel {
   #region Public Variables

   // The mode of the panel
   public enum Mode { None = 1, QuestNode = 2, GiftOffer = 3 }

   // The Text element showing the current NPC dialogue line
   public TextMeshProUGUI npcDialogueText;

   // Our head animation
   public SimpleAnimation headAnim;

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

   // The load blockers before the data is received
   public GameObject[] loadBlockers;

   // The cached quest data
   QuestData cachedQuestData;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;
   }

   public override void show () {
      base.show();

      // Get the head image from the npc and update it
      headAnim.setNewTexture(_npc.getHeadIconTexture());

      // Start typing out our intro text
      AutoTyper.SlowlyRevealText(npcDialogueText, _npcDialogueLine);
   }

   public void initLoadBlockers (bool isActive) {
      if (isActive) {
         // Clear out the old clickable options
         clearDialogueOptions();
         questListSection.SetActive(false);
         questNodeSection.SetActive(false);
         giftOfferSection.SetActive(false);
         npcDialogueText.enabled = false;
      }

      foreach (GameObject obj in loadBlockers) {
         obj.SetActive(isActive);
      }
   }

   public void updatePanelWithQuestSelection (int npcId, string npcName,
      int friendshipLevel, string greetingText, bool canOfferGift, bool hasTradeGossipDialogue, bool hasGoodbyeDialogue,
      bool isHireable, int landMonsterId, int questId, int questNodeId, int dialogueId, int[] itemStock) {
      initLoadBlockers(false);

      // Show the correct section
      configurePanelForMode(Mode.QuestNode);

      // Initialize the NPC characteristics
      setNPC(npcId, npcName, friendshipLevel);

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
      QuestData questData = NPCQuestManager.self.getQuestData(questId);
      if (questData != null) {
         cachedQuestData = questData;

         // Verifies if the user has enough friendship to start the quest
         string questStatus = "";
         bool canStartQuest = true;

         if (questNodeId + 1 > questData.questDataNodes.Length) {
            addDialogueOptionRow(Mode.QuestNode, ClickableText.Type.NPCDialogueEnd,
            () => dialogueEndClickedOn(), true);

         } else {
            QuestDataNode questDataNode = new List<QuestDataNode>(questData.questDataNodes).Find(_ => _.questDataNodeId == questNodeId);
            if (friendshipLevel < questDataNode.friendshipLevelRequirement) {
               canStartQuest = false;
               questStatus = "Friendship too Low";
            } else {
               questStatus = "In Progress";
            }

            // Create a clickable text row for the quest
            QuestDialogueNode dialogueNode = new List<QuestDialogueNode>(questDataNode.questDialogueNodes).Find(_ => _.dialogueIdIndex == dialogueId);
            npcDialogueText.enabled = true;
            _npcDialogueLine = dialogueNode.npcDialogue;
            if (isShowing()) {
               AutoTyper.SlowlyRevealText(npcDialogueText, _npcDialogueLine);
            }

            // Clear the quest objectives grid
            questObjectivesContainer.DestroyChildren();
            if (dialogueNode.itemRequirements == null || dialogueNode.itemRequirements.Length == 0) {
               questObjectivesGO.SetActive(false);
            } else {
               questObjectivesGO.SetActive(true);
            }

            // Add each quest objective
            bool hasCompleteIngredients = false;
            if (dialogueNode.itemRequirements.Length < 1) {
               hasCompleteIngredients = true;
            } else {
               hasCompleteIngredients = displayItemRequirements(dialogueNode.itemRequirements, itemStock);
            }

            if (hasCompleteIngredients) {
               addDialogueOptionRow(Mode.QuestNode, ClickableText.Type.NPCDialogueOption,
                  () => questSelectionRowClickedOn(questId, questNodeId, dialogueId), canStartQuest, dialogueNode.playerDialogue, questStatus);
            } else {
               D.editorLog("Not enough ingredients!", Color.red);
               addDialogueOptionRow(Mode.QuestNode, ClickableText.Type.NPCDialogueEnd, () => dialogueEndClickedOn(), true);
            }
         }
      } else {
         NPCData npcData = NPCManager.self.getNPCData(npcId);
         npcDialogueText.enabled = true;
         _npcDialogueLine = npcData.greetingTextStranger;
         if (isShowing()) {
            AutoTyper.SlowlyRevealText(npcDialogueText, _npcDialogueLine);
         }
         addDialogueOptionRow(Mode.QuestNode, ClickableText.Type.NPCDialogueEnd, () => dialogueEndClickedOn(), true);
      }
      
      /*
      // Create a clickable text row for the gift offering
      if (canOfferGift) {
         addDialogueOptionRow(Mode.QuestNode, ClickableText.Type.Gift,
            () => giftRowClickedOn(), true);
      }*/
   }

   public void updatePanelWithQuestNode (int friendshipLevel, int questId, int questNodeId, int dialogueId,
      bool areObjectivesCompleted, bool isEnabled, int[] itemStock) {
      initLoadBlockers(false);

      if (areObjectivesCompleted && !isEnabled) {
         areObjectivesCompleted = false;
      }

      // Show the correct section
      configurePanelForMode(Mode.QuestNode);

      // Clear out the old clickable options
      clearDialogueOptions();

      string questStatus = "In Progress";
      QuestData questNode = NPCQuestManager.self.getQuestData(questId);
      if (questNodeId + 1 > questNode.questDataNodes.Length) {
         D.editorLog("No quest left to take", Color.green);

         addDialogueOptionRow(Mode.QuestNode, ClickableText.Type.NPCDialogueEnd,
         () => dialogueEndClickedOn(), true);
      } else {
         QuestDataNode questDataNode = new List<QuestDataNode>(questNode.questDataNodes).Find(_ => _.questDataNodeId == questNodeId);
         QuestDialogueNode dialogueNode = new List<QuestDialogueNode>(questDataNode.questDialogueNodes).Find(_ => _.dialogueIdIndex == dialogueId);
         npcDialogueText.enabled = true;
         _npcDialogueLine = dialogueNode.npcDialogue; 
         if (isShowing()) {
            AutoTyper.SlowlyRevealText(npcDialogueText, _npcDialogueLine);
         }

         // Clear the quest objectives grid
         questObjectivesContainer.DestroyChildren();
         if (dialogueNode.itemRequirements == null || dialogueNode.itemRequirements.Length == 0) {
            questObjectivesGO.SetActive(false);
         } else {
            questObjectivesGO.SetActive(true);
         }

         // Add each quest objective
         bool hasCompleteIngredients = false;
         if (dialogueNode.itemRequirements.Length < 1) {
            hasCompleteIngredients = true;
         } else {
            hasCompleteIngredients = displayItemRequirements(dialogueNode.itemRequirements, itemStock);
         }

         if (!hasCompleteIngredients) {
            D.editorLog("Not enough ingredients!", Color.red);
            addDialogueOptionRow(Mode.QuestNode, ClickableText.Type.NPCDialogueEnd, () => dialogueEndClickedOn(), true);
         } else {
            addDialogueOptionRow(Mode.QuestNode, ClickableText.Type.NPCDialogueOption, () => questSelectionRowClickedOn(questId, questNodeId, dialogueId), true, dialogueNode.playerDialogue, questStatus);
         }
      }
   }

   private bool displayItemRequirements (Item[] itemRequirementList, int[] itemStock) {
      bool hasCompleteIngredients = true;
      int itemIndexCount = 0;
      foreach (Item itemRequirement in itemRequirementList) {
         // Create a quest objective cell
         NPCPanelQuestObjectiveCell cell = Instantiate(questObjectiveCellPrefab);
         cell.transform.SetParent(questObjectivesContainer.transform);
         cell.updateCellContent(itemRequirement, itemRequirement.count, itemStock[itemIndexCount]);
         if (itemStock[itemIndexCount] >= itemRequirement.count) {
            // Add logic here if item reaches requirement
         } else {
            hasCompleteIngredients = false;
            D.editorLog("Not enough ingredients! " + itemStock[itemIndexCount] + " / " + itemRequirement.count, Color.red);
         }
         itemIndexCount++;
      }

      return hasCompleteIngredients;
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

   public void questSelectionRowClickedOn (int questId, int questNodeId, int dialogueId) {
      D.editorLog("Quest node was clicked");
      Global.player.rpc.Cmd_SelectNextNPCDialogue(_npc.npcId, questId, questNodeId, dialogueId);

      initLoadBlockers(true);
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

   public void setNPC(int npcId, string npcName, int friendshipLevel) {
      _npc = NPCManager.self.getNPC(npcId);

      // Fill in the details for this NPC
      nameText.text = npcName;
      friendshipLevelText.enabled = friendshipLevel >= 0;
      friendshipRankText.enabled = friendshipLevel >= 0;

      // Set the friendship level
      _friendshipLevel = friendshipLevel;
      friendshipLevelText.text = friendshipLevel.ToString();

      // Set the friendship rank
      _friendshipRank = NPCFriendship.getRank(friendshipLevel);
      friendshipRankText.text = NPCFriendship.getRankName(_friendshipRank);
   }

   private void configurePanelForMode(Mode mode) {
      switch (mode) {
         /*case Mode.QuestList:
            questListSection.SetActive(true);
            questNodeSection.SetActive(false);
            giftOfferSection.SetActive(false);
            break;*/
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
      if (mode == Mode.QuestNode) {
         container = dialogueOptionRowContainerForQuestNode;
      } else {
         D.debug("The NPC Panel mode " + mode.ToString() + " does not handle dialogue options");
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
