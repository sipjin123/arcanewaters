using System.Collections.Generic;
using System.Linq;
using Steamworks;
using Store;
using UnityEngine;
using UnityEngine.UI;
using static PaletteToolManager;

public class StoreScreen : Panel
{
   #region Public Variables

   // The flag value for palettes/dyes that should be available in the store
   public const int GEM_STORE_TAG = 6;

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

   // The reference to the label holding the current tab's name
   public Text tabTitleText;

   // The reference to the label holding the price of the item in the description container
   public Text descPriceText;

   // The reference to the store filter
   public StoreFilter storeFilter;

   // The reference to the Help Button
   public GameObject helpButton;

   // The reference to the control holding the store boxes
   public GridLayoutGroup gridLayout;

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

   private void OnEnable () {
      storeFilter.onFilterChanged.RemoveAllListeners();
      storeFilter.onFilterChanged.AddListener(onStoreFilterValueChanged);
   }

   private void OnDisable () {
      storeFilter.onFilterChanged.RemoveAllListeners();
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

      // Reset filter
      _currentFilterOption = "";

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

   public void onReceiveStoreResources (StoreResourcesResponse resources) {
      // The store is disabled
      if (!resources.isStoreEnabled) {
         if (PanelManager.self) {
            PanelManager.self.noticeScreen.confirmButton.onClick.RemoveAllListeners();
            PanelManager.self.noticeScreen.confirmButton.onClick.AddListener(() => {
               PanelManager.self.noticeScreen.hide();
            });
            PanelManager.self.noticeScreen.show(resources.gemStoreIsDisabledMessage);
         }

         hide();
         return;
      }

      deselectAllItems();

      if (Global.userObjects != null) {
         this.nameText.text = Global.userObjects.userInfo.username;
         updateCharacterPreview(showPreview: true);
      }

      List<StoreItem> storeItems = resources.items;

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

         performTabSwitch((int) _currentStoreTabType - 1);
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
      StoreItemBox itemBox = selectedItem;

      // Refresh
      this.characterStack.updateLayers(Global.userObjects);

      // Did they unselect the current item?
      if (itemBox == null) {
         selectedItem = null;
         itemTitleText.text = "";
         descriptionText.text = "";
         descPriceText.text = "---";
         characterStack.updateLayers(Global.userObjects);

         characterStack.synchronizeAnimationIndexes();
         return;
      }

      selectedItem = itemBox;
      itemTitleText.text = itemBox.itemName;
      descriptionText.text = itemBox.itemDescription;
      descPriceText.text = itemBox.itemCost.ToString();

      if (itemBox is StoreDyeBox dyeItemBox) {
         // Try to compute a description text
         descriptionText.text = computeDyeDescription(dyeItemBox.palette);
      }

      if (itemBox is StoreGemBox storeGemBox) {
         descPriceText.text = storeGemBox.getDisplayCost();
      } else {
         descPriceText.text += " GEMS";
      }

      if (itemBox is StoreHairDyeBox hairDyeBox) {
         string mergedPalette = Item.parseItmPalette(Item.overridePalette(hairDyeBox.palette.paletteName, Global.userObjects.userInfo.hairPalettes));
         characterStack.updateHair(Global.userObjects.userInfo.hairType, mergedPalette);

      } else if (itemBox is StoreHaircutBox hairBox) {
         characterStack.updateHair(hairBox.haircut.type, Global.userObjects.userInfo.hairPalettes);

      } else if (itemBox is StoreArmorDyeBox armorDyeBox) {
         ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataBySqlId(Global.userObjects.armor.itemTypeId);
         string mergedPalette = Item.parseItmPalette(Item.overridePalette(armorDyeBox.palette.paletteName, Global.userObjects.armor.paletteNames));

         if (armorData == null) {
            armorData = EquipmentXMLManager.self.armorStatList.First();
         }

         if (armorData != null) {
            characterStack.updateArmor(Global.player.gender, armorData.armorType, mergedPalette);
         }

      } else if (itemBox is StoreWeaponDyeBox weaponDyeBox) {
         WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(Global.userObjects.weapon.itemTypeId);
         string mergedPalette = Item.parseItmPalette(Item.overridePalette(weaponDyeBox.palette.paletteName, Global.userObjects.weapon.paletteNames));

         if (weaponData == null) {
            weaponData = EquipmentXMLManager.self.weaponStatList.First();
         }

         if (weaponData != null) {
            characterStack.updateWeapon(Global.player.gender, weaponData.weaponType, mergedPalette);
         }

      } else if (itemBox is StoreHatDyeBox hatDyeBox) {
         HatStatData hatData = EquipmentXMLManager.self.getHatData(Global.userObjects.hat.itemTypeId);
         string mergedPalette = Item.parseItmPalette(Item.overridePalette(hatDyeBox.palette.paletteName, Global.userObjects.hat.paletteNames));

         if (hatData == null) {
            hatData = EquipmentXMLManager.self.hatStatList.First();
         }

         if (hatData != null) {
            characterStack.updateHats(Global.player.gender, hatData.hatType, mergedPalette);
         }

      } else if (itemBox is StoreHatBox hatBox) {
         HatStatData hatData = EquipmentXMLManager.self.getHatData(hatBox.hat.sqlId);

         if (hatData == null) {
            hatData = EquipmentXMLManager.self.hatStatList.First();
         }

         if (hatData != null) {
            characterStack.updateHats(Global.player.gender, hatData.hatType, string.Empty);
         }
      }

      itemBox.select();
      characterStack.synchronizeAnimationIndexes();
   }

   public void onStoreItemBoxClicked (StoreItemBox itemBox) {
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

      foreach (StoreItemBox itemBox in _storeItemBoxes) {
         itemBox.toggleHover(false);
         itemBox.toggleSelection(false);
      }
   }

   public void filterItems () {
      // Go through all of our items and toggle which ones are showing
      _currentFilterOption = storeFilter.getCurrentOptionText();
      foreach (StoreItemBox itemBox in _storeItemBoxes) {
         bool show = itemBox.storeTabCategory == _currentStoreTabType;
         itemBox.gameObject.SetActive(show);

         if (_currentStoreTabType == StoreTab.StoreTabType.Haircuts && itemBox is StoreHaircutBox haircutItemBox) {
            haircutItemBox.gameObject.SetActive(haircutItemBox.haircut.getGender() == Global.player.gender);
         }

         if (shouldShowStoreFilter()) {
            if (!Util.areStringsEqual(_currentFilterOption, StoreFilterConstants.ALL)) {
               if (isDyeTab()) {
                  // Apply the filter (if necessary)
                  if (itemBox is StoreDyeBox dyeBox) {
                     if (Util.areStringsEqual(_currentFilterOption, StoreFilterConstants.PRIMARY)) {
                        if (!dyeBox.palette.isPrimary()) {
                           itemBox.gameObject.SetActive(false);
                        }
                     }

                     if (Util.areStringsEqual(_currentFilterOption, StoreFilterConstants.SECONDARY)) {
                        if (!dyeBox.palette.isSecondary()) {
                           itemBox.gameObject.SetActive(false);
                        }
                     }

                     if (Util.areStringsEqual(_currentFilterOption, StoreFilterConstants.ACCENT)) {
                        if (!dyeBox.palette.isAccent()) {
                           itemBox.gameObject.SetActive(false);
                        }
                     }
                  }
               }

               if (_currentStoreTabType == StoreTab.StoreTabType.ShipSkins) {
                  if (itemBox is StoreShipSkinBox shipSkinBox && !_currentFilterOption.Equals(shipSkinBox.getShipName(), System.StringComparison.OrdinalIgnoreCase)) {
                     itemBox.gameObject.SetActive(false);
                  }
               }
            }
         }
      }

      // Update layout based on the selected tab
      updateBoxContainerLayout(_currentStoreTabType);
   }

   private void updateBoxContainerLayout (StoreTab.StoreTabType tabType) {
      if (gridLayout == null) {
         return;
      }

      if (tabType == StoreTab.StoreTabType.ShipSkins) {
         gridLayout.cellSize = new Vector2(140, 200);
         gridLayout.spacing = new Vector2(8, 8);
         gridLayout.constraintCount = 4;
      } else if (tabType == StoreTab.StoreTabType.Gems) {
         gridLayout.cellSize = new Vector2(130, 180);
         gridLayout.spacing = new Vector2(0, 0);
         gridLayout.constraintCount = 4;
      } else {
         gridLayout.cellSize = new Vector2(87, 150);
         gridLayout.spacing = new Vector2(12, 16);
         gridLayout.constraintCount = 6;
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
      deselectAllItems();
      updateCharacterPreview(showPreview: true);
      updateTabTitle();
      regenerateStoreFilter();
      updateStoreFilter();
      filterItems();
      updateHelp();
   }

   private void updateTabTitle () {
      if (tabTitleText == null) {
         return;
      }

      string title = "";

      switch (_currentStoreTabType) {
         case StoreTab.StoreTabType.None:
            break;
         case StoreTab.StoreTabType.Gems:
         case StoreTab.StoreTabType.Haircuts:
         case StoreTab.StoreTabType.Consumables:
            title = _currentStoreTabType.ToString();
            break;
         case StoreTab.StoreTabType.HairDyes:
            title = "Hair Dyes";
            break;
         case StoreTab.StoreTabType.ArmorDyes:
            title = "Armor Dyes";
            break;
         case StoreTab.StoreTabType.HatDyes:
            title = "Hat Dyes";
            break;
         case StoreTab.StoreTabType.WeaponDyes:
            title = "Weapon Dyes";
            break;
         case StoreTab.StoreTabType.ShipSkins:
            title = "Ship Skins";
            break;
         case StoreTab.StoreTabType.Hats:
            title = "Hats";
            break;
      }

      tabTitleText.text = title;
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
      box.title = $"{storeItem.quantity} Gems";
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

   protected StoreHatBox createHatBox(StoreItem storeItem) {
      HatStatData hatStatData = EquipmentXMLManager.self.getHatData(storeItem.itemId);

      if (hatStatData == null) {
         return null;
      }

      StoreHatBox box = Instantiate(PrefabsManager.self.hatBoxPrefab);
      prepareStoreItemBox(storeItem, box);
      box.hat = hatStatData;
      tryOverrideNameAndDescription(storeItem, box);
      box.initialize();
      return box;
   }

   #region Dye

   protected StoreItemBox createDyeBox (StoreItem storeItem) {
      PaletteToolData palette = PaletteSwapManager.self.getPalette(storeItem.itemId);

      if (palette == null) {
         return null;
      }

      PaletteImageType paletteImageType = (PaletteImageType) palette.paletteType;
      StoreItemBox createdBox = null;

      switch (paletteImageType) {
         case PaletteImageType.Armor:
            createdBox = createArmorDyeBox(storeItem);
            break;
         case PaletteImageType.Hair:
            createdBox = createHairDyeBox(storeItem);
            break;
         case PaletteImageType.Hat:
            createdBox = createHatDyeBox(storeItem);
            break;
         case PaletteImageType.Weapon:
            createdBox = createWeaponDyeBox(storeItem);
            break;
      }

      return createdBox;
   }

   protected StoreHairDyeBox createHairDyeBox (StoreItem storeItem) {
      PaletteToolData palette = PaletteSwapManager.self.getPalette(storeItem.itemId);

      if (palette == null) {
         return null;
      }

      StoreHairDyeBox box = Instantiate(PrefabsManager.self.hairDyeBoxPrefab);
      prepareStoreItemBox(storeItem, box);
      box.palette = palette;
      box.itemName = palette.paletteDisplayName;
      box.itemDescription = palette.paletteDescription;
      tryOverrideNameAndDescription(storeItem, box);
      box.initialize();
      return box;
   }

   protected StoreArmorDyeBox createArmorDyeBox (StoreItem storeItem) {
      PaletteToolData palette = PaletteSwapManager.self.getPalette(storeItem.itemId);

      if (palette == null) {
         return null;
      }

      StoreArmorDyeBox box = Instantiate(PrefabsManager.self.armorDyeBoxPrefab);
      prepareStoreItemBox(storeItem, box);
      box.palette = palette;
      box.itemName = palette.paletteDisplayName;
      box.itemDescription = palette.paletteDescription;
      box.playerArmor = Global.userObjects.armor;
      tryOverrideNameAndDescription(storeItem, box);
      box.initialize();
      return box;
   }

   protected StoreWeaponDyeBox createWeaponDyeBox (StoreItem storeItem) {
      PaletteToolData palette = PaletteSwapManager.self.getPalette(storeItem.itemId);

      if (palette == null) {
         return null;
      }

      StoreWeaponDyeBox box = Instantiate(PrefabsManager.self.weaponDyeBoxPrefab);
      prepareStoreItemBox(storeItem, box);
      box.palette = palette;
      box.itemName = palette.paletteDisplayName;
      box.itemDescription = palette.paletteDescription;
      box.playerWeapon = Global.userObjects.weapon;
      tryOverrideNameAndDescription(storeItem, box);
      box.initialize();
      return box;
   }

   protected StoreHatDyeBox createHatDyeBox (StoreItem storeItem) {
      PaletteToolData palette = PaletteSwapManager.self.getPalette(storeItem.itemId);

      if (palette == null) {
         return null;
      }

      StoreHatDyeBox box = Instantiate(PrefabsManager.self.hatDyeBoxPrefab);
      prepareStoreItemBox(storeItem, box);
      box.palette = palette;
      box.itemName = palette.paletteDisplayName;
      box.itemDescription = palette.paletteDescription;
      box.playerHat = Global.userObjects.hat;
      tryOverrideNameAndDescription(storeItem, box);
      box.initialize();

      if (!box.isDyeApplicable()) {
         Destroy(box.gameObject);
         return null;
      }

      return box;
   }

   #endregion

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
            case Item.Category.Pet:
               createdBox = createPetBox(item);
               break;
            case Item.Category.Consumable:
               createdBox = createConsumableBox(item);
               break;
            case Item.Category.Hats:
               createdBox = createHatBox(item);
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

   #region Filter

   private bool shouldShowStoreFilter () {
      return (_currentStoreTabType == StoreTab.StoreTabType.ArmorDyes ||
        _currentStoreTabType == StoreTab.StoreTabType.HatDyes ||
        _currentStoreTabType == StoreTab.StoreTabType.WeaponDyes ||
        _currentStoreTabType == StoreTab.StoreTabType.ShipSkins);
   }

   private void regenerateStoreFilter () {
      this.storeFilter.clearOptions();
      List<string> options = new List<string>();

      if (isDyeTab()) {
         options.AddRange(new[] { StoreFilterConstants.PRIMARY, StoreFilterConstants.SECONDARY, StoreFilterConstants.ACCENT });
      }

      if (_currentStoreTabType == StoreTab.StoreTabType.ShipSkins) {
         options.AddRange(computeShipNames().Select(Util.UppercaseFirst));
         options.Sort();
      }

      options.Insert(0, StoreFilterConstants.ALL);
      this.storeFilter.addOptions(options);
   }

   public void onStoreFilterValueChanged (int valueIndex) {
      D.debug($"Filter state changed. The new value is {_currentFilterOption}");

      if (shouldShowStoreFilter()) {
         filterItems();
      }
   }

   private void updateStoreFilter () {
      if (this.storeFilter == null) {
         return;
      }

      this.storeFilter.gameObject.SetActive(shouldShowStoreFilter());

      if (this.storeFilter.hasOption(_currentFilterOption)) {
         this.storeFilter.activateOption(_currentFilterOption);
         return;
      }

      if (this.storeFilter.getOptionsCount() > 0) {
         this.storeFilter.activateFirstOption();
      }
   }

   #endregion

   #region Help

   private bool shouldShowHelp () {
      return _currentStoreTabType == StoreTab.StoreTabType.Hats;
   }

   private string computeHelpTooltip () {
      if (_currentStoreTabType == StoreTab.StoreTabType.Hats) {
         return "These hats can be painted with dyes!\n" +
            "Each colored area requires a different type of dye.\n\n" +
            "On light blue areas use Primary dyes.\n" +
            "On deep blue areas use Secondary dyes.\n" +
            "On red areas use Accent dyes.";
      }

      return "";
   }

   private void updateHelp () {
      if (helpButton == null) {
         return;
      }

      helpButton.SetActive(shouldShowHelp());
      if (helpButton.TryGetComponent(out ToolTipComponent tooltip)) {
         tooltip.message = computeHelpTooltip();
      }
   }

   #endregion

   #region Utils

   private bool isDyeTab () {
      return _currentStoreTabType == StoreTab.StoreTabType.ArmorDyes ||
         _currentStoreTabType == StoreTab.StoreTabType.HairDyes ||
         _currentStoreTabType == StoreTab.StoreTabType.HatDyes ||
         _currentStoreTabType == StoreTab.StoreTabType.WeaponDyes;
   }

   private string[] computeShipNames () {
      // Return the cached ship names if available
      if (_shipNamesOptionsCache == null) {
         List<ShipData> shipDatas = ShipDataManager.self.shipDataList;
         List<ShipData> validShipData = new List<ShipData>();
         List<string> validShipNames = new List<string>();

         foreach (ShipData ship in shipDatas) {
            if (validShipData.Any(_ => _.shipType == ship.shipType)) {
               continue;
            }

            if (validShipData.Find(_ => _.shipType == ship.shipType) == null) {
               validShipData.Add(ship);
            }
         }

         foreach (ShipData validShip in validShipData) {
            validShipNames.Add(validShip.shipName);
         }

         _shipNamesOptionsCache = validShipNames.ToArray();
      }

      return _shipNamesOptionsCache;
   }

   private string computeDyeDescription (PaletteToolData palette) {
      if (palette == null) {
         return "Common Dye.";
      }

      string desc = palette.paletteDescription;

      desc += $" {(PaletteImageType)palette.paletteType} Dye.";

      // Hide the subcategory information for primary dyes
      if (!Util.isEmpty(palette.subcategory) && !palette.isPrimary()) {
         desc += $" ({Util.UppercaseFirst(palette.subcategory)})";
      }

      desc = trimInside(desc);
      return Util.UppercaseFirst(desc);
   }

   private string trimInside (string source) {
      string[] parts = source.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
      string result = "";

      foreach (string part in parts) {
         result += part.Trim() + " ";
      }

      return result.Trim();
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

   // Current value of the store filter
   private string _currentFilterOption;

   // Cache for the ship names used in the ship skins dropdown
   private string[] _shipNamesOptionsCache;

   #endregion
}
