using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Xml.Serialization;
using System.Text;
using System.Xml;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

[Serializable]
public class AuctionItemData {
   #region Public Variables

   // The unique database entry
   public int auctionId;

   // The associated mail id, where the item is stored as attachment until the auction ends
   public int mailId;

   // The item data
   public Item.Category itemCategory;
   public string itemName;
   public int itemCount;

   // The seller id
   public int sellerId;

   // The seller name
   public string sellerName;

   // The bidding data
   public int highestBidPrice;
   public int highestBidUser;
   public bool isBuyoutAllowed;
   public int buyoutPrice;

   // The date at which the auction ends
   public long expiryDate;

   // The auctioned item - only available when the auction is active
   public Item item = null;

   #endregion

   public AuctionItemData () {

   }

#if IS_SERVER_BUILD

   public AuctionItemData (MySqlDataReader dataReader, bool readItemData) {
      this.auctionId = DataUtil.getInt(dataReader, "auctionId");
      this.mailId = DataUtil.getInt(dataReader, "mailId");
      this.itemCategory = (Item.Category) DataUtil.getInt(dataReader, "itemCategory");
      this.itemName = DataUtil.getString(dataReader, "itemName");
      this.itemCount = DataUtil.getInt(dataReader, "itemCount");
      this.sellerId = DataUtil.getInt(dataReader, "sellerId");
      this.sellerName = DataUtil.getString(dataReader, "sellerName");
      this.highestBidPrice = DataUtil.getInt(dataReader, "highestBidPrice");
      this.highestBidUser = DataUtil.getInt(dataReader, "highestBidUser");
      this.isBuyoutAllowed = DataUtil.getBoolean(dataReader, "isBuyoutAllowed");
      this.buyoutPrice = DataUtil.getInt(dataReader, "buyoutPrice");
      this.expiryDate = DataUtil.getDateTime(dataReader, "expiryDate").ToBinary();

      // Read the item fields, when available
      if (readItemData) {
         int itemId = DataUtil.getInt(dataReader, "itmId");
         Item.Category category = (Item.Category) DataUtil.getInt(dataReader, "itmCategory");
         int itemTypeId = DataUtil.getInt(dataReader, "itmType");
         string palettes = DataUtil.getString(dataReader, "itmPalettes");
         string data = DataUtil.getString(dataReader, "itmData");
         int count = DataUtil.getInt(dataReader, "itmCount");
         int durability = DataUtil.getInt(dataReader, "durability");

         this.item = new Item(itemId, category, itemTypeId, count, palettes, data, durability);
      } else {
         this.item = new Item();
      }
   }

#endif

   public TimeSpan getTimeLeftUntilExpiry () {
      return DateTime.FromBinary(expiryDate) - DateTime.UtcNow;
   }

   public string getEstimatedTimeLeftUntilExpiry () {
      TimeSpan timeLeft = getTimeLeftUntilExpiry();

      if (timeLeft.Ticks <= 0) {
         return "Ended";
      }

      if (timeLeft.TotalMinutes <= 10) {
         return "Ending soon";
      }

      if (timeLeft.TotalHours <= 1) {
         return "~1 hour left";
      }

      if (timeLeft.TotalHours <= 3) {
         return "~3 hours left";
      }

      if (timeLeft.TotalHours <= 24) {
         return "~1 day left";
      }

      return "Many days left";
   }

   public static string getXmlData (AuctionItemData itemData) {
      XmlSerializer ser = new XmlSerializer(itemData.GetType());
      StringBuilder sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, itemData);
      }
      string longString = sb.ToString();
      return longString;
   }

   public static string getXmlDataGroup (List<AuctionItemData> itemData) {
      XmlSerializer ser = new XmlSerializer(itemData.GetType());
      StringBuilder sb = new StringBuilder();
      using (var writer = XmlWriter.Create(sb)) {
         ser.Serialize(writer, itemData);
      }
      string longString = sb.ToString();
      return longString;
   }
}