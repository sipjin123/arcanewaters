using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DeliverObjectiveRow : MonoBehaviour
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

   // The button for updating data
   public Button updateButton;

   // The button for deleting data
   public Button deleteButton;

   // Holds the icon of the item
   public Image itemIcon;

   #endregion

   public void setRowForDeliverObjective (QuestObjectiveDeliver deliverObjective) {
      if (deliverObjective.category == Item.Category.None || deliverObjective.itemTypeId == 0) {
         itemCategory.text = "(Select)";
         itemTypeId.text = "(Select)";
      } else {
         itemCategory.text = ((int) deliverObjective.category).ToString();
         itemTypeId.text = deliverObjective.itemTypeId.ToString();
      }

      itemCategoryName.text = deliverObjective.category.ToString();
      itemTypeName.text = Util.getItemName(deliverObjective.category, deliverObjective.itemTypeId);
      itemIcon.sprite = Util.getRawSpriteIcon(deliverObjective.category, deliverObjective.itemTypeId);

      count.text = deliverObjective.count.ToString();
   }

   public QuestObjectiveDeliver getModifiedDeliverObjective () {
      // Create a new deliver objective object and initialize it with the modified values
      QuestObjectiveDeliver deliverObjective = new QuestObjectiveDeliver(
         (Item.Category) int.Parse(itemCategory.text), int.Parse(itemTypeId.text), int.Parse(count.text));

      return deliverObjective;
   }

   public void destroyRow () {
      Destroy(gameObject, .25f);
   }

   #region Private Variables

   #endregion
}
