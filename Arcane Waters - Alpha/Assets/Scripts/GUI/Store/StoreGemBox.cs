using UnityEngine;
using UnityEngine.UI;

public class StoreGemBox : StoreItemBox
{
   #region Public Variables

   // The metadata of the StoreGemBox
   public StoreGemBoxMetadata metadata;

   #endregion

   public void initialize () {
      if (Util.isBatch()) {
         return;
      }

      // The sprite sizes aren't standardized right now, so compensate for that
      if (this.imageIcon.sprite.bounds.size.y > .50f) {
         this.imageIcon.transform.parent.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.LowerCenter;
      }
   }

   #region Private Variables

   #endregion
}
