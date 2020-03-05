using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ItemRewardRow : GenericItemRow
{
   #region Public Variables

   // Quantity of the item
   public InputField count;

   // The button for updating data
   public Button updateButton;

   #endregion

   public void setRowForItemReward (QuestRewardItem itemReward) {
      itemData = itemReward.data;
      modifyContent(itemReward.category, itemReward.itemTypeId, itemReward.data);

      count.text = itemReward.count.ToString();
      if (itemReward.category == Item.Category.Quest_Item) {
         itemReward.count = 1;
         count.gameObject.SetActive(false);
      } else {
         count.gameObject.SetActive(true);
      }
   }

   public QuestRewardItem getModifiedItemReward () {
      int newID = int.Parse(itemTypeId.text);
      Item.Category category = (Item.Category) int.Parse(itemCategory.text);

      // Create a new item reward object and initialize it with the modified values
      QuestRewardItem itemReward = new QuestRewardItem (
         category, newID, int.Parse(count.text));

      itemReward.data = itemData;

      if (category == Item.Category.Blueprint) {
         itemReward.itemTypeId = Blueprint.modifyID(category, newID);
      }
      return itemReward;
   }

   #region Private Variables

   #endregion
}
