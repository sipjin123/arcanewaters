using UnityEngine;
using System;

#if IS_SERVER_BUILD

using MySql.Data.MySqlClient;

#endif

[Serializable]
public class Prop : RecipeItem
{
   #region Public Variables

   // The Type
   public enum Type
   {
      None = 0, Tree = 1, Stump = 2, Bush = 3, Chair = 4, Table = 5
   }

   // The type
   public Type type;

   #endregion Public Variables

   public Prop () {
      type = Type.None;
   }

#if IS_SERVER_BUILD

   public Prop (MySqlDataReader dataReader) {
      type = (Type) DataUtil.getInt(dataReader, "itmType");
      id = DataUtil.getInt(dataReader, "itmId");
      category = (Category) DataUtil.getInt(dataReader, "itmCategory");
      itemTypeId = DataUtil.getInt(dataReader, "itmType");
      data = DataUtil.getString(dataReader, "itmData");

      // Defaults
      paletteName1 = DataUtil.getString(dataReader, "itmPalette1");
      paletteName2 = DataUtil.getString(dataReader, "itmPalette2");

      foreach (string kvp in this.data.Split(',')) {
         if (!kvp.Contains("=")) {
            continue;
         }
      }
   }

#endif

   public Prop (int id, Type type, string newPalette1, string newPalette2) {
      category = Category.Prop;
      this.id = id;
      this.type = type;
      itemTypeId = (int) type;
      count = 1;
      paletteName1 = newPalette1;
      paletteName2 = newPalette2;
      data = "";
   }

   public Prop (int id, int itemTypeId, string newPalette1, string newPalette2, string data, int count = 1) {
      category = Category.Prop;
      this.id = id;
      this.count = count;
      this.itemTypeId = itemTypeId;
      type = (Type) itemTypeId;
      paletteName1 = newPalette1;
      paletteName2 = newPalette2;
      this.data = data;
   }

   public override string getDescription () {
      switch (type) {
         case Type.Tree:
            return "A Plantable Tree";

         default:
            return base.getDescription();
      }
   }

   public override string getTooltip () {
      Color color = Rarity.getColor(getRarity());
      string colorHex = ColorUtility.ToHtmlStringRGBA(color);

      return string.Format("<color={0}>{1}</color> ({2}, {3})\n\n{4}",
         "#" + colorHex, getName(), paletteName1, paletteName2, getDescription());
   }

   public override string getName () {
      return getName(type);
   }

   public static string getName (Type type) {
      switch (type) {
         case Type.Tree:
            return "Tree";

         default:
            return "";
      }
   }

   public override string getIconPath () {
      return getIconPath(type);
   }

   public static string getIconPath (Type type) {
      return "Icons/Props/prop_" + type;
   }
}