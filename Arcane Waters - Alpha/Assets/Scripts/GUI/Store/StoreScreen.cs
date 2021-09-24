using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System.Linq;
using Store;
using static Store.StoreItem;
using Newtonsoft.Json;
using Steamworks;
using static PaletteToolManager;

public class StoreScreen : Panel
{
   #region Public Variables

   // The text that displays our Gold count
   public Text goldText;

   // The text that displays our Gems count
   public Text gemsText;

   // The text that displays our character's name
   public Text nameText;

   // The text that displays the title of the currently selected item
   public Text itemTitleText;

   // The text that displays a description of the currently selected item
   public Text descriptionText;

   // The currently selected item, if any
   public StoreItemBox selectedItem;

   // An image we use to show which item is selected, if any
   public Image itemSelectionOutline;

   // The button for buying the selected item
   public Button buyButton;

   // The container for our store items
   public GameObject itemsContainer;

   // Our Character Stack
   public CharacterStack characterStack;

   // Reference to the tabs holder
   public AdminPanelTabs tabs;

   // The reference to the load blocker
   public GameObject loadBlocker;

   // Self
   public static StoreScreen self;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;
   }

   public override void Update () {
      base.Update();

      // Enable or disable stuff based on whether there's a selection
      buyButton.interactable = (selectedItem != null);
   }

   public void showPanel (int gold, int gems) {
      toggleBlocker(true);

      // Show our gold and gem count
      this.goldText.text = gold + "";
      this.gemsText.text = gems + "";
      this.nameText.text = Global.userObjects.userInfo.username;
      
      destroyStoreBoxes();
      deselectAllItems();
      updateCharacterPreview(showPreview: false);

      // Request Store Items
      Global.player.rpc.Cmd_RequestStoreResources(requestStoreItems: true);

      // Display the panel
      if (!isShowing()) {
         PanelManager.self.linkPanel(Type.Store);
      }
   }

   public void refreshPanel () {
      // Request Store Items
      Global.player.rpc.Cmd_RequestStoreResources(requestStoreItems: false);
   }

   public void onReceiveStoreResources (List<StoreItem> storeItems) {
      deselectAllItems();

      if (Global.userObjects != null) {
         this.nameText.text = Global.userObjects.userInfo.username;
         updateCharacterPreview(showPreview: true);
      }

      if (storeItems == null || storeItems.Count == 0) {
         storeItems = _storeItemsCache;
      }

      if (storeItems != null && storeItems.Count > 0) {
         this._storeItemsCache = storeItems;
         destroyStoreBoxes();

         // Create Boxes
         createStoreBoxes(storeItems);

         // Routinely check if we purchased gems
         CancelInvoke(nameof(checkGems));
         InvokeRepeating(nameof(checkGems), 5f, 5f);

         // Initialize tabs
         initializeTabs();

         // If no tab is selected, display the gems tab
         if (_currentStoreTabType == StoreTab.StoreTabType.None) {
            _currentStoreTabType = StoreTab.StoreTabType.Gems;
         }

         filterItems();
      }

      toggleBlocker(false);
   }

   private void toggleBlocker (bool show) {
      if (loadBlocker == null) {
         return;
      }

      loadBlocker.SetActive(show);
   }

   public void updateGemsAmount (int amount) {
      this.gemsText.text = amount + "";
   }

   public void buyItemButtonPressed () {
      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => onConfirmPurchase());

      // Show a confirmation panel with the user name
      PanelManager.self.confirmScreen.show("Are you sure you want to buy " + selectedItem.itemName + "?");
   }

   protected void onConfirmPurchase () {
      PanelManager.self.confirmScreen.hide();

      // Can't buy a null item
      if (selectedItem == null) {
         D.debug("Store item selected was null. Canceling Purchase.");
         return;
      }

      D.debug("Store purchase confirmed. Transferring control to server...");

      // Send the request off to the server
      uint appId = 0;

      if (SteamManager.Initialized) {
         appId = SteamUtils.GetAppID().m_AppId;
      }

      toggleBlocker(show: true);

      Global.player.rpc.Cmd_BuyStoreItem(selectedItem.itemId, appId);

      D.debug("Store purchase: Control transferred.");
   }

   private void updateCharacterPreview (bool showPreview) {
      this.characterStack.gameObject.SetActive(showPreview);
      StoreItemBox itemBox = this.selectedItem;

      // Did they unselect the current item?
      if (itemBox == null) {
         this.selectedItem = null;
         this.itemTitleText.text = "";
         this.descriptionText.text = "";
         this.characterStack.updateLayers(Global.userObjects);
      } else {
         this.selectedItem = itemBox;
         itemTitleText.text = itemBox.itemName;
         descriptionText.text = itemBox.itemDescription;

         if (itemBox is StoreHairDyeBox) {
            StoreHairDyeBox hairBox = (StoreHairDyeBox) itemBox;
            characterStack.updateHair(Global.userObjects.userInfo.hairType, hairBox.palette.paletteName);
         } else if (itemBox is StoreHaircutBox) {
            StoreHaircutBox hairBox = (StoreHaircutBox) itemBox;
            characterStack.updateHair(hairBox.haircut.type, Global.userObjects.userInfo.hairPalettes);
         } else if (itemBox is StoreArmorDyeBox) {
            StoreArmorDyeBox armorBox = (StoreArmorDyeBox) itemBox;
            ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(Global.userObjects.armor.itemTypeId);

            if (armorData == null) {
               armorData = EquipmentXMLManager.self.armorStatList.First();
            }

            if (armorData != null) {
               characterStack.updateArmor(Global.player.gender, armorData.armorType, armorBox.palette.paletteName);
            }

            itemBox.select();
         }
      }

      characterStack.synchronizeAnimationIndexes();
   }

   public void onStoreItemBoxClicked(StoreItemBox itemBox) {
      bool wasSelected = itemBox.isSelected();
      deselectAllItems();

      if (!wasSelected) {
         this.selectedItem = itemBox;
         this.selectedItem.select();
      }

      updateCharacterPreview(showPreview: true);
   }

   private void deselectAllItems () {
      this.selectedItem = null;

      foreach (StoreItemBox itemBox in itemsContainer.GetComponentsInChildren<StoreItemBox>(true)) {
         itemBox.toggleHover(false);
         itemBox.toggleSelection(false);
      }
   }

   public void filterItems () {
      // Go through all of our items and toggle which ones are showing
      foreach (StoreItemBox itemBox in itemsContainer.GetComponentsInChildren<StoreItemBox>(true)) {
         bool show = itemBox.storeTabCategory == _currentStoreTabType;
         itemBox.gameObject.SetActive(show);

         if (_currentStoreTabType == StoreTab.StoreTabType.Haircuts && itemBox is StoreHaircutBox haircutItemBox) {
            haircutItemBox.gameObject.SetActive(haircutItemBox.haircut.getGender() == Global.player.gender);
         }
      }

      // Update layout based on the selected tab
      updateBoxContainerLayout(_currentStoreTabType);
   }

   private void updateBoxContainerLayout (StoreTab.StoreTabType tabType) {
      GridLayoutGroup gridLayout = itemsContainer.GetComponent<GridLayoutGroup>();

      if (gridLayout == null) {
         return;
      }

      if (tabType == StoreTab.StoreTabType.ShipSkins) {
         gridLayout.cellSize = new Vector2(148, 200);
         gridLayout.spacing = new Vector2(16, 16);
         gridLayout.constraintCount = 3;
      } else {
         gridLayout.cellSize = new Vector2(100, 150);
         gridLayout.spacing = new Vector2(24, 16);
         gridLayout.constraintCount = 4;
      }
   }

   private void initializeTabs () {
      if (tabs == null) {
         return;
      }

      tabs.onTabPressed.RemoveAllListeners();
      tabs.onTabPressed.AddListener(performTabSwitch);
   }

   public void performTabSwitch (int tabIndex) {
      _currentStoreTabType = (StoreTab.StoreTabType) (tabIndex + 1);
      D.debug($"New Store Tab Type is: { _currentStoreTabType }");
      filterItems();
   }

   protected void checkGems () {
      // If the store is showing and we have a player, get an updated Gems count
      if (this.isShowing() && Global.player != null) {
         Global.player.rpc.Cmd_UpdateGems();
      }
   }

   #region Boxes

   private void prepareStoreItemBox (StoreItem item, StoreItemBox box) {
      box.transform.SetParent(this.transform);
      box.itemId = item.id;
      box.itemCost = item.price;
      box.itemQuantity = item.quantity;
      box.itemCategory = item.category;
   }

   private void tryOverrideNameAndDescription (StoreItem storeItem, StoreItemBox box) {
      if (storeItem == null || box == null) {
         return;
      }

      if (storeItem.overrideItemName) {
         box.itemName = storeItem.displayName;
      }

      if (storeItem.overrideItemDescription) {
         box.itemDescription = storeItem.displayDescription;
      }
   }

   protected StoreGemBox createGemBox (StoreItem storeItem) {
      GemsData gemsData = GemsXMLManager.self.getGemsData(storeItem.itemId);

      if (gemsData == null) {
         return null;
      }

      StoreGemBox box = Instantiate(PrefabsManager.self.gemBoxPrefab);
      prepareStoreItemBox(storeItem, box);
      box.gemsBundle = gemsData;
      box.itemName = box.gemsBundle.itemName;
      box.itemDescription = box.gemsBundle.itemDescription;
      tryOverrideNameAndDescription(storeItem, box);
      box.initialize();
      return box;
   }

   protected StoreHaircutBox createHaircutBox (StoreItem storeItem) {
      HaircutData haircutData = HaircutXMLManager.self.getHaircutData(storeItem.itemId);

      if (haircutData == null) {
         return null;
      }

      StoreHaircutBox box = Instantiate(PrefabsManager.self.haircutBoxPrefab);
      prepareStoreItemBox(storeItem, box);
      box.haircut = haircutData;
      box.itemName = box.haircut.itemName;
      box.itemDescription = box.haircut.itemDescription;
      tryOverrideNameAndDescription(storeItem, box);
      box.initialize();
      return box;
   }

   protected StoreShipSkinBox createShipSkinBox (StoreItem storeItem) {
      ShipSkinData shipSkinData = ShipSkinXMLManager.self.getShipSkinData(storeItem.itemId);

      if (shipSkinData == null) {
         return null;
      }

      StoreShipSkinBox box = Instantiate(PrefabsManager.self.shipBoxPrefab);
      prepareStoreItemBox(storeItem, box);
      box.shipSkin = shipSkinData;
      box.itemName = box.shipSkin.itemName;
      box.itemDescription = box.shipSkin.itemDescription;
      tryOverrideNameAndDescription(storeItem, box);
      box.initialize();
      return box;
   }

   protected StoreItemBox createDyeBox (StoreItem storeItem) {
      DyeData dyeData = DyeXMLManager.self.getDyeData(storeItem.itemId);

      if (dyeData == null) {
         return null;
      }

      PaletteToolData palette = PaletteSwapManager.self.getPalette(dyeData.paletteId);

      if (palette == null) {
         return null;
      }

      if (palette.paletteType == (int) PaletteImageType.Hair) {
         if (palette.isPrimary()) {
            return createHairDyeBox(storeItem);
         }
      }

      if (palette.paletteType == (int) PaletteImageType.Armor) {

         bool isActuallyHat = false;

         if (palette.paletteName.ToLower().StartsWith("hat")) {
            isActuallyHat = true;
         }

         if (isActuallyHat) {
            return createHatDyeBox(storeItem);
         } else {
            return createArmorDyeBox(storeItem);
         }
      }

      if (palette.paletteType == (int) PaletteImageType.Weapon) {
         return createWeaponDyeBox(storeItem);
      }

      return null;
   }

   protected StoreHairDyeBox createHairDyeBox (StoreItem storeItem) {
      DyeData dyeData = DyeXMLManager.self.getDyeData(storeItem.itemId);

      if (dyeData == null) {
         return null;
      }

      StoreHairDyeBox box = Instantiate(PrefabsManager.self.hairDyeBoxPrefab);
      prepareStoreItemBox(storeItem, box);
      box.hairdye = dyeData;
      box.itemName = box.hairdye.itemName;
      box.itemDescription = box.hairdye.itemDescription;
      tryOverrideNameAndDescription(storeItem, box);
      box.initialize();
      return box;
   }

   protected StoreArmorDyeBox createArmorDyeBox (StoreItem storeItem) {
      DyeData dyeData = DyeXMLManager.self.getDyeData(storeItem.itemId);

      if (dyeData == null) {
         return null;
      }

      StoreArmorDyeBox box = Instantiate(PrefabsManager.self.armorDyeBoxPrefab);
      prepareStoreItemBox(storeItem, box);
      box.armorDye = dyeData;
      box.itemName = box.armorDye.itemName;
      box.itemDescription = box.armorDye.itemDescription;
      box.playerArmor = Global.userObjects.armor;
      tryOverrideNameAndDescription(storeItem, box);
      box.initialize();
      return box;
   }

   protected StoreWeaponDyeBox createWeaponDyeBox (StoreItem storeItem) {
      DyeData dyeData = DyeXMLManager.self.getDyeData(storeItem.itemId);

      if (dyeData == null) {
         return null;
      }

      StoreWeaponDyeBox box = Instantiate(PrefabsManager.self.weaponDyeBoxPrefab);
      prepareStoreItemBox(storeItem, box);
      box.weaponDye = dyeData;
      box.itemName = box.weaponDye.itemName;
      box.itemDescription = box.weaponDye.itemDescription;
      box.playerWeapon = Global.userObjects.weapon;
      tryOverrideNameAndDescription(storeItem, box);
      box.initialize();
      return box;
   }

   protected StoreHatDyeBox createHatDyeBox (StoreItem storeItem) {
      DyeData dyeData = DyeXMLManager.self.getDyeData(storeItem.itemId);

      if (dyeData == null) {
         return null;
      }

      StoreHatDyeBox box = Instantiate(PrefabsManager.self.hatDyeBoxPrefab);
      prepareStoreItemBox(storeItem, box);
      box.hatDye = dyeData;
      box.itemName = box.hatDye.itemName;
      box.itemDescription = box.hatDye.itemDescription;
      box.playerHat = Global.userObjects.hat;
      tryOverrideNameAndDescription(storeItem, box);
      box.initialize();
      return box;
   }

   protected StoreHairDyeBox createPetBox (StoreItem storeItem) {
      StoreHairDyeBox box = Instantiate(PrefabsManager.self.hairDyeBoxPrefab);
      prepareStoreItemBox(storeItem, box);
      tryOverrideNameAndDescription(storeItem, box);
      return box;
   }

   protected StoreConsumableBox createConsumableBox (StoreItem storeItem) {
      ConsumableData consumableData = ConsumableXMLManager.self.getConsumableData(storeItem.itemId);

      if (consumableData == null) {
         return null;
      }

      StoreConsumableBox box = Instantiate(PrefabsManager.self.consumableBoxPrefab);
      prepareStoreItemBox(storeItem, box);
      box.consumable = consumableData;
      box.itemName = box.consumable.itemName;
      box.itemDescription = box.consumable.itemDescription;
      tryOverrideNameAndDescription(storeItem, box);
      box.initialize();
      return box;
   }

   private void destroyStoreBoxes () {
      if (itemsContainer == null) {
         return;
      }

      itemsContainer.DestroyChildren();
   }

   protected void createStoreBoxes (List<StoreItem> items) {
      if (_storeItemBoxes == null) {
         _storeItemBoxes = new List<StoreItemBox>();
      }

      _storeItemBoxes.Clear();

      foreach (StoreItem item in items) {
         if (item == null || !item.isEnabled) {
            continue;
         }

         StoreItemBox createdBox = null;

         switch (item.category) {
            case Item.Category.None:
               break;
            case Item.Category.Gems:
               createdBox = createGemBox(item);
               break;
            case Item.Category.ShipSkin:
               createdBox = createShipSkinBox(item);
               break;
            case Item.Category.Haircut:
               createdBox = createHaircutBox(item);
               break;
            case Item.Category.Dye:
               createdBox = createDyeBox(item);
               break;
            case Item.Category.Hats:
               createdBox = createHatDyeBox(item);
               break;
            case Item.Category.Pet:
               createdBox = createPetBox(item);
               break;
            case Item.Category.Consumable:
               createdBox = createConsumableBox(item);
               break;
         }

         if (createdBox == null) {
            continue;
         }

         _storeItemBoxes.Add(createdBox);

         // Try to add the box to the container here (Testing)
         createdBox.transform.SetParent(itemsContainer.transform);
      }
   }

   public StoreItemBox[] getBoxesOfCategory (Item.Category category) {
      return _storeItemBoxes.Where(_ => _.itemCategory == category).ToArray();
   }

   public T[] getBoxesOfType<T> () {
      return _storeItemBoxes.OfType<T>().ToArray();
   }

   public StoreItemBox[] getAllBoxes () {
      return _storeItemBoxes.ToArray();
   }

   #endregion

   #region Private Variables

   // Store values of all boxes used for choosing hair palette; Key is box hashes
   private Dictionary<int, string> _paletteHairDye = new Dictionary<int, string>();

   // Current Store Tab
   private StoreTab.StoreTabType _currentStoreTabType;

   // Store Items
   private List<StoreItemBox> _storeItemBoxes = new List<StoreItemBox>();

   // Store Items Cache
   private List<StoreItem> _storeItemsCache;

   #endregion
}
