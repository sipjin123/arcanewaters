using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AdminBattleManager : MonoBehaviour
{
   #region Public Variables

   // A multiplier applied to all battler attack cooldowns
   public float attackCooldownMultiplier = 1f;

   // A multiplier applied to all battler idle animations
   public float idleAnimationSpeedMultiplier = 1f;

   // A multiplier applied to all battler jumps duration (when battlers move across the screen)
   public float jumpDurationMultiplier = 1f;

   // A multiplier applied to the battler attack duration (sword swings, gun fire, etc)
   public float attackDurationMultiplier = 1f;

   // Self
   public static AdminBattleManager self;

   #endregion

   public void Awake () {
      self = this;
   }

   #region Private Variables

   #endregion
}

