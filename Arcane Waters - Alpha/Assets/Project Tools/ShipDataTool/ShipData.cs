using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class ShipData
{
   // Type of ship, key value
   public Ship.Type shipType = Ship.Type.Barge;

   // The custom name of the ship
   public string shipName = "GenericShip";

   // The id of the ship
   public int shipID = 0;

   // Type of skin the ship uses
   public Ship.SkinType skinType;

   // Base hp of the ship
   public int baseHealth = 100;
   
   // Attack range of the ship
   public int baseRange = 100;
   
   // Damage of the ship
   public int baseDamage = 10;
   
   // Movement speed of the ship
   public int baseSpeed = 90;
   
   // Cost of the ship in the ship yard
   public int basePrice = 5000;
   
   // Number of sailors the ship has
   public int baseSailors = 10;
   
   // Count of cargo rooms in the ship
   public int baseCargoRoom = 18;

   // Count of supply rooms in the ship
   public int baseSupplyRoom = 10;

   // Type of sail the ship uses
   public Ship.SailType sailType = Ship.SailType.Caravel_1;

   // Type of mast the ship uses
   public Ship.MastType mastType = Ship.MastType.Caravel_1;

   // Sprite paths
   public string spritePath;
   public string rippleSpritePath;
   public string avatarIconPath;
}