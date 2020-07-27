using UnityEngine;
using System.Collections;
using System.Xml.Serialization;
using System.Text;
using System;

public class QuestObjectiveDeliver : QuestObjective
{
   #region Public Variables
   
   // The category of the item to deliver
   [XmlIgnore]
   public Item.Category category;

   [XmlElement("category")]
   public int CategoryInt
   {
      get { return (int) category; }
      set { category = (Item.Category) value; }
   }

   // The type of the item to deliver
   public int itemTypeId;

   // The number of items to deliver
   public int count;

   // The data content of this item
   public string data;

   // Currency Path
   public const string GOLD_ICON_PATH = "Assets/Sprites/Icons/gold_icon.png";

   #endregion

   public QuestObjectiveDeliver () {

   }

   public QuestObjectiveDeliver (Item.Category category, int itemTypeId, int count) {
      this.category = category;
      this.itemTypeId = itemTypeId;
      this.count = count;
   }

   // Must be called from the background thread!
   public override bool canObjectiveBeCompletedDB (int userId) {
      if (category != Item.Category.Currency) {
         // Retrieve the number of items the users has in inventory
         int itemsInInventory = DB_Main.getItemCountByType(userId, (int) category, itemTypeId);

         // Determines if the objective is completed
         return canObjectiveBeCompleted(itemsInInventory);
      } else {
         int currentCurrency = DB_Main.getGold(userId);

         // Determines if the objective is completed
         return canObjectiveBeCompleted(currentCurrency);
      }
   }

   // Must be called from the background thread!
   public override void completeObjective (int userId) {
      if (category != Item.Category.Currency) {
         // Retrieve the number of items the users has in inventory
         int itemsInInventory = DB_Main.getItemCountByType(userId, (int) category, itemTypeId);

         // Retrieve the item ID
         int itemId = DB_Main.getItemID(userId, (int) category, itemTypeId);

         // Remove the required amount of items from the inventory
         DB_Main.decreaseQuantityOrDeleteItem(userId, itemId, count);
      } else {
         // Reduce gold
         DB_Main.addGold(userId, -count);
      }
   }

   // Must be called from the background thread!
   public override int getObjectiveProgress (int userId) {
      if (category != Item.Category.Currency) {
         // Retrieve the number of items the users has in inventory
         int itemsForObjective = DB_Main.getItemCountByType(userId, (int) category, itemTypeId);

         // Clamp the number of items to the required amount
         if (itemsForObjective > count) {
            itemsForObjective = count;
         }

         // Return the item count
         return itemsForObjective;
      } else {
         int itemsForObjective = DB_Main.getGold(userId);

         // Clamp the number of items to the required amount
         if (itemsForObjective > count) {
            itemsForObjective = count;
         }
         // Return the item count
         return itemsForObjective;
      }
   }

   public override bool canObjectiveBeCompleted (int progress) {
      if (progress >= count) {
         return true;
      } else {
         return false;
      }
   }

   public override string getObjectiveDescription () {
      // Builds the description
      StringBuilder builder = new StringBuilder();
      builder.Append("Deliver ");
      builder.Append(count.ToString());
      builder.Append(" ");

      // Adds the name of the item
      switch (category) {
         case Item.Category.Weapon:
            builder.Append(Weapon.getName(itemTypeId));
            break;
         case Item.Category.Armor:
            builder.Append(Armor.getName(itemTypeId));
            break;
         case Item.Category.Hats:
            builder.Append(Armor.getName(itemTypeId));
            break;
         case Item.Category.Potion:
            builder.Append("Potion");
            break;
         case Item.Category.Usable:
            builder.Append(((UsableItem.Type) itemTypeId).ToString());
            break;
         case Item.Category.CraftingIngredients:
            builder.Append(CraftingIngredients.getName((CraftingIngredients.Type) itemTypeId));
            break;
         case Item.Category.Blueprint:
            // TODO: Ensure this is never called
            Debug.LogWarning("Deprecated Call");
            //builder.Append(Blueprint.getName(itemTypeId));
            break;
         case Item.Category.Currency:
            builder.Append("Gold");
            break;
         default:
            break;
      }

      // Ads an 's' if multiple items are required
      if (count > 1) {
         builder.Append("s");
      }

      return builder.ToString();
   }

   public override Sprite getIcon () {
      if (category == Item.Category.Currency) {
         return ImageManager.getSprite(GOLD_ICON_PATH);
      } else {
         Item item = new Item(-1, category, itemTypeId, 1, "", "", "").getCastItem();
         string path = item.getIconPath();
         return ImageManager.getSprite(path);
      }
   }

   public override string getProgressString (int progress) {
      return progress.ToString() + "/" + count.ToString();
   }

   #region Private Variables

   #endregion
}