using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class StoreHairDyeBox : StoreItemBox {
   #region Public Variables

   // The metadata for this box
   public StoreHairDyeBoxMetadata metadata;

   #endregion

   public void initialize () {
      if (this.imageIcon == null) {
         return;
      }

      this.imageIcon.material = new Material(this.imageIcon.material);

      if (metadata == null) {
         return;
      }

      this.imageIcon.GetComponent<RecoloredSprite>().recolor(this.metadata.paletteName);
   }

   #region Private Variables

   #endregion
}
