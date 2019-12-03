using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class BattlerDefenseUI : MonoBehaviour {
   #region Public Variables

   // Enemy Defense stat display
   public StatBreakdownTemplate physicalDefBreakdown, fireDefBreakdown, earthDefBreakdown, airDefBreakdown, waterDefBreakdown;
   [HideInInspector] public float physicalDefenseMultiplier, fireDefenseMultiplier, earthDefenseMultiplier, airDefenseMultiplier, waterDefenseMultiplier;
   [HideInInspector] public float physicalDefenseMultiplierBase, fireDefenseMultiplierBase, earthDefenseMultiplierBase, airDefenseMultiplierBase, waterDefenseMultiplierBase;
   [HideInInspector] public float physicalDefenseMultiplierPerLvl, fireDefenseMultiplierPerLvl, earthDefenseMultiplierPerLvl, airDefenseMultiplierPerLvl, waterDefenseMultiplierPerLvl;
   public float outputDefensePhys, outputDefenseFire, outputDefenseEarth, outputDefenseAir, outputDefenseWater;

   #endregion

   #region Private Variables

   #endregion
}
