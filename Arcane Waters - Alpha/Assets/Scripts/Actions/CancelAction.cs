using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class CancelAction : BattleAction {
   #region Public Variables

   // The amount of time to subtract
   public float timeToSubtract = 0f;

   #endregion

   public CancelAction (int battleId, int sourceId, int targetId, float actionEndTime, float timeToSubtract) {
      this.battleId = battleId;
      this.sourceId = sourceId;
      this.targetId = targetId;
      this.actionEndTime = actionEndTime;
      this.timeToSubtract = timeToSubtract;
      this.battleActionType = BattleActionType.Cancel;
   }

   public override bool Equals (object rhs) {
      if (rhs is CancelAction) {
         var other = rhs as CancelAction;
         return battleId == other.battleId && sourceId == other.sourceId &&
             actionEndTime == other.actionEndTime;
      }
      return false;
   }

   public override int GetHashCode () {
      unchecked // Overflow is fine, just wrap
      {
         int hash = 17;
         hash = hash * 23 + battleId.GetHashCode();
         hash = hash * 23 + sourceId.GetHashCode();
         hash = hash * 23 + actionEndTime.GetHashCode();
         return hash;
      }
   }

   #region Private Variables

   #endregion
}
