using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class CropOffer {
   #region Public Variables

   // The ID for this offer
   public int id;

   // The Area type
   public Area.Type areaType;

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

   public CropOffer(int id, Area.Type areaType, Crop.Type cropType, int amount, int pricePerUnit, Rarity.Type rarity) {
      this.id = id;
      this.areaType = areaType;
      this.cropType = cropType;
      this.amount = amount;
      this.pricePerUnit = pricePerUnit;
      this.rarity = rarity;
   }

   #region Private Variables
      
   #endregion
}
