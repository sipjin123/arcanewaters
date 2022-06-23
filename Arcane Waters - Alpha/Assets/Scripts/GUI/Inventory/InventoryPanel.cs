using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using NubisDataHandling;

public class InventoryPanel : Panel
{

   #region Public Variables

   // The number of items to display per page
   public static int ITEMS_PER_PAGE = 48;

   // The container of the item cells
   public GameObject itemCellsContainer;

   // The prefab we use for creating item cells
   public ItemCell itemCellPrefab;

   // Load Blocker when data is fetching
   public GameObject loadBlockerSmall;

   // Load Blocker when actions are executed
   public GameObject loadBlockerLarge;

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

   // The gold text field
   public Text goldText;

   // The gems text field
   public Text gemsText;

   // The character info section
   public CharacterInfoSection characterInfoSection;

   // The equipment stats section
   public EquipmentStatsGrid equipmentStats;

   // The generic warning components
   public GameObject genericWarningPanel;
   public Text genericWarningMessage;

   // The cached job data
   public Jobs jobsData;

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
      showBlocker();
      NubisDataFetcher.self.getUserInventory(itemTabs.categoryFilters, _currentPage, ITEMS_PER_PAGE, 0, Panel.Type.Inventory);
   }

   public void preloadPanel () {
      loadBlockerSmall.SetActive(true);
   }

   public void clearPanel () {
      // Clear out any current items
      itemCellsContainer.DestroyChildren();

      // Load the character stack using the cached user info
      if (Global.player != null) {
         characterInfoSection.setPlayer(Global.player);
         updateGoldAndGems(Global.player);
         equipmentStats.refreshStats(Global.player);
      }
   }

   private void updateGoldAndGems (NetEntity player) {
      if (Global.player != null && player == Global.player) {
         // Update the gold and gems count, with commas in thousands place
         goldText.text = string.Format("{0:n0}", Global.lastUserGold);
         gemsText.text = string.Format("{0:n0}", Global.lastUserGems);
      }
   }

   public void receiveItemForDisplay (List<Item> itemArray, UserObjects userObjects, GuildInfo guildInfo, List<Item.Category> categoryFilter,
      int pageIndex, int totalItems, bool overrideEquipmentCache, Jobs jobsData) {
      hideBlocker();
      Global.lastUserGold = userObjects.userInfo.gold;
      Global.lastUserGems = userObjects.userInfo.gems;
      this.jobsData = jobsData;
      closeWarningPanel();

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
         Global.setUserEquipment(userObjects.weapon, userObjects.armor, userObjects.hat, userObjects.ring, userObjects.necklace, userObjects.trinket);
      }

      // Select the correct tab
      itemTabs.updateCategoryTabs(categoryFilter[0]);

      // Clear out any current items
      itemCellsContainer.DestroyChildren();

      // Create the item cells
      foreach (Item item in itemArray) {
         instantiateItemCell(item, itemCellsContainer.transform);
      }

      // Update the CharacterInfo section
      characterInfoSection.setUserObjects(userObjects);
      updateGoldAndGems(Global.player);
      equipmentStats.refreshStats(userObjects);

      int equippedWeaponId = 0;
      int equippedHatId = 0;
      int equippedArmorId = 0;
      int equippedRingId = 0;
      int equippedNecklaceId = 0;
      int equippedTrinketId = 0;
      if (Global.player is PlayerBodyEntity) {
         GearManager gearMngr = (Global.player as PlayerBodyEntity).gearManager;
         equippedWeaponId = (Global.player as PlayerBodyEntity).weaponManager.weaponType;
         equippedHatId = (Global.player as PlayerBodyEntity).hatsManager.hatType;
         equippedArmorId = (Global.player as PlayerBodyEntity).armorManager.armorType;
         equippedRingId = gearMngr.ringSpriteId;
         equippedNecklaceId = gearMngr.necklaceSpriteId;
         equippedTrinketId = gearMngr.trinketSpriteId;
      } else if (Global.player is PlayerShipEntity) {
         equippedWeaponId = (Global.player as PlayerShipEntity).weaponType;
         equippedHatId = (Global.player as PlayerShipEntity).hatType;
         equippedArmorId = (Global.player as PlayerShipEntity).armorType;
         equippedRingId = (Global.player as PlayerShipEntity).ringType;
         equippedNecklaceId = (Global.player as PlayerShipEntity).necklaceType;
         equippedTrinketId = (Global.player as PlayerShipEntity).trinketType;
      }

      subscribeClickEventsForCell(characterInfoSection.equippedWeaponCell, equippedWeaponId == 0);
      subscribeClickEventsForCell(characterInfoSection.equippedArmorCell, equippedArmorId == 0);
      subscribeClickEventsForCell(characterInfoSection.equippedHatCell, equippedHatId == 0);
      subscribeClickEventsForCell(characterInfoSection.equippedRingCell, equippedRingId == 0);
      subscribeClickEventsForCell(characterInfoSection.equippedNecklaceCell, equippedNecklaceId == 0);
      subscribeClickEventsForCell(characterInfoSection.equippedTrinketCell, equippedTrinketId == 0);

      // Trigger the tutorial
      TutorialManager3.self.tryCompletingStep(TutorialTrigger.OpenInventory);
   }

   private void subscribeClickEventsForCell (ItemCell cell, bool tooltipOnly) {
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

      if (tooltipOnly) {
         if (cell is ItemCellInventory) {
            ItemCellInventory inventoryCell = (ItemCellInventory) cell;
            inventoryCell.onPointerEnter.AddListener(() => {
               if (inventoryCell.tooltipObject != null) {
                  inventoryCell.tooltipObject.SetActive(true);
                  inventoryCell.tooltipMessage.text = inventoryCell.equipmentType.ToString();
               }
            });
            inventoryCell.onPointerExit.AddListener(() => {
               if (inventoryCell.tooltipObject != null) {
                  inventoryCell.tooltipObject.SetActive(false);
               }
            });
            inventoryCell.enablePointerEvents();
            inventoryCell.updateCellColor(100);
            inventoryCell.show();
            inventoryCell.tooltip.message = "";
            cell.GetComponent<Button>().interactable = false;
         }
      } else {
         cell.rightClickEvent.AddListener(() => showContextMenu(cell));
         cell.doubleClickEvent.AddListener(() => InventoryManager.tryEquipOrUseItem(cell.getItem(), jobsData));
         cell.shiftClickEvent.AddListener(() => {
            if (ChatPanel.self != null) {
               ChatPanel.self.addItemInsertToInput(cell.getItem());
            }
         });
         cell.onDragStarted.AddListener(() => tryGrabItem(cell as ItemCellInventory));
         cell.onPointerEnter.AddListener(() => {
            // Play hover sfx
            SoundEffectManager.self.playFmodGuiHover(SoundEffectManager.HOVER_CURSOR_ITEMS);

            equipmentStats.setStatModifiers(cell.getItem());
         });
         cell.GetComponent<Button>().interactable = true;
         cell.onPointerExit.AddListener(() => equipmentStats.clearStatModifiers());
      }
   }

   private ItemCell instantiateItemCell (Item item, Transform parent) {
      // Instantiates the cell
      ItemCell cell = Instantiate(itemCellPrefab, parent, false);

      // Initializes the cell
      cell.setCellForItem(item);

      // Subscribe the click events
      if (!cell.isDisabledItem) {
         subscribeClickEventsForCell(cell, false);
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
         if (EquipmentXMLManager.self.isJobLevelValid(jobsData, castedItem)) {
            if (InventoryManager.isEquipped(castedItem.id)) {
               PanelManager.self.contextMenuPanel.addButton("Unequip", () => InventoryManager.equipOrUnequipItem(castedItem, jobsData));
            } else {
               PanelManager.self.contextMenuPanel.addButton("Equip", () => InventoryManager.equipOrUnequipItem(castedItem, jobsData));
            }
         } else {
            if (!InventoryManager.isEquipped(castedItem.id)) {
               PanelManager.self.contextMenuPanel.addButton("Equip", () => InventoryManager.equipOrUnequipItem(castedItem, jobsData), forceFalseVal);
            }
         }
      }

      if (castedItem.canBeUsed()) {
         PanelManager.self.contextMenuPanel.addButton("Use", () => InventoryManager.useItem(castedItem));
      }

      if (castedItem.canBeTrashed()) {
         PanelManager.self.contextMenuPanel.addButton("Trash", () => InventoryManager.trashItem(castedItem));
      }

      PanelManager.self.contextMenuPanel.addButton("Link In Chat", () => ChatPanel.self.addItemInsertToInput(castedItem));

      PanelManager.self.contextMenuPanel.show("");
   }

   public bool forceFalseVal () {
      return false;
   }

   public void tryGrabItem (ItemCellInventory itemCell) {
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
         grabbedItem.activate(castedItem, itemCell.getItemSprite(), tryDropGrabbedItem);
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
            // Check level requirement
            // If level requirements are met
            if (!EquipmentXMLManager.self.isLevelValid(LevelUtil.levelForXp(Global.player.XP), _grabbedItemCell.getItem())) {
               triggerWarningPanel("Insufficient level requirement!");
            } else {
               Global.player.rpc.Cmd_UpdateItemShortcut(box.slotNumber, _grabbedItemCell.getItem().id);
            }
            stopGrabbingItem();
            return;
         }

         // Items can also be dragged from the equipment slots to the inventory or vice-versa
         bool equipped = InventoryManager.isEquipped(_grabbedItemCell.getItem().id);
         bool droppedInInventory = inventoryDropZone.isInZone(screenPosition);
         bool droppedInEquipmentSlots = equipmentDropZone.isInZone(screenPosition);

         if ((equipped && droppedInInventory) ||
            (!equipped && droppedInEquipmentSlots)) {
            //SoundEffectManager.self.playSoundEffect(SoundEffectManager.INVENTORY_DROP, transform);

            // Deactivate the grabbed item object
            if (!EquipmentXMLManager.self.isJobLevelValid(jobsData, _grabbedItemCell.getItem())) {
               string warningMsg = "Insufficient job level requirement!";
               triggerWarningPanel(warningMsg);
               stopGrabbingItem();
               return;
            } else if (!EquipmentXMLManager.self.isLevelValid(LevelUtil.levelForXp(Global.player.XP), _grabbedItemCell.getItem())) {
               string warningMsg = "Insufficient level requirement!";
               triggerWarningPanel(warningMsg);
               stopGrabbingItem();
               return;
            } else {
               // Equip or unequip the item
               InventoryManager.equipOrUnequipItem(_grabbedItemCell.getItem(), jobsData);
               grabbedItem.deactivate();
            }

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

   public void triggerWarningPanel (string warningMessage) {
      genericWarningPanel.SetActive(true);
      genericWarningMessage.text = warningMessage;
   }

   public void closeWarningPanel () {
      genericWarningPanel.SetActive(false);
   }

   public void showBlocker (bool large = false, bool forced = false) {
      // If forced is false, the blockers won't appear if either is visible
      if (loadBlockerLarge.activeSelf || loadBlockerSmall.activeSelf) {
         if (!forced) {
            return;
         }
      }

      if (large) {
         loadBlockerLarge.SetActive(true);
      } else {
         loadBlockerSmall.SetActive(true);
      }
   }

   public void hideBlocker () {
      loadBlockerLarge.SetActive(false);
      loadBlockerSmall.SetActive(false);
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
