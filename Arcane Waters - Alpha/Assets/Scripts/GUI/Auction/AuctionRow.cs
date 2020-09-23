using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using UnityEngine.EventSystems;

public class AuctionRow : MonoBehaviour
{
   #region Public Variables

   // The item cell
   public ItemCell itemCell;

   // The item count
   public Text itemCount;

   // The item name
   public Text itemName;

   // The rarity stars
   public Image star1Image;
   public Image star2Image;
   public Image star3Image;

   // The bid info
   public Text bidAmounts;

   // The time left
   public Text timeLeftText;

   // The row tooltip
   public Tooltipped tooltip;

   // The colors for different auction statuses
   public Color ownAuctionColor;
   public Color expiredAuctionColor;

   // All the text components that will be colored
   public Text[] rowTexts;

   // The displayed auction data
   [HideInInspector]
   public AuctionItemData auctionData;

   #endregion

   public void setRowForAuction (AuctionItemData auction) {
      auctionData = auction;
      
      itemCell.hideBackground();
      itemCell.hideItemCount();
      itemCell.disablePointerEvents();

      if (auction.itemCount > 1) {
         itemCount.text = "(" + auction.itemCount + ")";
      } else {
         itemCount.text = "";
      }
      itemName.text = auction.itemName;
      bidAmounts.text = string.Format("{0:n0}", auction.highestBidPrice)
         + "/" 
         + string.Format("{0:n0}", auction.buyoutPrice);

      // Display an estimated time left - to avoid last minute bids
      timeLeftText.text = auction.getEstimatedTimeLeftUntilExpiry();

      // Determine the time left until the auction expire
      TimeSpan timeLeft = auction.getTimeLeftUntilExpiry();

      if (timeLeft.Ticks <= 0) {
         itemCell.gameObject.SetActive(false);

         star1Image.enabled = false;
         star2Image.enabled = false;
         star3Image.enabled = false;

         foreach (Text t in rowTexts) {
            t.color = expiredAuctionColor;
         }
      } else {
         // Set detailed item data only if the auction is still running (otherwise the item could not exist anymore)
         itemCell.setCellForItem(auction.item);
         tooltip.text = itemCell.getItem().getTooltip();

         // Rarity stars
         if (auction.item.category == Item.Category.Weapon ||
            auction.item.category == Item.Category.Armor ||
            auction.item.category == Item.Category.Hats) {
            Sprite[] rarityStars = Rarity.getRarityStars(auction.item.getRarity());
            star1Image.sprite = rarityStars[0];
            star2Image.sprite = rarityStars[1];
            star3Image.sprite = rarityStars[2];
            star1Image.enabled = true;
            star2Image.enabled = true;
            star3Image.enabled = true;
         } else {
            star1Image.enabled = false;
            star2Image.enabled = false;
            star3Image.enabled = false;
         }

         // Recolor the text when the seller is the local user
         if (Global.player != null && auction.sellerId == Global.player.userId) {
            foreach (Text t in rowTexts) {
               t.color = ownAuctionColor;
            }
         }
      }
   }

   public void onRowPressed () {
      ((AuctionPanel) PanelManager.self.get(Panel.Type.Auction)).onAuctionRowPressed(auctionData);
   }

   #region Private Variables

   #endregion
}
