using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class ItemShortcutInfo
{
   #region Public Variables

   // The shortcut slot number
   public int slotNumber;

   // The item id
   public int itemId;

   // The item triggered by the shortcut
   public Item item;

   #endregion

   public ItemShortcutInfo () { }

#if IS_SERVER_BUILD

   public ItemShortcutInfo (MySqlDataReader dataReader) {
      this.slotNumber = DataUtil.getInt(dataReader, "slotNumber");
      this.itemId = DataUtil.getInt(dataReader, "itemId");

      int itemId = dataReader.GetInt32("itmId");
      Item.Category itemCategory = (Item.Category) dataReader.GetInt32("itmCategory");
      int itemTypeId = dataReader.GetInt32("itmType");
      string palette1 = dataReader.GetString("itmPalette1");
      string palette2 = dataReader.GetString("itmPalette2");
      string data = dataReader.GetString("itmData");
      int count = dataReader.GetInt32("itmCount");

      this.item = new Item(itemId, itemCategory, itemTypeId, count, palette1, palette2, data);
   }

#endif

   public ItemShortcutInfo (int slotNumber, int itemId) {
      this.slotNumber = slotNumber;
      this.itemId = itemId;
   }

   public override bool Equals (object rhs) {
      if (rhs is ItemShortcutInfo) {
         var other = rhs as ItemShortcutInfo;
         return (slotNumber == other.slotNumber && itemId == other.itemId);
      }
      return false;
   }

   public override int GetHashCode () {
      return 17 + 31 * slotNumber.GetHashCode()
         + 883 * itemId.GetHashCode();
   }

   #region Private Variables

   #endregion
}
