using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;

public class ShipyardScreen : Panel {
   #region Public Variables

   // The prefab we use for creating rows
   public ShipyardRow rowPrefab;

   // The container for our rows
   public GameObject rowsContainer;

   // The text we want to type out
   public TextMeshProUGUI greetingText;

   // Our money text
   public Text moneyText;

   // Self
   public static ShipyardScreen self;

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
      Global.player.rpc.Cmd_GetShipsForArea(shopName);

      // Greeting message is decided from the XML Data of the Shop
      greetingText.text = "";
   }

   public void buyButtonPressed (int shipId) {
      ShipInfo shipInfo = getShipInfo(shipId);

      // Associate a new function with the confirmation button
      PanelManager.self.confirmScreen.confirmButton.onClick.RemoveAllListeners();
      PanelManager.self.confirmScreen.confirmButton.onClick.AddListener(() => buyButtonConfirmed(shipId));

      // Show a confirmation panel
      PanelManager.self.confirmScreen.show("Do you want to buy the " + shipInfo.shipType + "?");
   }

   protected void buyButtonConfirmed (int shipId) {
      // Hide the confirm screen
      PanelManager.self.confirmScreen.hide();

      // Send the request to the server
      Global.player.rpc.Cmd_BuyShip(shipId);
   }

   public void updatePanelWithShips (int gold, List<ShipInfo> shipList, string greetingText) {
      this.greetingText.text = greetingText;
      _greetingText = greetingText;
      moneyText.text = gold + "";

      try {
         // Start typing out our intro text
         AutoTyper.SlowlyRevealText(this.greetingText, _greetingText);
      } catch {
         D.editorLog("Issue with auto typer", Color.red);
      }

      // Clear out any old info
      rowsContainer.DestroyChildren();

      foreach(ShipInfo shipInfo in shipList) { 
         // Create a new row
         ShipyardRow row = Instantiate(rowPrefab, rowsContainer.transform, false);
         row.transform.SetParent(rowsContainer.transform, false);

         row.skillPrefabHolder.DestroyChildren();
         foreach (int abilityId in shipInfo.shipAbilities.ShipAbilities) {
            ShipUISkillTemplate template = Instantiate(row.skillPrefab, row.skillPrefabHolder.transform).GetComponent<ShipUISkillTemplate>();
            template.skillName.text = ShipAbilityManager.self.getAbility(abilityId).abilityName; 

            string iconPath = ShipAbilityManager.self.getAbility(abilityId).skillIconPath;
            template.skillIcon.sprite = ImageManager.getSprite(iconPath);
         }

         row.setRowForItem(shipInfo);
      }
   }

   protected ShipInfo getShipInfo (int shipId) {
      foreach (ShipyardRow row in rowsContainer.GetComponentsInChildren<ShipyardRow>()) {
         if (row.shipInfo.shipId == shipId) {
            return row.shipInfo;
         }
      }

      return null;
   }

   #region Private Variables

   // Keeps track of what our starting text is
   protected string _greetingText = "";

   #endregion
}
