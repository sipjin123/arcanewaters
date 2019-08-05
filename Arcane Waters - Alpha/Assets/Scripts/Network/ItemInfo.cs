using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class ItemInfo {
   #region Public Variables

   // The item ID
   public int itemId;

   // The Item category
   public Item.Category itemCategory;

   // The Item type (can be cast to enum by subclasses)
   public int itemType;

   // The Item data (parsed by subclasses)
   public string itemData;

   // Colors
   public ColorType color1;
   public ColorType color2;

   // Quantity of fetched item
   public int itemCount;

   #endregion

   public ItemInfo () { }

   #if IS_SERVER_BUILD

   public ItemInfo (MySqlDataReader dataReader) {
      this.itemId = DataUtil.getInt(dataReader, "itmId");
      this.itemCategory = (Item.Category) DataUtil.getInt(dataReader, "itmCategory");
      this.itemType = DataUtil.getInt(dataReader, "itmType");
      this.itemData = DataUtil.getString(dataReader, "itmData");
      this.itemCount = DataUtil.getInt(dataReader, "itmCount");

      // Defaults
      this.color1 = (ColorType) DataUtil.getInt(dataReader, "itmColor1");
      this.color2 = (ColorType) DataUtil.getInt(dataReader, "itmColor2");

      foreach (string kvp in itemData.Split(',')) {
         if (!kvp.Contains("=")) {
            continue;
         }

         // Get the left and right side of the equal
         /*string key = kvp.Split('=')[0];
         string value = kvp.Split('=')[1];

         if ("color1".Equals(key)) {
            this.color1 = (ColorType) Convert.ToInt32(value);
         }*/
      }
   }

   #endif

   public ItemInfo (ItemInfo itemInfo) {
      this.itemId = itemInfo.itemId;
      this.itemCategory = itemInfo.itemCategory;
      this.itemType = itemInfo.itemType;
      this.itemData = itemInfo.itemData;
      this.color1 = itemInfo.color1;
      this.color2 = itemInfo.color2;
      this.itemCount = itemInfo.itemCount;
   }

   public override bool Equals (object rhs) {
      if (rhs is ItemInfo) {
         var other = rhs as ItemInfo;
         return itemId == other.itemId;
      }
      return false;
   }

   public override int GetHashCode () {
      return itemId.GetHashCode();
   }

   public string getName () {
      switch (itemCategory) {
         case Item.Category.Armor:
            return Armor.getName((Armor.Type) itemType);
         case Item.Category.Weapon:
            return Weapon.getName((Weapon.Type) itemType);
         default:
            D.warning("No getName() defined for item type: " + itemType);
            return "Unknown";
      }
   }

   #region Private Variables

   #endregion
}
