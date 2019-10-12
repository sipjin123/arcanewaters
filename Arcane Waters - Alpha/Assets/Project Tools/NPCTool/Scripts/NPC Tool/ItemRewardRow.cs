using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class ItemRewardRow : MonoBehaviour
{
   #region Public Variables

   // The components displaying the parameters
   public Text itemCategory;
   public Text itemTypeId;
   public InputField count;
   public Text itemCategoryName;
   public Text itemTypeName;

   // Button for changing selection data after clicking category
   public Button updateCategoryButton;

   // Button for changing selection data after clicking type
   public Button updateTypeButton;

   // Button for updating data upon clicking the type
   public Button updateButton;

   // Button for deleting selected data 
   public Button deleteButton;

   // Holds the icon for the item image
   public Image itemIcon;

   #endregion

   public void setRowForItemReward (QuestRewardItem itemReward) {
      if (itemReward.category == Item.Category.None || itemReward.itemTypeId == 0) {
         itemCategory.text = "(Select)";
         itemTypeId.text = "(Select)";
      } else {
         itemCategory.text = ((int) itemReward.category).ToString();
         itemTypeId.text = itemReward.itemTypeId.ToString();
      }

      itemCategoryName.text = itemReward.category.ToString();
      itemTypeName.text = Util.getItemName(itemReward.category, itemReward.itemTypeId);
      itemIcon.sprite = Util.getRawSpriteIcon(itemReward.category, itemReward.itemTypeId);

      count.text = itemReward.count.ToString();
   }

   public QuestRewardItem getModifiedItemReward () {
      // Create a new item reward object and initialize it with the modified values
      QuestRewardItem itemReward = new QuestRewardItem (
         (Item.Category) int.Parse(itemCategory.text), int.Parse(itemTypeId.text), int.Parse(count.text));

      return itemReward;
   }

   public Item getItem() {
      return new Item {
         category = (Item.Category)int.Parse(itemCategory.text),
         itemTypeId = int.Parse(itemTypeId.text)
      };
   }

   public void destroyRow() {
      Destroy(gameObject, .25f);
   }

   #region Private Variables

   #endregion
}
