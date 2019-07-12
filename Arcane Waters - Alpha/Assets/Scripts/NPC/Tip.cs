using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class Tip {
   #region Public Variables

   // The Area that this tip is for
   public Area area;

   // The Crop Offer that this tip is for
   public CropOffer offer;
      
   #endregion

   public Tip (Area area, CropOffer offer) {
      this.area = area;
      this.offer = offer;
   }

   public string getMessage () {
      string areaName = Area.getName(area.areaType);
      Biome.Type biomeType = Area.getBiome(area.areaType);
      string biomeName = Biome.getName(biomeType);
      string cropString = string.Format("<color={0}>{1}</color>", Rarity.getColor(offer.rarity), offer.cropType.ToString());

      return string.Format("I recently heard that the Merchant shop at {0} in the {1} is looking to buy {2}.", areaName, biomeName, cropString);
   }

   #region Private Variables
      
   #endregion
}
