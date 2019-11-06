using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShipData
{
   // Type of ship, key value
   public Ship.Type shipType;

   // The custom name of the ship
   public string shipName;

   // The id of the ship
   public int shipID;

   // Type of skin the ship uses
   public Ship.SkinType skinType;

   // Base hp of the ship
   public int baseHealth;
   
   // Attack range of the ship
   public int baseRange;
   
   // Damage of the ship
   public int baseDamage;
   
   // Movement speed of the ship
   public int baseSpeed;
   
   // Cost of the ship in the ship yard
   public int basePrice;
   
   // Number of sailors the ship has
   public int baseSailors;
   
   // Count of cargo rooms in the ship
   public int baseCargoRoom;

   // Count of supply rooms in the ship
   public int baseSupplyRoom;

   // Type of sail the ship uses
   public Ship.SailType sailType;

   // Type of mast the ship uses
   public Ship.MastType mastType;

   // Sprite paths
   public string spritePath;
   public string rippleSpritePath;
   public string avatarIconPath;
}