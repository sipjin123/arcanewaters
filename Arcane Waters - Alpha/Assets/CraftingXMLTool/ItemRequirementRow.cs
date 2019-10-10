using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ItemRequirementRow : MonoBehaviour {
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

   // Reference to the crafting ingredient
   public CraftingIngredientPanel ingredientPanel;

   // Current category of the item
   public Item.Category currentCategory;

   // Current type of the item
   public int currentType;

   #endregion

   public void initializeSetup () {
      deleteButton.onClick.AddListener(() => deleteData());
      changeItemTypeButton.onClick.AddListener(() => popupSelectionPanel());
      changeItemCategoryButton.onClick.AddListener(() => popupSelectionPanel());
   }

   private void popupSelectionPanel() {
      ingredientPanel.popupChoices();
      ingredientPanel.confirmSelectionButton.onClick.RemoveAllListeners();
      ingredientPanel.confirmSelectionButton.onClick.AddListener(() => {
         currentType = ingredientPanel.selectedTypeID;
         currentCategory = ingredientPanel.selectedCategory;
         updateDisplayName();
         ingredientPanel.updateData();
      });
   }

   public void updateDisplayName() {
      itemCategoryString.text = currentCategory.ToString();
      itemTypeString.text = Util.getItemName(currentCategory, currentType).ToString();
      ingredientPanel.popUpSelectionPanel.SetActive(false);
   }

   private void deleteData() {
      ItemRequirementRow row = ingredientPanel.itemRowList.Find(_ => _ == this);
      ingredientPanel.itemRowList.Remove(row);
      ingredientPanel.updateData();
      Destroy(gameObject);
   }

   #region Private Variables

   #endregion
}
