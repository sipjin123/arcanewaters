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

   public Armor (int id, int armorType, string paletteNames) {
      this.category = Category.Armor;
      this.id = id;
      this.itemTypeId = armorType;
      this.count = 1;
      this.paletteNames = paletteNames;
      this.data = "";
   }

   public Armor (int id, int itemTypeId, string paletteNames, string data, int durability, int count = 1) {
      this.category = Category.Armor;
      this.id = id;
      this.count = count;
      this.itemTypeId = itemTypeId;
      this.paletteNames = paletteNames;
      this.data = data;
      this.durability = durability;
   }

   public Armor (int id, int armorType) {
      this.category = Category.Armor;
      this.id = id;
      this.count = 1;
      this.itemTypeId = armorType;
      this.paletteNames = "";
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

      string palettes = Item.trimItmPalette(paletteNames);

      return string.Format("<color={0}>{1}</color> (" + palettes + ")\n\n{2}\n\nArmor = <color=red>{3}</color>",
         "#" + colorHex, getName(), getDescription(), getArmorValue());
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
         D.debug("Cannot get Defense, Armor data does not exist! Go to Equipment Editor and make new data: (" + itemTypeId + ")");
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
      ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataByType(armorType);
      if (armorData == null) {
         D.debug("Cannot get Base Armor, Armor data does not exist! Go to Equipment Editor and make new data: (" + armorType + ")");
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

      ArmorStatData armorData = EquipmentXMLManager.self.getArmorDataByType(armorType);
      if (armorData == null) {
         D.debug("Cannot get Name, Armor data does not exist! Go to Equipment Editor and make new data :: (" + armorType + ")");
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
      return new Armor(0, 0, "");
   }

   public override string getIconPath () {
      if (getArmorData() == null) {
         return iconPath;
      }

      return getArmorData().equipmentIconPath;
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
      Armor armor = new Armor(itemId, armorType, Item.parseItmPalette(new string[2] { PaletteDef.Armor.Black, PaletteDef.Armor.White }), data, MAX_DURABILITY, 1);

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
         paletteNames = item.paletteNames,
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