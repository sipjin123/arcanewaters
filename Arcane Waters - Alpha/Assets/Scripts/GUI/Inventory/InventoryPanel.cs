using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using NubisDataHandling;

public class InventoryPanel : Panel, IPointerClickHandler {

   #region Public Variables

   // The number of items to display per page
   public static int ITEMS_PER_PAGE = 42;

   // The container of the item cells
   public GameObject itemCellsContainer;

   // The prefab we use for creating item cells
   public ItemCell itemCellPrefab;

   // The cell containers for the equipped items
   public GameObject equippedWeaponCellContainer;
   public GameObject equippedArmorCellContainer;

   // Our character stack
   public CharacterStack characterStack;

   // The page number text
   public Text pageNumberText;

   // The next page button
   public Button nextPageButton;

   // The previous page button
   public Button previousPageButton;

   // The text components
   public Text characterNameText;
   public Text goldText;
   public Text gemsText;

   // The common object used to display any grabbed item
   public GrabbedItem grabbedItem;

   // The zone where grabbed items can be dropped
   public ItemDropZone inventoryDropZone;
   public ItemDropZone equipmentDropZone;

   // The context menu common to all cells
   public GameObject contextMenu;

   // The buttons of the context menu
   public GameObject equipButtonGO;
   public GameObject unequipButtonGO;
   public GameObject useButtonGO;
   public GameObject trashButtonGO;

   // The inventory tab renderers
   public Image allTabRenderer;
   public Image weaponTabRenderer;
   public Image armorTabRenderer;
   public Image ingredientsTabRenderer;
   public Image othersTabRenderer;
   public Text allTabIconRenderer;
   public Image weaponTabIconRenderer;
   public Image armorTabIconRenderer;
   public Image ingredientsTabIconRenderer;
   public Image othersTabIconRenderer;

   // The inventory tab buttons
   public Button allTabButton;
   public Button weaponTabButton;
   public Button armorTabButton;
   public Button ingredientsTabButton;
   public Button othersTabButton;

   // The stat rows for each element
   public InventoryStatRow physicalStatRow;
   public InventoryStatRow fireStatRow;
   public InventoryStatRow earthStatRow;
   public InventoryStatRow airStatRow;
   public InventoryStatRow waterStatRow;

   // Self
   public static InventoryPanel self;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;

      // Hide the context menu
      contextMenu.SetActive(false);

      // Deactivate the grabbed item
      grabbedItem.deactivate();

      // Create a hashset with all the item categories
      HashSet<Item.Category> workSet = new HashSet<Item.Category>();
      foreach (Item.Category c in Enum.GetValues(typeof(Item.Category))) {
         workSet.Add(c);
      }

      // Remove the categories managed by the other tabs
      workSet.Remove(Item.Category.None);
      workSet.Remove(Item.Category.Weapon);
      workSet.Remove(Item.Category.Armor);
      workSet.Remove(Item.Category.CraftingIngredients);

      // Save the categories in a list
      _othersTabCategories = new List<Item.Category>();
      foreach (Item.Category c in workSet) {
         _othersTabCategories.Add(c);
      }

