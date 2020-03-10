using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class NPCPanelQuestObjectiveCell : MonoBehaviour
{
   #region Public Variables

   // The icon representing the objective
   public Image icon;

   // The text displaying the progress of the quest
   public Text progressText;

   // The tooltip when hovering the icon
   public Tooltipped tooltip;

   // The color for completed objectives
   public Color completedObjectiveColor;

   // The color for incompleted objectives
   public Color incompletedObjectivesColor;

   #endregion

   public void setCellForQuestObjective(QuestObjective objective, int progress, bool isEnabled) {
      icon.sprite = objective.getIcon();
      progressText.text = objective.getProgressString(progress);
      tooltip.text = objective.getObjectiveDescription();

      // Set the color of the progress text
      if (objective.canObjectiveBeCompleted(progress) && isEnabled) {
         progressText.color = completedObjectiveColor;
      } else {
         progressText.color = incompletedObjectivesColor;
      }
   }
   
   #region Private Variables

   #endregion
}
