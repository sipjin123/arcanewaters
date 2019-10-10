using UnityEngine;
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
      // Generate random colors for the item
      Array values = Enum.GetValues(typeof(ColorType));
      System.Random random = new System.Random();
      ColorType c1 = (ColorType) values.GetValue(random.Next(values.Length));
      ColorType c2 = (ColorType) values.GetValue(random.Next(values.Length));

      // Create the item
      Item item = new Item(-1, category, itemTypeId, count, c1, c2, "");

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
