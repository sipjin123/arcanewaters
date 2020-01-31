using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class MonsterAbilityManager : MonoBehaviour {
   #region Public Variables

   // Ability data list
   public List<BasicAbilityData> basicAbilityList;
   public List<AttackAbilityData> attackAbilityList;
   public List<BuffAbilityData> buffAbilityList;

   #endregion

   public void updateWithAbilityData (Dictionary<int, BasicAbilityData> basicAbilityData, Dictionary<int, AttackAbilityData> attackData, Dictionary<int, BuffAbilityData> buffData) {
      basicAbilityList = new List<BasicAbilityData>();
      attackAbilityList = new List<AttackAbilityData>();
      buffAbilityList = new List<BuffAbilityData>();

      foreach (KeyValuePair<int, BasicAbilityData> basicAbility in basicAbilityData) {
         basicAbilityList.Add(basicAbility.Value);
      }
      foreach (KeyValuePair<int, AttackAbilityData> attackAbility in attackData) {
         attackAbilityList.Add(attackAbility.Value);
      }
      foreach (KeyValuePair<int, BuffAbilityData> buffAbility in buffData) {
         buffAbilityList.Add(buffAbility.Value);
      }
   }

   #region Private Variables
      
   #endregion
}
