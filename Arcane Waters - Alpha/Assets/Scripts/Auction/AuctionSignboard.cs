﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using TMPro;
using NubisDataHandling;

public class AuctionSignboard : Signboard
{
   #region Public Variables

   #endregion

   protected override void onClick () {
      AuctionPanel panel = (AuctionPanel) PanelManager.self.get(Panel.Type.Auction);
      PanelManager.self.linkIfNotShowing(Panel.Type.Auction);
      panel.refreshPanel();
   }

   #region Private Variables

   #endregion
}
