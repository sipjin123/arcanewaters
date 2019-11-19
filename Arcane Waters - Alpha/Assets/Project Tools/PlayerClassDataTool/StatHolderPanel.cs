using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class StatHolderPanel : MonoBehaviour {
   #region Public Variables

   #endregion

   public PlayerStats getStatData () {
      PlayerStats statData = new PlayerStats();

      // Per Level stats
      statData.userDefaultStats.hpPerLevel = float.Parse(_hpPerLevel.text);
      statData.userDefaultStats.armorPerLevel = float.Parse(_armorPerLevel.text);
      statData.userDefaultStats.apPerLevel = float.Parse(_apPerLevel.text);

      // Base Stats
      statData.userDefaultStats.bonusAP = float.Parse(_addedAP.text);
      statData.userDefaultStats.bonusMaxHP = float.Parse(_addedMaxHP.text);
      statData.userDefaultStats.bonusArmor = float.Parse(_addedArmor.text);
      statData.userDefaultStats.bonusATK = float.Parse(_addedATK.text);
      statData.userDefaultStats.bonusATKPerLevel = float.Parse(_atkPerlevel.text);

      // Special Stats
      statData.userDefaultStats.bonusINT = float.Parse(_bonusINT.text);
      statData.userDefaultStats.bonusVIT = float.Parse(_bonusVIT.text);
      statData.userDefaultStats.bonusPRE = float.Parse(_bonusPRE.text);
      statData.userDefaultStats.bonusSPT = float.Parse(_bonusSPT.text);
      statData.userDefaultStats.bonusLUK = float.Parse(_bonusLUK.text);
      statData.userDefaultStats.bonusSTR = float.Parse(_bonusSTR.text);

      // Special Stats per level
      statData.userDefaultStats.bonusINTperLevel = float.Parse(_bonusINTperLevel.text);
      statData.userDefaultStats.bonusVITperLevel = float.Parse(_bonusVITperLevel.text);
      statData.userDefaultStats.bonusPREperLevel = float.Parse(_bonusPREperLevel.text);
      statData.userDefaultStats.bonusSPTperLevel = float.Parse(_bonusSPTperLevel.text);
      statData.userDefaultStats.bonusLUKperLevel = float.Parse(_bonusLUKperLevel.text);
      statData.userDefaultStats.bonusSTRperLevel = float.Parse(_bonusSTRperLevel.text);

      // Defenses Stats
      statData.userCombatStats.bonusResistancePhys = float.Parse(_bonusDEFPhys.text);
      statData.userCombatStats.bonusResistanceFire = float.Parse(_bonusDEFFire.text);
      statData.userCombatStats.bonusResistanceEarth = float.Parse(_bonusDEFEarth.text);
      statData.userCombatStats.bonusResistanceWind = float.Parse(_bonusDEFWind.text);
      statData.userCombatStats.bonusResistanceWater = float.Parse(_bonusDEFWater.text);
      statData.userCombatStats.bonusResistanceAll = float.Parse(_bonusDEFAll.text);

      // Defenses Stats per level
      statData.userCombatStats.bonusResistancePhysPerLevel = float.Parse(_bonusDEFPhysPerLevel.text);
      statData.userCombatStats.bonusResistanceFirePerLevel = float.Parse(_bonusDEFFirePerLevel.text);
      statData.userCombatStats.bonusResistanceEarthPerLevel = float.Parse(_bonusDEFEarthPerLevel.text);
      statData.userCombatStats.bonusResistanceWindPerLevel = float.Parse(_bonusDEFWindPerLevel.text);
      statData.userCombatStats.bonusResistanceWaterPerLevel = float.Parse(_bonusDEFWaterPerLevel.text);
      statData.userCombatStats.bonusResistanceAllPerLevel = float.Parse(_bonusDEFAllPerLevel.text);

      // Damage Stats
      statData.userCombatStats.bonusDamagePhys = float.Parse(_bonusATKPhys.text);
      statData.userCombatStats.bonusDamageFire = float.Parse(_bonusATKFire.text);
      statData.userCombatStats.bonusDamageEarth = float.Parse(_bonusATKEarth.text);
      statData.userCombatStats.bonusDamageWind = float.Parse(_bonusATKWind.text);
      statData.userCombatStats.bonusDamageWater = float.Parse(_bonusATKWater.text);
      statData.userCombatStats.bonusDamageAll = float.Parse(_bonusATKAll.text);

      // Damage Stats per level
      statData.userCombatStats.bonusDamagePhysPerLevel = float.Parse(_bonusATKPhysPerLevel.text);
      statData.userCombatStats.bonusDamageFirePerLevel = float.Parse(_bonusATKFirePerLevel.text);
      statData.userCombatStats.bonusDamageEarthPerLevel = float.Parse(_bonusATKEarthPerLevel.text);
      statData.userCombatStats.bonusDamageWindPerLevel = float.Parse(_bonusATKWindPerLevel.text);
      statData.userCombatStats.bonusDamageWaterPerLevel = float.Parse(_bonusATKWaterPerLevel.text);
      statData.userCombatStats.bonusDamageAllPerLevel = float.Parse(_bonusATKAllPerLevel.text);

      // Ship Data
      statData.userShipStats.bonusShipDamage = float.Parse(_damageBase.text);
      statData.userShipStats.bonusShipDamagePerLevel = float.Parse(_damagePerLevel.text);
      statData.userShipStats.bonusShipHealth = float.Parse(_healthBase.text);
      statData.userShipStats.bonusShipHealthPerLevel = float.Parse(_healthPerLevel.text);

      statData.userShipStats.bonusShipRange = float.Parse(_rangeBase.text);
      statData.userShipStats.bonusShipRangePerLevel = float.Parse(_rangePerLevel.text);
      statData.userShipStats.bonusShipSpeed = float.Parse(_speedBase.text);
      statData.userShipStats.bonusShipSpeedPerLevel = float.Parse(_speedPerLevel.text);

      statData.userShipStats.bonusShipSailors = float.Parse(_sailorBase.text);
      statData.userShipStats.bonusShipSailorsPerLevel = float.Parse(_sailorPerLevel.text);
      statData.userShipStats.bonusShipCargoRoom = float.Parse(_cargoRoomBase.text);
      statData.userShipStats.bonusShipCargoRoomPerLevel = float.Parse(_cargoRoomPerLevel.text);
      statData.userShipStats.bonusShipSupplyRoom = float.Parse(_supplyRoomBase.text);
      statData.userShipStats.bonusShipSupplyRoomPerLevel = float.Parse(_supplyRoomPerLevel.text);

      return statData;
   }

   public void loadStatData (PlayerStats classData) {
      _hpPerLevel.text = classData.userDefaultStats.hpPerLevel.ToString();
      _armorPerLevel.text = classData.userDefaultStats.armorPerLevel.ToString();
      _apPerLevel.text = classData.userDefaultStats.apPerLevel.ToString();

      _addedAP.text = classData.userDefaultStats.bonusAP.ToString();
      _addedMaxHP.text = classData.userDefaultStats.bonusMaxHP.ToString();
      _addedArmor.text = classData.userDefaultStats.bonusArmor.ToString();
      _addedATK.text = classData.userDefaultStats.bonusATK.ToString();
      _atkPerlevel.text = classData.userDefaultStats.bonusATKPerLevel.ToString();

      // Base Stats
      _bonusINT.text = classData.userDefaultStats.bonusINT.ToString();
      _bonusVIT.text = classData.userDefaultStats.bonusVIT.ToString();
      _bonusPRE.text = classData.userDefaultStats.bonusPRE.ToString();
      _bonusSPT.text = classData.userDefaultStats.bonusSPT.ToString();
      _bonusLUK.text = classData.userDefaultStats.bonusLUK.ToString();
      _bonusSTR.text = classData.userDefaultStats.bonusSTR.ToString();

      // Stats per level
      _bonusINTperLevel.text = classData.userDefaultStats.bonusINTperLevel.ToString();
      _bonusVITperLevel.text = classData.userDefaultStats.bonusVITperLevel.ToString();
      _bonusPREperLevel.text = classData.userDefaultStats.bonusPREperLevel.ToString();
      _bonusSPTperLevel.text = classData.userDefaultStats.bonusSPTperLevel.ToString();
      _bonusLUKperLevel.text = classData.userDefaultStats.bonusLUKperLevel.ToString();
      _bonusSTRperLevel.text = classData.userDefaultStats.bonusSTRperLevel.ToString();

      // Defense Stats base
      _bonusDEFPhys.text = classData.userCombatStats.bonusResistancePhys.ToString();
      _bonusDEFFire.text = classData.userCombatStats.bonusResistanceFire.ToString();
      _bonusDEFEarth.text = classData.userCombatStats.bonusResistanceEarth.ToString();
      _bonusDEFWind.text = classData.userCombatStats.bonusResistanceWind.ToString();
      _bonusDEFWater.text = classData.userCombatStats.bonusResistanceWater.ToString();
      _bonusDEFAll.text = classData.userCombatStats.bonusResistanceAll.ToString();

      // Attack Stats base
      _bonusATKPhys.text = classData.userCombatStats.bonusDamagePhys.ToString();
      _bonusATKFire.text = classData.userCombatStats.bonusDamageFire.ToString();
      _bonusATKEarth.text = classData.userCombatStats.bonusDamageEarth.ToString();
      _bonusATKWind.text = classData.userCombatStats.bonusDamageWind.ToString();
      _bonusATKWater.text = classData.userCombatStats.bonusDamageWater.ToString();
      _bonusATKAll.text = classData.userCombatStats.bonusDamageAll.ToString();

      // Defense Stats per level
      _bonusDEFPhysPerLevel.text = classData.userCombatStats.bonusResistancePhysPerLevel.ToString();
      _bonusDEFFirePerLevel.text = classData.userCombatStats.bonusResistanceFirePerLevel.ToString();
      _bonusDEFEarthPerLevel.text = classData.userCombatStats.bonusResistanceEarthPerLevel.ToString();
      _bonusDEFWindPerLevel.text = classData.userCombatStats.bonusResistanceWindPerLevel.ToString();
      _bonusDEFWaterPerLevel.text = classData.userCombatStats.bonusResistanceWaterPerLevel.ToString();
      _bonusDEFAllPerLevel.text = classData.userCombatStats.bonusResistanceAllPerLevel.ToString();

      // Attack Stats per level
      _bonusATKPhysPerLevel.text = classData.userCombatStats.bonusDamagePhysPerLevel.ToString();
      _bonusATKFirePerLevel.text = classData.userCombatStats.bonusDamageFirePerLevel.ToString();
      _bonusATKEarthPerLevel.text = classData.userCombatStats.bonusDamageEarthPerLevel.ToString();
      _bonusATKWindPerLevel.text = classData.userCombatStats.bonusDamageWindPerLevel.ToString();
      _bonusATKWaterPerLevel.text = classData.userCombatStats.bonusDamageWaterPerLevel.ToString();
      _bonusATKAllPerLevel.text = classData.userCombatStats.bonusDamageAllPerLevel.ToString();

      // Ship Stats
      _damageBase.text = classData.userShipStats.bonusShipDamage.ToString();
      _damagePerLevel.text = classData.userShipStats.bonusShipDamagePerLevel.ToString();
      _healthBase.text = classData.userShipStats.bonusShipHealth.ToString();
      _healthPerLevel.text = classData.userShipStats.bonusShipHealthPerLevel.ToString();

      _speedBase.text = classData.userShipStats.bonusShipSpeed.ToString();
      _speedPerLevel.text = classData.userShipStats.bonusShipSpeedPerLevel.ToString();
      _rangeBase.text = classData.userShipStats.bonusShipRange.ToString();
      _rangePerLevel.text = classData.userShipStats.bonusShipRangePerLevel.ToString();

      _sailorBase.text = classData.userShipStats.bonusShipSailors.ToString();
      _sailorPerLevel.text = classData.userShipStats.bonusShipSailorsPerLevel.ToString();
      _cargoRoomBase.text = classData.userShipStats.bonusShipCargoRoom.ToString();
      _cargoRoomPerLevel.text = classData.userShipStats.bonusShipCargoRoomPerLevel.ToString();
      _supplyRoomBase.text = classData.userShipStats.bonusShipSupplyRoom.ToString();
      _supplyRoomPerLevel.text = classData.userShipStats.bonusShipSupplyRoomPerLevel.ToString();
   }

   #region Private Variables
#pragma warning disable 0649
   // Default stats
   [SerializeField]
   private InputField _hpPerLevel, _armorPerLevel, _apPerLevel, _atkPerlevel,
      _addedAP,
      _addedMaxHP,
      _addedArmor,
      _addedATK;

   // Attack stats
   [SerializeField]
   private InputField _bonusATKPhys,
      _bonusATKFire,
      _bonusATKEarth,
      _bonusATKWind,
      _bonusATKWater,
      _bonusATKAll;

   // Attack stats per level
   [SerializeField]
   private InputField _bonusATKPhysPerLevel,
      _bonusATKFirePerLevel,
      _bonusATKEarthPerLevel,
      _bonusATKWindPerLevel,
      _bonusATKWaterPerLevel,
      _bonusATKAllPerLevel;

   // Defensive stats
   [SerializeField]
   private InputField _bonusDEFPhys,
      _bonusDEFFire,
      _bonusDEFEarth,
      _bonusDEFWind,
      _bonusDEFWater,
      _bonusDEFAll;

   // Defensive stats per level
   [SerializeField]
   private InputField _bonusDEFPhysPerLevel,
      _bonusDEFFirePerLevel,
      _bonusDEFEarthPerLevel,
      _bonusDEFWindPerLevel,
      _bonusDEFWaterPerLevel,
      _bonusDEFAllPerLevel;

   // Player stats
   [SerializeField]
   private InputField _bonusINT,
      _bonusVIT,
      _bonusPRE,
      _bonusSPT,
      _bonusLUK,
      _bonusSTR;

   // Player stats per Level
   [SerializeField]
   private InputField _bonusINTperLevel,
      _bonusVITperLevel,
      _bonusPREperLevel,
      _bonusSPTperLevel,
      _bonusLUKperLevel,
      _bonusSTRperLevel;

   // Player Ship stats 
   [SerializeField]
   private InputField _damageBase,
      _damagePerLevel,
      _healthBase,
      _healthPerLevel,
      _speedBase,
      _speedPerLevel,
      _rangeBase,
      _rangePerLevel,
      _sailorBase,
      _sailorPerLevel,
      _cargoRoomBase,
      _cargoRoomPerLevel,
      _supplyRoomBase,
      _supplyRoomPerLevel;
#pragma warning restore 0649 
   #endregion
}
