using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class InventoryPanel : Panel, IPointerClickHandler {
   #region Public Variables

   // The number of items to display per page
   public static int ITEMS_PER_PAGE = 20;

   // The currently selected itemId
   public static int selectedItemId = 0;

   // The container of the item rows
   public GameObject itemRowContainer;

   // The components we manage
   public Button equipButton;
   public Button useButton;
   public Button trashButton;
   public Text characterNameText;
   public Text equipText;
   public Text itemTitleText;
   public Text itemInfoText;
   public Text goldText;
   public Text gemsText;
   public Text pageText;
   public Button pageLeftButton;
   public Button pageRightButton;
   // public CharPreviewPanel previewPanel;

   // The icons we use
   public Sprite armorIcon;
   public Sprite weaponIcon;
   public Sprite itemIcon;
   public Sprite foodIcon;
   public Sprite ingredientIcon;
   public Sprite blueprintIcon;

   // The prefab we use for creating item rows
   public ItemRow itemRowPrefab;

   // Our character stack
   public CharacterStack characterStack;

   // Self
   public static InventoryPanel self;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;
   }

   public override void Update () {
      base.Update();

      // Update the Inventory/Page title bar
      int maxPageNumber = getMaxPageNumber();
      pageText.text = (maxPageNumber == 1) ? "INVENTORY" : string.Format("INVENTORY PAGE {0} OF {1}", _pageNumber, maxPageNumber);

      // Insert the player's name
      if (Global.player != null) {
         characterNameText.text = Global.player.entityName;
      }

      // Hide or show the page left/right buttons
      pageLeftButton.gameObject.SetActive(maxPageNumber > 1);
      pageRightButton.gameObject.SetActive(maxPageNumber > 1);

      // Look up our selected Item
      Item selectedItem = getSelectedItem();

      // Enable/disable buttons accordingly
      equipButton.interactable = (selectedItem is EquippableItem);
      useButton.interactable = (selectedItem != null && selectedItem.canBeUsed());
      trashButton.interactable = selectedItem != null && selectedItem.canBeTrashed();

      // Update the button text
      equipText.text = "Equip";

      if (isEquipped(selectedItemId)) {
         equipText.text = "Unequip";
      }

      // We don't show the preview panel while at sea
      // previewPanel.gameObject.SetActive(!(Global.player is ShipCaptain));

      // Update the Info text
      if (selectedItem == null) {
         itemTitleText.text = "";
         itemInfoText.text = "";
      } else {
         itemTitleText.text = selectedItem.getName();
         itemInfoText.text = selectedItem.getDescription();
      }

      // Check if our selected item changed
      if (selectedItem != _previouslySelectedItem) {
         itemSelectionJustChanged();
      }

      // Keep track of what item was selected this frame
      _previouslySelectedItem = selectedItem;
   }

   public void requestInventoryFromServer (int pageNumber=-1) {
      if (pageNumber >= 0) {
         _pageNumber = pageNumber;
      }

      // Get the latest info from the server to show in our character stack
      Global.player.rpc.Cmd_RequestItemsFromServer(_pageNumber, ITEMS_PER_PAGE);
   }

   public void OnPointerClick (PointerEventData eventData) {
      // If the black background outside is clicked, hide the Inventory panel
      if (eventData.rawPointerPress == this.gameObject) {
         PanelManager.self.popPanel();
      }
   }

   public void setItemsToDisplay (int gold, int gems, int totalItemCount, List<Item> itemList) {
      selectedItemId = 0;

      // Update the gold adn gems count, with commas in thousands place
      goldText.text = string.Format("{0:n0}", gold);
      gemsText.text = string.Format("{0:n0}", gems);

      // Cycle over all of the items
      foreach (Item item in itemList) {
         ColorKey colorKey = null;

         // Create a new row instance, set the parent and name
         ItemRow row = Instantiate(itemRowPrefab);
         row.item = item;
         row.transform.SetParent(itemRowContainer.transform);
         row.name = "Item Row - " + item.getName() + " - " + item.id;

         // Update the icon
         if (item.category == Item.Category.Armor) {
            row.itemIcon.sprite = ImageManager.getSprite("Icons/Armor/" + ((Armor.Type)item.itemTypeId));
            colorKey = new ColorKey(Global.player.gender, (Armor.Type) item.itemTypeId);
         } else if (item.category == Item.Category.Weapon) {
            row.itemIcon.sprite = ImageManager.getSprite("Icons/Weapons/" + ((Weapon.Type) item.itemTypeId));
            colorKey = new ColorKey(Global.player.gender, (Weapon.Type) item.itemTypeId);
         } else if (item.category == Item.Category.Potion) {
            row.itemIcon.sprite = foodIcon;
         } else if (item.category == Item.Category.Usable) {
            row.itemIcon.sprite = itemIcon;
         } else if (item.category == Item.Category.Blueprint) {
            row.itemIcon.sprite = blueprintIcon;
         } else if (item.category == Item.Category.CraftingIngredients) {
            row.itemIcon.sprite = ImageManager.getSprite("Icons/CraftingIngredients/" + ((CraftingIngredients.Type) item.itemTypeId));
         } else {
            row.itemIcon.sprite = itemIcon;
         }

         // Recolor
         if (colorKey != null) {
            row.itemIcon.GetComponent<RecoloredSprite>().recolor(colorKey, item.color1, item.color2);
         }

         // Update the name and count
         row.itemText.text = item.getName();
         row.itemCount.text = item.count + "";

         if (item is EquippableItem) {
            // Show the appropriate color boxes
            row.colorBox1.color = ColorDef.get(item.color1).color;
            row.colorBox1.enabled = MaterialManager.self.getRecolorCount(colorKey) >= 1;
            row.colorBox2.color = ColorDef.get(item.color2).color;
            row.colorBox2.enabled = MaterialManager.self.getRecolorCount(colorKey) >= 2;

            // Show the E icon for equipped items
            row.equippedIcon.enabled = isEquipped(item.id);
         } else {
            row.colorBox1.enabled = false;
            row.colorBox2.enabled = false;
            row.equippedIcon.enabled = false;
         }
      }

      // Store them for later reference
      _itemList = itemList;
   }

   public void equipSelected () {
      Item selectedItem = getSelectedItem();

      // Check which type of item we requested to equip/unequip
      if (selectedItem is Weapon) {
         // Check if it's currently equipped or not
         int itemIdToSend = isEquipped(selectedItemId) ? 0 : selectedItemId;
         Global.player.rpc.Cmd_RequestSetWeaponId(itemIdToSend);
      } else if (selectedItem is Armor) {
         // Check if it's currently equipped or not
         int itemIdToSend = isEquipped(selectedItemId) ? 0 : selectedItemId;
         Global.player.rpc.Cmd_RequestSetArmorId(itemIdToSend);
      }
   }

   public void useSelected () {
      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => confirmedUseItem(selectedItemId));

      // Show a confirmation panel
      PanelManager.self.confirmScreen.show("Are you sure you want to use your " + getSelectedItem().getName() + "?");
   }

   public void trashSelected () {
      if (selectedItemId <= 0 || getSelectedItem() == null) {
         D.warning("No selected item available to trash.");
         return;
      }

      if (!getSelectedItem().canBeTrashed()) {
         PanelManager.self.noticeScreen.show("This item can not be trashed.");
         return;
      }

      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => sendDeleteItemRequest(selectedItemId));

      // Show a confirmation panel with the user name
      PanelManager.self.confirmScreen.show("Are you sure you want to trash " + getSelectedItem().getName() + "?");
   }

   public void adjustPageNumber (int amount) {
      _pageNumber += amount;

      // Make sure we haven't gone beyond the limit
      if (_pageNumber <= 0) {
         _pageNumber = getMaxPageNumber();

      } else if (_pageNumber > getMaxPageNumber()) {
         _pageNumber = 1;
      }

      // Then send a request to the server for the new data
      requestInventoryFromServer();
   }

   public void updateEquippedIcons () {
      // Cycle over our item rows to find the equippable items
      foreach (ItemRow row in itemRowContainer.GetComponentsInChildren<ItemRow>()) {
         if (row.item is EquippableItem) {
            row.equippedIcon.enabled = isEquipped(row.item.id);
         }
      }
   }

   public void setEquippedIds(int armorId, int weaponId) {
      _equippedArmorId = armorId;
      _equippedWeaponId = weaponId;

      // Now update our icons in case they need to change
      updateEquippedIcons();
   }

   public void removeDeletedItem (int itemId) {
      ItemRow rowToDelete = null;

      // Cycle over our item rows
      foreach (ItemRow row in itemRowContainer.GetComponentsInChildren<ItemRow>()) {
         if (row.item.id == itemId) {
            rowToDelete = row;
         }
      }

      // If we found the row, delete it now
      if (rowToDelete != null) {
         Destroy(rowToDelete.gameObject);
      }

      // If this item was selected, unselect it
      if (selectedItemId == itemId) {
         selectedItemId = 0;
      }

      // If this item was marked as equipped, unmark it
      if (_equippedArmorId == itemId) {
         _equippedArmorId = 0;
      } else if (_equippedWeaponId == itemId) {
         _equippedWeaponId = 0;
      }
   }

   public void receiveItemsFromServer (UserObjects userObjects, int pageNumber, int gold, int gems, int totalItemCount, int equippedArmorId, int equippedWeaponId, Item[] itemArray) {
      _userObjects = userObjects;
      _pageNumber = pageNumber;
      _totalItemCount = totalItemCount;

      // Clear out any current items
      itemRowContainer.DestroyChildren();

      // Recreate the item list as the proper subclasses, now that they've been through the serialization process
      List<Item> itemList = new List<Item>();
      foreach (Item item in itemArray) {
#if UNITY_EDITOR
         itemList.Add(item.getCastItem());
#else
         if (item.category != Item.Category.CraftingIngredients) {
            itemList.Add(item.getCastItem());
         }
#endif
      }

      // Add the new items that we received
      setItemsToDisplay(gold, gems, totalItemCount, itemList);

      // Update the equipped icons
      setEquippedIds(equippedArmorId, equippedWeaponId);

      // Update our character preview stack
      self.characterStack.updateLayers(userObjects);
   }

   protected int getMaxPageNumber () {
      // Default to the first page
      int maxPageNumber = 1;

      // Divide the total number of items this player has by the number of items we want to display on each page
      maxPageNumber = ((_totalItemCount - 1) / ITEMS_PER_PAGE) + 1;

      // Can't be less than 1
      if (maxPageNumber <= 0) {
         maxPageNumber = 1;
      }

      return maxPageNumber;
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

   protected void sendDeleteItemRequest (int itemId) {
      Global.player.rpc.Cmd_DeleteItem(itemId);
   }

   protected Item getSelectedItem () {
      Item selectedItem = null;

      foreach (Item item in _itemList) {
         if (item.id == selectedItemId) {
            selectedItem = item;
         }
      }

      return selectedItem;
   }

   protected void confirmedUseItem (int itemId) {
      PanelManager.self.confirmScreen.hide();
      Global.player.rpc.Cmd_UseItem(selectedItemId);
   }

   protected void itemSelectionJustChanged () {
      Item selectedItem = getSelectedItem();

      // First, reset our character to default
      characterStack.updateLayers(_userObjects);

      // Let us preview certain item types
      /*if (selectedItem is Weapon) {
         Weapon weapon = (Weapon) selectedItem;
         characterStack.updateWeapon(_userObjects.userInfo.gender, weapon.type, weapon.color1, weapon.color2);
      } else if (selectedItem is Armor) {
         Armor armor = (Armor) selectedItem;
         characterStack.updateArmor(_userObjects.userInfo.gender, armor.type, armor.color1, armor.color2);
      } else if (selectedItem is UsableItem) {
         UsableItem usable = (UsableItem) selectedItem;
         if (usable.itemType == UsableItem.Type.HairDye) {
            characterStack.updateHair(_userObjects.userInfo.hairType, usable.color1, _userObjects.userInfo.hairColor2);
         } else if (usable.itemType == UsableItem.Type.Haircut) {
            characterStack.updateHair(usable.getHairType(), _userObjects.userInfo.hairColor1, _userObjects.userInfo.hairColor2);
         }
      }*/
   }

#region Private Variables

   // The most recent list of items we displayed
   protected List<Item> _itemList = new List<Item>();

   // The current page number we're on
   protected int _pageNumber = 1;

   // The total number of items we have
   protected int _totalItemCount = 0;

   // The currently equipped armor id
   protected static int _equippedArmorId;

   // The current equipped weapon id
   protected static int _equippedWeaponId;

   // The last set of User Objects that we received
   protected UserObjects _userObjects;

   // The item that was selected in the previous frame
   protected Item _previouslySelectedItem;

#endregion
}
