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
      modifyContent(itemReward.category, itemReward.itemTypeId);

      count.text = itemReward.count.ToString();
      if (itemReward.category == Item.Category.Quest_Item) {
         itemReward.count = 1;
         count.gameObject.SetActive(false);
      } else {
         count.gameObject.SetActive(true);
      }
   }

   public QuestRewardItem getModifiedItemReward () {
      // Create a new item reward object and initialize it with the modified values
      QuestRewardItem itemReward = new QuestRewardItem (
         (Item.Category) int.Parse(itemCategory.text), int.Parse(itemTypeId.text), int.Parse(count.text));

      return itemReward;
   }

   #region Private Variables

   #endregion
}
