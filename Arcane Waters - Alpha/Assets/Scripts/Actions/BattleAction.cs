using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BattleAction {
   #region Public Variables

   // The id of the Battle that this BattleAction is for
   public int battleId;

   // The userId of the action's source
   public int sourceId;

   // The userId of the action's target
   public int targetId;

   // The server timestamp for when this action was marked as ending
   public float actionEndTime;

   // The cooldown duration that this action requires
   public float cooldownDuration;

   // The ability ID that was used for the attack
   // This will be used to grab the ability index from the source battler ability inventory.
   public int abilityInventoryIndex;

   // Global ability ID, direct reference to the global ability IDs
   public int abilityGlobalID;

   // The change in the source Battler AP that occurred as a result of this action
   public int sourceApChange;

   // The change in the target Battler AP that occurred as a result of this action
   public int targetApChange;
   
   // The type of the action that will be executed, either attack, buff/debuff or stance change.
   public BattleActionType battleActionType;

   #endregion

   public BattleAction () { }

   #region Private Variables

   #endregion
}

public enum BattleActionType
{
   UNDEFINED = 0,
   Attack = 1,
   Stance = 2,
   BuffDebuff = 3,
   Cancel = 4
}
