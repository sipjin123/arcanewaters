public class StoreConsumableBox : StoreItemBox
{
   #region Public Variables

   // The consumable that this box displays
   public ConsumableData consumable;

   #endregion

   public void initialize () {
      if (this.imageIcon == null) {
         return;
      }
   }

   #region Private Variables

   #endregion
}
