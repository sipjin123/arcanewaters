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
   public List<BasicAbilityData> equippedAbilitiesBPs = new List<BasicAbilityData>();

   // Temporary variables, until we fully unify the ability data to handle stances too.
   public BasicAbilityData balancedStance;
   public BasicAbilityData defenseStance;
   public BasicAbilityData offenseStance;

   // Instance
   public static AbilityInventory self;

   #endregion

   private void Awake () {
      self = this;
   }

   #region Private Variables

   #endregion
}
