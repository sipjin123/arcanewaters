using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AbilityInventory : MonoBehaviour{
   #region Public Variables

   public List<BasicAbilityData> playerAbilities = new List<BasicAbilityData>();

   [Space(8)]

   public BasicAbilityData balancedStance;
   public BasicAbilityData offenseStance;
   public BasicAbilityData defenseStance;

   // Instance
   public static AbilityInventory self;

   #endregion

   private void Awake () {
      self = this;
   }

   #region Private Variables

   #endregion
}
