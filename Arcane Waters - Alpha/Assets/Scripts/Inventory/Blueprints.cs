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
      None = 0, Sword_1 = 1, Sword_2 = 2, Sword_3 = 3, Sword_4 = 4, Sword_5 = 5,
   }

   // The type of equipment
   public enum EquipmentType
   {
      None = 0, Weapon = 1, Armor = 2,
   }

   // The type
   public Type type;

   public EquipmentType equipmentType;

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
      return getItemData(type).getDescription();
   }

   public override string getTooltip () {
      Color color = Rarity.getColor(getRarity());
      string colorHex = ColorUtility.ToHtmlStringRGBA(color);

      return string.Format("<color={0}>{1}</color> ({2}, {3})\n\n{4}",
         "#" + colorHex, getName(), color1, color2, getDescription());
   }

   public override string getName () {
      return getName(type);
   }

   public static string getName (Blueprint.Type recipeType) {
      return getItemData(recipeType).getName();
   }

   private static Item getItemData (Blueprint.Type recipeType) {
      string recipepString = recipeType.ToString();
      Category itemCategory = Category.Weapon;
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
   }

   public static Blueprint getEmpty () {
      return new Blueprint(0, Blueprint.Type.None, ColorType.None, ColorType.None);
   }

   public EquipmentType getEquipmentType() {
      string nameComparison = type.ToString();
      foreach (Weapon.Type val in Enum.GetValues(typeof(Weapon.Type))) {
         if(val.ToString() == nameComparison) {
            return EquipmentType.Weapon;
         }
      }
      foreach (Weapon.Type val in Enum.GetValues(typeof(Armor.Type))) {
         if (val.ToString() == nameComparison) {
            return EquipmentType.Armor;
         }
      }

      return EquipmentType.None;
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