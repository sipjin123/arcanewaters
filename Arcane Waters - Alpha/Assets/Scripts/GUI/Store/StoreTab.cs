using UnityEngine;
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
      Haircuts = 2,

      // Hair Dyes
      HairDyes = 3,

      // Armor Dyes
      ArmorDyes = 4,

      // Hat Dyes
      HatDyes = 5,

      // Weapon Dyes
      WeaponDyes = 6,

      // Ship Skins
      ShipSkins = 7,

      // Consumables
      Consumables = 8,
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
