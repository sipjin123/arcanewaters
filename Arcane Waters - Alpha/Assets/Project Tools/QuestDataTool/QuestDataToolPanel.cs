using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class QuestDataToolPanel : MonoBehaviour {
   #region Public Variables

   // The quest template
   public QuestDataToolTemplate questDataTemplate;

   // The quest holder
   public Transform questNodeParent;

   // Name of the quest group
   public InputField questGroupName;

   // Info of the quest group
   public InputField questGroupDescription;

   // Image and button for replacing the icon
   public Image questGroupIcon;
   public Button questGroupIconButton;

   // Adds a dialogue node
   public Button addDialogueNodeButton;

   // Adds a quest node
   public Button addQuestNode;

   // Save button and cancel button
   public Button saveButton, cancelButton, saveDialogueButton;

   // Parent holding all quest dialogues
   public Transform questDialogueHolder;

   // The quest ui template
   public QuestToolDialogueTemplate questToolDialogueTemplate;

   // The current quest data node being reviewed
   public QuestDataToolTemplate selectedQuestDataNode;

   #endregion

   private void Awake () {
      saveButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
      });
      cancelButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
      });

      saveDialogueButton.onClick.AddListener(() => {
         saveDialogueDataToNode();
      });

      addQuestNode.onClick.AddListener(() => {
         createQuestTemplate(new QuestDataNode(), true);
      });

      addDialogueNodeButton.onClick.AddListener(() => {
         createDialogueTemplate(new QuestDialogueNode());
      });
   }

   public void loadPanel (QuestData questData) {
      if (questData.questDataNodes != null) {
         foreach (QuestDataNode questNode in questData.questDataNodes) {
            createQuestTemplate(questNode);
         }
      }
   }

   private void saveDialogueDataToNode () {
      List<QuestDialogueNode> dialogueNodeList = new List<QuestDialogueNode>();
      foreach (Transform dialogueChildObj in questDialogueHolder) {
         QuestToolDialogueTemplate newDialogueTemplate = dialogueChildObj.GetComponent<QuestToolDialogueTemplate>();
         QuestDialogueNode newDialogueNode = newDialogueTemplate.getData();
         dialogueNodeList.Add(newDialogueNode);
      }

      selectedQuestDataNode.questDataCache.questDialogueNodes = dialogueNodeList.ToArray();
   }

   private void createQuestTemplate (QuestDataNode questNode, bool autoSelect = false) {
      QuestDataToolTemplate questNodeTemplate = Instantiate(questDataTemplate.gameObject, questNodeParent).GetComponent<QuestDataToolTemplate>();
      questNodeTemplate.loadQuestData(questNode);
      questNodeTemplate.selectQuestButton.onClick.AddListener(() => {
         if (selectedQuestDataNode != null) {
            selectedQuestDataNode.highlightTemplate(false);
         }
         selectedQuestDataNode = questNodeTemplate;
         selectedQuestDataNode.highlightTemplate(true);

         // Loads all the dialogues of the selected quest node
         questDialogueHolder.gameObject.DestroyChildren();

         if (questNode.questDialogueNodes != null) {
            foreach (QuestDialogueNode questDialogueData in questNode.questDialogueNodes) {
               createDialogueTemplate(questDialogueData);
            }
         }
      });

      if (autoSelect) {
         questNodeTemplate.selectQuestButton.onClick.Invoke();
      }
   }

   private void createDialogueTemplate (QuestDialogueNode questDialogueData) {
      QuestToolDialogueTemplate questDialogueTemplate = Instantiate(questToolDialogueTemplate.gameObject, questDialogueHolder).GetComponent<QuestToolDialogueTemplate>();
      questDialogueTemplate.setDialogueData(questDialogueData);
   }

   private List<QuestDataNode> getQuestNodesData () {
      List<QuestDataNode> newQuestDataList = new List<QuestDataNode>();
      foreach (Transform questNodeChild in questNodeParent) {
         QuestDataToolTemplate questDataToolTemp = questNodeChild.GetComponent<QuestDataToolTemplate>();
         newQuestDataList.Add(questDataToolTemp.questDataCache);
      }
      return newQuestDataList;
   }

   #region Private Variables

   #endregion
}
