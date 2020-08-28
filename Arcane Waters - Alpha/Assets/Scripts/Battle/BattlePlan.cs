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
   public BasicAbilityData ability;

   // The battlers we're going to target
   public List<Battler> targets;

   // The battlers we can potentially give buff to
   public List<Battler> targetAllies;

   #endregion

   public BattlePlan () { }

   /// <summary>
   /// An initialized ability MUST be added.
   /// </summary>
   /// <param name="target"></param>
   /// <param name="ability"></param>
   public BattlePlan (Battler target, BasicAbilityData ability) {
      this.ability = ability;

      this.targets = new List<Battler>();
      this.targets.Add(target);
   }

   /// <summary>
   /// An initialized ability MUST be added.
   /// </summary>
   /// <param name="ability"></param>
   /// <param name="targets"></param>
   public BattlePlan (BasicAbilityData ability, List<Battler> targets) {
      this.ability = ability;
      this.targets = targets;
   }

   #region Private Variables

   #endregion
}
