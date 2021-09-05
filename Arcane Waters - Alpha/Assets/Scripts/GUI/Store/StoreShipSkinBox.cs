using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class StoreShipSkinBox : StoreItemBox {
   #region Public Variables

   // The type of ship skin to apply
   public ShipSkinData shipSkin;

   #endregion

   public void initialize () {
      if (Util.isBatch()) {
         return;
      }

      if (shipSkin == null) {
         return;
      }

      this.imageIcon.sprite = Ship.computeDisplaySpriteForShip(shipSkin.shipType, shipSkin.skinType);
   }

   #region Private Variables

   #endregion
}
