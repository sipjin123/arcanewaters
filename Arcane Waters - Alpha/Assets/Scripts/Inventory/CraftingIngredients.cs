using UnityEngine;
using System;

#if IS_SERVER_BUILD

using MySql.Data.MySqlClient;

#endif

[Serializable]
public class CraftingIngredients : RecipeItem
{
   #region Public Variables

   // The Type
   public enum Type
   {
      None = 0, Lizard_Scale = 1, Lizard_Claw = 2, Gold_Ore = 3, Lumber = 4, Flint = 5, Silver_Ore = 6, Lizard_Tail = 7, Reptile_Bile = 8, Meat = 9,
      Wooden_Bark = 10, Solid_Oak = 11, Shattered_Trunk = 12, Tree_Root = 13,
   }

   // The type
   public Type type;

   #endregion Public Variables

   public CraftingIngredients () {
      this.type = Type.None;
   }

#if IS_SERVER_BUILD

   public CraftingIngredients (MySqlDataReader dataReader) {
      this.type = (CraftingIngredients.Type) DataUtil.getInt(dataReader, "itmType");
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

   public CraftingIngredients (int id, CraftingIngredients.Type recipeType, int primaryColorId, int secondaryColorId) {
      this.category = Category.CraftingIngredients;
      this.id = id;
      this.type = recipeType;
      this.itemTypeId = (int) recipeType;
      this.count = 1;
      this.color1 = (ColorType) primaryColorId;
      this.color2 = (ColorType) secondaryColorId;
      this.data = "";
   }

   public CraftingIngredients (int id, CraftingIngredients.Type recipeType, ColorType primaryColorId, ColorType secondaryColorId) {
      this.category = Category.CraftingIngredients;
      this.id = id;
      this.type = recipeType;
      this.itemTypeId = (int) recipeType;
      this.count = 1;
      this.color1 = primaryColorId;
      this.color2 = secondaryColorId;
      this.data = "";
   }

   public CraftingIngredients (int id, int itemTypeId, ColorType color1, ColorType color2, string data, int count = 1) {
      this.category = Category.CraftingIngredients;
      this.id = id;
      this.count = count;
      this.itemTypeId = itemTypeId;
      this.type = (Type) itemTypeId;
      this.color1 = color1;
      this.color2 = color2;
      this.data = data;
   }

   public override string getDescription () {
      switch (type) {
         case Type.Lizard_Claw:
            return "A well made claw";

         case Type.Lizard_Scale:
            return "A powerful Scale";

         case Type.Gold_Ore:
            return "A shiny golden ore";

         case Type.Silver_Ore:
            return "A shiny silver ore";

         case Type.Flint:
            return "A Flint";

         case Type.Lumber:
            return "A Lumber";

         case Type.Lizard_Tail:
            return "A Lizard Tail";

         case Type.Reptile_Bile:
            return "A Reptile Bile good for binding";

         case Type.Meat:
            return "Meat good for eatin";

         case Type.Wooden_Bark:
            return "A Wooden Bark for weapons";

         case Type.Solid_Oak:
            return "A Solid Oak for ship upgrades";

         case Type.Shattered_Trunk:
            return "A Shattered Trunk for crafting";

         case Type.Tree_Root:
            return "A Tree Root for binding";

         default:
            return "";
      }
   }

   public override string getTooltip () {
      Color color = Rarity.getColor(getRarity());
      string colorHex = ColorUtility.ToHtmlStringRGBA(color);

      return string.Format("<color={0}>{1}</color> ({2}, {3})\n\n{4}\n\nDamage = <color=red>{5}</color>",
         "#" + colorHex, getName(), color1, color2, getDescription());
   }

   public override string getName () {
      return getName(type);
   }

   public static string getName (CraftingIngredients.Type recipeType) {
      switch (recipeType) {
         case Type.Lizard_Claw:
            return "Lizard Claw";

         case Type.Lizard_Scale:
            return "Lizard Scale";

         case Type.Gold_Ore:
            return "Gold Ore";

         case Type.Silver_Ore:
            return "Silver Ore";

         case Type.Lumber:
            return "Lumber";

         case Type.Flint:
            return "Flint";

         case Type.Lizard_Tail:
            return "Lizard Tail";

         case Type.Reptile_Bile:
            return "Reptile Bile";

         case Type.Meat:
            return "Meat";

         case Type.Wooden_Bark:
            return "Wooden Bark";

         case Type.Solid_Oak:
            return "Solid Oak";

         case Type.Shattered_Trunk:
            return "Shattered Trunk";

         case Type.Tree_Root:
            return "Tree Root";

         default:
            return "";
      }
   }

   public static CraftingIngredients getEmpty () {
      return new CraftingIngredients(0, CraftingIngredients.Type.None, ColorType.None, ColorType.None);
   }

   public override bool canBeTrashed () {
      switch (this.type) {
         default:
            return base.canBeTrashed();
      }
   }

   public override string getIconPath () {
      return "Icons/CraftingIngredients/" + this.type;
   }
}