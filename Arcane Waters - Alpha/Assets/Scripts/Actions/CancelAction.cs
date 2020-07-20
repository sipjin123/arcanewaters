using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class CancelAction : BattleAction {
   #region Public Variables

   // The amount of time to subtract
   public float timeToSubtract = 0f;

   #endregion

   public CancelAction () {

   }

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

   public string serialize () {
      string serialized = "";

      serialized += "CancelAction" + ",";
      serialized += this.battleId + ",";
      serialized += this.sourceId + ",";
      serialized += targetId + ",";
      serialized += actionEndTime + ",";
      serialized += timeToSubtract + ",";
      serialized += (int)BattleActionType.Cancel + ",";

      return serialized;
   }

   public static CancelAction deseralize (string serialized) {
      CancelAction action = new CancelAction();
      string[] stringArray = serialized.Split(',');

      action.battleId = Convert.ToInt32(stringArray[1]);
      action.sourceId = Convert.ToInt32(stringArray[2]);
      action.targetId = Convert.ToInt32(stringArray[3]);
      action.actionEndTime = Convert.ToInt32(stringArray[4]);
      action.timeToSubtract = Convert.ToInt32(stringArray[5]);
      action.battleActionType = (BattleActionType) Convert.ToInt32(stringArray[6]);

      return action;
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
