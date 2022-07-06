using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;
using System;

public class AuctionManager : MonoBehaviour
{

   #region Public Variables

   // Self
   public static AuctionManager self;
   
   // The cost for starting an auction (fraction)
   public static int AUCTION_COST_FRACTION = 30;

   // The user bidding data
   public class BidderData {
      public int userId;
      public int bidAmount;
   }

   #endregion

   void Awake () {
      self = this;
   }

   public static int computeAuctionCost(int bid) {
      return Mathf.CeilToInt(bid * (AUCTION_COST_FRACTION / 100.0f));
   }

   public void startAuctionManagement() {
      // Deliver items when auctions expire
      InvokeRepeating(nameof(deliverAuctionItems), 30f, 30f);
   }

   private void deliverAuctionItems () {
      // Get our server
      NetworkedServer server = ServerNetworkingManager.self.server;

      // Check that our server is the main server
      if (server == null || !server.isMasterServer()) {
         return;
      }

      List<AuctionItemData> auctions;

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         // Get the list of expired auctions that have not yet been delivered
         auctions = DB_Main.getAuctionsToDeliver();

         // Deliver the item by setting the recipient in the associated mail
         foreach (AuctionItemData auction in auctions) {
            if (auction.highestBidUser > 0) {
               // Send the item to the winner. This mail is a system mail, thus the sender name can be overriden
               DB_Main.deliverAuction(auction.auctionId, auction.mailId, auction.highestBidUser,
                  $"Congratulations! You won the auction for {auction.itemName}. You paid {auction.highestBidPrice} gold.",
                  MailManager.AUCTION_SYSTEM_USERNAME);

               // Give the gold to the seller and report that the auction was successfully finished with a sell
               DB_Main.addGold(auction.sellerId, auction.highestBidPrice);
               MailManager.sendSystemMailWithNoAttachments(auction.sellerId,
                  "Auction Results",
                  $"You have successfully sold your {auction.itemName}! You received {auction.highestBidPrice} gold.",
                  MailManager.AUCTION_SYSTEM_USERNAME);

               // Notify the other bidders
               List<AuctionManager.BidderData> bidderUserIds = DB_Main.getBidders(auction.auctionId);

               foreach (AuctionManager.BidderData bidderData in bidderUserIds) {
                  // The winner has already been notified
                  if (bidderData.userId != auction.highestBidUser) {
                     MailManager.sendSystemMailWithNoAttachments(bidderData.userId,
                        "Auction Results",
                        $"Sorry, You were outbid in the auction for {auction.itemName}! The gold you bid has been returned to you.",
                        MailManager.AUCTION_SYSTEM_USERNAME);

                     // Refund the bidders their bids
                     DB_Main.addGold(bidderData.userId, bidderData.bidAmount);
                  }
               }

            } else {
               // If no one bid, return the item to the seller
               DB_Main.deliverAuction(auction.auctionId, auction.mailId, auction.sellerId, 
                  $"Returning your item.  Your {auction.itemName} had no bids and did not sell for your asking price.", 
                  MailManager.AUCTION_SYSTEM_USERNAME);
            }
         }

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (AuctionItemData auction in auctions) {
               // If someone won the auction, the user has 'sold an item'
               if (auction.highestBidUser > 0) {
                  AchievementManager.registerUserAchievement(auction.sellerId, ActionType.SellItem);

                  // Send confirmation message - item sold
                  ServerNetworkingManager.self.sendConfirmationMessage(ConfirmMessage.Type.SoldAuctionItem, auction.sellerId, "Your auction of " + auction.itemName + " has sold for " + auction.highestBidPrice + " gold.");
               } else {
                  // Send confirmation message - item has not been sold
                  ServerNetworkingManager.self.sendConfirmationMessage(ConfirmMessage.Type.ReturnAuctionItem, auction.sellerId, "Your auction of " + auction.itemName + " has not sold.");
               }
            }
         });
      });
   }

   #region Private Variables

   #endregion
}
