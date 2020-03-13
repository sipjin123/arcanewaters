using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Xml.Serialization;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

[Serializable]
public class Armor : EquippableItem {
   #region Public Variables

   // The material type to be used on this equipment
   public MaterialType materialType;

   #endregion

   public Armor () {
      this.itemTypeId = 0;
   }

   #if IS_SERVER_BUILD

   public Armor (MySqlDataReader dataReader) {
      this.itemTypeId = DataUtil.getInt(dataReader, "itmType");
      this.id = DataUtil.getInt(dataReader, "itmId");
      this.category = (Item.Category) DataUtil.getInt(dataReader, "itmCategory");
      this.itemTypeId = DataUtil.getInt(dataReader, "itmType");
      this.data = DataUtil.getString(dataReader, "itmData");

      // Defaults
      this.color1 = (ColorType) DataUtil.getInt(dataReader, "itmColor1");
      this.color2 = (ColorType) DataUtil.getInt(dataReader, "itmColor2");

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

   public Armor (int id, int armorType, ColorType color1, ColorType color2) {
      this.category = Category.Armor;
      this.id = id;
      this.itemTypeId = armorType;
      this.itemTypeId = (int) armorType;
      this.count = 1;
      this.color1 = color1;
      this.color2 = color2;
      this.data = "";
   }

   public Armor (int id, int itemTypeId, ColorType color1, ColorType color2, string data, int count = 1) {
      this.category = Category.Armor;
      this.id = id;
      this.count = count;
      this.itemTypeId = itemTypeId;
      this.color1 = color1;
      this.color2 = color2;
      this.data = data;
   }

   public Armor (int id, int armorType) {
      this.category = Category.Armor;
      this.id = id;
      this.count = 1;
      this.itemTypeId = armorType;
      this.itemTypeId = armorType;
      this.color1 = ColorType.None;
      this.color2 = ColorType.None;
      this.data = "";
   }

   public override string getDescription () {
      if (getArmorData() == null) {
         return itemDescription;
      }

      return getArmorData().equipmentDescription;
   }

   public override string getTooltip () {
      Color color = Rarity.getColor(getRarity());
      string colorHex = ColorUtility.ToHtmlStringRGBA(color);

      return string.Format("<color={0}>{1}</color> ({2}, {3})\n\n{4}\n\nArmor = <color=red>{5}</color>",
         "#" + colorHex, getName(), color1, color2, getDescription(), getArmorValue());
   }

   public override string getName () {
      return itemName;
   }

   public int getArmorValue () {
      if (getArmorData() != null) {
         return getArmorData().armorBaseDefense;
      }

      return 0;
   }

   public virtual float getDefense (Element element) {
      if (getArmorData() == null) {
         D.warning("Cannot get Defense, Armor data does not exist! Go to Equipment Editor and make new data: (" + itemTypeId + ")");
         return 5;
      }

      switch (element) {
         case Element.Air:
            return getArmorData().airResist;
         case Element.Fire:
            return getArmorData().fireResist;
         case Element.Water:
            return getArmorData().waterResist;
         case Element.Earth:
            return getArmorData().earthResist;
      }
      return getArmorData().armorBaseDefense;
   }

   public static int getBaseArmor (int armorType) {
      ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(armorType);
      if (armorData == null) {
         D.warning("Cannot get Base Armor, Armor data does not exist! Go to Equipment Editor and make new data: (" + armorType + ")");
         return 5;
      }

      return armorData.armorBaseDefense;
   }

   public static float getArmorModifier (Rarity.Type rarity) {
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

      return (body.armorManager.equippedArmorId == id);
   }

   public static string getName (int armorType) {
      if (armorType == 0) {
         return "None";
      }

      ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(armorType);
      if (armorData == null) {
         D.warning("Cannot get Name, Armor data does not exist! Go to Equipment Editor and make new data :: (" + armorType + ")");
         return "Undefined";
      }

      return armorData.equipmentName;
   }

   public static List<int> getTypes (bool includeNone = false) {
      List<int> list = new List<int>();

      // Cycle over all of the various Armor types
      for (int armorType = 0; armorType < EquipmentXMLManager.self.armorStatList.Count; armorType ++) {
         // Only include the "None" type if it was specifically requested
         if (armorType == 0 && !includeNone) {
            continue;
         }

         list.Add(armorType);
      }

      return list;
   }

   public static Armor getEmpty () {
      return new Armor(0, 0, ColorType.None, ColorType.None);
   }

   public override string getIconPath () {
      if (getArmorData() == null) {
         return iconPath;
      }

      return getArmorData().equipmentIconPath;
   }

   public override ColorKey getColorKey () {
      return new ColorKey(Global.player.gender, "armor_" + this.itemTypeId.ToString());
   }

   public static Armor generateRandom (int itemId, int armorType) {
      // Decide what the rarity should be
      Rarity.Type rarity = Rarity.getRandom();

      // Alter the damage based on the rarity
      float baseArmor = getBaseArmor(armorType);
      int armorValue = (int) (baseArmor * getArmorModifier(rarity));

      // Alter the price based on the rarity
      int price = (int) (getBaseSellPrice(Category.Armor, (int) armorType) * Rarity.getItemShopPriceModifier(rarity));

      // Let's use nice numbers
      armorValue = Util.roundToPrettyNumber(armorValue);
      price = Util.roundToPrettyNumber(price);

      string data = string.Format("armor={0}, rarity={1}, price={2}", armorValue, (int) rarity, price);
      int stockCount = Rarity.getRandomItemStockCount(rarity);
      Armor armor = new Armor(itemId, armorType, ColorType.Black, ColorType.White, data, stockCount);

      return armor;
   }

   public static Armor castItemToArmor (Item item) {
      Armor newArmor = new Armor {
         category = Category.Armor,
         itemTypeId = item.itemTypeId,
         id = item.id,
         iconPath = item.iconPath,
         itemDescription = item.itemDescription,
         itemName = item.itemName,
         data = item.data,
         color1 = item.color1,
         color2 = item.color2
      };

      return newArmor;
   }

   private ArmorStatData getArmorData () {
      if (_armorStatData == null) {
         _armorStatData = ArmorStatData.getStatData(data, itemTypeId);
      }

      return _armorStatData;
   }

   #region Private Variables

   // Cached armor data from xml data variable
   [XmlIgnore]
   private ArmorStatData _armorStatData;

   #endregion
}