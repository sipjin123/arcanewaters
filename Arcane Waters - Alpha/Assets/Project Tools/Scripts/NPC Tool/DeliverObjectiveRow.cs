using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DeliverObjectiveRow : MonoBehaviour
{
   #region Public Variables

   // The components displaying the parameters
   public InputField itemCategory;
   public InputField itemTypeId;
   public InputField count;

   #endregion

   public void setRowForDeliverObjective (QuestObjectiveDeliver deliverObjective) {
      itemCategory.text = ((int) deliverObjective.category).ToString();
      itemTypeId.text = deliverObjective.itemTypeId.ToString();
      count.text = deliverObjective.count.ToString();
   }

   public QuestObjectiveDeliver getModifiedDeliverObjective () {
      // Create a new deliver objective object and initialize it with the modified values
      QuestObjectiveDeliver deliverObjective = new QuestObjectiveDeliver(
         (Item.Category) int.Parse(itemCategory.text), int.Parse(itemTypeId.text), int.Parse(count.text));

      return deliverObjective;
   }

   #region Private Variables

   #endregion
}
