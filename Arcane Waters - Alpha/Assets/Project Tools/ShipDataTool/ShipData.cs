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

   // Attack range of the ship
   public int baseRange = 100;
   public int baseRangeMin = 80;
   public int baseRangeMax = 100;

   // Ship Size, the wakes will depend on this variable
   public ShipSize shipSize = ShipSize.None;

   // Damage of the ship
   public int baseDamage = 10;
   public int baseDamageModifierMin = 5;
   public int baseDamageModifierMax = 10;

   // Movement speed of the ship
   public int baseSpeed = 90;
   public int baseSpeedMin = 50;
   public int baseSpeedMax = 150;

   // Cost of the ship in the ship yard
   public int basePrice = 5000;
   
   // Number of sailors the ship has
   public int baseSailors = 10;
   public int baseSailorsMin = 1;
   public int baseSailorsMax = 10;

   // Count of cargo rooms in the ship
   public int baseCargoRoom = 18;
   public int baseCargoRoomMin = 1;
   public int baseCargoRoomMax = 18;

   // Count of supply rooms in the ship
   public int baseSupplyRoom = 10;
   public int baseSupplyRoomMin = 1;
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

   // List of ship ability names
   public List<ShipAbilityPair> shipAbilities = new List<ShipAbilityPair>();
}

[Serializable]
public class ShipAbilityPair
{
   // Name of the ability
   public string abilityName;

   // Id of the ability
   public int abilityId;

   // The ship data
   [XmlIgnore]
   public ShipAbilityData shipAbilityData;
}