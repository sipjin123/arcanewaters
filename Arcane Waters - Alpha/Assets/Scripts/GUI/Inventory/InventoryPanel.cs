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
   public GameObject equippedHatCellContainer;

   // Load Blocker when data is fetching
   public GameObject loadBlocker;

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
      loadBlocker.SetActive(true);
      NubisDataFetcher.self.fetchEquipmentData(_currentPage, ITEMS_PER_PAGE, _categoryFilters.ToArray());
   }

   public void clearPanel () {
      // Clear out any current items
      itemCellsContainer.DestroyChildren();
      equippedWeaponCellContainer.DestroyChildren();
      equippedArmorCellContainer.DestroyChildren();
      equippedHatCellContainer.DestroyChildren();
      characterStack.gameObject.SetActive(false);

      if (Global.player != null) {
         characterNameText.text = Global.player.entityName;
         goldText.text = "";
         gemsText.text = "";
      }

      // Load the character stack using the cached user info
      if (Global.getUserObjects() != null) {
         characterStack.gameObject.SetActive(true);
         characterStack.updateLayers(Global.getUserObjects(), false);

         // Clear old data
         equippedWeaponCellContainer.DestroyChildren();
         equippedArmorCellContainer.DestroyChildren();
         equippedHatCellContainer.DestroyChildren();

         // Quick load the cached equipment
         UserObjects userObj = Global.getUserObjects();
         if (userObj.weapon.itemTypeId != 0) {
            loadCachedEquipment(userObj.weapon);
         }
         if (userObj.armor.itemTypeId != 0) {
            loadCachedEquipment(userObj.armor);
         }
         if (userObj.hat.itemTypeId != 0) {
            loadCachedEquipment(userObj.hat);
         }

         // Update the gold and gems count, with commas in thousands place
         goldText.text = string.Format("{0:n0}", Global.lastUserGold);
         gemsText.text = string.Format("{0:n0}", Global.lastUserGems);
      }
   }

   private void loadCachedEquipment (Item item) {
      // Instantiates the cell
      ItemCell cell = Instantiate(itemCellPrefab, itemCellsContainer.transform, false);
      try {
         // If the item is equipped, place the item cell in the equipped slots
         if (item.category == Item.Category.Weapon) {
            Weapon newWeapon = new Weapon { category = Item.Category.Weapon, itemTypeId = item.itemTypeId };
            if (item.itemTypeId > 0 && item.data.Length > 0 && item.data.StartsWith(EquipmentXMLManager.VALID_XML_FORMAT)) {
               WeaponStatData weaponData = Util.xmlLoad<WeaponStatData>(item.data);
               newWeapon = WeaponStatData.translateDataToWeapon(weaponData);
               newWeapon.itemTypeId = weaponData.sqlId;
            } else {
               WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(item.itemTypeId);
               newWeapon.itemTypeId = weaponData.sqlId;
               newWeapon.data = WeaponStatData.serializeWeaponStatData(weaponData);
            }

            cell.setCellForItem(newWeapon);
            cell.transform.SetParent(equippedWeaponCellContainer.transform, false);
            refreshStats(newWeapon);
         } else if (item.category == Item.Category.Armor) {
            Armor newArmor = new Armor { category = Item.Category.Armor, itemTypeId = item.itemTypeId };
            if (item.itemTypeId > 0 && item.data.Length > 0 && item.data.StartsWith(EquipmentXMLManager.VALID_XML_FORMAT)) {
               ArmorStatData armorData = Util.xmlLoad<ArmorStatData>(item.data);
               newArmor = ArmorStatData.translateDataToArmor(armorData);
               newArmor.itemTypeId = armorData.sqlId;
            } else {
               ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(item.itemTypeId);
               newArmor.itemTypeId = armorData.sqlId;
               newArmor.data = ArmorStatData.serializeArmorStatData(armorData);
            }

            cell.setCellForItem(newArmor);
            cell.transform.SetParent(equippedArmorCellContainer.transform, false);
            refreshStats(Armor.castItemToArmor(newArmor));
         } else if (item.category == Item.Category.Hats) {
            Hat newHat = new Hat { category = Item.Category.Hats, itemTypeId = item.itemTypeId };
            if (item.itemTypeId > 0 && item.data.Length > 0 && item.data.StartsWith(EquipmentXMLManager.VALID_XML_FORMAT)) {
               HatStatData hatData = Util.xmlLoad<HatStatData>(item.data);
               newHat = HatStatData.translateDataToHat(hatData);
               newHat.itemTypeId = hatData.sqlId;
            } else {
               HatStatData hatData = EquipmentXMLManager.self.getHatData(item.itemTypeId);
               newHat.itemTypeId = hatData.sqlId;
               newHat.data = HatStatData.serializeHatStatData(hatData);
            }

            cell.setCellForItem(newHat);
            cell.transform.SetParent(equippedHatCellContainer.transform, false);
            refreshStats(Hat.castItemToHat(newHat));
         }
      } catch {
         D.editorLog("Failed to process item: " + item.data, Color.red);
      }
   }

   public void receiveItemForDisplay (Item[] itemArray, UserObjects userObjects, Item.Category category, int pageIndex, int totalItems, bool overrideEquipmentCache) {
      loadBlocker.SetActive(false);
      Global.lastUserGold = userObjects.userInfo.gold;
      Global.lastUserGems = userObjects.userInfo.gems;
 
      // Update the current page number
      _currentPage = pageIndex;

      // Calculate the maximum page number
      // TODO: Confirm max page alteration
      _maxPage = Mathf.CeilToInt((float) totalItems / ITEMS_PER_PAGE);

      // Update the current page text
      pageNumberText.text = "Page " + (_currentPage + 1).ToString() + " of " + _maxPage.ToString();

      // Update the navigation buttons
      updateNavigationButtons();

      // Update the gold and gems count, with commas in thousands place
      goldText.text = string.Format("{0:n0}", userObjects.userInfo.gold);
      gemsText.text = string.Format("{0:n0}", userObjects.userInfo.gems);

      // Update the character preview
      characterStack.gameObject.SetActive(true);
      characterStack.updateLayers(userObjects);

      // Update cached user object equipment
      if (overrideEquipmentCache) {
         Global.setUserEquipment(userObjects.weapon, userObjects.armor, userObjects.hat);
      }

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
      equippedHatCellContainer.DestroyChildren();

      // Create the item cells
      foreach (Item item in itemArray) {
         if (item.itemTypeId != 0) {
            if (item.category != Item.Category.Blueprint) {
               // Instantiates the cell
               ItemCell cell = Instantiate(itemCellPrefab, itemCellsContainer.transform, false);

               // Initializes the cell
               cell.setCellForItem(item);

               if (InventoryManager.isEquipped(item.id)) {
                  if (item.category == Item.Category.Weapon) {
                     equippedWeaponCellContainer.DestroyChildren();
                     cell.transform.SetParent(equippedWeaponCellContainer.transform, false);
                     refreshStats(Weapon.castItemToWeapon(item));
                  } else if (item.category == Item.Category.Armor) {
                     equippedArmorCellContainer.DestroyChildren();
                     cell.transform.SetParent(equippedArmorCellContainer.transform, false);
                     refreshStats(Armor.castItemToArmor(item));
                  } else if (item.category == Item.Category.Hats) {
                     equippedHatCellContainer.DestroyChildren();
                     cell.transform.SetParent(equippedHatCellContainer.transform, false);
                     refreshStats(Hat.castItemToHat(item));
                  }
               }

               // Set the cell click events
               cell.leftClickEvent.RemoveAllListeners();
               cell.rightClickEvent.RemoveAllListeners();
               cell.doubleClickEvent.RemoveAllListeners();
               cell.rightClickEvent.AddListener(() => showContextMenu(cell));
               cell.doubleClickEvent.AddListener(() => InventoryManager.tryEquipOrUseItem(cell.getItem()));
            } else {
               D.editorLog("Warning, Item Type is 0", Color.red);
            }
         } 
      }

      // Trigger the tutorial
      TutorialManager3.self.tryCompletingStep(TutorialTrigger.OpenInventory);
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
      _currentPage = 0;
      refreshPanel();
   }

   public void onWeaponTabButtonPress () {
      _categoryFilters.Clear();
      _categoryFilters.Add(Item.Category.Weapon);
      _currentPage = 0;
      refreshPanel();
   }

   public void onArmorTabButtonPress () {
      _categoryFilters.Clear();
      _categoryFilters.Add(Item.Category.Armor);
      _currentPage = 0;
      refreshPanel();
   }

   public void onIngredientsTabButtonPress () {
      _categoryFilters.Clear();
      _categoryFilters.Add(Item.Category.CraftingIngredients);
      _currentPage = 0;
      refreshPanel();
   }

   public void onOthersTabButtonPress () {
      // Get the list of categories managed by this tab
      _categoryFilters.Clear();
      foreach (Item.Category c in _othersTabCategories) {
         _categoryFilters.Add(c);
      }

      _currentPage = 0;
      refreshPanel();
   }

   public void refreshStats (Weapon equippedWeapon) {
      physicalStatRow.setEquippedWeapon(equippedWeapon);
      fireStatRow.setEquippedWeapon(equippedWeapon);
      earthStatRow.setEquippedWeapon(equippedWeapon);
      airStatRow.setEquippedWeapon(equippedWeapon);
      waterStatRow.setEquippedWeapon(equippedWeapon);
   }

   public void refreshStats (Hat equippedHat) {
      physicalStatRow.setEquippedHat(equippedHat);
      fireStatRow.setEquippedHat(equippedHat);
      earthStatRow.setEquippedHat(equippedHat);
      airStatRow.setEquippedHat(equippedHat);
      waterStatRow.setEquippedHat(equippedHat);
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
      if (InventoryManager.isEquipped(item.id)) {
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

   public void OnPointerClick (PointerEventData eventData) {
      // If the black background outside is clicked, hide the Inventory panel
      if (eventData.rawPointerPress == this.gameObject) {
         PanelManager.self.popPanel();
      }
   }

   #region Private Variables

   // The index of the current page
   private int _currentPage = 0;

   // The maximum page index (starting at 1)
   private int _maxPage = 3;

   // The category used to filter the displayed items
   private List<Item.Category> _categoryFilters = new List<Item.Category> { Item.Category.None };

   // The list of categories listed by the 'others' tab
   private List<Item.Category> _othersTabCategories;

   // The cell from which an item was grabbed
   private ItemCellInventory _grabbedItemCell;

   #endregion
}
