using UnityEngine;
using System;

#if IS_SERVER_BUILD

using MySql.Data.MySqlClient;

#endif

[Serializable]
public class Blueprint : RecipeItem
{
   #region Public Variables

   // The Type
   public enum Type
   {
      None = 0, Sword_Steel = 1, Staff_Mage = 8, Lance_Steel = 9, Mace_Steel = 10, Mace_Star = 11, Sword_Rune = 12,
      Sword_1 = 101, Sword_2 = 102, Sword_3 = 103, Sword_4 = 104, Sword_5 = 105,
      Sword_6 = 106, Sword_7 = 107, Sword_8 = 108,
      Seeds = 150, Pitchfork = 151, WateringPot = 152,

      Gun_6 = 200, Gun_7 = 201, Gun_2 = 202, Gun_3 = 203,

      Cloth = 301, Leather = 302, Steel = 303, Sash = 304, Tunic = 305,
      Posh = 306, Formal = 307, Casual = 308, Plate = 309, Wool = 310,
      Strapped = 311,

   }

   // The type
   public Type type;

   // The type of blueprint equipment if Weapon or Armor
   public Item.Category equipmentType;

   #endregion Public Variables

   public Blueprint () {
      this.type = Type.None;
   }

#if IS_SERVER_BUILD

   public Blueprint (MySqlDataReader dataReader) {
      this.type = (Blueprint.Type) DataUtil.getInt(dataReader, "itmType");
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
      }
   }

#endif

   public Blueprint (int id, Blueprint.Type recipeType, int primaryColorId, int secondaryColorId) {
      this.category = Category.Blueprint;
      this.id = id;
      this.type = recipeType;
      this.itemTypeId = (int) recipeType;
      this.count = 1;
      this.color1 = (ColorType) primaryColorId;
      this.color2 = (ColorType) secondaryColorId;
      this.data = "";
   }

   public Blueprint (int id, Blueprint.Type recipeType, ColorType primaryColorId, ColorType secondaryColorId) {
      this.category = Category.Blueprint;
      this.id = id;
      this.type = recipeType;
      this.itemTypeId = (int) recipeType;
      this.count = 1;
      this.color1 = primaryColorId;
      this.color2 = secondaryColorId;
      this.data = "";
   }

   public Blueprint (int id, int itemTypeId, ColorType color1, ColorType color2, string data, int count = 1) {
      this.category = Category.Blueprint;
      this.id = id;
      this.count = count;
      this.itemTypeId = itemTypeId;
      this.type = (Type) itemTypeId;
      this.color1 = color1;
      this.color2 = color2;
      this.data = data;
   }

   public override string getDescription () {
      return "A blueprint design for: "+getItemData(type).getDescription();
   }

   public override string getTooltip () {
      Color color = Rarity.getColor(getRarity());
      string colorHex = ColorUtility.ToHtmlStringRGBA(color);

      return string.Format("<color={0}>{1}</color> ({2}, {3})\n\n{4}",
         "#" + colorHex, getName(), color1, color2, getDescription());
   }

   public override string getName () {
      if (type.ToString() == "") {
         return "Missing Design";
      }
      return getName(type)+" Design";
   }

   public static string getName (Blueprint.Type recipeType) {
      return getItemData(recipeType).getName();
   }

   public static Item getItemData (Blueprint.Type recipeType) {
      string recipepString = recipeType.ToString();

      Item.Category itemCategory = getEquipmentType(recipeType);
      if (itemCategory == Category.Weapon) {
         Weapon.Type weaponType = Weapon.Type.None;

         foreach (var item in Enum.GetValues(typeof(Weapon.Type))) {
            string weaponString = item.ToString();
            if (recipepString == weaponString) {
               weaponType = (Weapon.Type) item;
            }
         }
         Weapon newWeapon = new Weapon {
            category = itemCategory,
            type = weaponType,
            itemTypeId = (int) weaponType,
            id = 0,
            count = 1,
         };
         return newWeapon;
      } else {
         Armor.Type armorType = Armor.Type.None;

         foreach (var item in Enum.GetValues(typeof(Armor.Type))) {
            string armorString = item.ToString();
            if (recipepString == armorString) {
               armorType = (Armor.Type) item;
            }
         }
         Armor newArmor = new Armor {
            category = itemCategory,
            type = armorType,
            itemTypeId = (int) armorType,
            id = 0,
            count = 1,
         };
         return newArmor;
      }
   }

   public static Blueprint getEmpty () {
      return new Blueprint(0, Blueprint.Type.None, ColorType.None, ColorType.None);
   }

   public static Item.Category getEquipmentType (Blueprint.Type blueprintType) {
      string nameComparison = blueprintType.ToString();
      foreach (Weapon.Type val in Enum.GetValues(typeof(Weapon.Type))) {
         if (val.ToString() == nameComparison) {
            return Item.Category.Weapon;
         }
      }
      foreach (Armor.Type val in Enum.GetValues(typeof(Armor.Type))) {
         if (val.ToString() == nameComparison) {
            return Item.Category.Armor;
         }
      }

      return Item.Category.None;
   }

   public override bool canBeTrashed () {
      switch (this.type) {
         default:
            return base.canBeTrashed();
      }
   }

   public override string getIconPath () {
      return "Icons/Blueprint/" + this.type;
   }
}