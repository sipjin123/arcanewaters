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
      public ulong itemId;

      // Item name
      public string name;

      // Item description
      public string description;

      // Item category
      public Category category;

      // Item quantity
      public int quantity;

      // Item price
      public int price;

      // Item metadata
      public string serializedMetadata;

      // Item currency mode
      public CurrencyMode currencyMode;

      // Item creation date
      public DateTime creationDate;

      // Item categories
      public enum Category
      {
         // None
         None = 0,

         // Gems
         Gems = 1,

         // Ship Skin
         ShipSkin = 2,

         // Haircut
         Haircut = 3,

         // Hair dye
         HairDye = 4,

         // Hats
         Hats = 5,

         // Pets
         Pets = 6
      }

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

      public StoreItem (ulong itemId, string name, string description, Category category, int quantity, int price, string metadata, DateTime creationDate, CurrencyMode currencyMode) {
         this.itemId = itemId;
         this.name = name;
         this.description = description;
         this.category = category;
         this.quantity = quantity;
         this.price = price;
         this.serializedMetadata = metadata;
         this.creationDate = creationDate;
         this.currencyMode = currencyMode;
      }

      #if IS_SERVER_BUILD

      public static StoreItem create (MySqlDataReader reader) {
         StoreItem item = new StoreItem();
         item.itemId = DataUtil.getUInt64(reader, "siItemId");
         item.name = DataUtil.getString(reader, "siItemName");
         item.description = DataUtil.getString(reader, "siItemDescription");
         item.category = (StoreItem.Category) DataUtil.getInt(reader, "siItemCategory");
         item.quantity = DataUtil.getInt(reader, "siItemQuantity");
         item.price = DataUtil.getInt(reader, "siItemPrice");
         item.serializedMetadata = DataUtil.getString(reader, "siItemMetadata");
         item.creationDate = DataUtil.getDateTime(reader, "siItemCreationDate");
         item.currencyMode = (StoreItem.CurrencyMode) DataUtil.getInt(reader, "siItemCurrencyMode");
         return item;
      }

      #endif

      #region Private Variables

      #endregion
   }
}
