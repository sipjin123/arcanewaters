using UnityEngine;
using System;
using System.Collections.Generic;

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
   public const string WEAPON_DATA_PREFIX = "blueprintType=weapon";
   public const string ARMOR_DATA_PREFIX = "blueprintType=armor";
   public const string HAT_DATA_PREFIX = "blueprintType=hat";
   public const string INGREDIENT_DATA_PREFIX = "blueprintType=ingredients";
   public const string RING_DATA_PREFIX = "blueprintType=ring";
   public const string NECKLACE_DATA_PREFIX = "blueprintType=necklace";
   public const string TRINKET_DATA_PREFIX = "blueprintType=trinket";

   // Set a generic blueprint icon 
   public const string BLUEPRINT_WEAPON_ICON = "Assets/Sprites/Icons/Blueprint/WeaponBP.png";
   public const string BLUEPRINT_ARMOR_ICON = "Assets/Sprites/Icons/Blueprint/ArmorBP.png";
   public const string BLUEPRINT_HAT_ICON = "Assets/Sprites/Icons/Blueprint/ArmorBP.png";
   public const string BLUEPRINT_INGREDIENT_ICON = "Assets/Sprites/Icons/Blueprint/IngredientBP.png";
   public const string BLUEPRINT_RING_ICON = "Assets/Sprites/Icons/Blueprint/IngredientBP.png";
   public const string BLUEPRINT_NECKLACE_ICON = "Assets/Sprites/Icons/Blueprint/IngredientBP.png";
   public const string BLUEPRINT_TRINKET_ICON = "Assets/Sprites/Icons/Blueprint/IngredientBP.png";

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
      this.paletteNames = DataUtil.getString(dataReader, "itmPalettes");

      foreach (string kvp in this.data.Split(',')) {
         if (!kvp.Contains("=")) {
            continue;
         }
      }
   }

#endif

   public Blueprint (int id, int recipeType, string newPalettes) {
      this.category = Category.Blueprint;
      this.id = id;
      this.itemTypeId = recipeType;
      this.count = 1;
      this.paletteNames = newPalettes;
      this.data = "";
   }

   public Blueprint (int id, int itemTypeId, string newPalettes, string data, int count = 1) {
      this.category = Category.Blueprint;
      this.id = id;
      this.count = count;
      this.itemTypeId = itemTypeId;
      this.paletteNames = newPalettes;
      this.data = data;
   }

   public static Blueprint getEmpty () {
      return new Blueprint(0, 1000, "");
   }

   public static Item.Category getEquipmentType (string data) {
      if (data.StartsWith(WEAPON_DATA_PREFIX)) {
         return Item.Category.Weapon;
      } else if (data.StartsWith(ARMOR_DATA_PREFIX)) {
         return Item.Category.Armor;
      } else if (data.StartsWith(HAT_DATA_PREFIX)) {
         return Item.Category.Hats;
      } else if (data.StartsWith(RING_DATA_PREFIX)) {
         return Item.Category.Ring;
      } else if (data.StartsWith(NECKLACE_DATA_PREFIX)) {
         return Item.Category.Necklace;
      } else if (data.StartsWith(TRINKET_DATA_PREFIX)) {
         return Item.Category.Trinket;
      } else if (data.StartsWith(INGREDIENT_DATA_PREFIX)) {
         return Item.Category.CraftingIngredients;
      }

      return Item.Category.None;
   }

   public override bool canBeTrashed () {
      return base.canBeTrashed();
   }

   public override bool canBeStacked () {
      return true;
   }
}