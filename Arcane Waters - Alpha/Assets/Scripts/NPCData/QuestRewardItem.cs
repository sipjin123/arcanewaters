﻿using UnityEngine;
using System.Collections;
using System.Xml.Serialization;
using System;

public class QuestRewardItem : QuestReward
{
   #region Public Variables

   // The category of the rewarded item
   [XmlIgnore]
   public Item.Category category;

   [XmlElement("category")]
   public int CategoryInt
   {
      get { return (int) category; }
      set { category = (Item.Category) value; }
   }

   // The type of the rewarded item
   public int itemTypeId;

   // The number of items to reward
   public int count;

   // The data content of this item
   public string data;

   #endregion

   public QuestRewardItem () {

   }

   public QuestRewardItem(Item.Category category, int itemTypeId, int count) {
      this.category = category;
      this.itemTypeId = itemTypeId;
      this.count = count;
   }

   // Must be called from the background thread!
   public override Item giveRewardToUser (int npcId, int userId) {
      // Create the item
      Item item = ItemGenerator.generate(category, itemTypeId, count);

      // Write the item in the database
      Item databaseItem = DB_Main.createItemOrUpdateItemCount(userId, item);

      // Reset the count to the rewarded amount
      databaseItem.count = count;

      // Return the item
      return databaseItem;
   }

   #region Private Variables

   #endregion

}
