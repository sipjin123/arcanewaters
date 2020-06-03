using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Linq;

public class CraftingIngredientPanel : MonoBehaviour {
   #region Public Variables

   // References the tool manager
   public CraftingToolManager craftingToolManager;

   // Reference to the current XML Template being modified
   public CraftableItemTemplate currentXMLTemplate;

   // Templates that store an individual item ingredient
   public ItemRequirementRow ingredientTemplate;

   // Parent that stores each ingredient template
   public Transform ingredientTemplateParent;

   // Button for adding new ingredient
   public Button addButton;

   // Button for updating the new data
   public Button saveExitButton;

   // Cached info of the current data being modified
   public CraftableItemRequirements currentCombinationData;

   // A list compilation for the items rows generated
   public List<ItemRequirementRow> itemRowList = new List<ItemRequirementRow>();

   // Buttons that pops up the item selection panel
   public Button changeItemCategoryButton;
   public Button changeItemTypeButton;

   // Displays the current item category selected
   public Text resultItemCategoryDisplay;

   // Displays the current item type selected
   public Text resultItemTypeDisplay;

   // Button that closes the whole edit panel
   public Button closeEntirePanelButton;

   // Button that closes the item category selection
   public Button closePopupButton;

   // Used for the generation of the Category/ItemType popup
   public GameObject popUpSelectionPanel;
   public Transform categoryButtonContainer;
   public GameObject categoryButtonsPrefab;
   public Transform typeButtonContainer;
   public GameObject typeButtonsPrefab;
   public Button confirmSelectionButton;

   // Cached int of current item type
   public int resultItemTypeInt;

   // Cached item type selected in the popup
   public int selectedTypeID;

   // Cached item category selected in the popup
   public Item.Category selectedCategory;

   // The icon of the result item
   public Image resultItemIcon;

   // Determines if the template is a new entry
   private bool emptyContent;

   // Current xml id being used
   public int xmlID;

   #endregion

   private void Awake () {
      saveExitButton.onClick.AddListener(() => saveExit());
      addButton.onClick.AddListener(() => addIngredient());
      closeEntirePanelButton.onClick.AddListener(() => closeEntirePanel());

      changeItemCategoryButton.onClick.AddListener(() => popupChoices());
      changeItemTypeButton.onClick.AddListener(() => popupChoices());

      closePopupButton.onClick.AddListener(() => {
         popUpSelectionPanel.SetActive(false);
      });

      resultItemCategoryDisplay.text = Item.Category.None.ToString();

      if (!MasterToolAccountManager.canAlterData()) {
         saveExitButton.gameObject.SetActive(false);
      }
   }

   private void closeEntirePanel() {
      updateRootTemplate();
      craftingToolManager.loadAllDataFiles();
      gameObject.SetActive(false);
   }

   public void updateData() {
      CraftableItemRequirements craftableRequirements = new CraftableItemRequirements();
      List<Item> cacheItemList = new List<Item>();

      Item.Category resultCategory = (Item.Category) Enum.Parse(typeof(Item.Category), resultItemCategoryDisplay.text);
      int resultType = resultItemTypeInt;
      Item resultItem = getItem(resultCategory, resultType, 1);
      craftableRequirements.resultItem = resultItem;

      CraftingToolScene.updateThisIcon(resultItemIcon, resultCategory, resultType);

      foreach (ItemRequirementRow item in itemRowList) {
         Item newItem = getItem(item.currentCategory, item.currentType, int.Parse(item.itemCount.text));
         cacheItemList.Add(newItem);
      }
      craftableRequirements.combinationRequirements = cacheItemList.ToArray();

      currentCombinationData = craftableRequirements;
      updateRootTemplate();
   }

   public void updateMainItemDisplay() {
      resultItemCategoryDisplay.text = selectedCategory.ToString();
      resultItemTypeDisplay.text = Util.getItemName(selectedCategory, selectedTypeID).ToString();
      resultItemTypeInt = selectedTypeID;
      popUpSelectionPanel.SetActive(false);

      updateData();

      selectedCategory = Item.Category.None;
      selectedTypeID = 0;
   }

   public void popupChoices() {
      popUpSelectionPanel.SetActive(true);
      confirmSelectionButton.onClick.RemoveAllListeners();
      confirmSelectionButton.onClick.AddListener(() => updateMainItemDisplay());
      categoryButtonContainer.gameObject.DestroyChildren();

      foreach (Item.Category category in Enum.GetValues(typeof(Item.Category))) {
         GameObject template = Instantiate(categoryButtonsPrefab, categoryButtonContainer);
         ItemCategoryTemplate categoryTemp = template.GetComponent<ItemCategoryTemplate>();
         categoryTemp.itemCategoryText.text = category.ToString();
         categoryTemp.itemIndexText.text = ((int)category).ToString();
         categoryTemp.itemCategory = category;

         categoryTemp.selectButton.onClick.AddListener(() => {
            selectedCategory = category;
            updateTypeOptions();
         });
         template.SetActive(true);
      }
      updateTypeOptions();
   }

