using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

[Serializable]
public class Item {
   #region Public Variables

   // The category of item this is
   public enum Category { None = 0, Weapon = 1, Armor = 2, Hats = 3, Potion = 4, Usable = 5, CraftingIngredients = 6 , Blueprint  = 7, Currency = 8, Quest_Item = 9 }

   // The category of item this is
   public Category category;

   // The item id from the database
   public int id;

   // The item type ID, which will be properly cast by subclasses
   public int itemTypeId;

   // Name of palette that changes color of item
   public string paletteName1 = "";

   // Name of palette that changes color of item
   public string paletteName2 = "";

   // The item data string from the database
   public string data = "";

   // The number of these items that are stacked together
   public int count = 1;

   // Name of the item
   public string itemName = "";

   // Info of the item
   public string itemDescription = "";

   // The icon path of the item
   public string iconPath = "";

   #endregion

   public Item () {

   }

   public void setBasicInfo (string name, string description, string iconPath) {
      this.itemName = name;
      this.itemDescription = description;
      this.iconPath = iconPath;
   }

   public Item (int id, Category category, int itemTypeId, int count, string paletteName1, string paletteName2, string data) {
      this.id = id;
      this.category = category;
      this.itemTypeId = itemTypeId;
      this.paletteName1 = paletteName1;
      this.paletteName2 = paletteName2;
      this.count = count;
      this.data = data;
   }

   public Item getCastItem () {
      switch (this.category) {
         case Category.Hats:
            return new Hat(this.id, this.itemTypeId, paletteName1, paletteName2, data, count);
         case Category.Armor:
            return new Armor(this.id, this.itemTypeId, paletteName1, paletteName2, data, count);
         case Category.Weapon:
            return new Weapon(this.id, this.itemTypeId, paletteName1, paletteName2, data, count);
         case Category.Usable:
            return new UsableItem(this.id, category, this.itemTypeId, count, paletteName1, paletteName2, data);
         case Category.CraftingIngredients:
            return new CraftingIngredients(this.id, this.itemTypeId, paletteName1, paletteName2, data, count);
         case Category.Blueprint:
            return new Blueprint(this.id, this.itemTypeId, paletteName1, paletteName2, data, count);
         case Category.Quest_Item:
            return new QuestItem(this.id, this.itemTypeId, paletteName1, paletteName2, data, count);
         default:
            D.warning("Unknown item category: " + category);
            return null;
      }
   }

   public virtual string getDescription () {
      // Handled by children classes
      return "";
   }

   public virtual string getTooltip () {
      // Simple items can just return their description
      return getDescription();
   }

   public virtual string getName () {
      // Handled by children classes
      return "";
   }

   public virtual bool canBeUsed () {
      // By default, items will not be useable
      return false;
   }

   public virtual bool canBeStacked () {
      // By default, items can be stacked
      return true;
   }

   public virtual bool canBeTrashed () {
      // By default, items can be trashed
      return true;
   }

   public virtual bool canBeEquipped () {
      // By default, items will not be equippable
      return false;
   }

   public virtual Rarity.Type getRarity () {
      foreach (string kvp in this.data.Replace(" ", "").Split(',')) {
         if (!kvp.Contains("=")) {
            continue;
         }

         // Get the left and right side of the equal
         string key = kvp.Split('=')[0];
         string value = kvp.Split('=')[1];

         if ("rarity".Equals(key)) {
            return (Rarity.Type) System.Convert.ToInt32(value);
         }
      }

      return Rarity.Type.Common;
   }

   public int getSellPrice () {
      foreach (string kvp in this.data.Replace(" ", "").Split(',')) {
         if (!kvp.Contains("=")) {
            continue;
         }

         // Get the left and right side of the equal
         string key = kvp.Split('=')[0];
         string value = kvp.Split('=')[1];
         
         if ("price".Equals(key)) {
            return System.Convert.ToInt32(value);
         }
      }

      return 1000;
   }

   public static int getBaseSellPrice (Category category, int itemTypeId) {
      // Armor prices
      if (category == Category.Armor) {
         switch (itemTypeId) {
            case 1:
               return 1000;
            default:
               return 1000;
         }
      }

      // Weapon prices
      if (category == Category.Weapon) {
         // TODO: update to a more dynamic approach
         switch (itemTypeId) {
            case 1://Weapon.Type.Sword_2:
               return 160;
            case 2://Weapon.Type.Sword_3:
               return 1250;
            case 3://Weapon.Type.Gun_2:
               return 6800;
            case 4://Weapon.Type.Sword_1:
               return 18200;
            case 5://Weapon.Type.Gun_3:
               return 56500;
            default:
               return 1000;
         }
      }

      return 1000;
   }

   public virtual string getIconPath () {
      // Subclasses will override this
      return "Icons/Weapons/" + itemTypeId;
   }

   public virtual string getBorderlessIconPath () {
      // Subclasses will override this
      return getIconPath();
   }

   public static List<Item> unserializeAndCast (string[] itemArray) {
      List<Item> castItems = new List<Item>();
      List<Item> items = Util.unserialize<Item>(itemArray);

      foreach (Item item in items) {
         castItems.Add(item.getCastItem());
      }

      return castItems;
   }

   public static string getColoredStockCount (int count) {
      string color = "white";

      if (count <= 3) {
         color = "red";
      } else if (count <= 10) {
         color = "yellow";
      } else {
         color = "green";
      }

      return string.Format("<color={0}>x {1}</color>", color, count);
   }

   public static bool isUsingEquipmentXML (Item.Category category) {
      if (category == Category.Armor || category == Category.Weapon || category == Category.Hats) {
         return true;
      }

      return false;
   }

   #region Private Variables

   #endregion
}
