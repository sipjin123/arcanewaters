﻿using System;
using System.Xml.Serialization;
using UnityEngine;

/// <summary>
/// Represents a description of an item - it's name, images and various properties.
/// When this item is used/owned/created inside the game, it takes form of 'ItemInstance'.
/// </summary>
// NOTE: Include '[XmlRoot("Item")]' in every derived class to avoid type errors when deserializing.
[Serializable]
[XmlRoot("Item")]
public class ItemDefinition
{
   #region Public Variables

   // Category type of an item
   public enum Category { None = 0, Weapon = 1, Armor = 2, Hats = 3, Potion = 4, Usable = 5, CraftingIngredients = 6, Blueprint = 7, Currency = 8, Quest_Item = 9, Prop = 10 }

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
         case Category.Weapon:
            return new WeaponDefinition();
         case Category.Armor:
            return new ArmorDefinition();
         case Category.Hats:
            return new HatDefinition();
         case Category.Potion:
            return new PotionDefinition();
         case Category.Usable:
            return new UsableItemDefinition();
         case Category.CraftingIngredients:
            return new CraftingIngredientDefinition();
         case Category.Blueprint:
            return new BlueprintDefinition();
         case Category.Currency:
            return new CurrencyDefinition();
         case Category.Quest_Item:
            return new QuestItemDefinition();
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
         case Category.Weapon:
            return deserialize<WeaponDefinition>(data);
         case Category.Armor:
            return deserialize<ArmorDefinition>(data);
         case Category.Hats:
            return deserialize<HatDefinition>(data);
         case Category.Potion:
            return deserialize<PotionDefinition>(data);
         case Category.Usable:
            return deserialize<UsableItemDefinition>(data);
         case Category.CraftingIngredients:
            return deserialize<CraftingIngredientDefinition>(data);
         case Category.Blueprint:
            return deserialize<BlueprintDefinition>(data);
         case Category.Currency:
            return deserialize<CurrencyDefinition>(data);
         case Category.Quest_Item:
            return deserialize<QuestItemDefinition>(data);
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

   #region Private Variables

   #endregion
}
