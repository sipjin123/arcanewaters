using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using TMPro;
using Mirror;
using System.Text;
using NubisDataHandling;

public class ItemSelectionScreen : MonoBehaviour
{
   #region Public Variables

   // The number of items per page
   public static int ITEMS_PER_PAGE = 20;

   // The currently selected itemId
   public static Item selectedItem;

   // The number of selected items
   public static int selectedItemCount = 1;

   // Our associated Canvas Group
   public CanvasGroup canvasGroup;

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

   public void show () {
      List<int> itemIdsToExclude = new List<int>();
      show(itemIdsToExclude);
   }

   public void show (List<int> itemIdsToExclude) {
      List<Item.Category> categoryFilter = new List<Item.Category>();
      show(itemIdsToExclude, categoryFilter);
   }

   public void show (List<int> itemIdsToExclude, List<Item.Category> categoryFilter) {
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
      this.canvasGroup.alpha = 1f;
      this.canvasGroup.blocksRaycasts = true;
      this.canvasGroup.interactable = true;
      this.gameObject.SetActive(true);
   }

   public void hide () {
      this.canvasGroup.alpha = 0f;
      this.canvasGroup.blocksRaycasts = false;
      this.canvasGroup.interactable = false;
   }

   public bool isShowing () {
      return this.gameObject.activeSelf && canvasGroup.alpha > 0f;
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

      // Set the item count slider to the max possible value
      itemCountSlider.maxValue = selectedItem.count;
      itemCountSlider.value = selectedItem.count;
      updateSelectedItemCount();

      // Activate the select button
      selectButton.interactable = true;
   }

   public void updateSelectedItemCount () {
      selectedItemCount = (int) itemCountSlider.value;
      itemCountText.text = selectedItemCount.ToString();
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

   #endregion
}