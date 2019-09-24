using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class AutomatedBattler : Battler {
   #region Public Variables

   #endregion

   // New, gets the very first attack.
   public AttackAbilityData getBasicAttack () {

      // Safe check
      if (getAbilities.Length <= 0) {
         Debug.LogError("This battler do not have any abilities");
         return null;
      }

      return getAbilities[0];
   }

   public override BattlePlan getBattlePlan (Battle battle) {
      //Ability ability = AbilityManager.getAbility(getDefaultAttack());
      Battler target = getRandomTargetFor(getBasicAttack(), battle);

      // Set up a list of targets
      List<Battler> targets = new List<Battler>();
      if (target != null) {
         targets.Add(target);
      }

      // By default, AI battlers will use the Monster attack ability
      return new BattlePlan(getBasicAttack(), targets);
   }

   protected Battler getRandomTargetFor (AttackAbilityData abilityData, Battle battle) {
      List<Battler> options = new List<Battler>();

      // Cycle over all of the participants in the battle
      foreach (Battler targetBattler in battle.getParticipants()) {
         // Check if the battler is on the other team and not dead
         if (targetBattler.teamType != this.teamType && !targetBattler.isDead()) {
            // If it's a Melee Ability, make sure the target isn't protected
            if (abilityData.isMelee() && targetBattler.isProtected(battle)) {
               continue;
            }

            options.Add(targetBattler);
         }
      }

      // If we have at least one option, choose a random target
      if (options.Count > 0) {
         return options[Random.Range(0, options.Count)];
      }

      // There weren't any available targets still alive
      return null;
   }

   protected Battler getBattlerForBuff (Battle battle, int globalAbilityID) {
      List<Battler> options = new List<Battler>();

      // Cycle over all of the participants in the battle
      foreach (Battler targetBattler in battle.getParticipants()) {
         // Check if the battler is on our team, not dead, and doesn't already have the buff
         if (targetBattler.teamType == this.teamType && !targetBattler.isDead() && !targetBattler.hasBuffOfType(globalAbilityID)) {
            options.Add(targetBattler);
         }
      }

      // If we have at least one option, choose a random target
      if (options.Count > 0) {
         return options[Random.Range(0, options.Count)];
      }

      // There weren't any available targets for the buff
      return null;
   }

   protected Battler getBattlerForHeal (Battle battle) {
      List<Battler> options = new List<Battler>();

      // Cycle over all of the participants in the battle
      foreach (Battler targetBattler in battle.getParticipants()) {
         // Check if the battler is on our team and not dead
         if (targetBattler.teamType == this.teamType && !targetBattler.isDead() && targetBattler.health < targetBattler.getStartingHealth()) {
            options.Add(targetBattler);
         }
      }

      // If we have at least one option, choose a random target
      if (options.Count > 0) {
         return options[Random.Range(0, options.Count)];
      }

      // There weren't any available targets for the buff
      return null;
   }

   public override IEnumerator animateDeath () {
      // By default, we don't wait at all
      yield return new WaitForSeconds(0f);

      // Play the Death sound
      playDeathSound();

      // Start playing our death animation
      playAnim(Anim.Type.Death_East);
   }

   #region Private Variables

   #endregion
}
