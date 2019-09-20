using UnityEngine;
using UnityEngine.Events;

// Custom battle tooltip event, will be executed whenever we want to trigger an event related to a BattleItemData.
public class BattleTooltipEvent : UnityEvent<BasicAbilityData> {
}
