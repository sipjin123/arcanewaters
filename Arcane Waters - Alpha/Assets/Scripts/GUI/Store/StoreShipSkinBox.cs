﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class StoreShipSkinBox : StoreItemBox {
   #region Public Variables

   // The type of ship skin to apply
   public ShipSkinData shipSkin;

   #endregion

   public void initialize () {
      this.storeTabCategory = StoreTab.StoreTabType.ShipSkins;

      if (Util.isBatch()) {
         return;
      }

      if (shipSkin == null) {
         return;
      }

      this.imageIcon.sprite = Ship.computeDisplaySpriteForShip(shipSkin.shipType, shipSkin.skinType, spriteIndex: 8);

      // Assign name for display purposes
      this.name = $"{nameof(StoreShipSkinBox)} - {shipSkin.skinType} - {shipSkin.shipType}";

      // The sprite for this ship is not centered. This logic tries to counteract that
      if (shipSkin.skinType == Ship.SkinType.Type_6_Frost) {
         this.imageIcon.rectTransform.Translate(-12, 0, 0);
      }

      ShipData data = ShipDataManager.self.shipDataList.Find(_ => _.shipType == shipSkin.shipType);
      
      if (data == null) {
         return;
      }

      _shipName = data.shipName;

      adjustDescription();
   }

   public void adjustDescription () {
      if (shipSkin == null) {
         return;
      }

      string newDesc = $"Skin for the '{_shipName}'.";

      if (string.IsNullOrWhiteSpace(this.itemDescription)) {
         this.itemDescription = newDesc;
      } else {
         this.itemDescription += " " + newDesc;
      }
   }

   public string getShipName () {
      return _shipName;
   }

   #region Private Variables

   // The name of the ship
   private string _shipName;

   #endregion
}
