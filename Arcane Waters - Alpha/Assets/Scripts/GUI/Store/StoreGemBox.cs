using UnityEngine;
using UnityEngine.UI;

public class StoreGemBox : StoreItemBox
{
   #region Public Variables

   // Reference to the gems data
   public GemsData gemsBundle;

   #endregion

   public void initialize () {
      this.storeTabCategory = StoreTab.StoreTabType.Gems;

      if (Util.isBatch()) {
         return;
      }

      // The sprite sizes aren't standardized right now, so compensate for that
      if (this.imageIcon.sprite.bounds.size.y > .50f) {
         this.imageIcon.transform.parent.GetComponent<HorizontalLayoutGroup>().childAlignment = TextAnchor.LowerCenter;
      }
   }

   public override string getDisplayCost () {
      float itemCostFloat = (float) itemCost / 100;
      return itemCostFloat.ToString("0") + " USD";
   }

   #region Private Variables

   #endregion
}
