﻿using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
using NubisDataHandling;
using UnityEngine.Events;

public class CraftingPanel : Panel
{
   #region Public Variables

   // The number of blueprint rows to display per page
   public static int ROWS_PER_PAGE = 9;

   // The mode of the panel
   public enum Mode { None = 0, NoBlueprintSelected = 1, BlueprintSelected = 2 }

   // The section displayed when no blueprint is selected
   public GameObject noBlueprintSelectedSection;

   // The section displayed when a blueprint is selected
   public GameObject blueprintSelectedSection;

   // The container for the blueprint rows
   public GameObject blueprintRowsContainer;

   // The container for the ingredient item cells
   public GameObject ingredientCellsContainer;

   // The container for the result item cell
   public GameObject resultItemContainer;

   // The prefab we use for creating blueprint rows
   public BlueprintRow blueprintRowPrefab;

   // List of created blueprint rows
   public List<BlueprintRow> blueprintRowList;

   // The prefab we use for creating ingredient cells
   public ItemCellIngredient ingredientCellPrefab;

   // The blockers that will popup when fetching crafting data
   public GameObject loadBlockerList, loadBlockerContent, loadBlockerIngredients;

   // The blockers that will popup when fetching refinement data
   public GameObject loadBlockerRefinementList, loadBlockerRefinementIngredients;

   // The prefab we use for creating item cells
   public ItemCell itemCellPrefab;

   // The name of the result item
   public Text itemNameText, refinementItemNameText;

   // The description of the result item
   public Text descriptionText, refinementDescriptionText;

   // The stat cells for each element
   public CraftingStatColumn physicalStatColumn;
   public CraftingStatColumn fireStatColumn;
   public CraftingStatColumn earthStatColumn;
   public CraftingStatColumn airStatColumn;
   public CraftingStatColumn waterStatColumn;

   // The refine button
   public Button refineButton;

   // The craft button
   public Button craftButton;

   // The page number text
   public Text pageNumberText;

   // The next page button
   public Button nextPageButton;

   // The previous page button
   public Button previousPageButton;

   // Self
   public static CraftingPanel self;

   // The buttons triggering the crafting or refinement
   public Button refinementTabButton, craftingTabButon;

   // Title text
   public Text titleText;

   // The different panel entities
   public GameObject[] refinementPanelEntities, craftingPanelEntities;

   // The max number of refinement items per page
   public const int REFINEMENT_ITEM_PAGE_MAX = 28;

   // Text displaying durability level of an item
   public Text durabilityText;

   // Parent that hold the items that can be refined
   public Transform refineableItemsHolder;

   // The parent holding the selected refine-able item
   public Transform refineAbleItemSelection;

   // Cached refineable item cell
   public ItemCell latestRefineableItem;

   // Parent holding the ingredient list needed for the refinement
   public Transform refinementIngredientsHolder;

   // An event triggered when receiving refinement data
   public UnityEvent receiveRefinementData = new UnityEvent();

   // Tab types
   public enum TabType
   {
      // None
      None = 0,

      // Crafting
      Crafting = 1,

