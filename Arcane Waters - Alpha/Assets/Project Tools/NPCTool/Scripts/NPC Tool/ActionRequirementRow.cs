using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ActionRequirementRow : MonoBehaviour {

   #region Public Variables

   // Action type index requirement for a quest to be revealed
   public int actionTypeIndex = 0;

   // UI showing the set action type index
   public Button actionTypeButton;
   public Text actionTypeLabel;

   // Removes this entry
   public Button deleteTypeButton;

   #endregion

   public QuestActionRequirement getModifiedItemReward () {
      QuestActionRequirement newRequirementRow = new QuestActionRequirement();
      newRequirementRow.actionTypeIndex = actionTypeIndex;
      newRequirementRow.actionTitle = NPCToolManager.instance.getAchievementData(actionTypeIndex).achievementName;

      return newRequirementRow;
   }

   public void setData (QuestActionRequirement actionRequirement) {
      actionTypeIndex = actionRequirement.actionTypeIndex;
      actionTypeLabel.text = NPCToolManager.instance.getAchievementData(actionRequirement.actionTypeIndex).achievementName;
   }

   #region Private Variables

   #endregion
}
