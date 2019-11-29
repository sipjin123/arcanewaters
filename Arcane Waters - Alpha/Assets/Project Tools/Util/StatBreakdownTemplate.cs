using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class StatBreakdownTemplate : MonoBehaviour {
   #region Public Variables

   // Holds the overall damage
   public Text totalDamage;

   // Holds the overall damage multiplier
   public Text totalMultiplier;

   // Text values of the template as base
   public Text[] damageText;

   // Text values of the template per level
   public Text[] damagePerLevelText;

   #endregion
}
