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
public class Weapon : EquippableItem {
   #region Public Variables

   public enum ActionType
   {
      None = 0,
      PlantCrop = 1,
      WaterCrop = 2,
      HarvestCrop = 3,
      CustomizeMap = 4,
      Shovel = 5,
      Chop = 6
   }

   // The generic value of the action type
   public int actionTypeValue = 0;

   // The weapon Class
   public enum Class { Any = 0, Melee = 1, Ranged = 2, Magic = 3 , Rum = 4}

   #endregion

   public Weapon () {
      this.category = Item.Category.Weapon;
      this.itemTypeId = 0;
   }

   #if IS_SERVER_BUILD

   public Weapon (MySqlDataReader dataReader) {
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

   public static WeaponCategory GetWeaponCategoryByClass (Class weaponClass) {
      switch (weaponClass) {
         case Weapon.Class.Melee:
            return WeaponCategory.Blade;
         case Weapon.Class.Ranged:
            return WeaponCategory.Gun;
         case Weapon.Class.Rum:
            return WeaponCategory.Rum;
         default:
            return WeaponCategory.None;
      }
   }

   public Weapon (int id, int weaponType, string paletteNames) {
      this.category = Category.Weapon;
      this.id = id;
      this.itemTypeId = weaponType;
      this.count = 1;
      this.paletteNames = paletteNames;
      this.data = "";
   }

   public Weapon (int id, int itemTypeId, string paletteNames, string data, int durability, int count = 1) {
      this.category = Category.Weapon;
      this.id = id;
      this.count = count;
      this.itemTypeId = itemTypeId;
      this.paletteNames = paletteNames;
      this.data = data;
      this.durability = durability;
   }

   public Weapon (int id, int weaponType) {
      this.category = Category.Weapon;
      this.id = id;
      this.count = 1;
      this.itemTypeId = weaponType;
      this.paletteNames = "";
      this.data = "";
   }

   public override string getDescription () {
      if (getWeaponData() == null) {
         return itemDescription;
      }

      return getWeaponData().equipmentDescription;
   }

   public override string getTooltip () {
      Color color = Rarity.getColor(getRarity());
      string colorHex = ColorUtility.ToHtmlStringRGBA(color);

      string palettes = Item.trimItmPalette(paletteNames);
      if (palettes != "") {
         palettes = "(" + palettes + ")";
      }
      float modifiedDamage = getDamage() * getDamageModifier(getRarity());

      return string.Format("<color={0}>{1}</color> " + palettes + "\n\n{2}\n\nDamage = <color=red>{3}</color>",
         "#" +colorHex, getName(), getDescription(), (int) modifiedDamage);
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
         D.debug("Weapon data does not exist! Go to Equipment Editor and make new data :: (" + weaponType + ")");
         return "Undefined";
      }

      return weaponData.equipmentName;
   }

   public ActionType getActionType () {
      WeaponStatData weaponData = getWeaponData();
      if (weaponData != null) {
         return weaponData.actionType;
      } else {
         return ActionType.None;
      }
   }

   public int getDamage () {
      return getWeaponData().weaponBaseDamage;
   }

   public virtual float getDamage (Element element) {
      if (getWeaponData() == null) {
         D.debug("Weapon data does not exist! Go to Equipment Editor and make new data: (" + this.itemTypeId + ")");
         return 10;
      }

      switch (element) {
         case Element.Air:
            return getWeaponData().weaponDamageAir;
         case Element.Fire:
            return getWeaponData().weaponDamageFire;
         case Element.Water:
            return getWeaponData().weaponDamageWater;
         case Element.Earth:
            return getWeaponData().weaponDamageEarth;
      }

      return getWeaponData().weaponBaseDamage;
   }

   public static float getDamageModifier (Rarity.Type rarity) {
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

   public static Class getClass (int type) {
      switch (type) {
         default:
            return Class.Melee;
      }
   }

   public static Weapon getEmpty() {
      return new Weapon(0, 0, "");
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
      Weapon weapon = new Weapon(itemId, (int) weaponType, "", data, MAX_DURABILITY, 1);

      return weapon;
   }

   public override bool canBeTrashed () {
      if (getWeaponData() == null) {
         return true;
      }

      return getWeaponData().canBeTrashed;
   }

   public override string getIconPath () {
      if (getWeaponData() == null) {
         return iconPath;
      }

      return getWeaponData().equipmentIconPath;
   }

   public override bool canBeStacked () {
      // Seed bags can be stacked
      return getActionType() == ActionType.PlantCrop;
   }

   public static Weapon castItemToWeapon (Item item) {
      Weapon newWeapon = new Weapon {
         category = Category.Weapon,
         itemTypeId = item.itemTypeId,
         id = item.id,
         iconPath = item.iconPath,
         itemDescription = item.itemDescription,
         itemName = item.itemName,
         data = item.data,
         paletteNames = item.paletteNames,
         count = item.count,
      };

      return newWeapon;
   }

   private WeaponStatData getWeaponData () {
      if (_weaponStatData == null) {
         if (!data.Contains(EquipmentXMLManager.VALID_XML_FORMAT)) {
            WeaponStatData fetchedData = EquipmentXMLManager.self.getWeaponData(itemTypeId);
            if (fetchedData == null) {
               D.debug("Missing weapon data: " + itemTypeId);
            } else {
               return fetchedData;
            }
         }
         return WeaponStatData.getStatData(data, itemTypeId);
      }

      return _weaponStatData;
   }

   #region Private Variables

   // Cached weapon data from xml data variable
   [XmlIgnore]
   private WeaponStatData _weaponStatData = default;

   #endregion
}
