using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;
using System.Linq;

[Serializable]
public class Item
{
   #region Public Variables

   // The category of item this is
   public enum Category
   {
      None = 0, Weapon = 1, Armor = 2, Hats = 3, Potion = 4, Usable = 5, CraftingIngredients = 6,
      Blueprint = 7, Currency = 8, Quest_Item = 9, Haircut = 10, Gems = 11, ShipSkin = 12, Dye = 13,
      Pet = 14, Consumable = 15, Crop = 16, Prop = 17
   }

   // The durability filter being used by the item
   public enum DurabilityFilter { None = 0, MaxDurability = 1, ReducedDurability = 2 }

   // The category of item this is
   public Category category;

   // The item id from the database
   public int id;

   // The item type ID, which will be properly cast by subclasses
   // This ID refers to generic type of item (Apple, Wood, Sword, etc.), not it's replicas.
   // Depending on the category, it may link to different things, ex:
   // A value of an enum, the ID of XML data entry in database, the ID of ItemDefinition in database
   public int itemTypeId;

   // Name of palettes that changes color of item
   public string paletteNames = "";

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

   // The durability
   public int durability = 100;

   // The max durability
   public const int MAX_DURABILITY = 100;

   // The amount of durability to deduct to the item
   public const int ITEM_DURABILITY_DEDUCTION = 1;

   // The amount to deduct the weapon durabiliy (max of 100%)
   public const int WEAPON_DURABILITY_DEDUCTION = 20;

   // The amount to deduct the armor durabiliy (max of 100%)
   public const int ARMOR_DURABILITY_DEDUCTION = 10;

   // Max cap in randomizing item durability deduction
   public const int PERCENT_CHANCE = 100;

   // Soul binding types
   public enum SoulBindingType
   {
      // No soul binding
      None = 0,

      // Soul binding on acquisition
      Acquisition = 1,

      // Soul binding on equip
      Equip = 2
   }

   #endregion

   public Item () {

   }

   public static bool isValidItem (Item item) {
      if (item == null) {
         D.debug("Item is null!");
         return false;
      }
      bool isValid = (item.category != Item.Category.None && item.itemTypeId > 0 && item.count > 0);
      if (!isValid) {
         D.debug("An invalid Item was being used! I:{" + item.id + "} C:{" + item.category + "} T:{" + item.itemTypeId + "} Q:{" + item.count + "} R:{" + item.getRarity() + "}");
      }
      return isValid;
   }

   public static Item defaultLootItem () {
      return new Item {
         category = Category.CraftingIngredients,
         count = 1,
         itemTypeId = (int) CraftingIngredients.Type.Wood,
         data = "",
         durability = 100,
         itemName = CraftingIngredients.Type.Wood.ToString(),
      };
   }

   public void setBasicInfo (string name, string description, string iconPath) {
      this.itemName = name;
      this.itemDescription = description;
      this.iconPath = iconPath;
   }

   public void setBasicInfo (string name, string description, string iconPath, string paletteNames) {
      this.itemName = name;
      this.itemDescription = description;
      this.iconPath = iconPath;
      this.paletteNames = paletteNames;
   }

   public Item (int id, Category category, int itemTypeId, int count, string paletteNames, string data, int durability) {
      this.id = id;
      this.category = category;
      this.itemTypeId = itemTypeId;
      this.paletteNames = paletteNames;
      this.count = count;
      this.data = data;
      this.durability = durability;
   }

   public bool tryGetCastItem<T> (out T castItem) where T : Item {
      castItem = getCastItem() as T;
      return castItem != null;
   }

