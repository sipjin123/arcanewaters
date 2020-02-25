using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Globalization;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

[Serializable]
public class Weapon : EquippableItem {
   #region Public Variables

   public enum ActionType
   {
      None = 0,
      PlantCrop = 1,
      WaterCrop = 2,
      HarvestCrop = 3
   }

   // The weapon Class
   public enum Class { Any = 0, Melee = 1, Ranged = 2, Magic = 3 }

   // The type
   public int type;

   // The material type to be used on this equipment
   public MaterialType materialType;

   #endregion

   public Weapon () {
      this.type = 0;
   }

   #if IS_SERVER_BUILD

   public Weapon (MySqlDataReader dataReader) {
      this.type = DataUtil.getInt(dataReader, "itmType");
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

   public Weapon (int id, int weaponType, int primaryColorId, int secondaryColorId) {
      this.category = Category.Weapon;
      this.id = id;
      this.type = weaponType;
      this.itemTypeId = (int) weaponType;
      this.count = 1;
      this.color1 = (ColorType) primaryColorId;
      this.color2 = (ColorType) secondaryColorId;
      this.data = "";
   }

   public Weapon (int id, int weaponType, ColorType primaryColorId, ColorType secondaryColorId) {
      this.category = Category.Weapon;
      this.id = id;
      this.type = weaponType;
      this.itemTypeId = (int) weaponType;
      this.count = 1;
      this.color1 = primaryColorId;
      this.color2 = secondaryColorId;
      this.data = "";
   }

   public Weapon (int id, int itemTypeId, ColorType color1, ColorType color2, string data, int count = 1) {
      this.category = Category.Weapon;
      this.id = id;
      this.count = count;
      this.itemTypeId = itemTypeId;
      this.type = itemTypeId;
      this.color1 = color1;
      this.color2 = color2;
      this.data = data;
   }

   public Weapon (int id, int weaponType) {
      this.category = Category.Weapon;
      this.id = id;
      this.count = 1;
      this.itemTypeId = (int) weaponType;
      this.type = weaponType;
      this.color1 = ColorType.None;
      this.color2 = ColorType.None;
      this.data = "";
   }

   public override string getDescription () {
      return itemDescription;
   }

   public override string getTooltip () {
      Color color = Rarity.getColor(getRarity());
      string colorHex = ColorUtility.ToHtmlStringRGBA(color);

      return string.Format("<color={0}>{1}</color> ({2}, {3})\n\n{4}\n\nDamage = <color=red>{5}</color>",
         "#" +colorHex, getName(), color1, color2, getDescription(), getDamage());
   }

   public override string getName () {
      return itemName;
   }

   public override bool isEquipped () {
      if (Global.player == null || !(Global.player is BodyEntity)) {
         return false;
      }

      BodyEntity body = (BodyEntity) Global.player;

      return (body.weaponManager.equippedWeaponId == id);
   }

   public static string getName (int weaponType) {
      if (weaponType == 0) {
         return "None";
      }

      WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(weaponType);
      if (weaponData == null) {
         D.warning("Weapon data does not exist! Go to Equipment Editor and make new data :: (" + weaponType + ")");
         return "Undefined";
      }

      return weaponData.equipmentName;
   }

   public int getDamage () {
      foreach (string kvp in this.data.Replace(" ", "").Split(',')) {
         if (!kvp.Contains("=")) {
            continue;
         }

         // Get the left and right side of the equal
         string key = kvp.Split('=')[0];
         string value = kvp.Split('=')[1];

         if ("damage".Equals(key)) { 
            return Convert.ToInt32(value);
         }
      }

      return 0;
   }

   public virtual float getDamage (Element element) {
      WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(this.type);
      if (weaponData == null) {
         D.warning("Weapon data does not exist! Go to Equipment Editor and make new data: (" + this.type + ")");
         return 10;
      }

      switch (element) {
         case Element.Air:
            return weaponData.weaponDamageAir;
         case Element.Fire:
            return weaponData.weaponDamageFire;
         case Element.Water:
            return weaponData.weaponDamageWater;
         case Element.Earth:
            return weaponData.weaponDamageEarth;
      }
      return weaponData.weaponBaseDamage;
   }

   public static int getBaseDamage (int weaponType) {
      WeaponStatData weaponData = EquipmentXMLManager.self.getWeaponData(weaponType);
      if (weaponData == null) {
         D.warning("Weapon data does not exist! Go to Equipment Editor and make new data: (" + weaponType + ")");
         return 10;
      }

      return weaponData.weaponBaseDamage;
   }

   public static float getDamageModifier (Rarity.Type rarity) {
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

   public static Class getClass (int type) {
      switch (type) {
         default:
            return Class.Melee;
      }
   }

   public static Weapon getEmpty() {
      return new Weapon(0, 0, ColorType.None, ColorType.None);
   }

   public static Weapon generateRandom (int itemId, int weaponType) {
      // Decide what the rarity should be
      Rarity.Type rarity = Rarity.getRandom();

      // TODO: Find a way to set item data from server 
      // Alter the damage based on the rarity
      float baseDamage = 10;//getBaseDamage(weaponType);
      int damage = (int) (baseDamage * getDamageModifier(rarity));

      // Alter the price based on the rarity
      int price = (int) (getBaseSellPrice(Category.Weapon, (int) weaponType) * Rarity.getItemShopPriceModifier(rarity));

      // Let's use nice numbers
      damage = Util.roundToPrettyNumber(damage);
      price = Util.roundToPrettyNumber(price);

      string data = string.Format("damage={0}, rarity={1}, price={2}", damage, (int) rarity, price);
      int stockCount = Rarity.getRandomItemStockCount(rarity);
      Weapon weapon = new Weapon(itemId, (int) weaponType, ColorType.Black, ColorType.White, data, stockCount);

      return weapon;
   }

   public override bool canBeTrashed () {
      return  EquipmentXMLManager.self.getWeaponData(type).canBeTrashed;
   }

   public override string getIconPath () {
      return iconPath;
   }

   public override ColorKey getColorKey () {
      return new ColorKey(Global.player.gender, this.type, new Weapon());
   }

   #region Private Variables

   #endregion
}
