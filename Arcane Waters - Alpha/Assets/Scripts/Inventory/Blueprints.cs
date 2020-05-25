using UnityEngine;
using System;

#if IS_SERVER_BUILD

using MySql.Data.MySqlClient;

#endif

[Serializable]
public class Blueprint : RecipeItem
{
   #region Public Variables

   // The different blueprint statuses
   public enum Status {
      Craftable = 1,
      NotCraftable = 2,
      MissingRecipe = 3// = Missing CraftableItemRequirements data
   }

   // The type of blueprint equipment if Weapon or Armor
   public Item.Category equipmentType;

   // Prefixes for ID
   public const string WEAPON_ID_PREFIX = "100";
   public const string ARMOR_ID_PREFIX = "200";
   public const string WEAPON_DATA_PREFIX = "blueprintType=weapon";
   public const string ARMOR_DATA_PREFIX = "blueprintType=armor";

   // Set a generic blueprint icon 
   public const string BLUEPRINT_WEAPON_ICON = "Assets/Sprites/Icons/Blueprint/WeaponBP.png";
   public const string BLUEPRINT_ARMOR_ICON = "Assets/Sprites/Icons/Blueprint/ArmorBP.png";

   #endregion Public Variables

   public Blueprint () {
      this.itemTypeId = 1000;
   }

#if IS_SERVER_BUILD

   public Blueprint (MySqlDataReader dataReader) {
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
      }
   }

#endif

   public Blueprint (int id, int recipeType, string newPalette1, string newPalette2) {
      this.category = Category.Blueprint;
      this.id = id;
      this.itemTypeId = recipeType;
      this.count = 1;
      this.paletteName1 = newPalette1;
      this.paletteName2 = newPalette2;
      this.data = "";
   }

   public Blueprint (int id, int itemTypeId, string newPalette1, string newPalette2, string data, int count = 1) {
      this.category = Category.Blueprint;
      this.id = id;
      this.count = count;
      this.itemTypeId = itemTypeId;
      this.paletteName1 = newPalette1;
      this.paletteName2 = newPalette2;
      this.data = data;
   }

   public static Blueprint getEmpty () {
      return new Blueprint(0, 1000, "", "");
   }

   public static Item.Category getEquipmentType (string data) {
      if (data.StartsWith(WEAPON_DATA_PREFIX)) {
         return Item.Category.Weapon;
      } else if (data.StartsWith(ARMOR_DATA_PREFIX)) {
         return Item.Category.Armor;
      }
      
      return Item.Category.None;
   }

   public static string createData (Item.Category resultCategory, int typeID) {
      switch (resultCategory) {
         case Category.Weapon:
            return WEAPON_DATA_PREFIX + typeID;
         case Category.Armor:
            return ARMOR_DATA_PREFIX + typeID;
      }

      return "";
   }

   public static int modifyID (Item.Category resultCategory, int typeID) {
      int modifiedID = typeID;
      if (resultCategory == Item.Category.Blueprint) {
         if (typeID.ToString().StartsWith(ARMOR_ID_PREFIX)) {
            modifiedID = int.Parse(typeID.ToString().Replace(Blueprint.ARMOR_ID_PREFIX, ""));
         } else if (typeID.ToString().StartsWith(WEAPON_ID_PREFIX)) {
            modifiedID = int.Parse(typeID.ToString().Replace(Blueprint.WEAPON_ID_PREFIX, ""));
         }
      } else {
         Debug.LogWarning("Invalid Category: " + resultCategory);
      }
      return modifiedID;
   }

   public override bool canBeTrashed () {
      return base.canBeTrashed();
   }

   public override bool canBeStacked () {
      return true;
   }
}