      // Initialize the stat rows
      physicalStatRow.clear();
      fireStatRow.clear();
      earthStatRow.clear();
      airStatRow.clear();
      waterStatRow.clear();
   }

   public void refreshPanel () {
      // Hide the context menu
      hideContextMenu();

      NubisDataFetcher.self.fetchEquipmentData(_currentPage, ITEMS_PER_PAGE, _categoryFilters.ToArray());
   }

   public void receiveItemForDisplay (Item[] itemArray, UserObjects userObjects, Item.Category category, int pageIndex) {
      _equippedWeaponId = userObjects.weapon.id;
      _equippedArmorId = userObjects.armor.id;

      // Update the current page number
      _currentPage = pageIndex;

      // Calculate the maximum page number
      _maxPage = Mathf.CeilToInt((float) itemArray.Length / ITEMS_PER_PAGE);

      // Update the current page text
      pageNumberText.text = "Page " + _currentPage.ToString() + " of " + _maxPage.ToString();

      // Update the navigation buttons
      updateNavigationButtons();

      // Update the gold and gems count, with commas in thousands place
      goldText.text = string.Format("{0:n0}", userObjects.userInfo.gold);
      gemsText.text = string.Format("{0:n0}", userObjects.userInfo.gems);

      // Update the character preview
      characterStack.updateLayers(userObjects);

      // Insert the player's name
      if (Global.player != null) {
         characterNameText.text = Global.player.entityName;
      }

      // Select the correct tab
      updateCategoryTabs(category);

      // Clear stat rows
      physicalStatRow.clear();
      fireStatRow.clear();
      earthStatRow.clear();
      airStatRow.clear();
      waterStatRow.clear();

      // Clear out any current items
      itemCellsContainer.DestroyChildren();
      equippedWeaponCellContainer.DestroyChildren();
      equippedArmorCellContainer.DestroyChildren();

      // Create the item cells
      foreach (Item item in itemArray) {
         if (item.category != Item.Category.Blueprint) {
            // Instantiates the cell
            ItemCell cell = Instantiate(itemCellPrefab, itemCellsContainer.transform, false);

            // Initializes the cell
            cell.setCellForItem(item);

            // If the item is equipped, place the item cell in the equipped slots
            if (item.id == _equippedWeaponId && item.id != 0) {
               cell.transform.SetParent(equippedWeaponCellContainer.transform, false);
               refreshStats(Weapon.castItemToWeapon(item));
            } else if (item.id == _equippedArmorId && item.id != 0) {
               cell.transform.SetParent(equippedArmorCellContainer.transform, false);
               refreshStats(Armor.castItemToArmor(item));
            }

            // Set the cell click events
            cell.leftClickEvent.RemoveAllListeners();
            cell.rightClickEvent.RemoveAllListeners();
            cell.doubleClickEvent.RemoveAllListeners();
            cell.leftClickEvent.AddListener(() => hideContextMenu());
            cell.rightClickEvent.AddListener(() => showContextMenu(cell));
            cell.doubleClickEvent.AddListener(() => tryEquipOrUseItem(cell.getItem()));
         }
      }
   }

   private void updateCategoryTabs (Item.Category itemCategory) {
      switch (itemCategory) {
         case Item.Category.None:
            allTabRenderer.enabled = true;
            weaponTabRenderer.enabled = false;
            armorTabRenderer.enabled = false;
            ingredientsTabRenderer.enabled = false;
            othersTabRenderer.enabled = false;

            allTabIconRenderer.enabled = true;
            weaponTabIconRenderer.enabled = false;
            armorTabIconRenderer.enabled = false;
            ingredientsTabIconRenderer.enabled = false;
            othersTabIconRenderer.enabled = false;

            allTabButton.interactable = false;
            weaponTabButton.interactable = true;
            armorTabButton.interactable = true;
            ingredientsTabButton.interactable = true;
            othersTabButton.interactable = true;
            break;
         case Item.Category.Weapon:
            allTabRenderer.enabled = false;
            weaponTabRenderer.enabled = true;
            armorTabRenderer.enabled = false;
            ingredientsTabRenderer.enabled = false;
            othersTabRenderer.enabled = false;

            allTabIconRenderer.enabled = false;
            weaponTabIconRenderer.enabled = true;
            armorTabIconRenderer.enabled = false;
            ingredientsTabIconRenderer.enabled = false;
            othersTabIconRenderer.enabled = false;

            allTabButton.interactable = true;
            weaponTabButton.interactable = false;
            armorTabButton.interactable = true;
            ingredientsTabButton.interactable = true;
            othersTabButton.interactable = true;
            break;
         case Item.Category.Armor:
            allTabRenderer.enabled = false;
            weaponTabRenderer.enabled = false;
            armorTabRenderer.enabled = true;
            ingredientsTabRenderer.enabled = false;
            othersTabRenderer.enabled = false;

            allTabIconRenderer.enabled = false;
            weaponTabIconRenderer.enabled = false;
            armorTabIconRenderer.enabled = true;
            ingredientsTabIconRenderer.enabled = false;
            othersTabIconRenderer.enabled = false;

            allTabButton.interactable = true;
            weaponTabButton.interactable = true;
            armorTabButton.interactable = false;
            ingredientsTabButton.interactable = true;
            othersTabButton.interactable = true;
            break;
         case Item.Category.CraftingIngredients:
            allTabRenderer.enabled = false;
            weaponTabRenderer.enabled = false;
            armorTabRenderer.enabled = false;
            ingredientsTabRenderer.enabled = true;
            othersTabRenderer.enabled = false;

            allTabIconRenderer.enabled = false;
            weaponTabIconRenderer.enabled = false;
            armorTabIconRenderer.enabled = false;
            ingredientsTabIconRenderer.enabled = true;
            othersTabIconRenderer.enabled = false;

            allTabButton.interactable = true;
            weaponTabButton.interactable = true;
            armorTabButton.interactable = true;
            ingredientsTabButton.interactable = false;
            othersTabButton.interactable = true;
            break;
         default:
            // "Others" tab
            allTabRenderer.enabled = false;
            weaponTabRenderer.enabled = false;
            armorTabRenderer.enabled = false;
            ingredientsTabRenderer.enabled = false;
            othersTabRenderer.enabled = true;

            allTabIconRenderer.enabled = false;
            weaponTabIconRenderer.enabled = false;
            armorTabIconRenderer.enabled = false;
            ingredientsTabIconRenderer.enabled = false;
            othersTabIconRenderer.enabled = true;

            allTabButton.interactable = true;
            weaponTabButton.interactable = true;
            armorTabButton.interactable = true;
            ingredientsTabButton.interactable = true;
            othersTabButton.interactable = false;
            break;
      }
   }

   public void onAllTabButtonPress () {
      _categoryFilters.Clear();
      _categoryFilters.Add(Item.Category.None);
      _currentPage = 1;
      refreshPanel();
   }

   public void onWeaponTabButtonPress () {
      _categoryFilters.Clear();
      _categoryFilters.Add(Item.Category.Weapon);
      _currentPage = 1;
      refreshPanel();
   }

   public void onArmorTabButtonPress () {
      _categoryFilters.Clear();
      _categoryFilters.Add(Item.Category.Armor);
      _currentPage = 1;
      refreshPanel();
   }

   public void onIngredientsTabButtonPress () {
      _categoryFilters.Clear();
      _categoryFilters.Add(Item.Category.CraftingIngredients);
      _currentPage = 1;
      refreshPanel();
   }

   public void onOthersTabButtonPress () {
      // Get the list of categories managed by this tab
      _categoryFilters.Clear();
      foreach (Item.Category c in _othersTabCategories) {
         _categoryFilters.Add(c);
      }

      _currentPage = 1;
      refreshPanel();
   }

   public void refreshStats (Weapon equippedWeapon) {
      physicalStatRow.setEquippedWeapon(equippedWeapon);
      fireStatRow.setEquippedWeapon(equippedWeapon);
      earthStatRow.setEquippedWeapon(equippedWeapon);
      airStatRow.setEquippedWeapon(equippedWeapon);
      waterStatRow.setEquippedWeapon(equippedWeapon);
   }

   public void refreshStats (Armor equippedArmor) {
      physicalStatRow.setEquippedArmor(equippedArmor);
      fireStatRow.setEquippedArmor(equippedArmor);
      earthStatRow.setEquippedArmor(equippedArmor);
      airStatRow.setEquippedArmor(equippedArmor);
      waterStatRow.setEquippedArmor(equippedArmor);
   }

   public void setStatModifiers (Item item) {
      // Skip equipped items
      if (item == null) {
         return;
      }

      if (item.id == _equippedArmorId || item.id == _equippedWeaponId) {
         return;
      }

      // Determine if the hovered item is a weapon or an armor
      if (item.category == Item.Category.Weapon) {
         Weapon weapon = Weapon.castItemToWeapon(item);

         // Set how the stats would change if the item was equipped
         physicalStatRow.setStatModifiersForWeapon(weapon);
         fireStatRow.setStatModifiersForWeapon(weapon);
         earthStatRow.setStatModifiersForWeapon(weapon);
         airStatRow.setStatModifiersForWeapon(weapon);
         waterStatRow.setStatModifiersForWeapon(weapon);

      } else if (item.category == Item.Category.Armor) {
         Armor armor = Armor.castItemToArmor(item);

         // Set how the stats would change if the item was equipped
         physicalStatRow.setStatModifiersForArmor(armor);
         fireStatRow.setStatModifiersForArmor(armor);
         earthStatRow.setStatModifiersForArmor(armor);
         airStatRow.setStatModifiersForArmor(armor);
         waterStatRow.setStatModifiersForArmor(armor);
      }
   }

   public void clearStatModifiers () {
      physicalStatRow.disableStatModifiers();
      fireStatRow.disableStatModifiers();
      earthStatRow.disableStatModifiers();
      airStatRow.disableStatModifiers();
      waterStatRow.disableStatModifiers();
   }

   public void showContextMenu (ItemCell itemCell) {
      // Set the selected item
      _selectedItem = itemCell.getItem();

      // If the item cannot be interacted with in any way, don't show the menu
      if (!_selectedItem.canBeEquipped() &&
         !_selectedItem.canBeUsed() &&
         !_selectedItem.canBeTrashed()) {
         return;
      }

      contextMenu.SetActive(true);

      // Move the menu to the mouse position
      contextMenu.transform.position = Input.mousePosition;

      // Activate the 'equipped' or 'unequipped' button
      if (_selectedItem.canBeEquipped()) {
         if (isEquipped(_selectedItem.id)) {
            equipButtonGO.SetActive(false);
            unequipButtonGO.SetActive(true);
         } else {
            equipButtonGO.SetActive(true);
            unequipButtonGO.SetActive(false);
         }
      } else {
         equipButtonGO.SetActive(false);
         unequipButtonGO.SetActive(false);
      }

      // Check if the item can be used
      if (_selectedItem.canBeUsed()) {
         useButtonGO.SetActive(true);
      } else {
         useButtonGO.SetActive(false);
      }

      // Check if the item can be trashed
      if (_selectedItem.canBeTrashed()) {
         trashButtonGO.SetActive(true);
      } else {
         trashButtonGO.SetActive(false);
      }
   }

   public void hideContextMenu () {
      if (contextMenu.activeSelf) {
         contextMenu.SetActive(false);
      }
   }

   public void tryGrabItem(ItemCellInventory itemCell) {
      Item castedItem = itemCell.getItem();

      // Only equippable items can be grabbed
      if (castedItem.canBeEquipped()) {
         _grabbedItemCell = itemCell;

         // Hide the cell being grabbed
         _grabbedItemCell.hide();

         // Initialize the common grabbed object
         grabbedItem.activate(castedItem, itemCell.getItemSprite(), itemCell.getItemColorKey());
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
         // Determine which action was performed
         bool equipped = isEquipped(_grabbedItemCell.getItem().id);
         bool droppedInInventory = inventoryDropZone.isInZone(screenPosition);
         bool droppedInEquipmentSlots = equipmentDropZone.isInZone(screenPosition);

         // Items can be dragged from the equipment slots to the inventory or vice-versa
         if ((equipped && droppedInInventory) ||
            (!equipped && droppedInEquipmentSlots)) {

            // Select the item
            _selectedItem = _grabbedItemCell.getItem();

            // Equip or unequip the item
            equipOrUnequipSelected();

            // Deactivate the grabbed item object
            grabbedItem.deactivate();
            _grabbedItemCell = null;
         } else {
            // Otherwise, simply stop grabbing
            stopGrabbingItem();
         }
      }
   }

   public void tryEquipOrUseItem(Item castedItem) {
      _selectedItem = castedItem;

      // Hide the context menu
      hideContextMenu();

      // Equip the item if it is equippable
      if (castedItem.canBeEquipped()) {
         equipOrUnequipSelected();
         return;
      }

      // Use the item if it is usable
      if (castedItem.canBeUsed()) {
         useSelected();
         return;
      }
   }

   public void equipOrUnequipSelected () {
      // Hide the context menu
      hideContextMenu();

      // Check which type of item we requested to equip/unequip
      if (_selectedItem is Weapon) {
         // Check if it's currently equipped or not
         int itemIdToSend = isEquipped(_selectedItem.id) ? 0 : _selectedItem.id;

         // Equip or unequip the item
         Global.player.rpc.Cmd_RequestSetWeaponId(itemIdToSend);
      } else if (_selectedItem is Armor) {
         // Check if it's currently equipped or not
         int itemIdToSend = isEquipped(_selectedItem.id) ? 0 : _selectedItem.id;

         // Equip or unequip the item
         Global.player.rpc.Cmd_RequestSetArmorId(itemIdToSend);
      }
   }

   public void useSelected () {
      // Hide the context menu
      hideContextMenu();

      // Show an error panel if the item cannot be used
      if (!_selectedItem.canBeUsed()) {
         PanelManager.self.noticeScreen.show("This item can not be used.");
         return;
      }

      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => confirmUseSelectedItem());

      // Show a confirmation panel
      PanelManager.self.confirmScreen.show("Are you sure you want to use your " + _selectedItem.getName() + "?");

      // Hide the context menu
      hideContextMenu();
   }

   public void confirmUseSelectedItem () {
      PanelManager.self.confirmScreen.hide();
      Global.player.rpc.Cmd_UseItem(_selectedItem.id);
   }

   public void trashSelected () {
      // Hide the context menu
      hideContextMenu();

      // Show an error panel if the item cannot be trashed
      if (!_selectedItem.canBeTrashed()) {
         PanelManager.self.noticeScreen.show("This item can not be trashed.");
         return;
      }

      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => confirmTrashSelectedItem());

      // Show a confirmation panel with the user name
      PanelManager.self.confirmScreen.show("Are you sure you want to trash " + _selectedItem.getName() + "?");

      // Hide the context menu
      hideContextMenu();
   }

   protected void confirmTrashSelectedItem () {
      Global.player.rpc.Cmd_DeleteItem(_selectedItem.id);
   }

   public void nextPage () {
      if (_currentPage < _maxPage) {
         _currentPage++;
         refreshPanel();
      }
   }
   
   public void previousPage () {
      if (_currentPage > 1) {
         _currentPage--;
         refreshPanel();
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

   public void OnPointerClick (PointerEventData eventData) {
      // If the black background outside is clicked, hide the Inventory panel
      if (eventData.rawPointerPress == this.gameObject) {
         PanelManager.self.popPanel();
      } else if (eventData.button == PointerEventData.InputButton.Left) {

         // Hides the context menu if it is active
         hideContextMenu();
      }
   }

   protected bool isEquipped (int itemId) {
      if (itemId <= 0) {
         return false;
      }

      if (itemId == _equippedArmorId || itemId == _equippedWeaponId) {
         return true;
      }

      return false;
   }

   #region Private Variables

   // The index of the current page
   private int _currentPage = 1;

   // The maximum page index (starting at 1)
   private int _maxPage = 1;

   // The category used to filter the displayed items
   private List<Item.Category> _categoryFilters = new List<Item.Category> { Item.Category.None };

   // The list of categories listed by the 'others' tab
   private List<Item.Category> _othersTabCategories;

   // The item for which the context menu was activated
   private Item _selectedItem;

   // The cell from which an item was grabbed
   private ItemCellInventory _grabbedItemCell;

   // The currently equipped armor id
   protected static int _equippedArmorId;

   // The current equipped weapon id
   protected static int _equippedWeaponId;

   #endregion
}
