﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaMonsterLootRow : MonoBehaviour
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
   public SeaMonsterDataPanel monsterPanel;

   // Current category of the item
   public Item.Category currentCategory;

   // Current type of the item
   public int currentType;

   // The icon of the item
   public Image itemIcon;

   // Change ratio slider
   public Slider chanceRatio;

   // Percentage indicator of drop ratio
   public InputField percentageField;
   public Text percentageText;

   #endregion

   public void initializeSetup () {
      if (chanceRatio != null) {
         chanceRatio.onValueChanged.AddListener(_ => updateSlider());
         chanceRatio.onValueChanged.Invoke(chanceRatio.value);
         percentageField.onValueChanged.AddListener(_ => { updateInputField(); });
      }
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
         if (currentCategory == Item.Category.Blueprint) {
            itemCategoryString.text = Item.Category.Blueprint.ToString();
            Item.Category modifiedCategory = Item.Category.None;

            int modifiedID = 0;
            if (currentType.ToString().StartsWith(Blueprint.WEAPON_PREFIX)) {
               modifiedID = int.Parse(currentType.ToString().Replace(Blueprint.WEAPON_PREFIX, ""));
               modifiedCategory = Item.Category.Weapon;
            } else {
               modifiedID = int.Parse(currentType.ToString().Replace(Blueprint.ARMOR_PREFIX, ""));
               modifiedCategory = Item.Category.Armor;
            }

            itemCategoryString.text = currentCategory.ToString();
            itemTypeString.text = Util.getItemName(modifiedCategory, modifiedID).ToString();
            itemIcon.sprite = Util.getRawSpriteIcon(modifiedCategory, modifiedID);
         } else {
            itemCategoryString.text = currentCategory.ToString();
            itemTypeString.text = Util.getItemName(currentCategory, currentType).ToString();
            itemIcon.sprite = Util.getRawSpriteIcon(currentCategory, currentType);
         }
      }
   }

   private void deleteData () {
      SeaMonsterLootRow row = monsterPanel.monsterLootList.Find(_ => _ == this);
      monsterPanel.monsterLootList.Remove(row);

      Destroy(gameObject);
   }

   private void updateSlider () {
      percentageText.text = chanceRatio.value.ToString("f2") + "%";
      percentageField.onValueChanged.RemoveAllListeners();
      percentageField.text = percentageText.text.Replace("@","");
      percentageField.onValueChanged.AddListener(_ => { updateInputField(); });
   }

   private void updateInputField () {
      percentageText.text = percentageField.text +"%";
      chanceRatio.onValueChanged.RemoveAllListeners();
      chanceRatio.value = float.Parse(percentageField.text);
      chanceRatio.onValueChanged.AddListener(_ => updateSlider());
   }

   #region Private Variables

   #endregion
}
