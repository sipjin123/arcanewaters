using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

[Serializable]
public class Armor : EquippableItem {
   #region Public Variables

   // The Type
   public enum Type {
      None = 0, Cloth = 1, Leather = 2, Steel = 3, Sash = 4, Tunic = 5,
      Posh = 6, Formal = 7, Casual = 8, Plate = 9, Wool = 10,
      Strapped = 11,
   }

   // The type
   public Type type;

   #endregion

   public Armor () {
      this.type = Type.None;
   }

   #if IS_SERVER_BUILD

   public Armor (MySqlDataReader dataReader) {
      this.type = (Armor.Type) DataUtil.getInt(dataReader, "itmType");
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

   public Armor (int id, Type armorType, ColorType color1, ColorType color2) {
      this.category = Category.Armor;
      this.id = id;
      this.type = armorType;
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
      this.type = (Type) itemTypeId;
      this.color1 = color1;
      this.color2 = color2;
      this.data = data;
   }

   public Armor (int id, Type weaponType) {
      this.category = Category.Armor;
      this.id = id;
      this.count = 1;
      this.itemTypeId = (int) weaponType;
      this.type = weaponType;
      this.color1 = ColorType.None;
      this.color2 = ColorType.None;
      this.data = "";
   }

   public override string getDescription () {
      ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(this.type);
      if (armorData == null) {
         D.warning("Armor data does not exist! Go to Equipment Editor and make new data");
         return "Undefined";
      }

      return armorData.equipmentDescription;
   }

   public override string getTooltip () {
      Color color = Rarity.getColor(getRarity());
      string colorHex = ColorUtility.ToHtmlStringRGBA(color);

      return string.Format("<color={0}>{1}</color> ({2}, {3})\n\n{4}\n\nArmor = <color=red>{5}</color>",
         "#" + colorHex, getName(), color1, color2, getDescription(), getArmorValue());
   }

   public override string getName () {
      return getName(type);
   }

   public int getArmorValue () {
      foreach (string kvp in this.data.Replace(" ", "").Split(',')) {
         if (!kvp.Contains("=")) {
            continue;
         }

         // Get the left and right side of the equal
         string key = kvp.Split('=')[0];
         string value = kvp.Split('=')[1];

         if ("armor".Equals(key)) {
            return Convert.ToInt32(value);
         }
      }

      return 0;
   }

   public virtual float getDefense (Element element) {
      ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(type);
      if (armorData == null) {
         D.warning("Armor data does not exist! Go to Equipment Editor and make new data");
         return 5;
      }

      switch (element) {
         case Element.Air:
            return armorData.airResist;
         case Element.Fire:
            return armorData.fireResist;
         case Element.Water:
            return armorData.waterResist;
         case Element.Earth:
            return armorData.earthResist;
      }
      return armorData.armorBaseDefense;
   }

   public static int getBaseArmor (Type armorType) {
      ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(armorType);
      if (armorData == null) {
         D.warning("Armor data does not exist! Go to Equipment Editor and make new data");
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

   public static string getName (Armor.Type armorType) {
      if (armorType == Type.None) {
         return "None";
      }

      ArmorStatData armorData = EquipmentXMLManager.self.getArmorData(armorType);
      if (armorData == null) {
         D.warning("Armor data does not exist! Go to Equipment Editor and make new data :: (" + armorType + ")");
         return "Undefined";
      }

      return armorData.equipmentName;
   }

   public static List<Type> getTypes (bool includeNone = false) {
      List<Type> list = new List<Type>();

      // Cycle over all of the various Armor types
      foreach (Type armorType in Enum.GetValues(typeof(Type))) {
         // Only include the "None" type if it was specifically requested
         if (armorType == Type.None && !includeNone) {
            continue;
         }

         list.Add(armorType);
      }

      return list;
   }

   public static Armor getEmpty () {
      return new Armor(0, Armor.Type.None, ColorType.None, ColorType.None);
   }

   public override string getIconPath () {
      return "Icons/Armor/" + this.type;
   }

   public override ColorKey getColorKey () {
      return new ColorKey(Global.player.gender, this.type);
   }

   public static Armor generateRandom (int itemId, Type armorType) {
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
      Armor armor = new Armor(itemId, (int) armorType, ColorType.Black, ColorType.White, data, stockCount);

      return armor;
   }

   #region Private Variables

   #endregion
}
