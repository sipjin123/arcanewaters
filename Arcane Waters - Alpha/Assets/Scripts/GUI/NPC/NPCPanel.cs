using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Text;

public class NPCPanel : Panel {
   #region Public Variables

   // The NPC associated with this panel
   public NPC npc;

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

   // The Text that shows our friendship number
   public Text friendshipText;

   // The container for our clickable dialogue rows
   public GameObject clickableRowContainer;

   // The prefab we use for creating dialogue rows
   public ClickableText clickableRowPrefab;

   // The animator to trigger when the friendship increases
   public Animator friendshipIncreaseAnimator;

   // The root of the quest objectives section
   public GameObject questObjectivesGO;

   // The title of the quest objectives section
   public Text questObjectiveTitle;

   // The container for the quest objective cells
   public GameObject questObjectivesContainer;

   // The prefab we use for creating quest objective cells
   public NPCPanelQuestObjectiveCell questObjectiveCellPrefab;

   // The color for dialogue options that cannot be clicked
   public Color disabledClickableRowColor;

   // Self
   public static NPCPanel self;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;
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
      AutoTyper.SlowlyRevealText(npcDialogueText, _npcDialogueLine);
   }

   public void updatePanelWithQuestSelection (int npcId, int friendshipLevel, int[] questIds) {
      // Set the panel content common to the different configurations
      setCommonPanelContent(npcId, NPCManager.self.getGreetingText(npcId), friendshipLevel);

      // Clear out the old clickable options
      clickableRowContainer.DestroyChildren();

      // Create a clickable text row for each quest in the list
      foreach (int questId in questIds) {
         // Retrieve the quest
         Quest quest = NPCManager.self.getQuest(npcId, questId);

         // Set the quest title
         string questTitle = quest.title;

         // Verifies if the user has enough friendship to start the quest
         bool canStartQuest = true;
         if (friendshipLevel < quest.friendshipLevelRequired) {
            canStartQuest = false;
            questTitle += " (friendship too low)";
         }

         // Create a clickable text row for the quest
         addDialogueOptionRow(ClickableText.Type.NPCDialogueOption,
            () => questSelectionRowClickedOn(questId), canStartQuest, questTitle);
      }

      // Create a clickable text row for the trade gossip
      addDialogueOptionRow(ClickableText.Type.TradeGossip,
         () => gossipRowClickedOn(), true);

      // Create a clickable text row for the 'Goodbye'
      addDialogueOptionRow(ClickableText.Type.NPCDialogueEnd,
         () => dialogueEndClickedOn(), true);
   }

   public void updatePanelWithQuestNode (int npcId, int friendshipLevel, int questId, int questNodeId,
      bool areObjectivesCompleted, int[] objectivesProgress) {
      // Get the quest node
      QuestNode node = NPCManager.self.getQuestNode(npcId, questId, questNodeId);

      // Set the panel content common to the different configurations
      setCommonPanelContent(npcId, node.npcText, friendshipLevel);

      // Clear out the old clickable options
      clickableRowContainer.DestroyChildren();

      // Create a clickable text row with the user's answer
      addDialogueOptionRow(ClickableText.Type.NPCDialogueOption, 
         () => nextQuestNodeRowClickedOn(questId), areObjectivesCompleted, node.userText);

      // Retrieve the quest objectives
      List<QuestObjective> questObjectives = node.objectives;

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
               cell.setCellForQuestObjective(o, objectivesProgress[k]);
            } else {
               D.warning("The quest objectives progress received from the server is inconsistent with the client's quest data");
               cell.setCellForQuestObjective(o, -1);
            }
            k++;
         }
      }
   }

   public void updatePanelWithTradeGossip (int npcId, string gossip) {
      // Set the panel content common to the different configurations
      setCommonPanelContent(npcId, gossip);

      // Clear out the old clickable options
      clickableRowContainer.DestroyChildren();

      // Create a clickable text row with the user's answer
      addDialogueOptionRow(ClickableText.Type.TradeGossipThanks, 
         () => backToQuestSelectionRowClickedOn(), true);
   }

   public void questSelectionRowClickedOn (int questId) {
      Global.player.rpc.Cmd_SelectNPCQuest(npc.npcId, questId);
   }

   public void nextQuestNodeRowClickedOn (int questId) {
      Global.player.rpc.Cmd_MoveToNextNPCQuestNode(npc.npcId, questId);
   }

   public void gossipRowClickedOn () {
      Global.player.rpc.Cmd_RequestNPCTradeGossipFromServer(npc.npcId);
   }

   public void dialogueEndClickedOn () {
      PanelManager.self.popPanel();
   }

   public void backToQuestSelectionRowClickedOn () {
      Global.player.rpc.Cmd_RequestNPCQuestSelectionListFromServer(npc.npcId);
   }

   private void setCommonPanelContent(int npcId, string npcText, int friendshipLevel) {
      // If the friendship level changed, play an animation
      if (_friendshipLevel != -1 && _friendshipLevel != friendshipLevel) {
         friendshipIncreaseAnimator.SetTrigger("friendshipChanged");
      }

      // Set the friendship level
      _friendshipLevel = friendshipLevel;
      friendshipText.text = friendshipLevel.ToString();

      setCommonPanelContent(npcId, npcText);
   }

   private void setCommonPanelContent (int npcId, string npcText) {
      this.npc = NPCManager.self.getNPC(npcId);

      // Set the current npc text line
      _npcDialogueLine = npcText;

      // If the panel is already showing, start writing the new text
      if (isShowing()) {
         AutoTyper.SlowlyRevealText(npcDialogueText, _npcDialogueLine);
      }

      // By default, hide the quest objectives section
      questObjectivesGO.SetActive(false);
   }

   private void addDialogueOptionRow (ClickableText.Type clickableType,
      UnityEngine.Events.UnityAction functionToCall, bool isInteractive, string text = null) {
      // Create a clickable text row
      ClickableText row = Instantiate(clickableRowPrefab);
      row.transform.SetParent(clickableRowContainer.transform);

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

   // Keeps track of what our starting text is
   protected string _npcDialogueLine = "";

   // Keep track of the current displayed friendship level
   protected int _friendshipLevel = -1;

   #endregion
}