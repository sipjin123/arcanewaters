using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AbilityInventory : MonoBehaviour{
   #region Public Variables

   // Temporary simulation of the current abilities equipped for the local player.
   // (Do not use this in battle directly, they must be instanced first.)
   // We will use these as the main blueprints.
   public List<AbilityData> equippedAbilitiesBPs = new List<AbilityData>();

   // Instance
   public static AbilityInventory self;

   #endregion

   private void Awake () {
      self = this;
   }

   // Scriptable objects must not be used directly from the asset.
   // They need to be instanced as they own, and they use that data before trying to access their data.
   public void initializeAbilities () {
      for (int i = 0; i < equippedAbilitiesBPs.Count; i++) {
         AbilityData instancedAbility = AbilityData.CreateInstance(equippedAbilitiesBPs[i]);
         _inBattleAbilities.Add(instancedAbility);
      }
   }

   // Whenever we want to have access to the local player equipped abilities, we will use this getter method.
   public List<AbilityData> equippedAbilities { get { return _inBattleAbilities; } }

   #region Private Variables

   // Instanced abilities that will be used inside the battle.
   private List<AbilityData> _inBattleAbilities = new List<AbilityData>();

   #endregion
}
