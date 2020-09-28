using UnityEngine;
using System.Xml.Serialization;
using System;

[Serializable]
[XmlRoot("Item")]
public class EquipmentDefinition : ItemDefinition {
   #region Public Variables

   // The price of the item in the store
   public int price;

   // Determines if this item can be trashed
   public bool canBeTrashed;

   // Determines if all colors should be set
   public bool setAllColors = false;

   // The path to the main texture of the equipment that is used in game
   public string mainTexturePath = "";

   // Rarity value modifiers
   public RarityModifier[] rarityModifiers = new RarityModifier[0];

   // Elemental value modifiers
   public ElementModifier[] elementModifiers = new ElementModifier[0];

   // The bonus stats earned for wearing the item
   public Stats statsData = new Stats();

   // The current rarity of the equipment data
   public Rarity.Type rarity = Rarity.Type.Common;

   #endregion

   #region Private Variables

   #endregion
}
