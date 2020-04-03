using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class StoreShipBox : StoreItemBox {
   #region Public Variables

   // The Type of Skin to apply
   public Ship.SkinType skinType;

   #endregion

   public override void Start () {
      base.Start();

      if (Util.isBatch()) {
         return;
      }

      // Show the east-facing sprite
      string shipType = skinType.ToString().Split('_')[0];
      Sprite[] sprites = ImageManager.getSprites("Ships/" + shipType + "/" + skinType);
      this.imageIcon.sprite = sprites[4];

      // The sprite sizes aren't standardized right now, so compensate for that
      if (this.imageIcon.sprite.bounds.size.y > .50f) {
         this.imageIcon.transform.parent.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.LowerCenter;
      }
   }

   #region Private Variables

   #endregion
}
