using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

/// <summary>
/// Represents a description of an item - it's name, images and various properties.
/// </summary>
// NOTE: Include '[XmlRoot("Item")]' in every derived class to avoid type errors when deserializing.
[Serializable]
[XmlRoot("Item")]
public class ItemDefinition
{
   #region Public Variables

   // Category type of an item
   public enum Category { None = 0, Prop = 10 }

   // Name of the item
   public string name = "";

   // Unique identifier of item's definition
   public int id;

   // The category of item this is
   public Category category;

   // Description of the item
   public string description = "";

   // The icon path of the item
   public string iconPath = "";

   // Id of the user that created this definition
   public int creatorUserId;

   #endregion

   protected ItemDefinition () {

   }

   public virtual bool canBeStacked () {
      // By default, items can be stacked
      return true;
   }

   public Sprite getIcon () {
      return ImageManager.getSprite(iconPath);
   }

   public string serialize () {
      XmlSerializer serializer = new XmlSerializer(GetType());
      var sb = new System.Text.StringBuilder();
      using (var writer = System.Xml.XmlWriter.Create(sb)) {
         serializer.Serialize(writer, this);
      }
      string weaponDataXML = sb.ToString();
      return weaponDataXML;
   }

   public static ItemDefinition create (Category category) {
      switch (category) {
         // case x : 
         // return <call appropriate subclass constructor with provided parameters>
         case Category.None:
            return new ItemDefinition();
         case Category.Prop:
            return new PropDefinition();
         default:
            D.debug($"Undefined creation of an item category: { category }");
            return new ItemDefinition();
      }
   }

   public static ItemDefinition deserialize (string data, Category category) {
      switch (category) {
         // case x : 
         // return <call appropriate subclass constructor with provided parameters>
         case Category.None:
            return deserialize<ItemDefinition>(data);
         case Category.Prop:
            return deserialize<PropDefinition>(data);
         default:
            D.debug($"Undefined deserialization of an item category: { category }");
            return deserialize<ItemDefinition>(data);
      }
   }

   public static T deserialize<T> (string data) where T : ItemDefinition {
      if (string.IsNullOrEmpty(data)) {
         return null;
      }

      try {
         T obj = Util.xmlLoad<T>(data);
         return obj;
      } catch (Exception ex) {
         D.error($"Error deserializing data to type: { typeof(T) }, provided data: { data }, exception: { ex }");
         return null;
      }
   }

   public List<Item> getStumpHarvestLoot () {
      return new List<Item> {
         new Item {
            category = Item.Category.CraftingIngredients,
            count = 1,
            itemTypeId = (int) CraftingIngredients.Type.Wood,
            data = "",
            durability = 100,
            itemName = CraftingIngredients.Type.Wood.ToString(),
         }
      };
   }

   // Hardcoded item rewards for when player gets his first house/farm
   // 2 Tables, 6 Chairs, 2 Stools, 6 Tree seeds, 4 Bush seeds
   public static List<Item> getFirstFarmRewards (int receiverUserId) {
      return new List<Item> {
         new Item(0, Item.Category.Weapon, 82, 2, "", "", 100),
         new Item(0, Item.Category.Weapon, 84, 2, "", "", 100),
         new Item(0, Item.Category.Weapon, 86, 2, "", "", 100),
         new Item(0, Item.Category.Weapon, 87, 2, "", "", 100),
         new Item(0, Item.Category.Weapon, 88, 2, "", "", 100)
      };
   }

   public static List<Item> getFirstHouseRewards (int receiverUserId) {
      return new List<Item> {
         new Item(0, Item.Category.Prop, 11, 2, "", "", 100),
         new Item(0, Item.Category.Prop, 10, 6, "", "", 100),
         new Item(0, Item.Category.Prop, 16, 2, "", "", 100)
      };
   }

   #region Private Variables

   #endregion
}
