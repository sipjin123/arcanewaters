using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class StanceAction : BattleAction
{
   #region Public Variables

   // The new stance type
   public BattlerBehaviour.Stance newStance;

   #endregion

   public StanceAction () { }

   public StanceAction (int battleId, int sourceId, float actionEndTime, BattlerBehaviour.Stance newStance) {
      this.battleId = battleId;
      this.sourceId = sourceId;
      this.actionEndTime = actionEndTime;
      this.newStance = newStance;
      this.battleActionType = BattleActionType.Stance;
   }

   public override bool Equals (object rhs) {
      if (rhs is StanceAction) {
         var other = rhs as StanceAction;
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

   public string serialize () {
      string serialized = "";

      serialized += this.battleId + ",";
      serialized += this.sourceId + ",";
      serialized += this.actionEndTime + ",";
      serialized += (int)this.newStance + ",";
      serialized += (int)this.battleActionType + ",";

      return serialized;
   }

   public static StanceAction deserialize (string serialized) {
      StanceAction action = new StanceAction();
      string[] stringArray = serialized.Split(',');

      action.battleId = Convert.ToInt32(stringArray[0]);
      action.sourceId = Convert.ToInt32(stringArray[1]);
      action.actionEndTime = Convert.ToSingle(stringArray[2]);
      action.newStance = (BattlerBehaviour.Stance) Convert.ToInt32(stringArray[3]);
      action.battleActionType = (BattleActionType) Convert.ToInt32(stringArray[4]);

      return action;
   }

   #region Private Variables

   #endregion
}
