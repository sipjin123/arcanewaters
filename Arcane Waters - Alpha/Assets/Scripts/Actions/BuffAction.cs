using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class BuffAction : BattleAction {
   #region Public Variables

   // The time at which the buff starts
   public float buffStartTime;

   // The time at which the buff ends
   public float buffEndTime;

   #endregion

   public BuffAction () { }

   public BuffAction (int battleId, Ability.Type type, int sourceId, int targetId, float buffStartTime, float buffEndTime,
           float cooldownDuration, float actionEndTime, int sourceApChange, int targetApChange) {
      this.battleId = battleId;
      this.abilityType = type;
      this.sourceId = sourceId;
      this.targetId = targetId;
      this.buffStartTime = buffStartTime;
      this.buffEndTime = buffEndTime;
      this.cooldownDuration = cooldownDuration;
      this.actionEndTime = actionEndTime;
      this.sourceApChange = sourceApChange;
      this.targetApChange = targetApChange;
   }

   public override bool Equals (object rhs) {
      if (rhs is BuffAction) {
         var other = rhs as BuffAction;
         return abilityType == other.abilityType &&
             sourceId == other.sourceId && targetId == other.targetId && buffStartTime == other.buffStartTime;
      }
      return false;
   }

   public override int GetHashCode () {
      unchecked // Overflow is fine, just wrap
      {
         int hash = 17;
         hash = hash * 23 + abilityType.GetHashCode();
         hash = hash * 23 + sourceId.GetHashCode();
         hash = hash * 23 + targetId.GetHashCode();
         hash = hash * 23 + buffStartTime.GetHashCode();
         return hash;
      }
   }

   public BuffTimer getBuffTimer () {
      BuffTimer buff = new BuffTimer();
      buff.buffType = this.abilityType;
      buff.buffStartTime = this.buffStartTime;
      buff.buffEndTime = this.buffEndTime;

      return buff;
   }

   public string serialize () {
      string serialized = "";

      serialized += "BuffAction" + ",";
      serialized += this.battleId + ",";
      serialized += (int) this.abilityType + ",";
      serialized += this.sourceId + ",";
      serialized += this.targetId + ",";
      serialized += this.buffStartTime + ",";
      serialized += this.buffEndTime + ",";
      serialized += this.cooldownDuration + ",";
      serialized += this.actionEndTime + ",";
      serialized += this.sourceApChange + ",";
      serialized += this.targetApChange + ",";

      return serialized;
   }

   public static BuffAction deseralize (string serialized) {
      BuffAction action = new BuffAction();
      string[] stringArray = serialized.Split(',');

      action.battleId = Convert.ToInt32(stringArray[1]);
      action.abilityType = (Ability.Type) Convert.ToInt32(stringArray[2]);
      action.sourceId = Convert.ToInt32(stringArray[3]);
      action.targetId = Convert.ToInt32(stringArray[4]);
      action.buffStartTime = Convert.ToSingle(stringArray[5]);
      action.buffEndTime = Convert.ToSingle(stringArray[6]);
      action.cooldownDuration = Convert.ToSingle(stringArray[7]);
      action.actionEndTime = Convert.ToSingle(stringArray[8]);
      action.sourceApChange = Convert.ToInt32(stringArray[9]);
      action.targetApChange = Convert.ToInt32(stringArray[10]);

      return action;
   }

   #region Private Variables

   #endregion
}
