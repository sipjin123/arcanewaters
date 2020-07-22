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

   // The timer
   public Image timerImage;

   // The timer sprites
   public Sprite[] timerSprites;
   public Sprite timerEmptySprite;

   // The time left text
   public Text timeLeftText;

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

      // Initializes the offer expiration countdown
      timeLeftText.text = "";
      timerImage.sprite = timerEmptySprite;
   }

   public override void show () {
      base.show();

      // Show the correct offers based on our current area
      Global.player.rpc.Cmd_GetOffersForArea(shopName);

      // Update the head icon image
      headAnim.setNewTexture(headIconSprite.texture);

      // Greeting message is decided from the XML Data of the Shop
      greetingText.text = "";
   }

   public override void Update () {
      base.Update();
      
      // Determine the time left until the offers expire
      TimeSpan timeLeft = TimeSpan.FromSeconds(CropOffer.REGEN_INTERVAL * 3600)
      - (DateTime.UtcNow - _lastCropRegenTime);

      // If the offers have expired, show a special message
      if (timeLeft.Ticks <= 0) {
         timeLeftText.text = "The offers have expired!";
         timerImage.sprite = timerEmptySprite;
      } else {
         // Show the seconds if it happened in the last 60s, the
         // minutes in the last 60m, or the hours.
         if (timeLeft.TotalSeconds <= 60) {
            if (timeLeft.Seconds <= 1) {
               timeLeftText.text = timeLeft.Seconds.ToString() + " second left";
            } else {
               timeLeftText.text = timeLeft.Seconds.ToString() + " seconds left";
            }
         } else if (timeLeft.TotalMinutes <= 60) {
            timeLeftText.text = timeLeft.Minutes.ToString() + " min left";
         } else {
            if (timeLeft.Hours <= 1) {
               timeLeftText.text = timeLeft.Hours.ToString() + " hour left";
            } else {
               timeLeftText.text = timeLeft.Hours.ToString() + " hours left";
            }
         }

         // Determine the timer sprite
         int spriteIndex = Mathf.FloorToInt(
            (float) timeLeft.TotalSeconds / ((CropOffer.REGEN_INTERVAL * 3600) / timerSprites.Length));
         timerImage.sprite = timerSprites[spriteIndex];
      }
   }

   public void sellButtonPressed (int offerId) {
      CropOffer offer = getOffer(offerId);

      // Calculate the time left until the offers regenerate
      float timeLeftUntilRegeneration = (CropOffer.REGEN_INTERVAL * 3600)
         - (float) (DateTime.UtcNow - _lastCropRegenTime).TotalSeconds;

      // Check if the offer has expired
      if (timeLeftUntilRegeneration <= 0 || offer == null) {
         // Show an error pannel
         PanelManager.self.noticeScreen.show("This offer has expired!");
         return;
      }

      // Check if everything has been sold
      if (offer.amount <= 0) {
         // Show an error pannel
         PanelManager.self.noticeScreen.show("This offer has sold out!");

         // Update the offers
         Global.player.rpc.Cmd_GetOffersForArea(ShopManager.DEFAULT_SHOP_NAME);
         return;
      }

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

   protected void sellButtonConfirmed (int offerId) {
      int amountToSell = int.Parse(PanelManager.self.tradeConfirmScreen.sliderText.text);

      // Hide the confirm screen
      PanelManager.self.confirmScreen.hide();

      // Send the request to the server
      Global.player.rpc.Cmd_SellCrops(offerId, amountToSell);
   }

   public void updatePanelWithOffers (int gold, List<CropOffer> offers, long lastCropRegenTime, string greetingText) {
      // Keep track of these offers for later reference
      _offers = offers;

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

      // Store the last regeneration time
      _lastCropRegenTime = DateTime.FromBinary(lastCropRegenTime);
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
