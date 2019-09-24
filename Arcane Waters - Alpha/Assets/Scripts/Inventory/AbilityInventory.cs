using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AbilityInventory : MonoBehaviour{
   #region Public Variables

   // Temporary simulation of the current abilities equipped for the local player
   // (Do not use this in battle directly, they must be instanced first)
   // We will use these as the main abilities
   public List<AttackAbilityData> playerAttackAbilities = new List<AttackAbilityData>();
   
   // Abilities that appear when the local player is selected
   public List<BasicAbilityData> playerBuffAbilities = new List<BasicAbilityData>();

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