   public Item getCastItem () {
      switch (this.category) {
         case Category.Hats:
            return new Hat(this.id, this.itemTypeId, paletteNames, data, durability, count);
         case Category.Armor:
            return new Armor(this.id, this.itemTypeId, paletteNames, data, durability, count);
         case Category.Weapon:
            return new Weapon(this.id, this.itemTypeId, paletteNames, data, durability, count);
         case Category.Usable:
            return new UsableItem(this.id, category, this.itemTypeId, count, paletteNames, data);
         case Category.CraftingIngredients:
            return new CraftingIngredients(this.id, this.itemTypeId, paletteNames, data, count);
         case Category.Blueprint:
            return new Blueprint(this.id, this.itemTypeId, paletteNames, data, count);
         case Category.Quest_Item:
            return new QuestItem(this.id, this.itemTypeId, paletteNames, data, count);
         case Category.Haircut:
            return new Haircut(this.id, this.itemTypeId, paletteNames, data, durability, count);
         case Category.ShipSkin:
            return new ShipSkin(this.id, this.itemTypeId, paletteNames, data, durability, count);
         case Category.Consumable:
            return new Consumable(this.id, this.itemTypeId, paletteNames, data, durability, count);
         case Category.Dye:
            return new Dye(this.id, this.itemTypeId, paletteNames, data, durability, count);
         case Category.Crop:
            return new CropItem(id, itemTypeId, paletteNames, data, durability, count);
         case Category.Prop:
            return new Prop(id, itemTypeId, paletteNames, data, durability, count);
         default:
            D.debug("Unknown item category: " + category);
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

   public static Rarity.Type getRarity (Item item) {
      foreach (string kvp in item.data.Replace(" ", "").Split(',')) {
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

   public static string trimItmPalette (string paletteRawString) {
      if (paletteRawString == null) {
         return "";
      }

      string palettes = paletteRawString.Trim();
      if (palettes.EndsWith(",")) {
         palettes = palettes.Remove(palettes.Length - 1);
      }
      return palettes;
   }

   public static string[] parseItmPalette (string paletteRawString) {
      if (paletteRawString == null || paletteRawString == "") {
         return new string[0];
      }

      List<string> toReturn = new List<string>();
      string[] palettes = paletteRawString.Split(',');
      for (int i = 0; i < palettes.Length; i++) {
         string p = palettes[i].Trim();
         if (p != "") {
            toReturn.Add(p);
         }
      }

      return toReturn.ToArray();
   }

   public static string parseItmPalette (string[] paletteList) {
      string palettes = "";
      if (paletteList != null && paletteList.Length > 0) {
         foreach (string p in paletteList) {
            palettes += p + ", ";
         }
         palettes = palettes.Trim();
         palettes = palettes.Remove(palettes.Length - 1);
      }

      return palettes;
   }

   // Replaces the palette values in destination with the palette values from source
   public static string[] overridePalette (string source, string destination) {
      // Merge palette with player's palette
      string[] sourcePaletteArray = Item.parseItmPalette(source);
      string[] destinationPaletteArray = Item.parseItmPalette(destination);

      List<string> finalPalettes = new List<string>();

      List<string> sourcePrimaryList = sourcePaletteArray.Where(_ => _.ToLower().Contains("primary")).ToList();
      List<string> sourceSecondaryList = sourcePaletteArray.Where(_ => _.ToLower().Contains("secondary")).ToList();
      List<string> sourceAccentList = sourcePaletteArray.Where(_ => _.ToLower().Contains("accent")).ToList();

      List<string> destinationPrimaryList = destinationPaletteArray.Where(_ => _.ToLower().Contains("primary")).ToList();
      List<string> destinationSecondaryList = destinationPaletteArray.Where(_ => _.ToLower().Contains("secondary")).ToList();
      List<string> destinationAccentList = destinationPaletteArray.Where(_ => _.ToLower().Contains("accent")).ToList();

      finalPalettes.AddRange(sourcePrimaryList.Any() ? sourcePrimaryList : destinationPrimaryList);
      finalPalettes.AddRange(sourceSecondaryList.Any() ? sourceSecondaryList : destinationSecondaryList);
      finalPalettes.AddRange(sourceAccentList.Any() ? sourceAccentList : destinationAccentList);

      return finalPalettes.ToArray();
   }

   public static bool isUsable (Category category) {
      return category == Category.Usable ||
         category == Category.ShipSkin ||
         category == Category.Haircut ||
         category == Category.Dye ||
         category == Category.Consumable;
   }

   public static bool isEquippable (Category category) {
      return category == Category.Weapon ||
         category == Category.Armor ||
         category == Category.Hats;
   }

   public static bool isRecolorable (Category category) {
      return category == Item.Category.Weapon ||
         category == Item.Category.Armor ||
         category == Item.Category.Hats ||
         category == Item.Category.Haircut ||
         category == Item.Category.ShipSkin;
   }

   #region Private Variables

   #endregion
}
