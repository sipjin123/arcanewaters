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
   public Type questType;

   public enum Type
   {
      None = 0, Message = 1, Letter = 2, 
   }

   #endregion Public Variables

   public QuestItem () {

   }

#if IS_SERVER_BUILD

   public QuestItem (MySqlDataReader dataReader) {
      this.questType = (Type)DataUtil.getInt(dataReader, "itmType");
      this.id = DataUtil.getInt(dataReader, "itmId");
      this.category = (Item.Category) DataUtil.getInt(dataReader, "itmCategory");
      this.itemTypeId = DataUtil.getInt(dataReader, "itmType");
      this.data = DataUtil.getString(dataReader, "itmData");
      this.questType = (Type) DataUtil.getInt(dataReader, "itmType");

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

   public QuestItem (int id, int recipeType, ColorType primaryColorId, ColorType secondaryColorId, string data = "", int count = 0) {
      this.category = Category.Quest_Item;
      this.id = id;
      this.questType = (Type) recipeType;
      this.itemTypeId = (int) recipeType;
      this.count = 1;
      this.color1 = (ColorType) primaryColorId;
      this.color2 = (ColorType) secondaryColorId;
      this.data = data;
   }

   public override string getDescription () {
      return "Undefined";
   }

   public override string getTooltip () {
      Color color = Rarity.getColor(getRarity());
      string colorHex = ColorUtility.ToHtmlStringRGBA(color);

      return string.Format("<color={0}>{1}</color> ({2}, {3})\n\n{4}",
         "#" + colorHex, getName(), color1, color2, getDescription());
   }

   public override string getName () {
      return questType.ToString();
   }

   public static string getName (int recipeTypeID) {
      return getItemData(recipeTypeID).getName();
   }

   public static Item getItemData (int questTypeID) {
      string recipeString = questTypeID.ToString();

      QuestItem questItem = new QuestItem {
         questType = (Type) questTypeID,
         itemTypeId = questTypeID,
         count = 0,
         color1 = ColorType.Black,
         color2 = ColorType.Black,
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
      return "Assets/Sprites/Icons/QuestItem/" + questType + ".png";
   }
}