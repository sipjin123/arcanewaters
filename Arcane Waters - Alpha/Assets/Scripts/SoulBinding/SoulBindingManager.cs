public class SoulBindingManager
{
   #region Public Variables

   #endregion

   public static bool Bkg_IsItemSoulBound (Item item) {
      if (item == null) {
         return false;
      }

      return DB_Main.isItemSoulBound(item.id);
   }

   public static bool Bkg_ShouldBeSoulBound (Item item, bool isBeingEquipped) {
      if (item == null) {
         return false;
      }

      // Check if the item is soul bindable
      Item.SoulBindingType soulBindingType = DB_Main.getSoulBindingType(item.category, item.itemTypeId);

      if (soulBindingType == Item.SoulBindingType.None) {
         return false;
      }

      if (soulBindingType == Item.SoulBindingType.Acquisition || soulBindingType == Item.SoulBindingType.Equip && isBeingEquipped) {
         return true;
      }

      return false;
   }

   #region Private Variables

   #endregion
}