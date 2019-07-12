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

   // The Type
   public enum Type {
      None = 0, Sword_Steel = 1, Staff_Mage = 8, Lance_Steel = 9, Mace_Steel = 10, Mace_Star = 11, Sword_Rune = 12,
      Sword_1 = 101, Sword_2 = 102, Sword_3 = 103, Sword_4 = 104, Sword_5 = 105,
      Sword_6 = 106, Sword_7 = 107, Sword_8 = 108,
      Seeds = 150, Pitchfork = 151, WateringPot = 152,

      Gun_6 = 200, Gun_7 = 201, Gun_2 = 202, Gun_3 = 203,
   }

   // The weapon Class
   public enum Class { Any = 0, Melee = 1, Ranged = 2, Magic = 3 }

   // The type
   public Type type;

   #endregion

   public Weapon () {
      this.type = Type.None;
   }

   #if IS_SERVER_BUILD

   public Weapon (MySqlDataReader dataReader) {
      this.type = (Weapon.Type) DataUtil.getInt(dataReader, "itmType");
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

   public Weapon (int id, Weapon.Type weaponType, int primaryColorId, int secondaryColorId) {
      this.category = Category.Weapon;
      this.id = id;
      this.type = weaponType;
      this.itemTypeId = (int) weaponType;
      this.count = 1;
      this.color1 = (ColorType) primaryColorId;
      this.color2 = (ColorType) secondaryColorId;
      this.data = "";
   }

   public Weapon (int id, Weapon.Type weaponType, ColorType primaryColorId, ColorType secondaryColorId) {
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
      this.type = (Type)itemTypeId;
      this.color1 = color1;
      this.color2 = color2;
      this.data = data;
   }

   public override string getDescription () {
      switch (type) {
         case Type.Sword_Steel:
            return "A well-made steel sword.";
         case Type.Lance_Steel:
            return "A powerful lance.";
         case Type.Staff_Mage:
            return "A mage's crystal staff.";
         case Type.Mace_Steel:
            return "A mace with razor spikes.";
         case Type.Mace_Star:
            return "A magical Golden Star mace.";
         case Type.Sword_Rune:
            return "A mysterious sword with glowing runes.";
         case Type.Sword_1:
            return "A large sword with a broad blade.";
         case Type.Sword_2:
            return "A small sword made for a novice.";
         case Type.Sword_3:
            return "A lightweight and basic sword.";
         case Type.Sword_4:
            return "A large and heavy sword.";
         case Type.Sword_5:
            return "A massive sword with a broad blade.";
         case Type.Sword_6:
            return "A short but sturdy sword.";
         case Type.Sword_7:
            return "A scimitar with a curved blade.";
         case Type.Sword_8:
            return "A long and thin blade for piercing.";
         case Type.Gun_6:
            return "A long-barreled rifle for powerful shots.";
         case Type.Gun_7:
            return "A large and sturdy pistol.";
         case Type.Gun_2:
            return "A small but powerful blunderbuss.";
         case Type.Gun_3:
            return "A wide-barreled blunderbuss.";
         case Type.Pitchfork:
            return "A basic but functional pitchfork.";
         case Type.Seeds:
            return "A bag of seeds ready for planting.";
         case Type.WateringPot:
            return "A simple little watering pot.";
         default:
            return "";
      }
   }

   public override string getTooltip () {
      Color color = Rarity.getColor(getRarity());
      string colorHex = ColorUtility.ToHtmlStringRGBA(color);

      return string.Format("<color={0}>{1}</color> ({2}, {3})\n\n{4}\n\nDamage = <color=red>{5}</color>",
         "#" +colorHex, getName(), color1, color2, getDescription(), getDamage());
   }

   public override string getName () {
      return getName(type);
   }

   public override bool isEquipped () {
      if (Global.player == null || !(Global.player is BodyEntity)) {
         return false;
      }

      BodyEntity body = (BodyEntity) Global.player;

      return (body.weaponManager.equippedWeaponId == id);
   }

   public static string getName (Weapon.Type weaponType) {
      switch (weaponType) {
         case Type.Sword_Steel:
            return "Steel Sword";
         case Type.Lance_Steel:
            return "Steel Lance";
         case Type.Staff_Mage:
            return "Mage Staff";
         case Type.Mace_Steel:
            return "Steel Mace";
         case Type.Mace_Star:
            return "Golden Star";
         case Type.Sword_Rune:
            return "Rune Blade";
         case Type.Sword_1:
            return "Falchion";
         case Type.Sword_2:
            return "Novice Sword";
         case Type.Sword_3:
            return "Light Sword";
         case Type.Sword_4:
            return "Claymore";
         case Type.Sword_5:
            return "Broad Sword";
         case Type.Sword_6:
            return "Short Sword";
         case Type.Sword_7:
            return "Scimitar";
         case Type.Sword_8:
            return "Rapier";
         case Type.Gun_2:
            return "Small Blunderbuss";
         case Type.Gun_3:
            return "Large Blunderbuss";
         case Type.Gun_6:
            return "Powder Rifle";
         case Type.Gun_7:
            return "Modified Pistol";
         case Type.Pitchfork:
            return "Pitchfork";
         case Type.Seeds:
            return "Seed Bag";
         case Type.WateringPot:
            return "Watering Pot";
         default:
            return "";
      }
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

   public virtual float getDamage (Ability.Element element) {
      // Placeholder
      return 10f;
   }

   public static int getBaseDamage (Type weaponType) {
      switch (weaponType) {
         default:
            return 10;
      }
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

   public static Class getClass (Weapon.Type type) {
      switch (type) {
         default:
            return Class.Melee;
      }
   }

   public static Weapon getEmpty() {
      return new Weapon(0, Weapon.Type.None, ColorType.None, ColorType.None);
   }

   public static Weapon generateRandom (int itemId, Type weaponType) {
      // Decide what the rarity should be
      Rarity.Type rarity = Rarity.getRandom();

      // Alter the damage based on the rarity
      float baseDamage = getBaseDamage(weaponType);
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
      switch (this.type) {
         case Type.Seeds:
         case Type.WateringPot:
         case Type.Pitchfork:
            return false;

         default:
            return base.canBeTrashed();
      }
   }

   public override string getIconPath () {
      return "Icons/Weapons/" + this.type;
   }

   public override ColorKey getColorKey () {
      return new ColorKey(Global.player.gender, this.type);
   }

   #region Private Variables

   #endregion
}
