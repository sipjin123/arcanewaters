using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class CropOffer {
   #region Public Variables

   // The maximum stock amount (demand)
   public static int MAX_STOCK = 3000;

   // The offer regeneration interval, common to all offers - in hours
   public static int REGEN_INTERVAL = 24;

   // The ID for this offer
   public int id;

   // The Area key
   public string areaKey;

   // The Crop Type
   public Crop.Type cropType;

   // The amount we're willing to buy
   public int amount;

   // The price per unit
   public int pricePerUnit;

   // The rarity of this offer
   public Rarity.Type rarity;

   #endregion

   public CropOffer () { }

   public CropOffer(int id, string areaKey, Crop.Type cropType, int amount, int pricePerUnit, Rarity.Type rarity) {
      this.id = id;
      this.areaKey = areaKey;
      this.cropType = cropType;
      this.amount = amount;
      this.pricePerUnit = pricePerUnit;
      this.rarity = rarity;
   }

   #region Private Variables
      
   #endregion
}
