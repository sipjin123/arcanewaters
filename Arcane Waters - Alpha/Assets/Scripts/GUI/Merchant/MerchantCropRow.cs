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

   // The amount left
   public Text stockAmount;

   // The gold amount
   public Text goldAmount;

   // The Sell Button
   public Button sellButton;
      
   #endregion

   public void setRowForCrop (CropOffer offer) {
      offerId = offer.id;
      cropImage.sprite = ImageManager.getSprite("Cargo/" + offer.cropType);
      cropName.text = Util.UppercaseFirst(offer.cropType.ToString());
      cropName.color = Rarity.getColor(offer.rarity);
      stockAmount.text = Item.getColoredStockCount(offer.amount);
      goldAmount.text = offer.pricePerUnit + "";

      // Associate a new function with the confirmation button
      sellButton.onClick.RemoveAllListeners();
      sellButton.onClick.AddListener(() => MerchantScreen.self.sellButtonPressed(offerId));
   }

   #region Private Variables
      
   #endregion
}
