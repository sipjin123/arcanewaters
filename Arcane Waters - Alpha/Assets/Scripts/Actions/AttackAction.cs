using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class AttackAction: BattleAction {
    #region Public Variables

    // The type of action this is
    public enum ActionType { Melee = 1 }

    // The type of action this is
    public ActionType actionType;

    // The amount of damage done by the action
    public int damage;

    // Whether or not the attack was blocked
    public bool wasBlocked = false;

    // Whether or not the attack was a critical
    public bool wasCritical = false;

    #endregion

   public AttackAction () { }

    public AttackAction(int battleId, ActionType actionType, int sourceId, int targetId, int damage, float actionEndTime,
            Ability.Type abilityType, bool wasCritical, bool wasBlocked, float cooldownDuration, int sourceApChange, int targetApChange) {
        this.battleId = battleId;
        this.actionType = actionType;
        this.sourceId = sourceId;
        this.targetId = targetId;
        this.damage = damage;
        this.actionEndTime = actionEndTime;
        this.abilityType = abilityType;
        this.wasCritical = wasCritical;
        this.wasBlocked = wasBlocked;
        this.cooldownDuration = cooldownDuration;
        this.sourceApChange = sourceApChange;
        this.targetApChange = targetApChange;
    }

    public override bool Equals(object rhs) {
        if (rhs is AttackAction) {
            var other = rhs as AttackAction;
            return battleId == other.battleId && actionType == other.actionType &&
                sourceId == other.sourceId && targetId == other.targetId && cooldownDuration == other.cooldownDuration;
        }
        return false;
    }

    public override int GetHashCode() {
        unchecked // Overflow is fine, just wrap
        {
            int hash = 17;
            hash = hash * 23 + battleId.GetHashCode();
            hash = hash * 23 + actionType.GetHashCode();
            hash = hash * 23 + sourceId.GetHashCode();
            hash = hash * 23 + targetId.GetHashCode();
            hash = hash * 23 + cooldownDuration.GetHashCode();
            return hash;
        }
    }

   public string serialize () {
      string serialized = "";

      serialized += "AttackAction" + ",";
      serialized += this.battleId + ",";
      serialized += (int)this.actionType + ",";
      serialized += this.sourceId + ",";
      serialized += this.targetId + ",";
      serialized += this.damage + ",";
      serialized += this.actionEndTime + ",";
      serialized += (int)this.abilityType + ",";
      serialized += this.wasCritical + ",";
      serialized += this.wasBlocked + ",";
      serialized += this.cooldownDuration + ",";
      serialized += this.sourceApChange + ",";
      serialized += this.targetApChange + ",";

      return serialized;
   }

   public static AttackAction deseralize (string serialized) {
      AttackAction action = new AttackAction();
      string[] stringArray = serialized.Split(',');

      action.battleId = Convert.ToInt32(stringArray[1]);
      action.actionType = (AttackAction.ActionType) Convert.ToInt32(stringArray[2]);
      action.sourceId = Convert.ToInt32(stringArray[3]);
      action.targetId = Convert.ToInt32(stringArray[4]);
      action.damage = Convert.ToInt32(stringArray[5]);
      action.actionEndTime = Convert.ToSingle(stringArray[6]);
      action.abilityType = (Ability.Type) Convert.ToInt32(stringArray[7]);
      action.wasCritical = Convert.ToBoolean(stringArray[8]);
      action.wasBlocked = Convert.ToBoolean(stringArray[9]);
      action.cooldownDuration = Convert.ToSingle(stringArray[10]);
      action.sourceApChange = Convert.ToInt32(stringArray[11]);
      action.targetApChange = Convert.ToInt32(stringArray[12]);

      return action;
   }

   #region Private Variables

   #endregion
}
