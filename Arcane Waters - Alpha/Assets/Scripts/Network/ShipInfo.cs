using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Xml.Serialization;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

public class ShipInfo {
   #region Public Variables

   // The ship ID from the database
   public int shipId;

   // The user ID that owns this ship
   public int userId;

   // The Ship type
   public Ship.Type shipType;

   // The Ship Skintype
   public Ship.SkinType skinType;

   // The Nation type
   public Nation.Type nationType;

   // The Mast type
   public Ship.MastType mastType;

   // The Sail type
   public Ship.SailType sailType;

   // The custom name of this Ship
   public string shipName;

   // The amount of supplies
   public int supplies;
   public int suppliesMax;

   // The cargo space
   public int cargoMax;

   // The current amount of health this ship has
   public int health;

   // The max amount of health this ship can have
   public int maxHealth;

   // The amount of damage this ship does
   public int damage;

   // The range of fire
   public int attackRange;

   // The speed of the ship
   public int speed;

   // The number of sailors it takes to run the ship
   public int sailors;

   // The rarity of the ship
   public Rarity.Type rarity;

   // The price of the ship, only valid for shops
   public int price;

   // Whether the ship has been sold, only valid for shops
   public bool hasSold;

   // XML Data of ship abilities
   public string shipAbilityXML;

   // Ship Abilities
   public ShipAbilityInfo shipAbilities = new ShipAbilityInfo();

   // Colors
   public string palette1;
   public string palette2;
   public string sailPalette1;
   public string sailPalette2;

   #endregion

   public ShipInfo () { }

   #if IS_SERVER_BUILD

   public ShipInfo (MySqlDataReader dataReader) {
      // We ignore some values for the Shipyard version
      // if (!(this is Shop_ShipInfo)) {
         this.shipId = DataUtil.getInt(dataReader, "shpId");
         this.userId = DataUtil.getInt(dataReader, "usrId");
         this.nationType = (Nation.Type) DataUtil.getInt(dataReader, "natType");
         this.health = DataUtil.getInt(dataReader, "health");
         this.shipName = DataUtil.getString(dataReader, "shpName");
      // }

      this.shipType = (Ship.Type) DataUtil.getInt(dataReader, "shpType");
      this.skinType = (Ship.SkinType) DataUtil.getInt(dataReader, "skinType");
      this.mastType = (Ship.MastType) DataUtil.getInt(dataReader, "mastType");
      this.sailType = (Ship.SailType) DataUtil.getInt(dataReader, "sailType");
      this.supplies = DataUtil.getInt(dataReader, "supplies");
      this.suppliesMax = DataUtil.getInt(dataReader, "suppliesMax");
      this.cargoMax = DataUtil.getInt(dataReader, "cargoMax");
      this.maxHealth = DataUtil.getInt(dataReader, "maxHealth");
      this.damage = DataUtil.getInt(dataReader, "damage");
      this.attackRange = DataUtil.getInt(dataReader, "attackRange");
      this.speed = DataUtil.getInt(dataReader, "speed");
      this.sailors = DataUtil.getInt(dataReader, "sailors");
      this.rarity = (Rarity.Type) DataUtil.getInt(dataReader, "rarity");

      // Colors
      this.palette1 = DataUtil.getString(dataReader, "palette1");
      this.palette2 = DataUtil.getString(dataReader, "palette1");
      this.sailPalette1 = DataUtil.getString(dataReader, "sailPalette1");
      this.sailPalette2 = DataUtil.getString(dataReader, "sailPalette2");

      this.shipAbilityXML = DataUtil.getString(dataReader, "shipAbilities");

      if (this.shipAbilityXML.Length > 0) {
         ShipAbilityInfo shipAbility = Util.xmlLoad<ShipAbilityInfo>(this.shipAbilityXML);
         this.shipAbilities = shipAbility;
      }
   }

   #endif

   public ShipInfo (int shipId, int userId, Ship.Type shipType,Ship.SkinType skinType, Ship.MastType mastType, Ship.SailType sailType, string shipName,
      string palette1, string palette2, string sailPalette1, string sailPalette2, int supplies, int suppliesMax, int cargoMax, int health, int maxHealth, int damage,
      int attackRange, int speed, int sailors, Rarity.Type rarity, ShipAbilityInfo shipAbilities) {
      this.shipId = shipId;
      this.userId = userId;
      this.shipType = shipType;
      this.skinType = skinType;
      this.mastType = mastType;
      this.sailType = sailType;
      this.shipName = shipName;
      this.palette1 = palette1;
      this.palette2 = palette2;
      this.sailPalette1 = sailPalette1;
      this.sailPalette2 = sailPalette2;
      this.supplies = supplies;
      this.suppliesMax = suppliesMax;
      this.cargoMax = cargoMax;
      this.health = health;
      this.maxHealth = maxHealth;
      this.damage = damage;
      this.attackRange = attackRange;
      this.speed = speed;
      this.sailors = sailors;
      this.rarity = rarity;
      this.shipAbilities = shipAbilities;
   }

   public override bool Equals (object rhs) {
      if (rhs is ShipInfo) {
         var other = rhs as ShipInfo;
         return shipId == other.shipId;
      }
      return false;
   }

   public override int GetHashCode () {
      return shipId.GetHashCode();
   }

   #region Private Variables

   #endregion
}

public class ShipAbilityInfo
{
   // The default attack ability of the ship
   public const int DEFAULT_ABILITY = 1;

   // Holds the collection of ability names
   public int[] ShipAbilities = new int[0];

   public ShipAbilityInfo () { }
   public ShipAbilityInfo (bool autoGenerate) {
      if (autoGenerate) {
         ShipAbilities = ShipAbilityManager.getRandomAbilities(3).ToArray();
      }
   }
}