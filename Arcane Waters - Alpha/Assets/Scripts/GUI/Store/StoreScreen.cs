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
      buyButton.enabled = (selectedItem != null);
   }

   public void showPanel (UserObjects userObjects, int gold, int gems) {
      toggleBlocker(true);

      _userObjects = userObjects;

      // Show our gold and gem count
      this.goldText.text = gold + "";
      this.gemsText.text = gems + "";
      this.nameText.text = userObjects.userInfo.username;

      // Update the character preview
      characterStack.updateLayers(userObjects);

      // Start with nothing selected
      selectItem(null);

      destroyStoreBoxes();

      // Request Store Items
      Global.player.rpc.Cmd_RequestStoreItems();

      // Display the panel
      if (!isShowing()) {
         PanelManager.self.linkPanel(Type.Store);
      }
   }

   public void onReceiveStoreItems (List<StoreItem> storeItems) {
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

      presentItems();

      toggleBlocker(false);
   }

   public void toggleBlocker (bool show) {
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

   public void selectItem (StoreItemBox itemBox) {
      deselectItems();

      // Did they unselect the current item?
      if (itemBox == this.selectedItem || itemBox == null) {
         this.selectedItem = null;
         itemTitleText.text = "";
         descriptionText.text = "";
         characterStack.updateLayers(_userObjects);
      } else {
         this.selectedItem = itemBox;
         itemTitleText.text = itemBox.itemName;
         descriptionText.text = itemBox.itemDescription;

         if (itemBox is StoreHairDyeBox) {
            StoreHairDyeBox hairBox = (StoreHairDyeBox) itemBox;
            characterStack.updateHair(_userObjects.userInfo.hairType, hairBox.palette.paletteName);
         } else if (itemBox is StoreHaircutBox) {
            StoreHaircutBox hairBox = (StoreHaircutBox) itemBox;
            characterStack.updateHair(hairBox.haircut.type, _userObjects.userInfo.hairPalettes);
         }

         itemBox.select();
      }

      characterStack.synchronizeAnimationIndexes();
   }

   public void deselectItems () {
      foreach (StoreItemBox itemBox in itemsContainer.GetComponentsInChildren<StoreItemBox>(true)) {
         itemBox.toggleHover(false);
         itemBox.toggleSelection(false);
      }
   }

   public void presentItems () {
      deselectItems();

      // Reset our selection
      if (selectedItem != null) {
         selectItem(selectedItem);
      }

      // Go through all of our items and toggle which ones are showing
      foreach (StoreItemBox itemBox in itemsContainer.GetComponentsInChildren<StoreItemBox>(true)) {
         if (itemBox is StoreHairDyeBox) {
            itemBox.gameObject.SetActive(_currentStoreTabType == StoreTab.StoreTabType.HairDyes);
         } else if (itemBox is StoreShipSkinBox) {
            itemBox.gameObject.SetActive(_currentStoreTabType == StoreTab.StoreTabType.ShipSkins);
         } else if (itemBox is StoreHaircutBox) {
            StoreHaircutBox haircutBox = (StoreHaircutBox) itemBox;
            itemBox.gameObject.SetActive(_currentStoreTabType == StoreTab.StoreTabType.Haircuts && haircutBox.haircut.getGender() == Global.player.gender);
         } else if (itemBox is StoreGemBox) {
            itemBox.gameObject.SetActive(_currentStoreTabType == StoreTab.StoreTabType.Gems);
         } else if (itemBox is StoreConsumableBox) {
            itemBox.gameObject.SetActive(_currentStoreTabType == StoreTab.StoreTabType.Consumables);
         }
      }

      // Update layout based on the selected tab
      updateBoxContainerLayout(_currentStoreTabType);
   }

   private void updateBoxContainerLayout(StoreTab.StoreTabType tabType) {
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

      presentItems();
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
   
   private void tryOverrideNameAndDescription(StoreItem storeItem, StoreItemBox box) {
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
      StoreGemBox box = Instantiate(PrefabsManager.self.gemBoxPrefab);
      prepareStoreItemBox(storeItem, box);
      GemsData gemsData = GemsXMLManager.self.getGemsData(storeItem.itemId);
      box.gemsBundle = gemsData;
      box.itemName = box.gemsBundle.itemName;
      box.itemDescription = box.gemsBundle.itemDescription;
      tryOverrideNameAndDescription(storeItem, box);
      box.initialize();
      return box;
   }

   protected StoreHaircutBox createHaircutBox (StoreItem storeItem) {
      StoreHaircutBox box = Instantiate(PrefabsManager.self.haircutBoxPrefab);
      prepareStoreItemBox(storeItem, box);
      HaircutData haircutData = HaircutXMLManager.self.getHaircutData(storeItem.itemId);
      box.haircut = haircutData;
      box.itemName = box.haircut.itemName;
      box.itemDescription = box.haircut.itemDescription;
      tryOverrideNameAndDescription(storeItem, box);
      box.initialize();
      return box;
   }

   protected StoreShipSkinBox createShipSkinBox (StoreItem storeItem) {
      StoreShipSkinBox box = Instantiate(PrefabsManager.self.shipBoxPrefab);
      prepareStoreItemBox(storeItem, box);
      ShipSkinData shipSkinData = ShipSkinXMLManager.self.getShipSkinData(storeItem.itemId);
      box.shipSkin = shipSkinData;
      box.itemName = box.shipSkin.itemName;
      box.itemDescription = box.shipSkin.itemDescription;
      tryOverrideNameAndDescription(storeItem, box);
      box.initialize();
      return box;
   }

   protected StoreHairDyeBox createHairDyeBox (StoreItem storeItem) {
      StoreHairDyeBox box = Instantiate(PrefabsManager.self.hairDyeBoxPrefab);
      prepareStoreItemBox(storeItem, box);
      HairDyeData hairdyeData = HairDyeXMLManager.self.getHairdyeData(storeItem.itemId);
      box.hairdye = hairdyeData;
      box.itemName = box.hairdye.itemName;
      box.itemDescription = box.hairdye.itemDescription;
      tryOverrideNameAndDescription(storeItem, box);
      box.initialize();
      return box;
   }

   protected StoreHaircutBox createHatBox (StoreItem storeItem) {
      StoreHaircutBox box = Instantiate(PrefabsManager.self.haircutBoxPrefab);
      prepareStoreItemBox(storeItem, box);
      tryOverrideNameAndDescription(storeItem, box);
      return box;
   }

   protected StoreHairDyeBox createPetBox (StoreItem storeItem) {
      StoreHairDyeBox box = Instantiate(PrefabsManager.self.hairDyeBoxPrefab);
      prepareStoreItemBox(storeItem, box);
      tryOverrideNameAndDescription(storeItem, box);
      return box;
   }

   protected StoreConsumableBox createConsumableBox (StoreItem storeItem) {
      StoreConsumableBox box = Instantiate(PrefabsManager.self.consumableBoxPrefab);
      prepareStoreItemBox(storeItem, box);
      ConsumableData consumableData = ConsumableXMLManager.self.getConsumableData(storeItem.itemId);
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
            case Item.Category.Hairdye:
               createdBox = createHairDyeBox(item);
               break;
            case Item.Category.Hats:
               createdBox = createHatBox(item);
               break;
            case Item.Category.Pet:
               createdBox = createPetBox(item);
               break;
            case Item.Category.Consumable:
               createdBox = createConsumableBox(item);
               break;
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

   // The last user objects that we received
   protected UserObjects _userObjects;

   // Store values of all boxes used for choosing hair palette; Key is box hashes
   private Dictionary<int, string> _paletteHairDye = new Dictionary<int, string>();

   // Current Store Tab
   private StoreTab.StoreTabType _currentStoreTabType;

   // Store Items
   private List<StoreItemBox> _storeItemBoxes = new List<StoreItemBox>();

   #endregion
}
