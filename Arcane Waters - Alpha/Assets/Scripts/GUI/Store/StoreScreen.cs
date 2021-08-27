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

   // The set of tabs of this Store screen
   public StoreTab.StoreTabType[] tabTypes;

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
      itemSelectionOutline.enabled = (selectedItem != null);
      buyButton.enabled = (selectedItem != null);

      // Keep the selection outline on top of the selected item box
      if (selectedItem != null) {
         itemSelectionOutline.transform.position = selectedItem.nameText.transform.position + new Vector3(0f, 64f);
      }
   }

   public void showPanel (UserObjects userObjects, int gold, int gems) {
      _userObjects = userObjects;

      // Show our gold and gem count
      this.goldText.text = gold + "";
      this.gemsText.text = gems + "";
      this.nameText.text = userObjects.userInfo.username;

      // Update the character preview
      characterStack.updateLayers(userObjects);

      // Request Store Items
      Global.player.rpc.Cmd_RequestStoreItems();

      toggleBlocker(true);

      // Display the panel
      if (!isShowing()) {
         PanelManager.self.linkPanel(Type.Store);
      }
   }

   public void onReceiveStoreItems (List<StoreItem> storeItems) {
      // Start with nothing selected
      selectItem(null);

      destroyStoreBoxes();

      // Create Boxes
      createStoreBoxes(storeItems);

      foreach (RecoloredSprite recoloredSprite in GetComponentsInChildren<RecoloredSprite>()) {
         StoreItemBox storeItemBox = recoloredSprite.GetComponentInParent<StoreItemBox>();
         if (storeItemBox is StoreHairDyeBox) {
            StoreHairDyeBox hairBox = (StoreHairDyeBox) storeItemBox;
            List<string> values = updateHairDyeBox(hairBox);
            recoloredSprite.recolor(Item.parseItmPalette(values.ToArray()));
         }
      }

      // Update our tabs
      changeDisplayedItems();

      // Routinely check if we purchased gems
      InvokeRepeating("checkGems", 5f, 5f);

      // Initialize tabs
      initializeTabs();

      toggleBlocker(false);
   }

   private void toggleBlocker (bool show) {
      if (loadBlocker != null) {
         loadBlocker.gameObject.SetActive(show);
      }
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

      Global.player.rpc.Cmd_BuyStoreItem(selectedItem.itemId, appId);

      D.debug("Store purchase: Control transferred.");
   }

   public void selectItem (StoreItemBox itemBox) {
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
            List<string> values = updateHairDyeBox(hairBox);

            characterStack.updateHair(_userObjects.userInfo.hairType, Item.parseItmPalette(values.ToArray()));
         } else if (itemBox is StoreHaircutBox) {
            StoreHaircutBox hairBox = (StoreHaircutBox) itemBox;
            characterStack.updateHair(hairBox.haircut.type, _userObjects.userInfo.hairPalettes);
         }
      }

      characterStack.synchronizeAnimationIndexes();
   }

   public void changeDisplayedItems () {
      // Reset our selection
      if (selectedItem != null) {
         selectItem(selectedItem);
      }

      // Go through all of our items and toggle which ones are showing
      foreach (StoreItemBox itemBox in itemsContainer.GetComponentsInChildren<StoreItemBox>(true)) {
         if (itemBox is StoreHairDyeBox) {
            itemBox.gameObject.SetActive(_currentStoreTabType == StoreTab.StoreTabType.HairStyles);
         } else if (itemBox is StoreShipBox) {
            itemBox.gameObject.SetActive(_currentStoreTabType == StoreTab.StoreTabType.ShipSkins);
         } else if (itemBox is StoreHaircutBox) {
            StoreHaircutBox haircutBox = (StoreHaircutBox) itemBox;
            itemBox.gameObject.SetActive(_currentStoreTabType == StoreTab.StoreTabType.HairCuts);

            // Hide hairstyles that are for the other gender
            if (haircutBox.haircut.getGender() != Global.player.gender) {
               itemBox.gameObject.SetActive(false);
            }
         } else if (itemBox is StoreGemBox) {
            StoreGemBox gemBox = (StoreGemBox) itemBox;
            itemBox.gameObject.SetActive(_currentStoreTabType == StoreTab.StoreTabType.Gems);
         }
      }
   }

   protected void checkGems () {
      // If the store is showing and we have a player, get an updated Gems count
      if (this.isShowing() && Global.player != null) {
         Global.player.rpc.Cmd_UpdateGems();
      }
   }

   private void initializeTabs () {
      if (tabs == null) {
         return;
      }

      tabs.onTabPressed.RemoveAllListeners();
      tabs.onTabPressed.AddListener(performTabSwitch);

      // Switch to the first tab
      tabs.performTabPressed(0);
   }

   public void performTabSwitch (int tabIndex) {

      if (tabIndex < 0 || tabs == null || tabTypes.Length < tabIndex) {
         return;
      }

      // Remember the selected tab
      _currentStoreTabType = tabTypes[tabIndex];

      D.debug($"New Store Tab Type is: { _currentStoreTabType }");

      changeDisplayedItems();
   }

   #region Boxes

   private List<string> updateHairDyeBox (StoreHairDyeBox hairBox) {
      if (_paletteHairDye.ContainsKey(hairBox.GetHashCode())) {
         _paletteHairDye[hairBox.GetHashCode()] = hairBox.metadata.paletteName;
      } else {
         _paletteHairDye.Add(hairBox.GetHashCode(), hairBox.metadata.paletteName);
      }

      List<string> values = new List<string>();

      foreach (string value in _paletteHairDye.Values) {
         values.Add(value);
      }

      return values;
   }

   public StoreItemBox getItemBox (ulong itemId) {
      foreach (StoreItemBox box in GetComponentsInChildren<StoreItemBox>()) {
         if (box.itemId == itemId) {
            return box;
         }
      }

      foreach (StoreItemBox box in StoreScreen.self.GetComponentsInChildren<StoreItemBox>()) {
         if (box.itemId == itemId) {
            return box;
         }
      }

      return null;
   }

   private void prepareStoreItemBox (StoreItem item, StoreItemBox box) {
      box.transform.SetParent(this.transform);
      box.itemId = item.id;
      box.itemCost = item.price;
      box.itemQuantity = item.quantity;
      box.itemCategory = item.category;
   }

   protected StoreGemBox createGemBox (StoreItem storeItem) {
      StoreGemBox box = Instantiate(PrefabsManager.self.gemBoxPrefab);
      prepareStoreItemBox(storeItem, box);
      GemsData gemsData = GemsXMLManager.self.getGemsData(storeItem.itemId);
      box.gemsBundle = gemsData;
      box.itemName = box.gemsBundle.itemName;
      box.itemDescription = box.gemsBundle.itemDescription;
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
      box.initialize();
      return box;
   }

   protected StoreShipBox createShipSkinBox (StoreItem item) {
      StoreShipBox box = Instantiate(PrefabsManager.self.shipBoxPrefab);
      prepareStoreItemBox(item, box);
      box.initialize();
      return box;
   }

   protected StoreHairDyeBox createHairDyeBox (StoreItem item) {
      StoreHairDyeBox box = Instantiate(PrefabsManager.self.hairDyeBoxPrefab);
      prepareStoreItemBox(item, box);
      box.initialize();
      return box;
   }

   protected StoreHaircutBox createHatBox (StoreItem item) {
      StoreHaircutBox box = Instantiate(PrefabsManager.self.haircutBoxPrefab);
      prepareStoreItemBox(item, box);
      return box;
   }

   protected StoreHairDyeBox createPetBox (StoreItem item) {
      StoreHairDyeBox box = Instantiate(PrefabsManager.self.hairDyeBoxPrefab);
      prepareStoreItemBox(item, box);
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

      int counter = 0;
      foreach (StoreItem item in items) {
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
         }

         _storeItemBoxes.Add(createdBox);

         // Try to add the box to the container here (Testing)
         createdBox.transform.SetParent(itemsContainer.transform);

         counter++;
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
