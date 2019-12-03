using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class BattlerAttackUI : MonoBehaviour {
   #region Public Variables

   // Player Damage stat display
   public StatBreakdownTemplate physicalDmgBreakdown, fireDmgBreakdown, earthDmgBreakdown, airDmgBreakdown, waterDmgBreakdown;
   [HideInInspector] public float physicalDamageMultiplier, fireDamageMultiplier, earthDamageMultiplier, airDamageMultiplier, waterDamageMultiplier;
   [HideInInspector] public float physicalDamageMultiplierBase, fireDamageMultiplierBase, earthDamageMultiplierBase, airDamageMultiplierBase, waterDamageMultiplierBase;
   [HideInInspector] public float physicalDamageMultiplierPerLvl, fireDamageMultiplierPerLvl, earthDamageMultiplierPerLvl, airDamageMultiplierPerLvl, waterDamageMultiplierPerLvl;
   public float outputDamagePhys, outputDamageFire, outputDamageEarth, outputDamageAir, outputDamageWater;

   #endregion

   #region Private Variables

   #endregion
}
