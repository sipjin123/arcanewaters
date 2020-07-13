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

   // Reveals the quest info
   public Button selectQuestButton;

   // Cached info of the quest data
   public QuestDataNode questDataCache;

   // The indicator that the template is selected
   public GameObject highlightObj;

   public void loadQuestData (QuestDataNode questData) {
      questDataCache = questData;
   }

   public void highlightTemplate (bool isActive) {
      highlightObj.SetActive(isActive);
   }
}
