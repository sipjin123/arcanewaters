using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

// ZERONEV COMMENT
/// <summary>
/// "Blueprint" for AI controlled battler actions.
/// </summary>
public class BattlePlan {
   #region Public Variables

   // The ability we're going to use
   public AbilityData ability;

   // The battlers we're going to target
   public List<Battler> targets;

   #endregion

   public BattlePlan () { }

   /// <summary>
   /// An initialized ability MUST be added.
   /// </summary>
   /// <param name="target"></param>
   /// <param name="ability"></param>
   public BattlePlan (Battler target, AbilityData ability) {
      this.ability = ability;

      this.targets = new List<Battler>();
      this.targets.Add(target);
   }

   /// <summary>
   /// An initialized ability MUST be added.
   /// </summary>
   /// <param name="ability"></param>
   /// <param name="targets"></param>
   public BattlePlan (AbilityData ability, List<Battler> targets) {
      this.ability = ability;
      this.targets = targets;
   }

   #region Private Variables

   #endregion
}
