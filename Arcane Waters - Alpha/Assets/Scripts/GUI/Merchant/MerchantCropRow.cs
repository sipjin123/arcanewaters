using UnityEngine;
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

   #endregion

   public void setRowForCrop (CropOffer offer) {
      offerId = offer.id;
      cropImage.sprite = ImageManager.getSprite("Cargo/" + offer.cropType);
      cropName.text = Util.UppercaseFirst(offer.cropType.ToString());

      // Determines the demand meter fill amount
      float demand = offer.demand / CropOffer.MAX_DEMAND;

      int demandIndex;
      demandIndex = Mathf.CeilToInt(demand * (DEMAND_METER_FILL_AMOUNTS.Length - 1));
      demandIndex = Mathf.Clamp(demandIndex, 0, DEMAND_METER_FILL_AMOUNTS.Length - 1);
      demandMeterImage.fillAmount = DEMAND_METER_FILL_AMOUNTS[demandIndex];

      goldAmount.text = offer.pricePerUnit + "";

      // Rarity stars
      Sprite[] rarityStars = Rarity.getRarityStars(offer.rarity);
      star1Image.sprite = rarityStars[0];
      star2Image.sprite = rarityStars[1];
      star3Image.sprite = rarityStars[2];

      // Associate a new function with the confirmation button
      sellButton.onClick.RemoveAllListeners();
      sellButton.onClick.AddListener(() => MerchantScreen.self.sellButtonPressed(offerId));
   }

   #region Private Variables

   // The fill amount values for the demand meter
   private static float[] DEMAND_METER_FILL_AMOUNTS = new float[]
   { 0f, 0.129f, 0.225f, 0.322f, 0.419f, 0.516f, 0.613f, 0.710f, 0.807f, 0.903f, 1f};

   #endregion
}
