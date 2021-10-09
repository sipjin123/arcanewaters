using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Xml.Serialization;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

[Serializable]
public class Hat : EquippableItem
{
   #region Public Variables

   #endregion

   public Hat () {
      this.category = Item.Category.Hats;
      this.itemTypeId = 0;
   }

#if IS_SERVER_BUILD

   public Hat (MySqlDataReader dataReader) {
      this.itemTypeId = DataUtil.getInt(dataReader, "itmType");
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

         // Get the left and right side of the equal
         /*string key = kvp.Split('=')[0];
         string value = kvp.Split('=')[1];

         if ("color1".Equals(key)) {
            this.color1 = (ColorType) Convert.ToInt32(value);
         }*/
      }
   }

#endif

   public Hat (int id, int hatType, string paletteNames) {
      this.category = Category.Hats;
      this.id = id;
      this.itemTypeId = hatType;
      this.count = 1;
      this.paletteNames = paletteNames;
      this.data = "";
   }

   public Hat (int id, int itemTypeId, string paletteNames, string data, int durability, int count = 1) {
      this.category = Category.Hats;
      this.id = id;
      this.count = count;
      this.itemTypeId = itemTypeId;
      this.paletteNames = paletteNames;
      this.data = data;
      this.durability = durability;
   }

   public Hat (int id, int hatType) {
      this.category = Category.Hats;
      this.id = id;
      this.count = 1;
      this.itemTypeId = hatType;
      this.paletteNames = "";
      this.data = "";
   }

   public override string getDescription () {
      if (getHatData() == null) {
         return itemDescription;
      }

      return getHatData().equipmentDescription;
   }

   public override string getTooltip () {
      Color color = Rarity.getColor(getRarity());
      string colorHex = ColorUtility.ToHtmlStringRGBA(color);

      string palettes = Item.trimItmPalette(paletteNames);
      if (palettes != "") {
         palettes = "(" + palettes + ")";
      }      

      return string.Format("<color={0}>{1}</color> " + palettes + "\n\n{2}\n\nHat = <color=red>{3}</color>",
         "#" + colorHex, getName(), getDescription(), getHatDefense());
   }

   public override string getName () {
      return itemName;
   }

   public int getHatDefense () {
      return getHatData().hatBaseDefense;
   }

   public virtual float getDefense (Element element) {
      if (getHatData() == null) {
         D.debug("Cannot get Defense, Hat data does not exist! Go to Equipment Editor and make new data: (" + itemTypeId + ")");
         return 5;
      }

      switch (element) {
         case Element.Air:
            return getHatData().airResist;
         case Element.Fire:
            return getHatData().fireResist;
         case Element.Water:
            return getHatData().waterResist;
         case Element.Earth:
            return getHatData().earthResist;
      }
      return getHatData().hatBaseDefense;
   }

   public static int getBaseDefense (int hatType) {
      HatStatData hatData = EquipmentXMLManager.self.getHatData(hatType);
      if (hatData == null) {
         D.debug("Cannot get Base Hat, Hat data does not exist! Go to Equipment Editor and make new data: (" + hatType + ")");
         return 5;
      }

      return hatData.hatBaseDefense;
   }

   public static float getDefenseModifier (Rarity.Type rarity) {
      switch (rarity) {
         case Rarity.Type.Uncommon:
            return 1.25f;
         case Rarity.Type.Rare:
            return 1.5f;
         case Rarity.Type.Epic:
            return 1.75f;
         case Rarity.Type.Legendary:
            return 2;
         default:
            return 1;
      }
   }

   public override bool isEquipped () {
      if (Global.player == null || !(Global.player is BodyEntity)) {
         return false;
      }

      BodyEntity body = (BodyEntity) Global.player;

      return (body.hatsManager.equippedHatId == id);
   }

   public static string getName (int hatType) {
      if (hatType == 0) {
         return "None";
      }

      HatStatData hatData = EquipmentXMLManager.self.getHatData(hatType);
      if (hatData == null) {
         D.debug("Cannot get Name, Hat data does not exist! Go to Equipment Editor and make new data :: (" + hatType + ")");
         return "Undefined";
      }

      return hatData.equipmentName;
   }

   public static List<int> getTypes (bool includeNone = false) {
      List<int> list = new List<int>();

      // Cycle over all of the various Hat types
      for (int hatType = 0; hatType < EquipmentXMLManager.self.hatStatList.Count; hatType++) {
         // Only include the "None" type if it was specifically requested
         if (hatType == 0 && !includeNone) {
            continue;
         }

         list.Add(hatType);
      }

      return list;
   }

   public static Hat getEmpty () {
      return new Hat(0, 0, "");
   }

   public override string getIconPath () {
      if (getHatData() == null) {
         return iconPath;
      }

      return getHatData().equipmentIconPath;
   }

   public static Hat generateRandom (int itemId, int hatType) {
      // Decide what the rarity should be
      Rarity.Type rarity = Rarity.getRandom();

      // Alter the damage based on the rarity
      float baseDefense = getBaseDefense(hatType);
      int defenseValue = (int) (baseDefense * getDefenseModifier(rarity));

      // Alter the price based on the rarity
      int price = (int) (getBaseSellPrice(Category.Hats, (int) hatType) * Rarity.getItemShopPriceModifier(rarity));

      // Let's use nice numbers
      defenseValue = Util.roundToPrettyNumber(defenseValue);
      price = Util.roundToPrettyNumber(price);

      string data = string.Format("hat={0}, rarity={1}, price={2}", defenseValue, (int) rarity, price);
      int stockCount = Rarity.getRandomItemStockCount(rarity);
      Hat hat = new Hat(itemId, hatType, (PaletteDef.Armor.Black + ", " + PaletteDef.Armor.White), data, MAX_DURABILITY, stockCount);

      return hat;
   }

   public static Hat castItemToHat (Item item) {
      Hat newHat = new Hat {
         category = Category.Hats,
         itemTypeId = item.itemTypeId,
         id = item.id,
         iconPath = item.iconPath,
         itemDescription = item.itemDescription,
         itemName = item.itemName,
         data = item.data,
         paletteNames = item.paletteNames,
      };

      return newHat;
   }

   private HatStatData getHatData () {
      if (_hatStatData == null) {
         if (!data.Contains(EquipmentXMLManager.VALID_XML_FORMAT)) {
            HatStatData fetchedData = EquipmentXMLManager.self.getHatData(itemTypeId);
            if (fetchedData == null) {
               D.debug("Missing hat data: " + itemTypeId);
            } else {
               return fetchedData;
            }
         }
         return HatStatData.getStatData(data, itemTypeId);
      }

      return _hatStatData;
   }

   #region Private Variables

   // Cached Hat data from xml data variable
   [XmlIgnore]
   private HatStatData _hatStatData;

   #endregion
}