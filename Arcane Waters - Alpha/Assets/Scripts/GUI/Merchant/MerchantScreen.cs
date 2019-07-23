using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;
using TMPro;
using System;

public class MerchantScreen : Panel {
   #region Public Variables

   // The prefab we use for creating crop rows
   public MerchantCropRow cropRowPrefab;

   // The container for our crop rows
   public GameObject cropRowsContainer;

   // The text we want to type out
   public TextMeshProUGUI greetingText;

   // The timer
   public Image timerImage;

   // The timer sprites
   public Sprite[] timerSprites;
   public Sprite timerEmptySprite;

   // Self
   public static MerchantScreen self;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;

      // Keep track of what our intro text is
      _greetingText = greetingText.text;
   }

   public override void show () {
      base.show();

      // Show the correct offers based on our current area
      Global.player.rpc.Cmd_GetOffersForArea();

      // Start typing out our intro text
      AutoTyper.SlowlyRevealText(greetingText, _greetingText);
   }

   public void sellButtonPressed (int offerId) {
      CropOffer offer = getOffer(offerId);

      // Calculate the time left until the offers regenerate
      float timeLeftUntilRegeneration = (CropOffer.REGEN_INTERVAL * 3600)
         - (float) (DateTime.UtcNow - _lastCropRegenTime).TotalSeconds;
      
      // Check if the offer has expired or if everything has been sold
      if (timeLeftUntilRegeneration <= 0 || offer == null || offer.amount <= 0) {
         // Show an error pannel
         PanelManager.self.noticeScreen.show("This offer has expired.");
      } else {
         TradeConfirmScreen confirmScreen = PanelManager.self.tradeConfirmScreen;

         // Update the Trade Confirm Screen
         confirmScreen.cropType = offer.cropType;
         confirmScreen.maxAmount = offer.amount;

         // Associate a new function with the confirmation button
         confirmScreen.confirmButton.onClick.RemoveAllListeners();
         confirmScreen.confirmButton.onClick.AddListener(() => sellButtonConfirmed(offerId));

         // Show a confirmation panel
         string cropName = System.Enum.GetName(typeof(Crop.Type), offer.cropType);
         confirmScreen.show("How many " + cropName + " do you want to sell?");
      }
   }

   protected void sellButtonConfirmed (int offerId) {
      int amountToSell = int.Parse(PanelManager.self.tradeConfirmScreen.sliderText.text);

      // Hide the confirm screen
      PanelManager.self.confirmScreen.hide();

      // Send the request to the server
      Global.player.rpc.Cmd_SellCrops(offerId, amountToSell);
   }

   public void updatePanelWithOffers (int gold, List<CropOffer> offers, long lastCropRegenTime) {
      // Keep track of these offers for later reference
      _offers = offers;

      // Clear out any old info
      cropRowsContainer.DestroyChildren();

      foreach (CropOffer offer in offers) {
         // Create a new row
         MerchantCropRow row = Instantiate(cropRowPrefab, cropRowsContainer.transform, false);
         row.transform.SetParent(cropRowsContainer.transform, false);
         row.setRowForCrop(offer);
      }

      // Determine the time left until the offers expire
      _lastCropRegenTime = DateTime.FromBinary(lastCropRegenTime);
      float timeLeftUntilRegeneration = (CropOffer.REGEN_INTERVAL * 3600)
         - (float) (DateTime.UtcNow - _lastCropRegenTime).TotalSeconds;

      // Determine the timer sprite
      if (timeLeftUntilRegeneration > 0) {
         int spriteIndex = Mathf.FloorToInt(
            timeLeftUntilRegeneration / ((CropOffer.REGEN_INTERVAL * 3600) / timerSprites.Length));
         timerImage.sprite = timerSprites[spriteIndex];
      } else {
         timerImage.sprite = timerEmptySprite;
      }
   }

   protected CropOffer getOffer (int offerId) {
      foreach (CropOffer offer in _offers) {
         if (offer.id == offerId) {
            return offer;
         }
      }

      return null;
   }

   #region Private Variables

   // The client keeps track of the Crop Offers locally
   protected List<CropOffer> _offers = new List<CropOffer>();

   // Keeps track of what our starting text is
   protected string _greetingText = "";

   // Keeps track of the time at which the offers were generated
   protected DateTime _lastCropRegenTime = DateTime.UtcNow;

   #endregion
}
