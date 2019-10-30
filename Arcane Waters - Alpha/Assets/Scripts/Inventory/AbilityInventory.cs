using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AbilityInventory : MonoBehaviour{
   #region Public Variables

   public HashSet<BasicAbilityData> playerAbilities = new HashSet<BasicAbilityData>();
  
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

   public void addPlayerAbility(BasicAbilityData ability) {
      playerAbilities.Add(ability);
   }

   public void addNewAbilities (BasicAbilityData[] abilities) {
      foreach (BasicAbilityData ability in abilities) {
         playerAbilities.Add(ability);
      }
   }

   #region Private Variables

   #endregion
}
