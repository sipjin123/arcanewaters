using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Globalization;
using System.Xml.Serialization;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

[Serializable]
public class Trinket : EquippableItem {
   public Trinket () {
      this.category = Item.Category.Trinket;
      this.itemTypeId = 0;
   }

   #if IS_SERVER_BUILD

   public Trinket (MySqlDataReader dataReader) {
      this.id = DataUtil.getInt(dataReader, "itmId");
      this.category = (Item.Category) DataUtil.getInt(dataReader, "itmCategory");
      this.itemTypeId = DataUtil.getInt(dataReader, "itmType");
      this.data = DataUtil.getString(dataReader, "itmData");

      // Defaults
      this.paletteNames = DataUtil.getString(dataReader, "itmPalettes");

      foreach (string kvp in this.data.Split(',')) {
         if (!kvp.Contains("=")) {
            continue;
         }
      }
   }

   #endif

   public Trinket (int id, int itemTypeId, string paletteNames, string data, int durability, int count = 1) {
      this.category = Category.Trinket;
      this.id = id;
      this.itemTypeId = itemTypeId;
      this.count = count;
      this.paletteNames = paletteNames;
      this.data = data;
      this.durability = durability;
   }

   public static Trinket castItemToTrinket (Item item) {
      Trinket newTrinket = new Trinket {
         category = Category.Trinket,
         itemTypeId = item.itemTypeId,
         id = item.id,
         iconPath = item.iconPath,
         itemDescription = item.itemDescription,
         itemName = item.itemName,
         data = item.data,
         paletteNames = item.paletteNames,
      };

      return newTrinket;
   }

   public override string getDescription () {
      if (getStatData() == null) {
         return itemDescription;
      }

      return getStatData().equipmentDescription;
   }

   public override string getTooltip () {
      Color color = Rarity.getColor(getRarity());
      string colorHex = ColorUtility.ToHtmlStringRGBA(color);

      string palettes = PaletteSwapManager.self.getPalettesDisplayName(paletteNames);
      return string.Format("<color={0}>{1}</color> " + palettes + "\n\n{2}",
         "#" + colorHex, getName(), getDescription());
   }

   public override string getName () {
      if (getStatData() == null) {
         return itemName;
      }

      return getStatData().equipmentName;
   }

   private TrinketStatData getStatData () {
      if (_trinketStatData == null) {
         if (!data.Contains(EquipmentXMLManager.VALID_XML_FORMAT)) {
            TrinketStatData fetchedData = EquipmentXMLManager.self.getTrinketData(itemTypeId);
            if (fetchedData == null) {
               D.debug("Missing trinket data: " + itemTypeId);
            } else {
               return fetchedData;
            }
         }
         return TrinketStatData.getStatData(data, itemTypeId);
      }

      return _trinketStatData;
   }

   public override bool isEquipped () {
      if (Global.player == null || !(Global.player is BodyEntity)) {
         return false;
      }

      BodyEntity body = (BodyEntity) Global.player;
      return (body.gearManager.equippedTrinketDbId == id);
   }

   #region Private Variables

   // Cached stat data from xml data variable
   [XmlIgnore]
   private TrinketStatData _trinketStatData = default;

   #endregion
}
