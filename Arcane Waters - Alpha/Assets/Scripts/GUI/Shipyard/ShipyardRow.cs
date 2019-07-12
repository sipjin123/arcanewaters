﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShipyardRow : MonoBehaviour {
   #region Public Variables

   // The icon
   public Image iconImage;

   // The text
   public Text itemName;

   // The gold amount
   public Text goldAmount;

   // The Button
   public Button buyButton;

   // The Ship associated with this row
   public ShipInfo shipInfo;

   // The stat texts
   public Text damageText;
   public Text healthText;
   public Text suppliesText;
   public Text cargoText;
   public Text speedText;
   public Text sailorsText;

   #endregion

   public void setRowForItem (ShipInfo shipInfo) {
      this.shipInfo = shipInfo;
      
      Sprite[] sprites = ImageManager.getSprites(Ship.getSkinPath(shipInfo.shipType, shipInfo.skinType));
      iconImage.sprite = sprites[4];
      itemName.text = "" + shipInfo.shipType;
      itemName.color = Rarity.getColor(shipInfo.rarity);
      goldAmount.text = shipInfo.price + "";

      // Fill in the stats
      damageText.text = "" + shipInfo.damage;
      healthText.text = "" + shipInfo.maxHealth;
      suppliesText.text = "" + shipInfo.suppliesMax;
      cargoText.text = "" + shipInfo.cargoMax;
      speedText.text = "" + shipInfo.speed;
      sailorsText.text = "" + shipInfo.sailors;

      // If it's already sold, update the button
      buyButton.interactable = !shipInfo.hasSold;
      buyButton.GetComponentInChildren<Text>().text = shipInfo.hasSold ? "Sold!" : "Buy";

      // Associate a new function with the confirmation button
      buyButton.onClick.RemoveAllListeners();
      buyButton.onClick.AddListener(() => ShipyardScreen.self.buyButtonPressed(shipInfo.shipId));
   }

   #region Private Variables

   #endregion
}
