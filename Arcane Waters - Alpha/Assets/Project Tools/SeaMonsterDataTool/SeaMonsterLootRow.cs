using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SeaMonsterLootRow : GenericItemRow
{
   #region Public Variables

   // Displays the item requirement
   public InputField itemCount;

   // Reference to the monster panel
   public SeaMonsterDataPanel monsterPanel;

   // Current category of the item
   public Item.Category currentCategory;

   // Current type of the item
   public int currentType;

   // The item data of the loot
   public string itemData;

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
      updateTypeButton.onClick.AddListener(() => popupSelectionPanel());
      updateCategoryButton.onClick.AddListener(() => popupSelectionPanel());
   }

   private void popupSelectionPanel () {
      monsterPanel.popupChoices();
      monsterPanel.currentLootTemplate = this;
      monsterPanel.confirmItemButton.onClick.AddListener(() => {
         currentType = monsterPanel.itemTypeIDSelected;
         currentCategory = monsterPanel.selectedCategory;
         itemData = monsterPanel.itemDataSelected;
         updateDisplayName();
      });
   }

   public void updateDisplayName () {
      modifyContent(currentCategory, currentType, itemData);
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
