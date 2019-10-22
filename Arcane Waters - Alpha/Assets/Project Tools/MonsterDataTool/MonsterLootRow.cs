using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MonsterLootRow : MonoBehaviour
{
   #region Public Variables

   // Button that handles the popup of item selection
   public Button changeItemTypeButton;
   public Button changeItemCategoryButton;

   // Displays the item requirement
   public InputField itemCount;

   // Displays the item category this template represents
   public Text itemCategoryString;

   // Displays the item type this template represents
   public Text itemTypeString;

   // Button that handles the deletion of the ingredient for the item
   public Button deleteButton;

   // Reference to the monster panel
   public MonsterDataPanel monsterPanel;

   // Current category of the item
   public Item.Category currentCategory;

   // Current type of the item
   public int currentType;

   // The icon of the item
   public Image itemIcon;

   // Change ratio slider
   public Slider chanceRatio;

   // Percentage indicator of drop ratio
   public Text percentageText;

   #endregion

   public void initializeSetup () {
      deleteButton.onClick.AddListener(() => deleteData());
      changeItemTypeButton.onClick.AddListener(() => popupSelectionPanel());
      changeItemCategoryButton.onClick.AddListener(() => popupSelectionPanel());
   }

   private void popupSelectionPanel () {
      monsterPanel.popupChoices();
      monsterPanel.currentLootTemplate = this;
      monsterPanel.confirmItemButton.onClick.AddListener(() => {
         currentType = monsterPanel.itemTypeIDSelected;
         currentCategory = monsterPanel.selectedCategory;
         updateDisplayName();
      });
   }

   public void updateDisplayName () {
      if (currentCategory == Item.Category.None || currentType == 0) {
         itemCategoryString.text = "(Select)";
         itemTypeString.text = "(Select)";
      } else {
         itemCategoryString.text = currentCategory.ToString();
         itemTypeString.text = Util.getItemName(currentCategory, currentType).ToString();
      }
      itemIcon.sprite = Util.getRawSpriteIcon(currentCategory, currentType);
   }

   private void deleteData () {
      MonsterLootRow row = monsterPanel.monsterLootList.Find(_ => _ == this);
      monsterPanel.monsterLootList.Remove(row);

      Destroy(gameObject);
   }

   public void dropPercentageUpdate () {
      percentageText.text = chanceRatio.value.ToString("f2")+"%";
   }

   #region Private Variables

   #endregion
}
