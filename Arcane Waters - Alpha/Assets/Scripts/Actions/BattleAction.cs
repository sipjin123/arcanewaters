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
    public Ability.Type abilityType;

    // The change in the source Battler AP that occurred as a result of this action
    public int sourceApChange;

    // The change in the target Battler AP that occurred as a result of this action
    public int targetApChange;

    #endregion

    public BattleAction() { }

    #region Private Variables

    #endregion
}
