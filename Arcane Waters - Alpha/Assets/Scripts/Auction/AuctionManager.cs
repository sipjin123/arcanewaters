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

   #endregion

   void Awake () {
      self = this;
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
               DB_Main.deliverAuction(auction.auctionId, auction.mailId, auction.highestBidUser);
               DB_Main.addGold(auction.sellerId, auction.highestBidPrice);
            } else {
               // If no one bid, return the item to the seller
               DB_Main.deliverAuction(auction.auctionId, auction.mailId, auction.sellerId);
            }
         }

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (AuctionItemData auction in auctions) {
               // If someone won the auction, the user has 'sold an item'
               if (auction.highestBidUser > 0) {
                  AchievementManager.registerUserAchievement(auction.sellerId, ActionType.SellItem);
               }
            }
         });
      });
   }

   #region Private Variables

   #endregion
}
