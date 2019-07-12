using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class BattlePlan {
   #region Public Variables

   // The ability we're going to use
   public Ability ability;

   // The battlers we're going to target
   public List<Battler> targets;

   #endregion

   public BattlePlan () { }

   public BattlePlan (Battler target, Ability ability) {
      this.ability = ability;

      this.targets = new List<Battler>();
      this.targets.Add(target);
   }

   public BattlePlan (Ability ability, List<Battler> targets) {
      this.ability = ability;
      this.targets = targets;
   }

   #region Private Variables

   #endregion
}
