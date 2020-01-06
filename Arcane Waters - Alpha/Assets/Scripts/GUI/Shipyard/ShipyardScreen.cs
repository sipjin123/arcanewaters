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

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;

      // Keep track of what our intro text is
      _greetingText = greetingText.text;
   }

   public override void show () {
      base.show();

      // Show the correct contents based on our current area
      Global.player.rpc.Cmd_GetShipsForArea();

      // Start typing out our intro text
      AutoTyper.SlowlyRevealText(greetingText, _greetingText);
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

   public void updatePanelWithShips (int gold, List<ShipInfo> shipList) {
      moneyText.text = gold + "";

      // Clear out any old info
      rowsContainer.DestroyChildren();

      for (int i = 0; i < 3; i ++) {
         // Look up the info for this row
         ShipInfo shipInfo = shipList[i];

         // Create a new row
         ShipyardRow row = Instantiate(rowPrefab, rowsContainer.transform, false);
         row.transform.SetParent(rowsContainer.transform, false);

         row.skillPrefabHolder.DestroyChildren();
         foreach (string abilityName in shipInfo.shipAbilities.ShipAbilities) {
            ShipUISkillTemplate template = Instantiate(row.skillPrefab, row.skillPrefabHolder.transform).GetComponent<ShipUISkillTemplate>();
            template.skillName.text = abilityName;

            string iconPath = ShipAbilityManager.self.getAbility(abilityName).skillIconPath;
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