   private void updateTypeOptions () {
      // Dynamically handles the type of item
      Dictionary<int, string> itemNameList = new Dictionary<int, string>();
      typeButtonContainer.gameObject.DestroyChildren();

      if (selectedCategory == Item.Category.Armor) {
         foreach (ArmorStatData armorStat in EquipmentXMLManager.self.armorStatList) {
            int newVal = armorStat.equipmentID;
            if (!itemNameList.ContainsKey(newVal)) {
               itemNameList.Add(newVal, armorStat.equipmentName.ToString());
            } 
         }
      } else if (selectedCategory == Item.Category.Hats) {
         foreach (HatStatData hatStat in EquipmentXMLManager.self.hatStatList) {
            int newVal = (int) hatStat.hatType;
            if (!itemNameList.ContainsKey(newVal)) {
               itemNameList.Add(newVal, hatStat.equipmentName.ToString());
            }
         }
      } else if (selectedCategory == Item.Category.Weapon) {
         foreach (WeaponStatData weaponStat in EquipmentXMLManager.self.weaponStatList) {
            int newVal = weaponStat.equipmentID;
            if (!itemNameList.ContainsKey(newVal)) {
               itemNameList.Add(newVal, weaponStat.equipmentName.ToString());
            }
         }
      } else {
         Type itemType = Util.getItemType(selectedCategory);

         if (itemType != null) {
            foreach (object item in Enum.GetValues(itemType)) {
               int newVal = (int) item;
               itemNameList.Add(newVal, item.ToString());
            }
         }
      }

      if (itemNameList.Count > 0) {
         var sortedList = itemNameList.OrderBy(r => r.Value);
         foreach (var item in sortedList) {
            GameObject template = Instantiate(typeButtonsPrefab, typeButtonContainer);
            ItemTypeTemplate itemTemp = template.GetComponent<ItemTypeTemplate>();
            itemTemp.itemTypeText.text = item.Value.ToString();
            itemTemp.itemIndexText.text = "" + item.Key;

            if (selectedCategory == Item.Category.Hats) {
               string imagePath = EquipmentXMLManager.self.getHatData(item.Key).equipmentIconPath;
               itemTemp.spriteIcon.sprite = ImageManager.getSprite(imagePath);
            } else if (selectedCategory == Item.Category.Armor) {
               string imagePath = EquipmentXMLManager.self.getArmorData(item.Key).equipmentIconPath;
               itemTemp.spriteIcon.sprite = ImageManager.getSprite(imagePath);
            } else if (selectedCategory == Item.Category.Weapon) {
               string imagePath = EquipmentXMLManager.self.getWeaponData(item.Key).equipmentIconPath;
               itemTemp.spriteIcon.sprite = ImageManager.getSprite(imagePath);
            } else {
               itemTemp.spriteIcon.sprite = Util.getRawSpriteIcon(selectedCategory, item.Key);
            }

            itemTemp.selectButton.onClick.AddListener(() => {
               selectedTypeID = (int) item.Key;
               confirmSelectionButton.onClick.Invoke();
               confirmSelectionButton.onClick.RemoveAllListeners();
               itemTemp.selectButton.onClick.RemoveAllListeners();
            });
         }
      }
   }

   private void addIngredient() {
      itemRowList.Add(itemRequirementRow());
   }

   private ItemRequirementRow itemRequirementRow() {
      ItemRequirementRow itemRow = Instantiate(ingredientTemplate, ingredientTemplateParent);
      itemRow.itemCount.text = "1";
      itemRow.initializeSetup();
      itemRow.updateDisplayName();

      itemRow.gameObject.SetActive(true);

      return itemRow;
   }

   private Item getItem(Item.Category categ, int typeID, int count) {
      Item resultItem = new Item(0, categ, typeID, count, "", "", "");
      return resultItem;
   }

   private void saveExit() {
      if (currentCombinationData != null) {
         updateRootTemplate();
         updateData();
         craftingToolManager.saveDataToFile(currentCombinationData, xmlID);
      }
      gameObject.SetActive(false);
   }

   private void updateRootTemplate() {
      if (currentCombinationData.resultItem.category != Item.Category.None && currentCombinationData.resultItem.itemTypeId != 0) {
         currentXMLTemplate.updateItemDisplay(currentCombinationData.resultItem);
      } 
   }

   public void setData(CraftableItemRequirements currData, int xmlID) {
      emptyContent = currData.resultItem.category == Item.Category.None;
      this.xmlID = xmlID;

      ingredientTemplateParent.gameObject.DestroyChildren();
      itemRowList = new List<ItemRequirementRow>();

      if (currData.combinationRequirements != null) {
         foreach (Item requiredItem in currData.combinationRequirements) {
            ItemRequirementRow itemRow = itemRequirementRow();
            itemRow.currentCategory = requiredItem.category;
            itemRow.currentType = requiredItem.itemTypeId;
            itemRow.itemCount.text = requiredItem.count.ToString();
            itemRow.updateDisplayName();

            itemRowList.Add(itemRow);
         }
      }

      if (currData.resultItem != null) {
         selectedCategory = currData.resultItem.category;
         selectedTypeID = currData.resultItem.itemTypeId;
         updateMainItemDisplay();
      }

      currentCombinationData = currData;
   }

   #region Private Variables
      
   #endregion
}
