using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class StoreHairDyeBox : StoreItemBox {
   #region Public Variables

   // The recolor, if any
   public string paletteName;

   #endregion

   public override void Start () {
      base.Start();

      if (this.imageIcon == null) {
         return;
      }
      
      this.imageIcon.material = new Material(this.imageIcon.material);
      this.imageIcon.GetComponent<RecoloredSprite>().recolor(this.paletteName, this.paletteName);
   }

   #region Private Variables

   #endregion
}
