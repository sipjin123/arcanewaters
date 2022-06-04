﻿using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Text;
using NubisDataHandling;
using UnityEngine.EventSystems;

public class ItemSelectionScreen : SubPanel
{
   #region Public Variables

   // The number of items per page
   public static int ITEMS_PER_PAGE = 20;

   // The currently selected itemId
   public static Item selectedItem;

   // The number of selected items
   public static int selectedItemCount = 1;

   // Load Blocker when data is fetching
   public GameObject loadBlocker;

   // The grid container for the item icons
   public GameObject itemCellsContainer;

   // The prefab we use for creating item cells
   public ItemCell itemCellPrefab;

   // The page number text
   public Text pageNumberText;

   // The next page button
   public Button nextPageButton;

   // The previous page button
   public Button previousPageButton;

   // The slider allowing to set the number of items
   public Slider itemCountSlider;

   // The text showing the item count value
   public Text itemCountText;

   // The confirm button
   public Button selectButton;

   // The cancel button
   public Button cancelButton;

   #endregion

   public override void show () {
      List<int> itemIdsToExclude = new List<int>();
      show(itemIdsToExclude);
   }

   public void show (List<int> itemIdsToExclude, Func<Item, string> descriptionTextHandler = null) {
      List<Item.Category> categoryFilter = new List<Item.Category>();
      show(itemIdsToExclude, categoryFilter, descriptionTextHandler);
   }

   public void show (List<int> itemIdsToExclude, List<Item.Category> categoryFilter, Func<Item, string> descriptionTextHandler = null) {
      // Handle description text
      _descriptionTextHandler = descriptionTextHandler;
      foreach (GameObject ob in _descriptionRowObjects) {
         ob.SetActive(_descriptionTextHandler != null);
      }
      if (_descriptionTextHandler != null) {
         _descriptionText.text = _descriptionTextHandler(null);
      }

      // Set the filters to be used every time the items are listed
      _categoryFilter = categoryFilter;
      _itemIdsToExclude = itemIdsToExclude;
      _currentPage = 0;

      // Clear out any current items
      itemCellsContainer.DestroyChildren();
      _cells.Clear();

      // Clear the selected item
      clearSelectedItem();

      // Removes the page number text
      pageNumberText.text = "";

      // Request the data from the server
      refreshPanel();

      // Make the panel visible
      base.show();
   }

   public void refreshPanel () {
      loadBlocker.SetActive(true);
      NubisDataFetcher.self.getInventoryForItemSelection(_categoryFilter, _itemIdsToExclude, _currentPage, ITEMS_PER_PAGE);
   }

   public void receiveItemsFromServer (List<Item> itemList, int pageNumber, int totalItemCount) {
      setLoadBlocker(false);

      // Update the current page number
      _currentPage = pageNumber;

      // Calculate the maximum page number
      _maxPage = Mathf.CeilToInt((float) totalItemCount / ITEMS_PER_PAGE);
      //if (_maxPage == 0) {
      //   _maxPage = 1;
      //}

      // Update the current page text
      pageNumberText.text = "Page " + (_currentPage + 1).ToString() + " of " + _maxPage.ToString();

      // Update the navigation buttons
      updateNavigationButtons();

      // Clear the selected item
      clearSelectedItem();

      // Clear out any current items
      itemCellsContainer.DestroyChildren();
      _cells.Clear();

      // Create the item cells
      foreach (Item item in itemList) {
         // Get the casted item
         Item castedItem = item.getCastItem();

         // Instantiates the cell
         ItemCell cell = Instantiate(itemCellPrefab, itemCellsContainer.transform, false);
         cell.transform.SetParent(itemCellsContainer.transform, false);

         // Initializes the cell
         cell.setCellForItem(castedItem);

         // Set the cell click events
         cell.leftClickEvent.RemoveAllListeners();
         cell.rightClickEvent.RemoveAllListeners();
         cell.doubleClickEvent.RemoveAllListeners();
         if (!cell.isDisabledItem) {
            cell.leftClickEvent.AddListener(() => setSelectedItem(cell));
         }

         // Saves the cell in a list for easy access
         _cells.Add(cell);
      }
   }

   public void setSelectedItem (ItemCell selectedCell) {
      selectedItem = selectedCell.itemCache;

      // Clear the previous selected item
      clearSelectedItem();

      // Activate the selection box in the selected cell
      selectedCell.showSelectedBox();

      // Set the item count slider to 1 or 0
      itemCountSlider.maxValue = selectedItem.count;
      itemCountSlider.value = Mathf.Min(1, selectedItem.count);

      // This is a small hack to make sure the slider's handle is on the right side when there's only 1 item available
      itemCountSlider.direction = itemCountSlider.minValue == itemCountSlider.maxValue
         ? Slider.Direction.RightToLeft
         : Slider.Direction.LeftToRight;

      updateSelectedItemCount();

      // Activate the select button
      selectButton.interactable = true;
   }

   public void updateSelectedItemCount () {
      selectedItemCount = (int) itemCountSlider.value;
      itemCountText.text = selectedItemCount.ToString();

      if (_descriptionTextHandler != null) {
         if (selectedItem == null) {
            _descriptionText.text = _descriptionTextHandler(null);
         } else {
            Item i = selectedItem.Clone().getCastItem();
            i.count = selectedItemCount;
            _descriptionText.text = _descriptionTextHandler(i);
         }
      }
   }

   public void nextPage () {
      if (_currentPage < _maxPage - 1) {
         _currentPage++;
         refreshPanel();
      }
   }

   public void previousPage () {
      if (_currentPage > 0) {
         _currentPage--;
         refreshPanel();
      }
   }

   private void updateNavigationButtons () {
      // Activate or deactivate the navigation buttons if we reached a limit
      previousPageButton.enabled = true;
      nextPageButton.enabled = true;

      if (_currentPage <= 0) {
         previousPageButton.enabled = false;
      }

      if (_currentPage >= _maxPage - 1) {
         nextPageButton.enabled = false;
      }
   }

   public void setLoadBlocker (bool isOn) {
      loadBlocker.SetActive(isOn);
   }

   private void clearSelectedItem () {
      // Set the values of the item count slider
      itemCountSlider.maxValue = 1;
      itemCountSlider.value = 1;
      updateSelectedItemCount();

      // Deactivate the selection box in all cells
      foreach (ItemCell cell in _cells) {
         cell.hideSelectedBox();
      }

      // Disables the select button
      selectButton.interactable = false;
   }

   #region Private Variables

   // The index of the current page
   private int _currentPage = 0;

   // The maximum page index (starting at 1)
   private int _maxPage = 1;

   // The category used to filter the displayed items
   private List<Item.Category> _categoryFilter = new List<Item.Category>();

   // The list of items that must not be displayed in the item selection
   private List<int> _itemIdsToExclude = new List<int>();

   // The list of item cells
   private List<ItemCell> _cells = new List<ItemCell>();

   // Items we activate for description row
   [SerializeField]
   private List<GameObject> _descriptionRowObjects = new List<GameObject>();

   // Description text
   [SerializeField]
   private Text _descriptionText = null;

   // Logic for updating description text
   private Func<Item, string> _descriptionTextHandler = null;

   #endregion
}