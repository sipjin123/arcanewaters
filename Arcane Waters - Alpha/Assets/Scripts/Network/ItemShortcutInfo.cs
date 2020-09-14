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

      int itemId = DataUtil.getInt(dataReader, "itmId");
      Item.Category itemCategory = (Item.Category) DataUtil.getInt(dataReader, "itmCategory");
      int itemTypeId = DataUtil.getInt(dataReader, "itmType");
      string palettes = DataUtil.getString(dataReader, "itmPalettes");
      string data = DataUtil.getString(dataReader, "itmData");
      int count = DataUtil.getInt(dataReader, "itmCount");

      this.item = new Item(itemId, itemCategory, itemTypeId, count, palettes, data);
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
