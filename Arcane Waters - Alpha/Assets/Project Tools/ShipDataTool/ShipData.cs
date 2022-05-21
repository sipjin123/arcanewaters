using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Xml.Serialization;

public enum ShipSize {
   None = 0,
   Small = 1,
   Medium = 2,
   Large = 3
}

[Serializable]
public class ShipSizeSpritePair {
   // The ship size
   public ShipSize shipSize;

   // The speed boost sprites
   public Sprite speedBoostSpriteFront, speedBoostSpriteBack;

   // The boost circle sprites
   public Sprite boostCircleOutline, boostCircleFill;
}

[Serializable]
public class ShipData
{
   // Type of ship, key value
   public Ship.Type shipType = Ship.Type.Type_1;

   // The custom name of the ship
   public string shipName = "GenericShip";

   // The id of the ship
   public int shipID = 0;

   // Type of skin the ship uses
   public Ship.SkinType skinType = Ship.SkinType.None;

   // Base hp of the ship
   public int baseHealth = 100;
   public int baseHealthMin = 100;
   public int baseHealthMax = 300;

   // Food of the ship
   public int baseFood = 300;
   public int baseFoodMin = 300;
   public int baseFoodMax = 500;

   // Attack range of the ship
   public int baseRange = 100;
   public int baseRangeMin = 80;
   public int baseRangeMax = 100;

   // Ship Size, the wakes will depend on this variable
   public ShipSize shipSize = ShipSize.None;

   // Damage of the ship
   public int baseDamage = 10;
   public float baseDamageModifierMin = .1f;// 10% damage
   public float baseDamageModifierMax = .5f;// 50% damage

   // Movement speed of the ship
   public int baseSpeed = 90;
   public int baseSpeedMin = 50;
   public int baseSpeedMax = 150;

   // Cost of the ship in the ship yard
   public int basePrice = 5000;
   
   // Number of sailors the ship has
   [Obsolete("This stat is removed - discussed in task 5960")]
   public int baseSailors = 10;
   [Obsolete("This stat is removed - discussed in task 5960")]
   public int baseSailorsMin = 1;
   [Obsolete("This stat is removed - discussed in task 5960")]
   public int baseSailorsMax = 10;

   // Count of cargo rooms in the ship
   public int baseCargoRoom = 18;
   public int baseCargoRoomMin = 1;
   public int baseCargoRoomMax = 18;

   // Count of supply rooms in the ship
   [Obsolete("This stat is removed - discussed in task 5960")]
   public int baseSupplyRoom = 10;
   [Obsolete("This stat is removed - discussed in task 5960")]
   public int baseSupplyRoomMin = 1;
   [Obsolete("This stat is removed - discussed in task 5960")]
   public int baseSupplyRoomMax = 10;

   // Type of sail the ship uses
   public Ship.SailType sailType = Ship.SailType.Type_1;

   // Type of mast the ship uses
   public Ship.MastType mastType = Ship.MastType.Type_1;

   // Sprite paths
   public string spritePath;
   public string rippleSpritePath;
   public string avatarIconPath;

   // Determines if skill is randomized
   public bool isSkillRandom;

   // The player level requirement to use this ship
   public int shipLevelRequirement;

   // List of ship ability names
   public List<ShipAbilityPair> shipAbilities = new List<ShipAbilityPair>();
}

[Serializable]
public class ShipAbilityPair {
   // Name of the ability
   public string abilityName;

   // Id of the ability
   public int abilityId;

   // The ship data
   [XmlIgnore]
   public ShipAbilityData shipAbilityData;
}

[Serializable]
public class PvpShipIconPair {
   // Ship type
   public Ship.Type shipType;

   // The sprite counterpart
   public Sprite shipSprite;
}