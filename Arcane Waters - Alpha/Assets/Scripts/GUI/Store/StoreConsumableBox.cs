public class StoreConsumableBox : StoreItemBox
{
   #region Public Variables

   // The consumable that this box displays
   public ConsumableData consumable;

   #endregion

   public void initialize () {
      this.storeTabCategory = StoreTab.StoreTabType.Consumables;

      if (this.imageIcon == null) {
         return;
      }

      imageIcon.sprite = ImageManager.getSprite(computeConsumableIconPath());
   }

   private string computeConsumableIconPath () {
      if (consumable == null || string.IsNullOrWhiteSpace(consumable.itemIconPath)) {
         return "Icons/Store/respec_item";
      }

      return consumable.itemIconPath;
   }

   #region Private Variables

   #endregion
}
