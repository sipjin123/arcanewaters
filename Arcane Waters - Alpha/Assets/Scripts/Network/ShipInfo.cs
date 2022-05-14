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

   // The ship ID from the web tool
   public int shipXmlId;

   // The user ID that owns this ship
   public int userId;

   // The Ship type
   public Ship.Type shipType;

   // The Ship Skintype
   public Ship.SkinType skinType;

   // The Mast type
   public Ship.MastType mastType;

   // The Sail type
   public Ship.SailType sailType;

   // The custom name of this Ship
   public string shipName;

   // The amount of supplies
   [Obsolete("This stat is removed - discussed in task 5960. TODO: remove column from DB.")]
   public int supplies;
   [Obsolete("This stat is removed - discussed in task 5960. TODO: remove column from DB.")]
   public int suppliesMax;

   // The cargo space
   public int cargoMax;

   // The current amount of health this ship has
   public int health;

   // The max amount of health this ship can have
   public int maxHealth;

   // The current amount of food this ship has
   public int food;

   // The max amount of food this ship can have
   public int maxFood = 500;

   // The amount of damage this ship does
   public float damage = 0.05f;

   // The range of fire
   public int attackRange;

   // The speed of the ship
   public int speed;

   // The number of sailors it takes to run the ship
   [Obsolete("This stat is removed - discussed in task 5960. TODO: remove column from DB.")]
   public int sailors;

   // The rarity of the ship
   [XmlElement(Namespace = "RarityType")]
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
         this.health = DataUtil.getInt(dataReader, "health");
         this.food = DataUtil.getInt(dataReader, "food");
         this.shipName = DataUtil.getString(dataReader, "shpName");
      // }

      this.shipType = (Ship.Type) DataUtil.getInt(dataReader, "shpType");
      this.skinType = (Ship.SkinType) DataUtil.getInt(dataReader, "skinType");
      this.mastType = (Ship.MastType) DataUtil.getInt(dataReader, "mastType");
      this.sailType = (Ship.SailType) DataUtil.getInt(dataReader, "sailType");
      this.cargoMax = DataUtil.getInt(dataReader, "cargoMax");
      this.maxHealth = DataUtil.getInt(dataReader, "maxHealth");
      this.maxFood = DataUtil.getInt(dataReader, "maxFood");
      this.damage = DataUtil.getFloat(dataReader, "damage");
      this.attackRange = DataUtil.getInt(dataReader, "attackRange");
      this.speed = DataUtil.getInt(dataReader, "speed");
      this.rarity = (Rarity.Type) DataUtil.getInt(dataReader, "rarity");

      // Colors
      this.palette1 = DataUtil.getString(dataReader, "palette1");
      this.palette2 = DataUtil.getString(dataReader, "palette1");
      this.sailPalette1 = DataUtil.getString(dataReader, "sailPalette1");
      this.sailPalette2 = DataUtil.getString(dataReader, "sailPalette2");

      this.shipAbilityXML = DataUtil.getString(dataReader, "shipAbilities");
      this.shipXmlId = DataUtil.getInt(dataReader, "shipXmlId");

      if (this.shipAbilityXML.Length > 0) {
         ShipAbilityInfo shipAbility = Util.xmlLoad<ShipAbilityInfo>(this.shipAbilityXML);
         this.shipAbilities = shipAbility;
      }
   }

   #endif

   public ShipInfo (int shipId, int userId, Ship.Type shipType, int shipXmlId, Ship.SkinType skinType, Ship.MastType mastType, Ship.SailType sailType, string shipName,
      string palette1, string palette2, string sailPalette1, string sailPalette2, int cargoMax, int health, int maxHealth, int food, int maxFood, float damage,
      int attackRange, int speed, Rarity.Type rarity, ShipAbilityInfo shipAbilities) {
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
      this.cargoMax = cargoMax;
      this.health = health;
      this.maxHealth = maxHealth;
      this.food = food;
      this.maxFood = maxFood;
      this.damage = damage;
      this.attackRange = attackRange;
      this.speed = speed;
      this.rarity = rarity;
      this.shipAbilities = shipAbilities;
      this.shipXmlId = shipXmlId;
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

[Serializable]
public class ShipAbilityInfo
{
   // The default attack ability of the ship
   public const int DEFAULT_ABILITY = 1;

   // The starting abilities for ships
   public static List<int> STARTING_ABILITIES = new List<int> { 1, 33, 34, 35, 36 };

   // Holds the collection of ability names
   public int[] ShipAbilities = new int[0];

   public ShipAbilityInfo () { }
   public ShipAbilityInfo (bool autoGenerate) {
      if (autoGenerate) {
         List<int> newAbilities = new List<int>();
         int[] randomizedAbilities = ShipAbilityManager.getRandomAbilities(2).ToArray();

         // Make sure that the default ability is part of the skill list
         newAbilities.Add(DEFAULT_ABILITY);

         // Assign the randomized abilities
         foreach (int abilityId in randomizedAbilities) {
            newAbilities.Add(abilityId);
         }

         ShipAbilities = newAbilities.ToArray();
      }
   }
}