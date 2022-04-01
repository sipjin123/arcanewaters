﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MerchantCropRow : MonoBehaviour {
   #region Public Variables

   // The offer ID associated with this row
   public int offerId;

   // The crop icon
   public Image cropImage;

   // The crop text
   public Text cropName;

   // The count text
   public Text ownedCropCount;

   // The demand meter
   public Image demandMeterImage;

   // The base of the demand meter
   public Image demandMeterBaseImage;

   // The gold icon
   public Image goldIcon;

   // The gold amount
   public Text goldAmount;

   // The rarity stars
   public Image star1Image;
   public Image star2Image;
   public Image star3Image;

   // The Sell Button
   public Button sellButton;

   // The star sprites for each rarity
   public Sprite baseStarSprite;
   public Sprite bronzeStarSprite;
   public Sprite silverStarSprite;
   public Sprite diamondStarSprite;

   // The Sell button tooltip component
   public ToolTipComponent sellButtonTooltip;

   // The demand bar tooltip component
   public ToolTipComponent demandMeterTooltip;

   #endregion

   public void setRowForCrop (CropOffer offer) {
      offerId = offer.id;
      cropImage.sprite = ImageManager.getSprite("Cargo/" + offer.cropType);
      cropName.text = Util.UppercaseFirst(offer.cropType.ToString());

      ownedCropCount.text = offer.userAvailableCrops.ToString();

      // Determines the demand meter fill amount
      float demand = offer.demand / CropOffer.MAX_DEMAND;

      int demandIndex;
      demandIndex = Mathf.CeilToInt(demand * (DEMAND_METER_FILL_AMOUNTS.Length - 1));
      demandIndex = Mathf.Clamp(demandIndex, 0, DEMAND_METER_FILL_AMOUNTS.Length - 1);
      demandMeterImage.fillAmount = DEMAND_METER_FILL_AMOUNTS[demandIndex];

      // Set demand description
      int demandAsInteger = (int) (demand * 100);
      if (demandAsInteger <= 20) {
         _demandDescription = "very low";
      } else {
         if (demandAsInteger > 20 && demandAsInteger <= 40) {
            _demandDescription = "low";
         } else {
            if (demandAsInteger > 40 && demandAsInteger <= 60) {
               _demandDescription = "average";
            } else {
               if (demandAsInteger > 60 && demandAsInteger <= 80) {
                  _demandDescription = "high";
               } else {
                  if (demandAsInteger > 80 && demandAsInteger <= 100) {
                     _demandDescription = "very high";
                  }
               }
            }
         }
      }
      demandMeterTooltip.message = "Demand for " + cropName.text + ": " + demandAsInteger.ToString() + "%  " + "(" + _demandDescription + ")";

      goldAmount.text = offer.pricePerUnit + "";

      // Rarity stars
      Sprite[] rarityStars = Rarity.getRarityStars(offer.rarity);
      star1Image.sprite = rarityStars[0];
      star2Image.sprite = rarityStars[1];
      star3Image.sprite = rarityStars[2];

      // Make sure the button can only be clicked if we have crops to sell
      if (offer.userAvailableCrops > 0) {
         sellButton.interactable = true;
         sellButtonTooltip.enabled = false;
      } else {
         sellButton.interactable = false;
         sellButtonTooltip.enabled = true;
         sellButtonTooltip.message = "You do not have any " + cropName.text.ToLower() + " to sell!";
      }
      
      // Associate a new function with the confirmation button
      sellButton.onClick.RemoveAllListeners();

      sellButton.onClick.AddListener(() => {
         MerchantScreen.self.sellButtonPressed(offerId);
      });
   }

   #region Private Variables

   // The fill amount values for the demand meter
   private static float[] DEMAND_METER_FILL_AMOUNTS = new float[]
   { 0f, 0.129f, 0.225f, 0.322f, 0.419f, 0.516f, 0.613f, 0.710f, 0.807f, 0.903f, 1f};

   // The description of the demand status
   private string _demandDescription;

   #endregion
}
