﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class StoreTab : ClickableTab {
   #region Public Variables

   // The type of the tab
   public enum StoreTabType
   {
      // None
      None = 0,

      // Gems
      Gems = 1,

      // Hair Styles
      HairStyles = 2,

      // Hair Cuts
      HairCuts = 3,

      // Ship Skins
      ShipSkins = 4
   }

   #endregion

   public override void OnPointerDown (PointerEventData eventData) {
      base.OnPointerDown(eventData);

      // Make the store update which items are displayed
      StoreScreen.self.filterItems();
   }

   #region Private Variables

   #endregion
}
