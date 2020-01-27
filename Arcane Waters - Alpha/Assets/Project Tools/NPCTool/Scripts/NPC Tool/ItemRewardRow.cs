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

      if (itemReward.category == Item.Category.Blueprint) {
         itemCategoryName.text = itemReward.category.ToString();
         Item.Category modifiedCategory = Item.Category.None;

         int modifiedID = 0;
         if (itemReward.itemTypeId.ToString().StartsWith(Blueprint.WEAPON_PREFIX)) {
            modifiedID = int.Parse(itemReward.itemTypeId.ToString().Replace(Blueprint.WEAPON_PREFIX, ""));
            modifiedCategory = Item.Category.Weapon;
         } else {
            modifiedID = int.Parse(itemReward.itemTypeId.ToString().Replace(Blueprint.ARMOR_PREFIX, ""));
            modifiedCategory = Item.Category.Armor;
         }

         itemTypeName.text = Util.getItemName(modifiedCategory, modifiedID);
         itemIcon.sprite = Util.getRawSpriteIcon(modifiedCategory, modifiedID);
      } else {
         itemCategoryName.text = itemReward.category.ToString();
         itemTypeName.text = Util.getItemName(itemReward.category, itemReward.itemTypeId);
         itemIcon.sprite = Util.getRawSpriteIcon(itemReward.category, itemReward.itemTypeId);
      }

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

   public bool isValidItem () {
      if (itemCategory.text.Length < 1)
         return false;
      if (itemTypeId.text.Length < 1)
         return false;

      try {
         Item newItem = new Item {
            category = (Item.Category) int.Parse(itemCategory.text),
            itemTypeId = int.Parse(itemTypeId.text)
         };
      } catch {
         return false;
      }

      return true;
   }

   public Item getItem () {
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
