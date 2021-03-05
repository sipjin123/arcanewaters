using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;
using System.Linq;

public class StoreScreen : Panel {
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

   // The Hairstyles tab
   public StoreTab hairstylesTab;

   // The Haircuts tab
   public StoreTab haircutsTab;

   // The Ships tab
   public StoreTab shipsTab;

   // Self
   public static StoreScreen self;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;
   }

   public override void Start () {
      base.Start();

      // Start with nothing selected
      selectItem(null);

      // Clear out the items from the Editor
      itemsContainer.DestroyChildren();

      // Create the various items for the store
      foreach (StoreHairDyeBox box in GemStoreManager.self.getHairstyles()) {
         box.transform.SetParent(itemsContainer.transform);
      }
      foreach (StoreHaircutBox box in GemStoreManager.self.getHaircuts()) {
         box.transform.SetParent(itemsContainer.transform);
      }
      foreach (StoreShipBox box in GemStoreManager.self.getShipSkins()) {
         box.transform.SetParent(itemsContainer.transform);
      }

      // Update our tabs
      changeDisplayedItems();

      // Routinely check if we purchased gems
      InvokeRepeating("checkGems", 5f, 5f);
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

      if (!isShowing()) {
         PanelManager.self.linkPanel(Type.Store);
      }

      foreach (RecoloredSprite recoloredSprite in GetComponentsInChildren<RecoloredSprite>()) {
         StoreItemBox storeItemBox = recoloredSprite.GetComponentInParent<StoreItemBox>();
         if (storeItemBox is StoreHairDyeBox) {
            StoreHairDyeBox hairBox = (StoreHairDyeBox) storeItemBox;
            List<string> values = updateHairDyeBox(hairBox);
            recoloredSprite.recolor(Item.parseItmPalette(values.ToArray()));
         }
      }

      // Update our character preview stack
      this.characterStack.updateLayers(userObjects);
   }

   public void updateGemsAmount (int amount) {
      this.gemsText.text = amount + "";
   }

   public void buyItemButtonPressed () {
      buyItem();
   }

   public void getGemsButtonPressed () {
      if (Global.player != null) {
         // TODO: Steam billing integration
      }
   }

   public void selectItem (StoreItemBox itemBox) {
      // Did they unselect the current item?
      if (itemBox == this.selectedItem) {
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
            characterStack.updateHair(hairBox.hairType, _userObjects.userInfo.hairPalettes);
         }
      }
   }

   private List<string> updateHairDyeBox (StoreHairDyeBox hairBox) {
      if (_paletteHairDye.ContainsKey(hairBox.GetHashCode())) {
         _paletteHairDye[hairBox.GetHashCode()] = hairBox.paletteName;
      } else {
         _paletteHairDye.Add(hairBox.GetHashCode(), hairBox.paletteName);
      }
      List<string> values = new List<string>();
      foreach (string value in _paletteHairDye.Values) {
         values.Add(value);
      }
      return values;
   }

   public void buyItem () {
      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => actuallyBuyItem());

      // Show a confirmation panel with the user name
      PanelManager.self.confirmScreen.show("Are you sure you want to buy " + selectedItem.itemName + "?");
   }

   public void changeDisplayedItems () {
      // Reset our selection
      if (selectedItem != null) {
         selectItem(selectedItem);
      }

      // Go through all of our items and toggle which ones are showing
      foreach (StoreItemBox itemBox in itemsContainer.GetComponentsInChildren<StoreItemBox>(true)) {
         if (itemBox is StoreHairDyeBox) {
            itemBox.gameObject.SetActive(hairstylesTab.isEnabled);
         } else if (itemBox is StoreShipBox) {
            itemBox.gameObject.SetActive(shipsTab.isEnabled);
         } else if (itemBox is StoreHaircutBox) {
            StoreHaircutBox haircutBox = (StoreHaircutBox) itemBox;
            itemBox.gameObject.SetActive(haircutsTab.isEnabled);

            // Hide hair cuts that are for the other gender
            if (haircutBox.isFemale() == Global.player.isMale()) {
               itemBox.gameObject.SetActive(false);
            }
         }
      }
   }

   protected void actuallyBuyItem () {
      PanelManager.self.confirmScreen.hide();

      // Can't buy a null item
      if (selectedItem == null) {
         return;
      }

      // Send the request off to the server
      Global.player.rpc.Cmd_BuyStoreItem(selectedItem.itemId);
   }

   protected void checkGems () {
      // If the store is showing and we have a player, get an updated Gems count
      if (this.isShowing() && Global.player != null) {
         Global.player.rpc.Cmd_UpdateGems();
      }
   }

   #region Private Variables

   // The last user objects that we received
   protected UserObjects _userObjects;

   // Store values of all boxes used for choosing hair palette; Key is box hashes
   private Dictionary<int, string> _paletteHairDye = new Dictionary<int, string>();

   #endregion
}
