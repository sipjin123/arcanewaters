using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class CropOffer {
   #region Public Variables

   // The maximum demand. Reaching 0 or maximum demand will jump to the lower/higher rarity and price values and reset the demand
   public static float MAX_DEMAND = 200;

   // The ID for this offer
   public int id;

   // The Area key
   public string areaKey;

   // The Crop Type
   public Crop.Type cropType;

   // The crop demand which influences its price
   public float demand = MAX_DEMAND / 2;

   // The base price per unit that will be modified by the rarity
   public int basePricePerUnit;

   // The price per unit
   public int pricePerUnit;

   // The rarity of this offer
   public Rarity.Type rarity;

   #endregion

   public CropOffer () { }

   public CropOffer(int id, string areaKey, Crop.Type cropType, float demand, int basePricePerUnit, Rarity.Type rarity) {
      this.id = id;
      this.areaKey = areaKey;
      this.cropType = cropType;
      this.demand = demand;
      this.basePricePerUnit = basePricePerUnit;
      this.rarity = rarity;
      recalculatePrice();
   }

   public void recalculatePrice () {
      pricePerUnit = (int) (basePricePerUnit * Rarity.getCropSellPriceModifier(rarity));
      pricePerUnit = Util.roundToPrettyNumber(pricePerUnit);
      pricePerUnit = Mathf.Clamp(pricePerUnit, 1, basePricePerUnit);
   }

   public bool isLowestRarity () {
      return rarity == Rarity.Type.Common;
   }

   public bool isHighestRarity () {
      return rarity == Rarity.Type.Legendary;
   }

   #region Private Variables
      
   #endregion
}
