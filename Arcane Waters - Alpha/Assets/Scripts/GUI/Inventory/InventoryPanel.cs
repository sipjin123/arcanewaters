using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using NubisDataHandling;

public class InventoryPanel : Panel {

   #region Public Variables

   // The number of items to display per page
   public static int ITEMS_PER_PAGE = 48;

   // The container of the item cells
   public GameObject itemCellsContainer;

   // The prefab we use for creating item cells
   public ItemCell itemCellPrefab;

   // Load Blocker when data is fetching
   public GameObject loadBlocker;

   // The page number text
   public Text pageNumberText;

   // The next page button
   public Button nextPageButton;

   // The previous page button
   public Button previousPageButton;

   // The common object used to display any grabbed item
   public GrabbedItem grabbedItem;

   // The zone where grabbed items can be dropped
   public ItemDropZone inventoryDropZone;
   public ItemDropZone equipmentDropZone;

   // The item tab filters
   public ItemTabs itemTabs;

   // The character info column
   public CharacterInfoColumn characterInfoColumn;

   // Self
   public static InventoryPanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;

      // Deactivate the grabbed item
      grabbedItem.deactivate();

      // Initialize the inventory tabs
      itemTabs.initialize(onTabButtonPress);
   }

   public void refreshPanel () {
      loadBlocker.SetActive(true);
      NubisDataFetcher.self.getUserInventory(itemTabs.categoryFilters, _currentPage, ITEMS_PER_PAGE, 0, Panel.Type.Inventory);
   }

   public void clearPanel () {
      // Clear out any current items
      itemCellsContainer.DestroyChildren();

      // Load the character stack using the cached user info
      if (Global.player != null) {
         characterInfoColumn.setPlayer(Global.player);
      }
   }

   public void receiveItemForDisplay (List<Item> itemArray, UserObjects userObjects, GuildInfo guildInfo, List<Item.Category> categoryFilter,
      int pageIndex, int totalItems, bool overrideEquipmentCache) {
      loadBlocker.SetActive(false);
      Global.lastUserGold = userObjects.userInfo.gold;
      Global.lastUserGems = userObjects.userInfo.gems;
 
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

      // Update cached user object equipment
      if (overrideEquipmentCache) {
         Global.setUserEquipment(userObjects.weapon, userObjects.armor, userObjects.hat);
      }

      // Select the correct tab
      itemTabs.updateCategoryTabs(categoryFilter[0]);

      // Clear out any current items
      itemCellsContainer.DestroyChildren();

      // Create the item cells
      foreach (Item item in itemArray) {
         instantiateItemCell(item, itemCellsContainer.transform);
      }

      // Update the CharacterInfo column
      characterInfoColumn.setPlayer(Global.player);

      int equippedWeaponId = (Global.player as PlayerBodyEntity).weaponManager.weaponType;
      if (equippedWeaponId != 0) {
         subscribeClickEventsForCell(characterInfoColumn.equippedWeaponCell);
      }

      int equippedArmorId = (Global.player as PlayerBodyEntity).armorManager.armorType;
      if (equippedArmorId != 0) {
         subscribeClickEventsForCell(characterInfoColumn.equippedArmorCell);
      } 

      int equippedHatId = (Global.player as PlayerBodyEntity).hatsManager.hatType;
      if (equippedHatId != 0) {
         subscribeClickEventsForCell(characterInfoColumn.equippedHatCell);
      }

      // Trigger the tutorial
      TutorialManager3.self.tryCompletingStep(TutorialTrigger.OpenInventory);
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

      cell.rightClickEvent.AddListener(() => showContextMenu(cell));
      cell.doubleClickEvent.AddListener(() => InventoryManager.tryEquipOrUseItem(cell.getItem()));
      cell.onPointerEnter.AddListener(() => {
         // Play hover sfx
         SoundEffectManager.self.playFmodGuiHover(SoundEffectManager.HOVER_CURSOR_ITEMS);

         characterInfoColumn.equipmentStats.setStatModifiers(cell.getItem());
      });
      cell.onPointerExit.AddListener(() => characterInfoColumn.equipmentStats.clearStatModifiers());
      cell.onDragStarted.AddListener(() => tryGrabItem(cell as ItemCellInventory));
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

   public void onTabButtonPress () {
      _currentPage = 0;
      refreshPanel();
   }

   public void showContextMenu (ItemCell itemCell) {
      // Get the selected item
      Item castedItem = itemCell.getItem();

      // If the item cannot be interacted with in any way, don't show the menu
      if (!castedItem.canBeEquipped() &&
         !castedItem.canBeUsed() &&
         !castedItem.canBeTrashed()) {
         return;
      }

      PanelManager.self.contextMenuPanel.clearButtons();

      // Add the context menu buttons
      if (castedItem.canBeEquipped()) {
         if (InventoryManager.isEquipped(castedItem.id)) {
            PanelManager.self.contextMenuPanel.addButton("Unequip", () => InventoryManager.equipOrUnequipItem(castedItem));
         } else {
            PanelManager.self.contextMenuPanel.addButton("Equip", () => InventoryManager.equipOrUnequipItem(castedItem));
         }
      }

      if (castedItem.canBeUsed()) {
         PanelManager.self.contextMenuPanel.addButton("Use", () => InventoryManager.useItem(castedItem));
      }

      if (castedItem.canBeTrashed()) {
         PanelManager.self.contextMenuPanel.addButton("Trash", () => InventoryManager.trashItem(castedItem));
      }

      PanelManager.self.contextMenuPanel.show("");
   }

   public void tryGrabItem(ItemCellInventory itemCell) {
      if (itemCell == null) {
         return;
      }

      Item castedItem = itemCell.getItem();

      // Only equippable items can be grabbed
      if (castedItem.canBeEquipped()) {
         _grabbedItemCell = itemCell;

         // Hide the cell being grabbed
         _grabbedItemCell.hide();

         // Initialize the common grabbed object
         grabbedItem.activate(castedItem, itemCell.getItemSprite());
      }
   }

   public void stopGrabbingItem () {
      if (_grabbedItemCell != null) {
         // Restore the grabbed cell
         _grabbedItemCell.show();
         _grabbedItemCell = null;

         // Deactivate the grabbed item
         grabbedItem.deactivate();
      }
   }

   public void tryDropGrabbedItem (Vector2 screenPosition) {
     if (_grabbedItemCell != null) {
         // Check if the item was dropped over a shortcut slot
         ShortcutBox box = PanelManager.self.itemShortcutPanel.getShortcutBoxAtPosition(screenPosition);
         if (box != null) {
            Global.player.rpc.Cmd_UpdateItemShortcut(box.slotNumber, _grabbedItemCell.getItem().id);
            stopGrabbingItem();
            return;
         }

         // Items can also be dragged from the equipment slots to the inventory or vice-versa
         bool equipped = InventoryManager.isEquipped(_grabbedItemCell.getItem().id);
         bool droppedInInventory = inventoryDropZone.isInZone(screenPosition);
         bool droppedInEquipmentSlots = equipmentDropZone.isInZone(screenPosition);
         
         if ((equipped && droppedInInventory) ||
            (!equipped && droppedInEquipmentSlots)) {

            SoundEffectManager.self.playSoundEffect(SoundEffectManager.INVENTORY_DROP, transform);

            // Equip or unequip the item
            InventoryManager.equipOrUnequipItem(_grabbedItemCell.getItem());

            // Deactivate the grabbed item object
            grabbedItem.deactivate();
            _grabbedItemCell = null;

            return;
         }

         // Otherwise, simply stop grabbing
         stopGrabbingItem();
      }
   }

   public void onPerksButtonPressed () {
      PerksPanel.self.show();
   }

   public void enableLoadBlocker () {
      loadBlocker.SetActive(true);
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

   public override void hide () {
      base.hide();

      // Make sure to also hide perks panel in case of hiding inventory
      PerksPanel.self.hide();
   }

   #region Private Variables

   // The index of the current page
   private int _currentPage = 0;

   // The maximum page index (starting at 1)
   private int _maxPage = 3;

   // The cell from which an item was grabbed
   private ItemCellInventory _grabbedItemCell;

   #endregion
}
