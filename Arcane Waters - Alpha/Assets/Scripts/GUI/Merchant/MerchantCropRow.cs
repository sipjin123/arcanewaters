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
      int demandIndex;
      demandIndex = Mathf.CeilToInt(
         (float) offer.amount / (CropOffer.MAX_STOCK / (DEMAND_METER_FILL_AMOUNTS.Length - 1)));
      demandIndex = Mathf.Clamp(demandIndex, 0, DEMAND_METER_FILL_AMOUNTS.Length - 1);
      demandMeterImage.fillAmount = DEMAND_METER_FILL_AMOUNTS[demandIndex];

      goldAmount.text = offer.pricePerUnit + "";

      // Rarity stars
      switch (offer.rarity) {
         case Rarity.Type.None:
            star1Image.sprite = baseStarSprite;
            star2Image.sprite = baseStarSprite;
            star3Image.sprite = baseStarSprite;
            break;
         case Rarity.Type.Common:
            star1Image.sprite = bronzeStarSprite;
            star2Image.sprite = baseStarSprite;
            star3Image.sprite = baseStarSprite;
            break;
         case Rarity.Type.Uncommon:
            star1Image.sprite = bronzeStarSprite;
            star2Image.sprite = bronzeStarSprite;
            star3Image.sprite = baseStarSprite;
            break;
         case Rarity.Type.Rare:
            star1Image.sprite = bronzeStarSprite;
            star2Image.sprite = bronzeStarSprite;
            star3Image.sprite = bronzeStarSprite;
            break;
         case Rarity.Type.Epic:
            star1Image.sprite = silverStarSprite;
            star2Image.sprite = silverStarSprite;
            star3Image.sprite = silverStarSprite;
            break;
         case Rarity.Type.Legendary:
            star1Image.sprite = diamondStarSprite;
            star2Image.sprite = diamondStarSprite;
            star3Image.sprite = diamondStarSprite;
            break;
         default:
            break;
      }

      // If everything has been sold, greys out the row
      if (offer.amount <= 0) {
         cropImage.color = GREYED_OUT_COLOR;
         cropName.color = GREYED_OUT_COLOR;
         demandMeterImage.color = GREYED_OUT_COLOR;
         demandMeterBaseImage.color = GREYED_OUT_COLOR;
         goldIcon.color = GREYED_OUT_COLOR;
         goldAmount.color = GREYED_OUT_COLOR;
         star1Image.color = GREYED_OUT_COLOR;
         star2Image.color = GREYED_OUT_COLOR;
         star3Image.color = GREYED_OUT_COLOR;
         sellButton.interactable = false;
      }

      // Associate a new function with the confirmation button
      sellButton.onClick.RemoveAllListeners();
      sellButton.onClick.AddListener(() => MerchantScreen.self.sellButtonPressed(offerId));
   }

   #region Private Variables

   // The greyed out color, when the offer expired
   private static Color GREYED_OUT_COLOR = new Color(137f / 255f, 137f / 255f, 137f / 255f, 87f / 255f);

   // The fill amount values for the demand meter
   private static float[] DEMAND_METER_FILL_AMOUNTS = new float[]
   { 0f, 0.129f, 0.225f, 0.322f, 0.419f, 0.516f, 0.613f, 0.710f, 0.807f, 0.903f, 1f};

   #endregion
}
