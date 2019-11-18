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
}

[Serializable]
public class UserDefaultStats
{
   // Base modifiers
   public int hpPerLevel;
   public int armorPerLevel;
   public int apPerLevel;
   public int bonusAP;
   public int bonusMaxHP;
   public int bonusArmor;
   public int bonusATK;
   public int bonusATKPerLevel;

   // Stat modifiers
   public int bonusINT;
   public int bonusSTR;
   public int bonusSPT;
   public int bonusLUK;
   public int bonusPRE;
   public int bonusVIT;

   // Stat modifiers per Level
   public int bonusINTperLevel;
   public int bonusSTRperLevel;
   public int bonusSPTperLevel;
   public int bonusLUKperLevel;
   public int bonusPREperLevel;
   public int bonusVITperLevel;
}

[Serializable]
public class UserShipStats
{
   // Ship Stats
   public int bonusShipDamage;
   public int bonusShipDamagePerLevel;

   public int bonusShipHealth;
   public int bonusShipHealthPerLevel;

   public int bonusShipSpeed;
   public int bonusShipSpeedPerLevel;

   public int bonusShipRange;
   public int bonusShipRangePerLevel;

   public int bonusShipSailors;
   public int bonusShipSailorsPerLevel;

   public int bonusShipCargoRoom;
   public int bonusShipCargoRoomPerLevel;

   public int bonusShipSupplyRoom;
   public int bonusShipSupplyRoomPerLevel;
}

[Serializable]
public class UserCombatStats
{
   // Defense modifiers
   public int bonusResistancePhys;
   public int bonusResistanceFire;
   public int bonusResistanceWater;
   public int bonusResistanceWind;
   public int bonusResistanceEarth;
   public int bonusResistanceAll;

   // Damage modifiers
   public int bonusDamagePhys;
   public int bonusDamageFire;
   public int bonusDamageWater;
   public int bonusDamageWind;
   public int bonusDamageEarth;
   public int bonusDamageAll;

   // Defense modifiers per level
   public int bonusResistancePhysPerLevel;
   public int bonusResistanceFirePerLevel;
   public int bonusResistanceWaterPerLevel;
   public int bonusResistanceWindPerLevel;
   public int bonusResistanceEarthPerLevel;
   public int bonusResistanceAllPerLevel;

   // Damage modifiers per level
   public int bonusDamagePhysPerLevel;
   public int bonusDamageFirePerLevel;
   public int bonusDamageWaterPerLevel;
   public int bonusDamageWindPerLevel;
   public int bonusDamageEarthPerLevel;
   public int bonusDamageAllPerLevel;
}