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
   public float hpPerLevel;
   public float armorPerLevel;
   public float apPerLevel;
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
   public float bonusResistancePhys;
   public float bonusResistanceFire;
   public float bonusResistanceWater;
   public float bonusResistanceWind;
   public float bonusResistanceEarth;
   public float bonusResistanceAll;

   // Damage modifiers
   public float bonusDamagePhys;
   public float bonusDamageFire;
   public float bonusDamageWater;
   public float bonusDamageWind;
   public float bonusDamageEarth;
   public float bonusDamageAll;

   // Defense modifiers per level
   public float bonusResistancePhysPerLevel;
   public float bonusResistanceFirePerLevel;
   public float bonusResistanceWaterPerLevel;
   public float bonusResistanceWindPerLevel;
   public float bonusResistanceEarthPerLevel;
   public float bonusResistanceAllPerLevel;

   // Damage modifiers per level
   public float bonusDamagePhysPerLevel;
   public float bonusDamageFirePerLevel;
   public float bonusDamageWaterPerLevel;
   public float bonusDamageWindPerLevel;
   public float bonusDamageEarthPerLevel;
   public float bonusDamageAllPerLevel;
}