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

   #endregion

   public override void Awake () {
      base.Awake();

      self = this;
   }

   public override void show () {
      base.show();
   }

   #region Private Variables

   #endregion
}
