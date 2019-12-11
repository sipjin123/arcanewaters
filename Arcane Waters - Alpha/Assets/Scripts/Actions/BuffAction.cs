using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

/// <summary>
/// Networked battle action.
/// </summary>
public class BuffAction : BattleAction {
   #region Public Variables

   // The time at which the buff starts
   public float buffStartTime;

   // The time at which the buff ends
   public float buffEndTime;

   // The value of the buff is there is one
   public int buffValue;

   #endregion

   public BuffAction () { }

   public BuffAction (int battleId, int abilityInventoryIndex, int sourceId, int targetId, float buffStartTime, float buffEndTime,
           float cooldownDuration, float actionEndTime, int sourceApChange, int targetApChange, int abilityGlobalID, int buffVal) {
      this.battleId = battleId;
      this.abilityInventoryIndex = abilityInventoryIndex;
      this.sourceId = sourceId;
      this.targetId = targetId;
      this.buffStartTime = buffStartTime;
      this.buffEndTime = buffEndTime;
      this.cooldownDuration = cooldownDuration;
      this.actionEndTime = actionEndTime;
      this.sourceApChange = sourceApChange;
      this.targetApChange = targetApChange;
      this.abilityGlobalID = abilityGlobalID;
      this.battleActionType = BattleActionType.BuffDebuff;
      this.buffValue = buffVal;
   }

   public override bool Equals (object rhs) {
      if (rhs is BuffAction) {
         var other = rhs as BuffAction;
         return abilityGlobalID == other.abilityGlobalID &&
             sourceId == other.sourceId && targetId == other.targetId && buffStartTime == other.buffStartTime;
      }
      return false;
   }

   public override int GetHashCode () {
      unchecked // Overflow is fine, just wrap
      {
         int hash = 17;
         hash = hash * 23 + abilityGlobalID.GetHashCode();
         hash = hash * 23 + sourceId.GetHashCode();
         hash = hash * 23 + targetId.GetHashCode();
         hash = hash * 23 + buffStartTime.GetHashCode();
         return hash;
      }
   }

   public BuffTimer getBuffTimer () {
      BuffTimer buff = new BuffTimer();
      buff.buffAbilityGlobalID = this.abilityGlobalID;
      //buff.buffType = this.abilityType;
      buff.buffStartTime = this.buffStartTime;
      buff.buffEndTime = this.buffEndTime;

      return buff;
   }

   public string serialize () {
      string serialized = "";

      serialized += "BuffAction" + ",";
      serialized += this.battleId + ",";
      serialized += (int) this.abilityInventoryIndex + ",";
      serialized += this.sourceId + ",";
      serialized += this.targetId + ",";
      serialized += this.buffStartTime + ",";
      serialized += this.buffEndTime + ",";
      serialized += this.cooldownDuration + ",";
      serialized += this.actionEndTime + ",";
      serialized += this.sourceApChange + ",";
      serialized += this.targetApChange + ",";
      serialized += this.abilityGlobalID + ",";
      serialized += (int) this.battleActionType + ",";
      serialized += this.buffValue + ",";

      return serialized;
   }

   public static BuffAction deseralize (string serialized) {
      BuffAction action = new BuffAction();
      string[] stringArray = serialized.Split(',');

      action.battleId = Convert.ToInt32(stringArray[1]);
      action.abilityInventoryIndex = Convert.ToInt32(stringArray[2]);
      action.sourceId = Convert.ToInt32(stringArray[3]);
      action.targetId = Convert.ToInt32(stringArray[4]);
      action.buffStartTime = Convert.ToSingle(stringArray[5]);
      action.buffEndTime = Convert.ToSingle(stringArray[6]);
      action.cooldownDuration = Convert.ToSingle(stringArray[7]);
      action.actionEndTime = Convert.ToSingle(stringArray[8]);
      action.sourceApChange = Convert.ToInt32(stringArray[9]);
      action.targetApChange = Convert.ToInt32(stringArray[10]);
      action.abilityGlobalID = Convert.ToInt32(stringArray[11]);
      action.battleActionType = (BattleActionType) Convert.ToInt32(stringArray[12]);
      action.buffValue = Convert.ToInt32(stringArray[13]);

      return action;
   }

   #region Private Variables

   #endregion
}
