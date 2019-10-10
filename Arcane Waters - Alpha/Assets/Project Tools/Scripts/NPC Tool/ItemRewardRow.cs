using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ItemRewardRow : MonoBehaviour
{
   #region Public Variables

   // The components displaying the parameters
   public InputField itemCategory;
   public InputField itemTypeId;
   public InputField count;

   // Button for updating data
   public Button updateButton;

   #endregion

   public void setRowForItemReward (QuestRewardItem itemReward) {
      itemCategory.text = ((int) itemReward.category).ToString();
      itemTypeId.text = itemReward.itemTypeId.ToString();
      count.text = itemReward.count.ToString();
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
