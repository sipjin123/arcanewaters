using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.EventSystems;

public class StoreTab : ClickableTab {
   #region Public Variables

   #endregion

   public override void OnPointerDown (PointerEventData eventData) {
      base.OnPointerDown(eventData);

      // Make the store update which items are displayed
      StoreScreen.self.changeDisplayedItems();
   }

   #region Private Variables

   #endregion
}
