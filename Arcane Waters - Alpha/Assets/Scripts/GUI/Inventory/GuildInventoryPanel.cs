using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using NubisDataHandling;

public class GuildInventoryPanel : SubPanel
{
   #region Public Variables

   // Self
   public static GuildInventoryPanel self;

   // The number of items to display per page
   public static int ITEMS_PER_PAGE = 48;

   // The container of the item cells
   public GameObject itemCellsContainer;

   // The prefab we use for creating item cells
   public ItemCell itemCellPrefab;

   // Load Blocker when data is fetching
   public GameObject loadBlockerSmall;

   // The page number text
   public Text pageNumberText;

   // The next page button
   public Button nextPageButton;

   // The previous page button
   public Button previousPageButton;

   // The item tab filters
   public ItemTabs itemTabs;

   #endregion

   private void Awake () {
      self = this;

      // Initialize the inventory tabs
      itemTabs.initialize(onTabButtonPress);

      base.hide();
   }

   public void refreshPanel () {
      showBlocker();

      NubisDataFetcher.self.getGuildInventory(itemTabs.categoryFilters.ToArray(), _currentPage, ITEMS_PER_PAGE);
   }

   public void clearPanel () {
      // Clear out any current items
      itemCellsContainer.DestroyChildren();
   }

   public void receiveItemForDisplay (List<Item> itemArray, Item.Category[] categoryFilter, int pageIndex, int totalItems) {
      hideBlocker();

      // Update the current page number
      _currentPage = pageIndex;

      // Calculate the maximum page number
      // TODO: Confirm max page alteration
      _maxPage = Mathf.CeilToInt((float) totalItems / ITEMS_PER_PAGE);
      if (_maxPage < 1) {
         _maxPage = 1;
      }
      // Update the current page text
      pageNumberText.text = "Page " + (_currentPage + 1).ToString() + " of " + _maxPage.ToString();

      // Update the navigation buttons
      updateNavigationButtons();

      // Select the correct tab
      itemTabs.updateCategoryTabs(categoryFilter[0]);

      // Clear out any current items
      itemCellsContainer.DestroyChildren();

      // Create the item cells
      foreach (Item item in itemArray) {
         instantiateItemCell(item, itemCellsContainer.transform);
      }
   }

   private void subscribeClickEventsForCell (ItemCell cell) {
      if (cell == null) {
         D.error("Cannot subscribe click events because cell is null");
         return;
      }

      // Set the cell click events
      cell.leftClickEvent.RemoveAllListeners();
      cell.rightClickEvent.RemoveAllListeners();
      cell.doubleClickEvent.RemoveAllListeners();
      cell.onPointerEnter.RemoveAllListeners();
      cell.onPointerExit.RemoveAllListeners();
      cell.onDragStarted.RemoveAllListeners();

      cell.onPointerEnter.AddListener(() => {
         // Play hover sfx
         SoundEffectManager.self.playFmodGuiHover(SoundEffectManager.HOVER_CURSOR_ITEMS);
      });
   }

   private ItemCell instantiateItemCell (Item item, Transform parent) {
      // Instantiates the cell
      ItemCell cell = Instantiate(itemCellPrefab, parent, false);

      // Initializes the cell
      cell.setCellForItem(item);

      // Subscribe the click events
      if (!cell.isDisabledItem) {
         subscribeClickEventsForCell(cell);
      }

      return cell;
   }

   public override void show () {
      // Clear the panel
      _currentPage = 0;
      _maxPage = 1;
      clearPanel();

      // Request fetch data
      refreshPanel();

      // Show GUI
      base.show();
   }

   public void onTabButtonPress () {
      _currentPage = 0;
      refreshPanel();
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

   public void onContributeButtonClick () {
      // Associate a new function with the select button
      PanelManager.self.itemSelectionScreen.selectButton.onClick.RemoveAllListeners();
      PanelManager.self.itemSelectionScreen.selectButton.onClick.AddListener(() => contributeItemSelected());

      // Associate a new function with the cancel button
      PanelManager.self.itemSelectionScreen.cancelButton.onClick.RemoveAllListeners();
      PanelManager.self.itemSelectionScreen.cancelButton.onClick.AddListener(() => PanelManager.self.itemSelectionScreen.hide());

      // Show the item selection screen
      PanelManager.self.itemSelectionScreen.show();
   }

   public void contributeItemSelected () {
      // Hide item selection screen
      PanelManager.self.itemSelectionScreen.hide();

      // Get the selected item
      Item selectedItem = ItemSelectionScreen.selectedItem;
      selectedItem.count = ItemSelectionScreen.selectedItemCount;

      // Have the player confirm his choise
      PanelManager.self.confirmScreen.cancelButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.cancelButton.onClick.AddListener(() => {
         PanelManager.self.itemSelectionScreen.show();
      });

      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => {
         contributeItem(ItemSelectionScreen.selectedItem.id, ItemSelectionScreen.selectedItemCount);
         PanelManager.self.confirmScreen.hide();
      });

      PanelManager.self.confirmScreen.showYesNo(
         "Are you sure you want to contribute " +
         selectedItem.count + " " + selectedItem.getCastItem().getName() + "?" + System.Environment.NewLine +
         "You will lose this item forever!");
   }

   private void contributeItem (int itemId, int amount) {
      showBlocker();

      PanelManager.self.itemSelectionScreen.hide();

      if (Global.player != null) {
         Global.player.rpc.Cmd_ContributeItemToGuildInventory(itemId, amount);
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

   public override void hide () {
      base.hide();
   }

   public void showBlocker () {
      if (loadBlockerSmall.activeSelf) {
         return;
      }

      loadBlockerSmall.SetActive(true);
   }

   public void hideBlocker () {
      loadBlockerSmall.SetActive(false);
   }

   #region Private Variables

   // The index of the current page
   private int _currentPage = 0;

   // The maximum page index (starting at 1)
   private int _maxPage = 3;

   #endregion
}
