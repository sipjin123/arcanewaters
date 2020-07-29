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
public class AuctionItemData
{
   public AuctionItemData () {
   
   }

   // Name of the item
   public string itemName;

   // Seller Name
   public string sellerName;

   // The unique database entry
   public int auctionId;

   // Item Data
   public int itemId;
   public int itemCategory;
   public int itemTypeId;
   public int itemPrice;

   // User Ids
   public int sellerId;
   public int buyerId;

   // Date related to the auctioned item
   public string auctionDateCreated;
   public string auctionDateExpiry;
   public string auctionDatePurchase;


#if IS_SERVER_BUILD

   public AuctionItemData (MySqlDataReader dataReader) {
      this.itemName = dataReader.GetString("itemName");
      this.sellerName = dataReader.GetString("sellerName");
      this.auctionId = dataReader.GetInt32("auctionId");

      this.itemId = dataReader.GetInt32("itemId");
      this.itemCategory = dataReader.GetInt32("itemCategory");
      this.itemTypeId = dataReader.GetInt32("itemType");
      this.itemPrice = dataReader.GetInt32("itemPrice");

      this.sellerId = dataReader.GetInt32("sellerId");
      this.buyerId = dataReader.GetInt32("buyerId");

      this.auctionDateCreated = dataReader.GetString("datePosted");
      this.auctionDateExpiry = dataReader.GetString("dateExpiry");
      this.auctionDatePurchase = dataReader.GetString("dateSold");
   }

#endif

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