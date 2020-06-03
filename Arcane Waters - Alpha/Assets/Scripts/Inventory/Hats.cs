using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Xml.Serialization;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

[Serializable]
public class Hats : EquippableItem
{
   #region Public Variables

   #endregion

   public Hats () {
      this.itemTypeId = 0;
   }

#if IS_SERVER_BUILD

   public Hats (MySqlDataReader dataReader) {
      this.itemTypeId = DataUtil.getInt(dataReader, "itmType");
      this.id = DataUtil.getInt(dataReader, "itmId");
      this.category = (Item.Category) DataUtil.getInt(dataReader, "itmCategory");
      this.itemTypeId = DataUtil.getInt(dataReader, "itmType");
      this.data = DataUtil.getString(dataReader, "itmData");

      // Defaults
      this.paletteName1 = DataUtil.getString(dataReader, "itmPalette1");
      this.paletteName2 = DataUtil.getString(dataReader, "itmPalette2");

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

   public Hats (int id, int hatType, string paletteName1, string paletteName2) {
      this.category = Category.Hats;
      this.id = id;
      this.itemTypeId = hatType;
      this.count = 1;
      this.paletteName1 = paletteName1;
      this.paletteName2 = paletteName2;
      this.data = "";
   }

   public Hats (int id, int itemTypeId, string paletteName1, string paletteName2, string data, int count = 1) {
      this.category = Category.Hats;
      this.id = id;
      this.count = count;
      this.itemTypeId = itemTypeId;
      this.paletteName1 = paletteName1;
      this.paletteName2 = paletteName2;
      this.data = data;
   }

   public Hats (int id, int hatType) {
      this.category = Category.Hats;
      this.id = id;
      this.count = 1;
      this.itemTypeId = hatType;
      this.paletteName1 = "";
      this.paletteName2 = "";
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

      return string.Format("<color={0}>{1}</color> ({2}, {3})\n\n{4}\n\nHat = <color=red>{5}</color>",
         "#" + colorHex, getName(), paletteName1, paletteName2, getDescription(), getHatDefense());
   }

   public override string getName () {
      return itemName;
   }

   public int getHatDefense () {
      if (getHatData() != null) {
         return getHatData().hatBaseDefense;
      }

      return 0;
   }

   public virtual float getDefense (Element element) {
      if (getHatData() == null) {
         D.warning("Cannot get Defense, Hat data does not exist! Go to Equipment Editor and make new data: (" + itemTypeId + ")");
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
         D.warning("Cannot get Base Hat, Hat data does not exist! Go to Equipment Editor and make new data: (" + hatType + ")");
         return 5;
      }

      return hatData.hatBaseDefense;
   }

   public static float getDefenseModifier (Rarity.Type rarity) {
      float randomModifier = Util.getBellCurveFloat(1.0f, .1f, .90f, 1.10f);

      switch (rarity) {
         case Rarity.Type.Uncommon:
            return randomModifier * 1.2f;
         case Rarity.Type.Rare:
            return randomModifier * 1.5f;
         case Rarity.Type.Epic:
            return randomModifier * 1.5f;
         case Rarity.Type.Legendary:
            return randomModifier * 3f; ;
         default:
            return randomModifier;
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
         D.warning("Cannot get Name, Hat data does not exist! Go to Equipment Editor and make new data :: (" + hatType + ")");
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

   public static Hats getEmpty () {
      return new Hats(0, 0, "", "");
   }

   public override string getIconPath () {
      if (getHatData() == null) {
         return iconPath;
      }

      return getHatData().equipmentIconPath;
   }

   public static Hats generateRandom (int itemId, int hatType) {
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
      Hats hat = new Hats(itemId, hatType, PaletteDef.Armor.Black, PaletteDef.Armor.White, data, stockCount);

      return hat;
   }

   public static Hats castItemToHat (Item item) {
      Hats newHat = new Hats {
         category = Category.Hats,
         itemTypeId = item.itemTypeId,
         id = item.id,
         iconPath = item.iconPath,
         itemDescription = item.itemDescription,
         itemName = item.itemName,
         data = item.data,
         paletteName1 = item.paletteName1,
         paletteName2 = item.paletteName2
      };

      return newHat;
   }

   private HatStatData getHatData () {
      if (_hatStatData == null) {
         _hatStatData = HatStatData.getStatData(data, itemTypeId);
      }

      return _hatStatData;
   }

   #region Private Variables

   // Cached Hat data from xml data variable
   [XmlIgnore]
   private HatStatData _hatStatData;

   #endregion
}