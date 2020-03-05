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
      itemData = deliverObjective.data;
      modifyContent(deliverObjective.category, deliverObjective.itemTypeId, deliverObjective.data);
         
      count.text = deliverObjective.count.ToString();
      if (deliverObjective.category == Item.Category.Quest_Item) {
         deliverObjective.count = 1;
         count.gameObject.SetActive(false);
      } else {
         count.gameObject.SetActive(true);
      }
   }

   public QuestObjectiveDeliver getModifiedDeliverObjective () {
      int newID = int.Parse(itemTypeId.text);
      Item.Category category = (Item.Category) int.Parse(itemCategory.text);

      // Create a new deliver objective object and initialize it with the modified values
      QuestObjectiveDeliver deliverObjective = new QuestObjectiveDeliver(
         category, newID, int.Parse(count.text));
      deliverObjective.data = itemData;

      if (category == Item.Category.Blueprint) {
         deliverObjective.itemTypeId = Blueprint.modifyID(category, newID);
      }

      return deliverObjective;
   }

   #region Private Variables

   #endregion
}
