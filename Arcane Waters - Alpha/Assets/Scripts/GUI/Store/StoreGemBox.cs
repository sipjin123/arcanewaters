using UnityEngine;
using UnityEngine.UI;

public class StoreGemBox : StoreItemBox
{
   #region Public Variables

   // The set of sprites used to display the different amount of gems
   public Sprite[] gemIcons;

   // Reference to the gems data
   public GemsData gemsBundle;

   // The title displayed in the box
   public string title;

   // Types of Store Gem Bundles
   public enum GemBundleTypes
   {
      // First type
      Pouch = 1,

      // Second type
      Satchel = 2,

      // Third type
      Crate = 3,

      // Fourth type
      Chest = 4
   }

   #endregion

   public override void Start () {
      base.Start();
      this.nameText.text = this.title;
   }

   public void initialize () {
      this.storeTabCategory = StoreTab.StoreTabType.Gems;

      if (Util.isBatch()) {
         return;
      }

      this.nameText.text = this.title;
      this.imageIcon.sprite = computeSpriteForGems();
   }

   public Sprite computeSpriteForGems() {
      if (gemIcons == null || gemIcons.Length < 4) {
         D.warning("StoreGemBox: The number of sprites specified is not enough. At least 4 are required.");
         return ImageManager.self.blankSprite;
      }

      if (itemId == (int) GemBundleTypes.Pouch) {
         return gemIcons[0];
      }

      if (itemId == (int) GemBundleTypes.Satchel) {
         return gemIcons[1];
      }

      if (itemId == (int) GemBundleTypes.Crate) {
         return gemIcons[2];
      }

      if (itemId == (int) GemBundleTypes.Chest) {
         return gemIcons[3];
      }

      return ImageManager.self.blankSprite;
   }

   public override string getDisplayCost () {
      float itemCostFloat = (float) itemCost / 100;
      return itemCostFloat.ToString("0") + " USD";
   }

   #region Private Variables

   #endregion
}
