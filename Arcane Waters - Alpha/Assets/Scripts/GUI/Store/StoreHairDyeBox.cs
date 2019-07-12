using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class StoreHairDyeBox : StoreItemBox {
   #region Public Variables

   // The recolor, if any
   public ColorType colorType;

   #endregion

   public override void Start () {
      base.Start();

      if (this.imageIcon == null) {
         return;
      }
      
      this.imageIcon.material = new Material(this.imageIcon.material);
      this.imageIcon.GetComponent<RecoloredSprite>().recolor(this.colorType, this.colorType);
   }

   #region Private Variables

   #endregion
}
