using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class QuestDataToolPanel : MonoBehaviour {
   #region Public Variables

   // Self
   public static QuestDataToolPanel self;

   // The quest template
   public QuestDataToolTemplate questDataTemplate;

   // The quest holder
   public Transform questNodeParent;

   // Name of the quest group
   public InputField questGroupName;

   // The auto generated id of the quest
   public Text questGroupId;

   // Info of the quest group
   public InputField questGroupDescription;

   // Image and button for replacing the icon
   public Image questGroupIcon;
   public Button questGroupIconButton;
   public Text questIconPath;

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

   // The cached data
   public QuestData cachedQuestData;

   // If the data is active in the sql
   public Toggle isActive;

   // The selection popup
   public GenericSelectionPopup selectionPopup;

   // The selected dialogue node
   public QuestToolDialogueTemplate selectedDialogueTemplate;

   // The hovered dialogue node
   public QuestToolDialogueTemplate hoveredDialogueTemplate;

   // The draggable node information
   public Transform draggedNodeInfo;

   // The selected draggable node text notice
   public Text draggedNodeTextSelected;

   // Canvas reference
   public Canvas canvas;

   #endregion

   private void Awake () {
      self = this;
      saveButton.onClick.AddListener(() => {
         saveDialogueDataToNode();
         processSavingData();
         gameObject.SetActive(false);
      });
      cancelButton.onClick.AddListener(() => {
         gameObject.SetActive(false);
      });

      saveDialogueButton.onClick.AddListener(() => {
         saveDialogueDataToNode();
      });

      addQuestNode.onClick.AddListener(() => {
         createQuestTemplate(new QuestDataNode { questDataNodeId = cachedQuestData.questDataNodes == null ? 0 : cachedQuestData.questDataNodes.Length }, true);
      });

      addDialogueNodeButton.onClick.AddListener(() => {
         int nodeChildCount = questDialogueHolder.childCount;
         createDialogueTemplate(new QuestDialogueNode { 
            dialogueIdIndex = nodeChildCount
         });
      });

      questGroupIconButton.onClick.AddListener(() => {
         selectionPopup.callImageTextSelectionPopup(GenericSelectionPopup.selectionType.NpcIcon, questGroupIcon, questIconPath);
      });
   }

   private void Update () {
      if (selectedDialogueTemplate != null) {
         Vector2 pos;
         RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, Input.mousePosition, canvas.worldCamera, out pos);
         draggedNodeInfo.transform.position = canvas.transform.TransformPoint(pos);
      }
   }

   private void LateUpdate () {
      if (selectedDialogueTemplate != null) {
         if (Input.GetKeyUp(KeyCode.Mouse0)) {
            if (hoveredDialogueTemplate != null) {
               if (hoveredDialogueTemplate != selectedDialogueTemplate) {
                  selectedDialogueTemplate.transform.SetSiblingIndex(hoveredDialogueTemplate.transform.GetSiblingIndex());
                  recalibrateDialogueIds();
               }
            } 

            selectedDialogueTemplate = null;
            draggedNodeInfo.gameObject.SetActive(false);
         }
      }
   }

   public void selectDialogueNode (QuestToolDialogueTemplate template) {
      selectedDialogueTemplate = template;
      draggedNodeInfo.gameObject.SetActive(true);
      draggedNodeTextSelected.text = template.dialogueIdText.text;
   }

   public void hoverDialogueNode (QuestToolDialogueTemplate template) {
      hoveredDialogueTemplate = template;
   }

   public void loadPanel (QuestData questData) {
      cachedQuestData = questData;
      questGroupName.text = questData.questGroupName;
      questGroupDescription.text = questData.questGroupDetails;
      questGroupId.text = questData.questId.ToString();
      isActive.isOn = questData.isActive;

      questNodeParent.gameObject.DestroyChildren();
      questDialogueHolder.gameObject.DestroyChildren();

      Sprite iconSprite = ImageManager.getSprite(questData.iconPath);
      if (iconSprite != null) {
         questIconPath.text = questData.iconPath;
         questGroupIcon.sprite = iconSprite;
      }

      if (questData.questDataNodes != null) {
         foreach (QuestDataNode questNode in questData.questDataNodes) {
            createQuestTemplate(questNode);
         }
      }
   }

   private void processSavingData () {
      QuestData newQuestData = new QuestData();
      newQuestData.questDataNodes = getQuestNodesData().ToArray();
      newQuestData.questGroupName = questGroupName.text;
      newQuestData.questId = int.Parse(questGroupId.text);
      newQuestData.questGroupDetails = questGroupDescription.text;
      newQuestData.isActive = isActive.isOn;
      newQuestData.iconPath = questIconPath.text;

      QuestDataToolManager.instance.saveData(newQuestData);
   }

   private void saveDialogueDataToNode () {
      List<QuestDialogueNode> dialogueNodeList = new List<QuestDialogueNode>();
      foreach (Transform dialogueChildObj in questDialogueHolder) {
         QuestToolDialogueTemplate newDialogueTemplate = dialogueChildObj.GetComponent<QuestToolDialogueTemplate>();
         QuestDialogueNode newDialogueNode = newDialogueTemplate.getData();
         dialogueNodeList.Add(newDialogueNode);
      }

      if (dialogueNodeList.Count > 0) {
         selectedQuestDataNode.questDataCache.questDialogueNodes = dialogueNodeList.ToArray();
      }
   }

   private void createQuestTemplate (QuestDataNode questNode, bool autoSelect = false) {
      QuestDataToolTemplate questNodeTemplate = Instantiate(questDataTemplate.gameObject, questNodeParent).GetComponent<QuestDataToolTemplate>();
      questNodeTemplate.loadQuestData(questNode);
      questNodeTemplate.deleteButton.onClick.AddListener(() => {
         questNodeTemplate.transform.SetParent(null);
         modifyQuestNodeIndexes();
         Destroy(questNodeTemplate.gameObject);
      });
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

   public void modifyQuestNodeOrder (QuestDataToolTemplate template, int index) {
      template.transform.SetSiblingIndex(index);
      modifyQuestNodeIndexes();
   }

   private void modifyQuestNodeIndexes () {
      int index = 0;
      foreach (Transform questNodeChild in questNodeParent) {
         QuestDataToolTemplate questDataToolTemp = questNodeChild.GetComponent<QuestDataToolTemplate>();
         questDataToolTemp.nodeIdText.text = index.ToString();
         index++;
      }
   }
   private void modifyQuestDialogueIndexes () {
      int index = 0;
      foreach (Transform questNodeChild in questDialogueHolder) {
         QuestToolDialogueTemplate dialogueTemplate = questNodeChild.GetComponent<QuestToolDialogueTemplate>();
         dialogueTemplate.dialogueIdText.text = index.ToString();
         index++;
      }
   }

   public void createDialogueTemplate (QuestDialogueNode questDialogueData, int indexId = -1) {
      QuestToolDialogueTemplate questDialogueTemplate = Instantiate(questToolDialogueTemplate.gameObject, questDialogueHolder).GetComponent<QuestToolDialogueTemplate>();
      questDialogueTemplate.setDialogueData(questDialogueData);
      questDialogueTemplate.deleteButton.onClick.AddListener(() => {
         questDialogueTemplate.transform.SetParent(null);
         modifyQuestDialogueIndexes();
         Destroy(questDialogueTemplate.gameObject);
      });
      if (indexId > -1) {
         questDialogueTemplate.transform.SetSiblingIndex(indexId);
      }
   }

   public void recalibrateDialogueIds () {
      int id = 0;
      foreach (Transform dialogueNode in questDialogueHolder) {
         dialogueNode.GetComponent<QuestToolDialogueTemplate>().dialogueIdText.text = id.ToString();
         id++;
      }
   }

   private List<QuestDataNode> getQuestNodesData () {
      List<QuestDataNode> newQuestDataList = new List<QuestDataNode>();
      int index = 0;
      foreach (Transform questNodeChild in questNodeParent) {
         QuestDataToolTemplate questDataToolTemp = questNodeChild.GetComponent<QuestDataToolTemplate>();
         questDataToolTemp.nodeIdText.text = index.ToString();
         questDataToolTemp.updateCache();
         newQuestDataList.Add(questDataToolTemp.questDataCache);
         index++;
      }
      return newQuestDataList;
   }

   #region Private Variables

   #endregion
}
