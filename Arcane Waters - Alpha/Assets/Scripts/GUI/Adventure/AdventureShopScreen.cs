using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;
using TMPro;

public class AdventureShopScreen : Panel {
   #region Public Variables

   // The prefab we use for creating rows
   public AdventureItemRow rowPrefab;

   // The container for our rows
   public GameObject rowsContainer;

   // Our head animation
   public SimpleAnimation headAnim;

   // The text we want to type out
   public TextMeshProUGUI greetingText;

   // Our money text
   public Text moneyText;

   // Self
   public static AdventureShopScreen self;

   // Name of the shop reference
   public string shopName = ShopManager.DEFAULT_SHOP_NAME;

   // The sprite of the animated head icon
   public Sprite headIconSprite = null;

   // An indicator that the data is being fetched
   public GameObject loadBlocker;

   // Notify the player no item is available for now
   public static string UNAVAILABLE_ITEMS = "I got nothing to sell right now, come back later.";

   // The item name if the xml data does not exist
   public static string UNKNOWN_ITEM = "Unknown Item";

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;
   }

   public override void show () {
      base.show();
      
      // Show the correct contents based on our current area
      Global.player.rpc.Cmd_GetItemsForArea(shopName);

      // Update the head icon image
      headAnim.setNewTexture(headIconSprite.texture);

      // Clear out any old info
      rowsContainer.DestroyChildren();

      // Create a blank template showing the loading icon
      Instantiate(loadBlocker, rowsContainer.transform);

      // Greeting message is decided from the XML Data of the Shop
      greetingText.text = "";
   }

   public void buyButtonPressed (int itemId) {
      Item item = getItem(itemId);
      string itemName = item.getName();

      switch (item.category) {
         case Item.Category.Weapon:
            WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(item.itemTypeId);
            if (weaponData != null) {
               itemName = weaponData.equipmentName;
            } else {
               itemName = UNKNOWN_ITEM;
            }
            break;
         case Item.Category.Armor:
            ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(item.itemTypeId);
            if (armorData != null) {
               itemName = armorData.equipmentName;
            } else {
               itemName = UNKNOWN_ITEM;
            }
            break;
         case Item.Category.Hats:
            HatStatData hatData = EquipmentXMLManager.self.getHatData(item.itemTypeId);
            if (hatData != null) {
               itemName = hatData.equipmentName;
            } else {
               itemName = UNKNOWN_ITEM;
            }
            break;
      }

      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => buyButtonConfirmed(itemId));

      // Show a confirmation panel
      PanelManager.self.confirmScreen.show("Do you want to buy the " + itemName + "?");
   }

   protected void buyButtonConfirmed (int itemId) {
      // Hide the confirm screen
      PanelManager.self.confirmScreen.hide();

      // Send the request to the server
      Global.player.rpc.Cmd_BuyItem(itemId, shopName);

      // Trigger the tutorial
      TutorialManager3.self.tryCompletingStep(TutorialTrigger.BuyWeapon);
   }

   public void updateGreetingText (string text) {
      _greetingText = text;
      greetingText.text = text;

      // Start typing out our intro text
      AutoTyper.SlowlyRevealText(greetingText, _greetingText);
   }

   public void updatePanelWithItems (int gold, List<Item> itemList) {
      moneyText.text = gold + "";

      if (itemList.Count < 1) {
         updateGreetingText(UNAVAILABLE_ITEMS);
      }

      // Clear out any old info
      rowsContainer.DestroyChildren();

      foreach (Item item in itemList) {
         // Create a new row
         AdventureItemRow row = Instantiate(rowPrefab, rowsContainer.transform, false);
         row.transform.SetParent(rowsContainer.transform, false);
         row.setRowForItem(item);
      }

      // Trigger the tutorial
      TutorialManager3.self.tryCompletingStep(TutorialTrigger.TalkShopOwner);
   }

   protected Item getItem (int itemId) {
      foreach (AdventureItemRow row in rowsContainer.GetComponentsInChildren<AdventureItemRow>()) {
         if (row.item.id == itemId) {
            return row.item;
         }
      }

      return null;
   }

   #region Private Variables

   // Keeps track of what our starting text is
   protected string _greetingText = "";

   #endregion
}
