using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class QuestDataToolTemplate : MonoBehaviour {
   // The quest title
   public InputField questTitle;

   // The quest level requirement 
   public InputField questLevelRequirement;

   // Id of the node
   public Text nodeIdText;

   // Reveals the quest info
   public Button selectQuestButton, deleteButton;

   // Cached info of the quest data
   public QuestDataNode questDataCache;

   // The indicator that the template is selected
   public GameObject highlightObj;

   // Moves the quest node up or down
   public Button moveUpButton, moveDownButton;

   private void Awake () {
      moveUpButton.onClick.AddListener(() => {
         QuestDataToolPanel.self.modifyQuestNodeOrder(this, transform.GetSiblingIndex() - 1);
      });

      moveDownButton.onClick.AddListener(() => {
         QuestDataToolPanel.self.modifyQuestNodeOrder(this, transform.GetSiblingIndex() + 1);
      });
   }

   public void loadQuestData (QuestDataNode questData) {
      questDataCache = questData;

      nodeIdText.text = questData.questDataNodeId.ToString();
      questTitle.text = questData.questNodeTitle;
      questLevelRequirement.text = questData.friendshipLevelRequirement.ToString();
   }

   public void highlightTemplate (bool isActive) {
      highlightObj.SetActive(isActive);
   }

   public void updateCache () {
      questDataCache.questNodeTitle = questTitle.text;
      questDataCache.friendshipLevelRequirement = int.Parse(questLevelRequirement.text);
      questDataCache.questDataNodeId = int.Parse(nodeIdText.text);
   }
}
