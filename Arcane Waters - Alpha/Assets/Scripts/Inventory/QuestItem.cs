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
      this.paletteNames =  DataUtil.getString(dataReader, "itmPalettes");

      foreach (string kvp in this.data.Split(',')) {
         if (!kvp.Contains("=")) {
            continue;
         }
      }
   }

#endif

   public QuestItem (int id, int recipeType, string newPalettes, string data = "", int count = 0) {
      this.category = Category.Quest_Item;
      this.id = id;
      this.questItemType = (Type) recipeType;
      this.itemTypeId = (int) recipeType;
      this.count = count;
      this.paletteNames = newPalettes;
      this.data = data;
   }

   public override string getDescription () {
      return itemDescription;
   }

   public override string getTooltip () {
      Color color = Rarity.getColor(getRarity());
      string colorHex = ColorUtility.ToHtmlStringRGBA(color);
      return string.Format("<color={0}>{1}</color> \n\n{2}\n\n",
         "#" + colorHex, getName(), getDescription());
   }

   public override string getName () {
      return itemName.ToString();
   }

   public static string getName (int recipeTypeID) {
      return getItemData(recipeTypeID).getName();
   }

   public static Item getItemData (int questTypeID) {
      QuestItem questItem = new QuestItem {
         questItemType = (Type) questTypeID,
         itemTypeId = questTypeID,
         count = 0,
         paletteNames = "",
         category = Category.Quest_Item,
         data = "",
         id = 0
      };

      return questItem;
   }

   public override bool canBeTrashed () {
      return false;
   }

   public override bool canBeStacked () {
      return true;
   }

   public override string getIconPath () {
      return "Assets/Sprites/Icons/QuestItem/" + questItemType + ".png";
   }
}