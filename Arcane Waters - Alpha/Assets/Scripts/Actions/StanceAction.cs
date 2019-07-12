using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class StanceAction : BattleAction {
    #region Public Variables

    // The new stance type
    public Battler.Stance newStance;

    #endregion

    public StanceAction(int battleId, int sourceId, float actionEndTime, Battler.Stance newStance) {
        this.battleId = battleId;
        this.sourceId = sourceId;
        this.actionEndTime = actionEndTime;
        this.newStance = newStance;
    }

    public override bool Equals(object rhs) {
        if (rhs is StanceAction) {
            var other = rhs as StanceAction;
            return battleId == other.battleId && sourceId == other.sourceId &&
                actionEndTime == other.actionEndTime;
        }
        return false;
    }

    public override int GetHashCode() {
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
