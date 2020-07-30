using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AuctionRootPanel : Panel {
   #region Public Variables

   // Self
   public static AuctionRootPanel self;

   // The main panels of this UI
   public AuctionMarketPanel marketPanel;
   public AuctionUserPanel userPanel;

   // The loading blockers
   public GameObject[] loadBlockers;

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;
   }

   public override void show () {
      base.show();
   }

   public void setBlockers (bool isOn) {
      foreach (GameObject obj in loadBlockers) {
         obj.SetActive(isOn);
      }
   }

   #region Private Variables

   #endregion
}

// Auction request Enum states
public enum AuctionRequestResult
{
   None = 0,
   Successfull = 1,
   InsufficientBid = 2,
   ItemUnavailable = 3,
   Buyout = 4,
   HighestBidder =5
}
