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
      None = 0, Sword_1 = 1, Sword_2 = 2, Sword_3 = 3, Lizard_Sword = 4,
   }

   // The type
   public Type type;

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
      switch (type) {
         case Type.Sword_1:
            return "A well-made claw.";

         case Type.Sword_2:
            return "A powerful Scale.";

         case Type.Sword_3:
            return "A Flint.";

         case Type.Lizard_Sword:
            return "A Lumber.";

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

   public static string getName (Blueprint.Type recipeType) {
      switch (recipeType) {
         case Type.Sword_1:
            return "Common Sword";

         case Type.Sword_2:
            return "Rare Sword";

         case Type.Sword_3:
            return "Epic Sword";

         case Type.Lizard_Sword:
            return "Lizard Sword";

         default:
            return "";
      }
   }

   public static Blueprint getEmpty () {
      return new Blueprint(0, Blueprint.Type.None, ColorType.None, ColorType.None);
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