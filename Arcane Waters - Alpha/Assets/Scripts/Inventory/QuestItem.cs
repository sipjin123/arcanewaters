using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

[Serializable]
public class QuestItem : Item
{
   #region Public Variables

   // The type of Quest Item
   public Type questItemType;

   public enum Type
   {
      None = 0, Message = 1, Letter = 2, 
   }

   #endregion Public Variables

   public QuestItem () {

   }

#if IS_SERVER_BUILD

   public QuestItem (MySqlDataReader dataReader) {
      this.questItemType = (Type) DataUtil.getInt(dataReader, "itmType");
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

   public QuestItem (int id, int recipeType, string newPalette1, string newPalette2, string data = "", int count = 0) {
      this.category = Category.Quest_Item;
      this.id = id;
      this.questItemType = (Type) recipeType;
      this.itemTypeId = (int) recipeType;
      this.count = 1;
      this.paletteName1 = newPalette1;
      this.paletteName2 = newPalette2;
      this.data = data;
   }

   public override string getDescription () {
      return "Undefined";
   }

   public override string getTooltip () {
      Color color = Rarity.getColor(getRarity());
      string colorHex = ColorUtility.ToHtmlStringRGBA(color);

      return string.Format("<color={0}>{1}</color> ({2}, {3})\n\n{4}",
         "#" + colorHex, getName(), paletteName1, paletteName2, getDescription());
   }

   public override string getName () {
      return questItemType.ToString();
   }

   public static string getName (int recipeTypeID) {
      return getItemData(recipeTypeID).getName();
   }

   public static Item getItemData (int questTypeID) {
      string recipeString = questTypeID.ToString();

      QuestItem questItem = new QuestItem {
         questItemType = (Type) questTypeID,
         itemTypeId = questTypeID,
         count = 0,
         paletteName1 = "",
         paletteName2 = "",
         category = Category.Quest_Item,
         data = "",
         id = 0
      };

      return questItem;
   }

   public override bool canBeTrashed () {
      return false;
   }

   public override string getIconPath () {
      return "Assets/Sprites/Icons/QuestItem/" + questItemType + ".png";
   }
}