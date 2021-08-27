#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif
using System;

namespace Store
{
   public class StoreItem
   {
      #region Public Variables

      // Item Id
      public ulong id;

      // Item category
      public Item.Category category;

      // Item quantity
      public int quantity;

      // Item price
      public int price;

      // Item currency mode
      public CurrencyMode currencyMode;

      // Item creation date
      public DateTime creationDate;

      // Reference to the item (id)
      public int itemId;

      // Item currencies
      public enum CurrencyMode
      {
         // Game Currency
         Game = 0,

         // Real Currency
         Real = 1
      }

      #endregion

      public StoreItem () {

      }

      public StoreItem (ulong itemId, string name, string description, Item.Category category, int quantity, int price, string metadata, DateTime creationDate, CurrencyMode currencyMode, int item = 0) {
         this.id = itemId;
         this.category = category;
         this.quantity = quantity;
         this.price = price;
         this.creationDate = creationDate;
         this.currencyMode = currencyMode;
         this.itemId = item;
      }

      #if IS_SERVER_BUILD

      public static StoreItem create (MySqlDataReader reader) {
         StoreItem item = new StoreItem();
         item.id = DataUtil.getUInt64(reader, "siItemId");
         item.category = (Item.Category) DataUtil.getInt(reader, "siItemCategory");
         item.quantity = DataUtil.getInt(reader, "siItemQuantity");
         item.price = DataUtil.getInt(reader, "siItemPrice");
         item.creationDate = DataUtil.getDateTime(reader, "siItemCreationDate");
         item.currencyMode = (StoreItem.CurrencyMode) DataUtil.getInt(reader, "siItemCurrencyMode");
         item.itemId = DataUtil.getInt(reader, "siItem"); 
         return item;
      }

      #endif

      #region Private Variables

      #endregion
   }
}
