using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Xml.Serialization;

[Serializable]
public class EquipmentStatData
{
   // Name of the item
   public string equipmentName = "Undefined";

   // Description of the item
   public string equipmentDescription;

   // The id of the item
   public int sqlId = 0;

   // The icon path of the item
   public string equipmentIconPath = "";

   // The price of the item in the store
   public int equipmentPrice;

   // Determines if this item can be trashed
   public bool canBeTrashed;

   // Rarity value modifiers
   public RarityModifier[] rarityModifiers;

   // Elemental value modifiers
   public ElementModifier[] elementModifiers;

   // Name of palettes
   public string palettes = "";

   // The bonus stats earned for wearing the item
   public Stats statsData = new Stats();

   // Determines if all colors should be set
   public bool setAllColors = false;

   // The current rarity of the equipment data
   public Rarity.Type rarity = Rarity.Type.Common;

}

public class RarityModifier
{
   // The rarity type
   [XmlElement(Namespace = "RarityType")]
   public Rarity.Type rarityType;

   // The value multiplier
   public float multiplier;
}

public class ElementModifier
{
   // The element of the item
   [XmlElement(Namespace = "Element")]
   public Element elementType;

   // The damage multiplier
   public float multiplier;
}
