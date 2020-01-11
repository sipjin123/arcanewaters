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

   // The text we want to type out
   public TextMeshProUGUI greetingText;

   // Our money text
   public Text moneyText;

   // Self
   public static AdventureShopScreen self;

   // Name of the shop reference
   public string shopName = ShopManager.DEFAULT_SHOP_NAME;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;
   }

   public override void show () {
      base.show();

      // Show the correct contents based on our current area
      Global.player.rpc.Cmd_GetItemsForArea(shopName);

      // Greeting message is decided from the XML Data of the Shop
      greetingText.text = "";
   }

   public void buyButtonPressed (int itemId) {
      Item item = getItem(itemId);

      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => buyButtonConfirmed(itemId));

      // Show a confirmation panel
      PanelManager.self.confirmScreen.show("Do you want to buy the " + item.getName() + "?");
   }

   protected void buyButtonConfirmed (int itemId) {
      // Hide the confirm screen
      PanelManager.self.confirmScreen.hide();

      // Send the request to the server
      Global.player.rpc.Cmd_BuyItem(itemId);
   }

   public void updateGreetingText (string text) {
      _greetingText = text;
      greetingText.text = text;

      // Start typing out our intro text
      AutoTyper.SlowlyRevealText(greetingText, _greetingText);
   }

   public void updatePanelWithItems (int gold, List<Item> itemList) {
      moneyText.text = gold + "";

      // Clear out any old info
      rowsContainer.DestroyChildren();

      foreach (Item item in itemList) {
         // Create a new row
         AdventureItemRow row = Instantiate(rowPrefab, rowsContainer.transform, false);
         row.transform.SetParent(rowsContainer.transform, false);
         row.setRowForItem(item);
      }
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
