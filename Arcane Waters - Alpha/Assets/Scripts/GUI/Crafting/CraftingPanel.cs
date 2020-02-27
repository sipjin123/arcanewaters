﻿using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

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

   // The prefab we use for creating ingredient cells
   public ItemCellIngredient ingredientCellPrefab;

   // The prefab we use for creating item cells
   public ItemCell itemCellPrefab;

   // The name of the result item
   public Text itemNameText;

   // The description of the result item
   public Text descriptionText;

   // The stat cells for each element
   public CraftingStatColumn physicalStatColumn;
   public CraftingStatColumn fireStatColumn;
   public CraftingStatColumn earthStatColumn;
   public CraftingStatColumn airStatColumn;
   public CraftingStatColumn waterStatColumn;

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

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;
   }

   public void refreshBlueprintList () {
      Global.player.rpc.Cmd_RequestBlueprintsFromServer(_currentPage, ROWS_PER_PAGE);
   }

   public void displayBlueprint (int itemId) {
      Global.player.rpc.Cmd_RequestSingleBlueprintFromServer(itemId);
   }

   public void refreshCurrentlySelectedBlueprint () {
      Global.player.rpc.Cmd_RequestSingleBlueprintFromServer(_selectedBlueprintId);
   }

   public void clearSelectedBlueprint () {
      _selectedBlueprintId = -1;
      configurePanelForMode(Mode.NoBlueprintSelected);
   }

   public void updatePanelWithBlueprintList (Item[] blueprintArray, Blueprint.Status[] blueprintStatusesArray, int pageNumber, int totalBlueprintCount) {
      // Update the current page number
      _currentPage = pageNumber;

      // Calculate the maximum page number
      _maxPage = Mathf.CeilToInt((float) totalBlueprintCount / ROWS_PER_PAGE);
      if (_maxPage == 0) {
         _maxPage = 1;
      }

      // Update the current page text
      pageNumberText.text = "Page " + _currentPage.ToString() + " of " + _maxPage.ToString();

      // Update the navigation buttons
      updateNavigationButtons();

      // Clear out any items in the list
      blueprintRowsContainer.DestroyChildren();

      // Create the blueprint rows
      for (int i = 0; i < blueprintArray.Length; i++) {
         Blueprint blueprint = (Blueprint)(blueprintArray[i].getCastItem());

         // Instantiates the row
         BlueprintRow row = Instantiate(blueprintRowPrefab, blueprintRowsContainer.transform, false);

         // Initializes the row
         row.setRowForBlueprint(blueprintArray[i], blueprint, _selectedBlueprintId == blueprint.id, blueprintStatusesArray[i]);
      }

      // Update the craft button
      updateCraftButton();

      // Set the panel mode if it has not been initialized yet
      if (_currentMode == Mode.None) {
         configurePanelForMode(Mode.NoBlueprintSelected);
      }
   }
   
   public void updatePanelWithSingleBlueprint (Item blueprint, Item[] equippedItems,
      List<Item> inventoryIngredients, List<Item> requiredIngredients, Item resultItem) {
      _selectedBlueprintId = blueprint.id;

      // Configure the panel
      configurePanelForMode(Mode.BlueprintSelected);

      // Keep track of the equipped weapon and armor
      _equippedArmor = null;
      _equippedWeapon = null;
      foreach (Item equippedItem in equippedItems) {
         Item castedEquippedItem = equippedItem.getCastItem();
         if (castedEquippedItem is Weapon) {
            _equippedWeapon = (Weapon) castedEquippedItem;
         } else if (castedEquippedItem is Armor) {
            _equippedArmor = (Armor) castedEquippedItem;
         }
      }
      
      // Clear any previous result item
      resultItemContainer.DestroyChildren();

      // Instantiate a cell for the result item
      ItemCell cell = Instantiate(itemCellPrefab, resultItemContainer.transform, false);
      blueprint.category = resultItem.category;

      // Initialize the cell
      cell.setCellForItem(blueprint);

      // Set the cell click events
      cell.leftClickEvent.RemoveAllListeners();
      cell.rightClickEvent.RemoveAllListeners();
      cell.doubleClickEvent.RemoveAllListeners();

      // Set the result item name
      itemNameText.text = blueprint.itemName;

      // Set the result item description
      descriptionText.text = blueprint.itemDescription;

      // Clear any existing statistic
      physicalStatColumn.clear();
      fireStatColumn.clear();
      earthStatColumn.clear();
      airStatColumn.clear();
      waterStatColumn.clear();

      // Set the result item statistics
      if (resultItem is Weapon) {
         Weapon weapon = (Weapon) resultItem;

         physicalStatColumn.setColumnForWeapon(weapon, _equippedWeapon);
         fireStatColumn.setColumnForWeapon(weapon, _equippedWeapon);
         earthStatColumn.setColumnForWeapon(weapon, _equippedWeapon);
         airStatColumn.setColumnForWeapon(weapon, _equippedWeapon);
         waterStatColumn.setColumnForWeapon(weapon, _equippedWeapon);
      } else if (resultItem is Armor) {
         Armor armor = (Armor) resultItem;

         physicalStatColumn.setColumnForArmor(armor, _equippedArmor);
         fireStatColumn.setColumnForArmor(armor, _equippedArmor);
         earthStatColumn.setColumnForArmor(armor, _equippedArmor);
         airStatColumn.setColumnForArmor(armor, _equippedArmor);
         waterStatColumn.setColumnForArmor(armor, _equippedArmor);
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

      // Refresh the blueprint list
      refreshBlueprintList();
   }

   public void craft () {
      if (_selectedBlueprintId != -1) {
         Global.player.rpc.Cmd_CraftItem(_selectedBlueprintId);
      }
   }

   public void updateCraftButton () {
      if (_selectedBlueprintId == -1 || !_canSelectedBlueprintBeCrafted) {
         craftButton.interactable = false;
      } else {
         craftButton.interactable = true;
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

   public void nextPage () {
      if (_currentPage < _maxPage) {
         _currentPage++;
         refreshBlueprintList();
      }
   }

   public void previousPage () {
      if (_currentPage > 1) {
         _currentPage--;
         refreshBlueprintList();
      }
   }

   private void updateNavigationButtons () {
      // Activate or deactivate the navigation buttons if we reached a limit
      previousPageButton.enabled = true;
      nextPageButton.enabled = true;

      if (_currentPage <= 1) {
         previousPageButton.enabled = false;
      }

      if (_currentPage >= _maxPage) {
         nextPageButton.enabled = false;
      }
   }

   #region Private Variables

   // The weapon currently equipped by the player
   private Weapon _equippedWeapon;

   // The armor currently equipped by the player
   private Armor _equippedArmor;

   // The index of the current page
   private int _currentPage = 1;

   // The maximum page index (starting at 1)
   private int _maxPage = 1;

   // The item id of the currently displayed blueprint
   private int _selectedBlueprintId = -1;

   // Gets set to true when the item can be crafted
   private bool _canSelectedBlueprintBeCrafted = false;

   // The current panel mode
   private Mode _currentMode = Mode.None;

   #endregion
}
