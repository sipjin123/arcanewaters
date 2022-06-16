using System.Collections.Generic;

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

   public static bool Bkg_ShouldBeSoulBoundOnEquipSpecifically (Item item) {
      if (item == null) {
         return false;
      }

      // Check if the item is soul bindable
      Item.SoulBindingType soulBindingType = DB_Main.getSoulBindingType(item.category, item.itemTypeId);

      return soulBindingType == Item.SoulBindingType.Equip;
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

   public static Item.SoulBindingType getItemSoulbindingTypeClient (Item item) {
      if (_soulbindingDataClient.TryGetValue((item.itemTypeId, item.category), out Item.SoulBindingType val)) {
         return val;
      }
      return Item.SoulBindingType.None;
   }

   public static void receiveItemSoulbindingDataFromServer (ItemTypeSoulbinding[] data) {
      _soulbindingDataClient.Clear();
      foreach (ItemTypeSoulbinding entry in data) {
         _soulbindingDataClient[(entry.itemTypeId, entry.itemCategory)] = entry.bindingType;
      }
   }

   #region Private Variables

   // The soulbinding data we know about on the client
   private static Dictionary<(int, Item.Category), Item.SoulBindingType> _soulbindingDataClient = new Dictionary<(int, Item.Category), Item.SoulBindingType>();

   #endregion
}