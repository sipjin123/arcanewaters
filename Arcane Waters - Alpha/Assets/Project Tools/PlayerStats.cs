using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

[Serializable]
public class PlayerStats
{
   public UserDefaultStats userDefaultStats = new UserDefaultStats();

   public UserCombatStats userCombatStats = new UserCombatStats();

   public UserShipStats userShipStats = new UserShipStats();

   public UserSpecialStats userSpecialStats = new UserSpecialStats();
}

[Serializable]
public class UserDefaultStats
{
   // Base modifiers
   public float hpPerLevel = 1;
   public float armorPerLevel = 1;
   public float apPerLevel = 1;
   public float bonusAP;
   public float bonusMaxHP;
   public float bonusArmor;
   public float bonusATK;
   public float bonusATKPerLevel;
   
   // Stat modifiers
   public float bonusINT;
   public float bonusSTR;
   public float bonusSPT;
   public float bonusLUK;
   public float bonusPRE;
   public float bonusVIT;

   // Stat modifiers per Level
   public float bonusINTperLevel;
   public float bonusSTRperLevel;
   public float bonusSPTperLevel;
   public float bonusLUKperLevel;
   public float bonusPREperLevel;
   public float bonusVITperLevel;
}

[Serializable]
public class UserShipStats
{
   // Ship Stats
   public float bonusShipDamage;
   public float bonusShipDamagePerLevel;

   public float bonusShipHealth;
   public float bonusShipHealthPerLevel;

   public float bonusShipSpeed;
   public float bonusShipSpeedPerLevel;

   public float bonusShipRange;
   public float bonusShipRangePerLevel;

   public float bonusShipSailors;
   public float bonusShipSailorsPerLevel;

   public float bonusShipCargoRoom;
   public float bonusShipCargoRoomPerLevel;

   public float bonusShipSupplyRoom;
   public float bonusShipSupplyRoomPerLevel;
}

[Serializable]
public class UserCombatStats
{
   // Defense modifiers
   public float bonusResistancePhys = 1;
   public float bonusResistanceFire = 1;
   public float bonusResistanceWater = 1;
   public float bonusResistanceAir = 1;
   public float bonusResistanceEarth = 1;
   public float bonusResistanceAll = 1;

   // Damage modifiers
   public float bonusDamagePhys = 1;
   public float bonusDamageFire = 1;
   public float bonusDamageWater = 1;
   public float bonusDamageAir = 1;
   public float bonusDamageEarth = 1;
   public float bonusDamageAll = 1;

   // Defense modifiers per level
   public float bonusResistancePhysPerLevel ;
   public float bonusResistanceFirePerLevel;
   public float bonusResistanceWaterPerLevel;
   public float bonusResistanceAirPerLevel;
   public float bonusResistanceEarthPerLevel;
   public float bonusResistanceAllPerLevel;

   // Damage modifiers per level
   public float bonusDamagePhysicalPerLevel;
   public float bonusDamageFirePerLevel;
   public float bonusDamageWaterPerLevel;
   public float bonusDamageAirPerLevel;
   public float bonusDamageEarthPerLevel;
   public float bonusDamageAllPerLevel;
}

[Serializable]
public class UserSpecialStats
{
   // Asks for discount from shops
   public float shopDiscountMultiplier = 1;

   // Added value to sellable items
   public float shopSellBargainMultiplier = 1;

   // Additional npc friendship rewards
   public float friendshipRewardBonus = 1;

   // Reduces cost of ships
   public float shipDiscount = 1;

   // Adds healing item and skill multiplier
   public float healMultiplier = 1;

   // Gains extra ores by chance
   public float miningBonusChance = 1;

   // Gains extra loots by chance
   public float extraLootChance = 1;

   // Adds crafting success rate
   public float craftSuccessChance = 1;

   // Adds crafting stat boost
   public float craftQualityMultiplier = 1;
}