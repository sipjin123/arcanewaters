using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DeliverObjectiveRow : GenericItemRow
{
   #region Public Variables

   // Quantity of the item
   public InputField count;

   // The button for updating data
   public Button updateButton;

   #endregion

   public void setRowForDeliverObjective (QuestObjectiveDeliver deliverObjective) {
      modifyContent(deliverObjective.category, deliverObjective.itemTypeId);
         
      count.text = deliverObjective.count.ToString();
      if (deliverObjective.category == Item.Category.Quest_Item) {
         deliverObjective.count = 1;
         count.gameObject.SetActive(false);
      } else {
         count.gameObject.SetActive(true);
      }
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