      // Refinement
      Refinement = 2
   }

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;
   }

   #region Common

   public void clearContent () {
      toggleBlockers(true);
      blueprintRowsContainer.DestroyChildren();
      resultItemContainer.DestroyChildren();
      ingredientCellsContainer.DestroyChildren();

      physicalStatColumn.clear();
      fireStatColumn.clear();
      earthStatColumn.clear();
      airStatColumn.clear();
      waterStatColumn.clear();

      descriptionText.text = "";
      refinementDescriptionText.text = "";
      itemNameText.text = "";
      refinementItemNameText.text = "";
      durabilityText.text = "";
   }

   public void toggleBlockers (bool isActive) {
      nextPageButton.interactable = !isActive;
      previousPageButton.interactable = !isActive;
      loadBlockerList.SetActive(isActive);
      loadBlockerContent.SetActive(isActive);
      loadBlockerIngredients.SetActive(isActive);
   }

   public void nextPage () {
      if (_currentPageIndex >= _lastPageIndex) {
         return;
      }

      _currentPageIndex++;

      if (craftingTabButon.gameObject.activeInHierarchy) {
         refreshBlueprintList();
      } else {
         refreshRefinementList();
      }
   }

   public void previousPage () {
      if (_currentPageIndex <= 0) {
         return;
      }

      _currentPageIndex--;

      if (craftingTabButon.gameObject.activeInHierarchy) {
         refreshBlueprintList();
      } else {
         refreshRefinementList();
      }
   }

   private void configurePanelForMode (Mode mode) {
      switch (mode) {
         case Mode.NoBlueprintSelected:
            noBlueprintSelectedSection.SetActive(true);
            blueprintSelectedSection.SetActive(false);
            break;
         case Mode.BlueprintSelected:
            noBlueprintSelectedSection.SetActive(false);
            blueprintSelectedSection.SetActive(true);
            break;
         default:
            break;
      }
      _currentMode = mode;
   }

   private void updateNavigationButtons () {
      // Activate or deactivate the navigation buttons if we reached a limit
      previousPageButton.interactable = true;
      nextPageButton.interactable = true;

      if (_currentPageIndex <= 0) {
         previousPageButton.interactable = false;
      }

      if (_currentPageIndex >= _lastPageIndex) {
         nextPageButton.interactable = false;
      }
   }

   #endregion

   #region Crafting

   public void craft () {
      if (_selectedBlueprintId != -1) {
         toggleBlockers(true);
         Global.player.rpc.Cmd_CraftItem(_selectedBlueprintId);
      }
   }

   public void refreshBlueprintList () {
      selectCraftingTab();
      toggleBlockers(true);
      NubisDataFetcher.self.fetchCraftableData(_currentPageIndex, ROWS_PER_PAGE);
   }

   public void updateCraftButton () {
      if (_selectedBlueprintId == -1 || !_canSelectedBlueprintBeCrafted) {
         craftButton.interactable = false;
      } else {
         craftButton.interactable = true;
      }
   }

   public void selectCraftingTab () {
      refineableItemsHolder.gameObject.DestroyChildren();
      refinementIngredientsHolder.gameObject.DestroyChildren();
      refineAbleItemSelection.gameObject.DestroyChildren();

      if (_currentTab != TabType.Crafting) {
         _currentPageIndex = 0;
      }

      _currentTab = TabType.Crafting;
      craftingTabButon.interactable = false;
      refinementTabButton.interactable = true;
      titleText.text = "Crafting";

      foreach (GameObject ui in refinementPanelEntities) {
         ui.SetActive(false);
      }
      foreach (GameObject ui in craftingPanelEntities) {
         ui.SetActive(true);
      }
   }

   public void displayBlueprint (int itemId) {
      toggleBlockers(true);
      NubisDataFetcher.self.checkCraftingInfo(itemId);
   }

   public void refreshCurrentlySelectedBlueprint () {
      toggleBlockers(true);
      NubisDataFetcher.self.checkCraftingInfo(_selectedBlueprintId);
   }

   public void clearSelectedBlueprint () {
      _selectedBlueprintId = -1;
      configurePanelForMode(Mode.NoBlueprintSelected);
   }

   public void updatePanelWithBlueprintList (Item[] blueprintArray, Blueprint.Status[] blueprintStatusesArray, int pageNumber, int blueprintsPerPage) {
      selectCraftingTab();

      // Update the current page number
      _currentPageIndex = pageNumber;

      // Calculate the maximum page number
      _lastPageIndex = blueprintArray.Length / ROWS_PER_PAGE;

      // Update the current page text
      pageNumberText.text = "Page " + (_currentPageIndex + 1).ToString() + " of " + (_lastPageIndex + 1).ToString();

      // Clear out any items in the list
      blueprintRowsContainer.DestroyChildren();
      blueprintRowList = new List<BlueprintRow>();

      // Create the blueprint rows
      Item[] blueprintsInPage = Util.getArraySlice(blueprintArray, pageNumber, blueprintsPerPage);
      Blueprint.Status[] statusesOfBlueprintsInPage = Util.getArraySlice(blueprintStatusesArray, pageNumber, blueprintsPerPage);

      for (int i = 0; i < blueprintsInPage.Count(); i++) {
         if (i < ROWS_PER_PAGE) {
            // Instantiates the row
            BlueprintRow row = Instantiate(blueprintRowPrefab, blueprintRowsContainer.transform, false);

            // Initializes the row
            row.setRowForBlueprint(blueprintsInPage[i], _selectedBlueprintId == blueprintsInPage[i].id, statusesOfBlueprintsInPage[i]);
            blueprintRowList.Add(row);
         }
      }

      // Update the craft button
      updateCraftButton();

      // Set the panel mode if it has not been initialized yet
      if (_currentMode == Mode.None) {
         configurePanelForMode(Mode.NoBlueprintSelected);
      }

      toggleBlockers(false);

      // Update the navigation buttons
      updateNavigationButtons();
   }

   public void updatePanelWithSingleBlueprintWebRequest (Item resultItem, List<Item> equippedItems, List<Item> inventoryIngredients, Item[] requiredIngredients) {
      _selectedBlueprintId = resultItem.id;

      // Configure the panel
      configurePanelForMode(Mode.BlueprintSelected);

      // Disables highlighted templates
      foreach (BlueprintRow bpRow in blueprintRowList) {
         bpRow.highlightTemplate(false);
      }

      // Highlights the currently selected template
      BlueprintRow blueprintRow = blueprintRowList.Find(_ => _.blueprintItemId == _selectedBlueprintId);
      if (blueprintRow != null) {
         blueprintRow.highlightTemplate(true);
      }

      // Keep track of the equipped weapon and armor
      _equippedArmor = null;
      _equippedWeapon = null;
      _equippedHat = null;
      foreach (Item equippedItem in equippedItems) {
         if (equippedItem.category == Item.Category.Weapon) {
            _equippedWeapon = Weapon.castItemToWeapon(equippedItem);
         } else if (equippedItem.category == Item.Category.Armor) {
            _equippedArmor = Armor.castItemToArmor(equippedItem);
         } else if (equippedItem.category == Item.Category.Hats) {
            _equippedHat = Hat.castItemToHat(equippedItem);
         }
      }

      // Clear any previous result item
      resultItemContainer.DestroyChildren();

      // Instantiate a cell for the result item
      ItemCell cell = Instantiate(itemCellPrefab, resultItemContainer.transform, false);

      // Initialize the cell
      cell.setCellForItem(resultItem);

      // Set the cell click events
      cell.leftClickEvent.RemoveAllListeners();
      cell.rightClickEvent.RemoveAllListeners();
      cell.doubleClickEvent.RemoveAllListeners();

      // Set the result item name
      itemNameText.text = resultItem.itemName;

      // Set the result item description
      descriptionText.text = resultItem.itemDescription;

      // Clear any existing statistic
      physicalStatColumn.clear();
      fireStatColumn.clear();
      earthStatColumn.clear();
      airStatColumn.clear();
      waterStatColumn.clear();

      // Set the result item statistics
      if (resultItem.category == Item.Category.Weapon) {
         Weapon weapon = Weapon.castItemToWeapon(resultItem);

         physicalStatColumn.setColumnForWeapon(weapon, _equippedWeapon);
         fireStatColumn.setColumnForWeapon(weapon, _equippedWeapon);
         earthStatColumn.setColumnForWeapon(weapon, _equippedWeapon);
         airStatColumn.setColumnForWeapon(weapon, _equippedWeapon);
         waterStatColumn.setColumnForWeapon(weapon, _equippedWeapon);
      } else if (resultItem.category == Item.Category.Armor) {
         Armor armor = Armor.castItemToArmor(resultItem);

         physicalStatColumn.setColumnForArmor(armor, _equippedArmor);
         fireStatColumn.setColumnForArmor(armor, _equippedArmor);
         earthStatColumn.setColumnForArmor(armor, _equippedArmor);
         airStatColumn.setColumnForArmor(armor, _equippedArmor);
         waterStatColumn.setColumnForArmor(armor, _equippedArmor);
      } else if (resultItem.category == Item.Category.Hats) {
         Hat hat = Hat.castItemToHat(resultItem);

         physicalStatColumn.setColumnForHat(hat, _equippedHat);
         fireStatColumn.setColumnForHat(hat, _equippedHat);
         earthStatColumn.setColumnForHat(hat, _equippedHat);
         airStatColumn.setColumnForHat(hat, _equippedHat);
         waterStatColumn.setColumnForHat(hat, _equippedHat);
      }

      // Clear all existing ingredients
      ingredientCellsContainer.DestroyChildren();

      // Keep track if the currently selected blueprint can be crafted
      _canSelectedBlueprintBeCrafted = true;

      // Set the ingredients
      foreach (Item ingredient in requiredIngredients) {
         // Get the casted item
         Item requiredIngredient = ingredient.getCastItem();

         // Get the inventory ingredient, if there is any
         Item inventoryIngredient = inventoryIngredients.Find(s =>
            s.itemTypeId == requiredIngredient.itemTypeId);

         // Get the ingredient count present in the user inventory
         int inventoryCount = 0;
         if (inventoryIngredient != null) {
            inventoryCount = inventoryIngredient.count;
         }

         // Instantiate an item cell
         ItemCellIngredient ingredientCell = Instantiate(ingredientCellPrefab, ingredientCellsContainer.transform, false);

         // Initialize the cell
         ingredientCell.setCellForItem(requiredIngredient, inventoryCount, requiredIngredient.count);

         // Set the cell click events
         ingredientCell.leftClickEvent.RemoveAllListeners();
         ingredientCell.rightClickEvent.RemoveAllListeners();
         ingredientCell.doubleClickEvent.RemoveAllListeners();

         // If there are not enough ingredients, the item cannot be crafted
         if (inventoryCount < requiredIngredient.count) {
            _canSelectedBlueprintBeCrafted = false;
         }
      }

      // Update the craft button
      updateCraftButton();
      toggleBlockers(false);
   }

   #endregion

   #region Refinement

   public void refine () {
      D.adminLog("Attempt to refine item:: " +
         "ID: " + latestRefineableItem.itemCache.id + " " +
         "Category: " + latestRefineableItem.itemCache.category + " " +
         "Type: " + latestRefineableItem.itemCache.itemTypeId + " " +
         "Durability: " + latestRefineableItem.itemCache.durability, D.ADMIN_LOG_TYPE.Refine);

      Global.player.rpc.Cmd_RefineItem(latestRefineableItem.itemCache.id);
      loadBlockerRefinementList.SetActive(true);
      loadBlockerRefinementIngredients.SetActive(true);
   }

   public void refreshRefinementList () {
      loadBlockerRefinementList.SetActive(true);
      loadBlockerRefinementIngredients.SetActive(true);

      if (latestRefineableItem != null) {
         latestRefineableItem.hideSelectedBox();
      }
      refineAbleItemSelection.gameObject.DestroyChildren();
      refinementIngredientsHolder.gameObject.DestroyChildren();

      NubisDataFetcher.self.getUserInventory(new List<Item.Category> {
         Item.Category.Weapon, Item.Category.Armor, Item.Category.Hats, Item.Category.Ring, Item.Category.Necklace, Item.Category.Trinket
      }, _currentPageIndex, ROWS_PER_PAGE, Item.DurabilityFilter.ReducedDurability, Type.Craft);
   }

   public void selectRefinementTab () {
      loadBlockerRefinementList.SetActive(true);
      loadBlockerRefinementIngredients.SetActive(true);
      blueprintRowsContainer.DestroyChildren();

      if (_currentTab != TabType.Refinement) {
         _currentPageIndex = 0;
      }

      _currentTab = TabType.Refinement;
      refinementTabButton.interactable = false;
      craftingTabButon.interactable = true;
      titleText.text = "Repair";

      foreach (GameObject ui in refinementPanelEntities) {
         ui.SetActive(true);
      }
      foreach (GameObject ui in craftingPanelEntities) {
         ui.SetActive(false);
      }

      NubisDataFetcher.self.getUserInventory(new List<Item.Category> {
         Item.Category.Weapon, Item.Category.Armor, Item.Category.Hats, Item.Category.Ring, Item.Category.Necklace, Item.Category.Trinket
      }, _currentPageIndex, ROWS_PER_PAGE, Item.DurabilityFilter.ReducedDurability, Panel.Type.Craft);
   }

   public void receiveRefineableItems (List<Item> itemList, int currentPageIndex) {
      refineableItemsHolder.gameObject.DestroyChildren();
      loadBlockerRefinementList.SetActive(false);
      loadBlockerRefinementIngredients.SetActive(false);
      refineButton.interactable = false;
      durabilityText.text = "";

      foreach (Item item in itemList) {
         ItemCell itemCell = Instantiate(itemCellPrefab, refineableItemsHolder);
         itemCell.setCellForItem(item);
         itemCell.leftClickEvent.RemoveAllListeners();
         itemCell.leftClickEvent.AddListener(() => onRefineableItemClicked(item, itemCell));

         // Adjust Tooltip (disables the default tooltip in order to allow clicking on the internal icon)
         if (itemCell.TryGetComponent(out ToolTipComponent tooltip)) {
            tooltip.tooltipType = ToolTipComponent.Type.DynamicText;
            tooltip.tooltipPlacement = ToolTipComponent.TooltipPlacement.AboveUIElement;
            tooltip.message = item.category == Item.Category.Blueprint ? EquipmentXMLManager.self.getItemName(item) : item.getTooltip();
            tooltip.message += Item.isUsingEquipmentXML(item.category) ? "\nDurability = " + item.durability : "";
            
            // Disable the preset tooltip game object
            itemCell.tooltip.gameObject.SetActive(false);
         }
      }

      receiveRefinementData.Invoke();
      _currentPageIndex = currentPageIndex;
      _lastPageIndex = currentPageIndex;

      // Update the current page text
      pageNumberText.text = "Page " + (_currentPageIndex + 1).ToString() + " of " + (_lastPageIndex + 1).ToString();

      updateNavigationButtons();
   }

   public void onRefineableItemClicked (Item item, ItemCell itemCell) {
      loadBlockerRefinementIngredients.SetActive(true);

      // Remove highlight of the recent item selected
      if (latestRefineableItem) {
         latestRefineableItem.hideSelectedBox();
      }

      // Cache this new item cell as the latest item cell selected
      latestRefineableItem = itemCell;

      // Highlight item cell
      itemCell.showSelectedBox();

      // Generate item to the item preview panel
      refineAbleItemSelection.gameObject.DestroyChildren();
      ItemCell selectedItemCell = Instantiate(itemCellPrefab, refineAbleItemSelection);
      selectedItemCell.setCellForItem(item);
      refinementIngredientsHolder.gameObject.DestroyChildren();

      // Display item durability
      durabilityText.text = item.durability.ToString();

      // Set title and description
      refinementItemNameText.text = EquipmentXMLManager.self.getItemName(item);
      refinementDescriptionText.text = EquipmentXMLManager.self.getItemDescription(item);

      Global.player.rpc.Cmd_RequestRefinementRequirement(item.id);
   }

   public void receiveRefineRequirementsForItem (int xmlId, Item[] currentPlayerItems, Item itemToRefine) {
      // Display requirements here
      refinementIngredientsHolder.gameObject.DestroyChildren();
      loadBlockerRefinementIngredients.SetActive(false);
      bool hasSufficientRequirements = true;

      // TODO: Replace this hard coded id into the id that is set in the web tool
      foreach (Item requiredItem in CraftingManager.self.getRefinementData(xmlId).combinationRequirements) {
         ItemCell requiredItemCell = Instantiate(itemCellPrefab, refinementIngredientsHolder);
         Item currentPlayerItem = currentPlayerItems.ToList().Find(_ => _.category == requiredItem.category && _.itemTypeId == requiredItem.itemTypeId);
         requiredItemCell.setCellForItem(requiredItem);

         int currentPlayerInventory = currentPlayerItem == null ? 0 : currentPlayerItem.count;
         requiredItemCell.itemCountText.text = currentPlayerInventory.ToString() + "/" + requiredItem.count;
         if (currentPlayerInventory >= requiredItem.count) {
            requiredItemCell.itemCountText.color = Color.green;
         } else {
            requiredItemCell.itemCountText.color = Color.red;
            hasSufficientRequirements = false;
         }
      }

      loadBlockerRefinementIngredients.SetActive(false);
      refineButton.interactable = hasSufficientRequirements;
   }

   #endregion

   #region Private Variables

   // The hat currently equipped by the player
   private Hat _equippedHat;

   // The weapon currently equipped by the player
   private Weapon _equippedWeapon;

   // The armor currently equipped by the player
   private Armor _equippedArmor;

   // The index of the current page
   private int _currentPageIndex = 1;

   // The maximum page index (starting at 1)
   private int _lastPageIndex = 1;

   // The item id of the currently displayed blueprint
   private int _selectedBlueprintId = -1;

   // Gets set to true when the item can be crafted
   private bool _canSelectedBlueprintBeCrafted = false;

   // The current panel mode
   private Mode _currentMode = Mode.None;

   // The current panel tab
   private TabType _currentTab = TabType.Crafting;

   #endregion
}
