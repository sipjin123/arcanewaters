using System;
using System.Xml.Serialization;
using UnityEngine;

/// <summary>
/// Represents a description of an item - it's name, images and various properties.
/// When this item is used/owned/created inside the game, it takes form of 'ItemInstance'.
/// </summary>
[Serializable]
public class ItemDefinition
{
   #region Public Variables

   // Category type of an item
   public enum Category { None = 0, Weapon = 1, Armor = 2, Hats = 3, Potion = 4, Usable = 5, CraftingIngredients = 6, Blueprint = 7, Currency = 8, Quest_Item = 9, Prop = 10 }

   // Unique identifier of item's definition
   public int id;

   // If item definition is enabled in the game;
   public bool enabled;

   // The category of item this is
   public Category category;

   // Name of the item
   public string name = "";

   // Description of the item
   public string description = "";

   // The icon path of the item
   public string iconPath = "";

   // Id of the user that created this definition
   public int creatorUserId;

   #endregion

   public string serialize () {
      XmlSerializer serializer = new XmlSerializer(GetType());
      var sb = new System.Text.StringBuilder();
      using (var writer = System.Xml.XmlWriter.Create(sb)) {
         serializer.Serialize(writer, this);
      }
      string weaponDataXML = sb.ToString();
      return weaponDataXML;
   }

   public static ItemDefinition deserialize (string data, Category category) {
      switch (category) {
         // case x : 
         // return <call appropriate subclass constructor with provided parameters>
         case Category.None:
            return deserialize<ItemDefinition>(data);
         default:
            D.error($"Undefined deserialization of an item category: { category }");
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

   #region Private Variables

   #endregion
}
