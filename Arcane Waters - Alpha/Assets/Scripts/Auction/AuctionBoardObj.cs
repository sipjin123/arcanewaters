using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using NubisDataHandling;

public class AuctionBoardObj : MonoBehaviour {
   #region Public Variables

   // Opens the auction board
   public Button openAuctionBoard;

   #endregion

   private void Awake () {
      openAuctionBoard.onClick.AddListener(() => {
         AuctionRootPanel panel = (AuctionRootPanel) PanelManager.self.get(Panel.Type.Auction);
         panel.show();
         panel.setBlockers(true);
         NubisDataFetcher.self.checkAuctionMarket(0, new Item.Category[4] { Item.Category.Weapon, Item.Category.Armor, Item.Category.Hats, Item.Category.CraftingIngredients }, false);
      });
   }

   #region Private Variables

   #endregion
}
