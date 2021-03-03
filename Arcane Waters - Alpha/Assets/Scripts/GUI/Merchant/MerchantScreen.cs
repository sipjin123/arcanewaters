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

   // Our head animation
   public SimpleAnimation headAnim;

   // The text we want to type out
   public TextMeshProUGUI greetingText;

   // Self
   public static MerchantScreen self;

   // Name of the shop reference
   public string shopName = ShopManager.DEFAULT_SHOP_NAME;

   // The sprite of the animated head icon
   public Sprite headIconSprite = null;

   #endregion

   public override void Awake () {
      base.Awake();
      self = this;

      // Clear out any crop rows
      cropRowsContainer.DestroyChildren();
   }

   public void refreshPanel () {
      // Show the correct offers based on our current area
      Global.player.rpc.Cmd_GetCropOffersForShop(shopName);
   }

   public override void close () {
      if (isShowing()) {
         TutorialManager3.self.tryCompletingStep(TutorialTrigger.CloseMerchantScreen);
      }
      base.close();
   }

   public void sellButtonPressed (int offerId) {
      CropOffer offer = getOffer(offerId);
      string cropName = System.Enum.GetName(typeof(Crop.Type), offer.cropType);
      int availableCropCount = CargoBoxManager.self.getCargoCount(offer.cropType);

      if (availableCropCount <= 0) {
         PanelManager.self.noticeScreen.show("You do not have any " + cropName + " to sell!");
         return;
      }

      Rarity.Type rarityToSellAt = offer.rarity;

      TradeConfirmScreen confirmScreen = PanelManager.self.tradeConfirmScreen;

      // Update the Trade Confirm Screen
      confirmScreen.cropType = offer.cropType;
      confirmScreen.maxAmount = offer.isLowestRarity() ? int.MaxValue : Mathf.CeilToInt(offer.demand);
      confirmScreen.maxAmount = Mathf.Clamp(confirmScreen.maxAmount, 0, availableCropCount);
      confirmScreen.pricePerUnit = offer.pricePerUnit;

      // Associate a new function with the confirmation button
      confirmScreen.confirmButton.onClick.RemoveAllListeners();
      confirmScreen.confirmButton.onClick.AddListener(() => {
         // Disable the button so it can't be clicked while we wait for a response from the server
         confirmScreen.confirmButton.interactable = false;
         sellButtonConfirmed(offerId, rarityToSellAt);
      });

      // Show a confirmation panel
      confirmScreen.show("How many " + cropName + " do you want to sell?");

      // Trigger the tutorial if the user has any of the selected crop to sell
      if (CargoBoxManager.self.getCargoCount(offer.cropType) > 0) {
         TutorialManager3.self.tryCompletingStep(TutorialTrigger.OpenTradeConfirmScreen);
      }
   }

   protected void sellButtonConfirmed (int offerId, Rarity.Type rarityToSellAt) {
      int amountToSell = PanelManager.self.tradeConfirmScreen.getAmount();

      // Hide the confirm screen
      PanelManager.self.confirmScreen.hide();

      // Send the request to the server
      Global.player.rpc.Cmd_SellCrops(offerId, amountToSell, rarityToSellAt, shopName);
   }

   public void updatePanelWithOffers (int gold, List<CropOffer> offers, string greetingText) {
      // Keep track of these offers for later reference
      _offers = offers;

      // Update the head icon image
      headAnim.setNewTexture(headIconSprite.texture);

      this.greetingText.text = greetingText;
      _greetingText = greetingText;

      // Start typing out our intro text
      AutoTyper.SlowlyRevealText(this.greetingText, _greetingText);

      // Clear out any old info
      cropRowsContainer.DestroyChildren();

      foreach (CropOffer offer in offers) {
         // Create a new row
         MerchantCropRow row = Instantiate(cropRowPrefab, cropRowsContainer.transform, false);
         row.transform.SetParent(cropRowsContainer.transform, false);
         row.setRowForCrop(offer);
      }

      Global.lastUserGold = gold;

      TutorialManager3.self.tryCompletingStep(TutorialTrigger.OpenMerchantScreen);
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

   #endregion
}
