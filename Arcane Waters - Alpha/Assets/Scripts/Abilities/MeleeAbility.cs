using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public abstract class MeleeAbility : AttackAbility {
   #region Public Variables

   #endregion

   public override Effect.Type getEffectType (Weapon.Type weaponType) {
      switch (weaponType) {
         case Weapon.Type.Pitchfork:
         case Weapon.Type.Sword_Steel:
         case Weapon.Type.Lance_Steel:
         case Weapon.Type.Sword_Rune:
         case Weapon.Type.Sword_1:
         case Weapon.Type.Sword_2:
         case Weapon.Type.Sword_3:
         case Weapon.Type.Sword_4:
         case Weapon.Type.Sword_5:
         case Weapon.Type.Sword_6:
         case Weapon.Type.Sword_7:
         case Weapon.Type.Sword_8:
            return Effect.Type.Slash_Physical;
         case Weapon.Type.Staff_Mage:
         case Weapon.Type.Mace_Steel:
         case Weapon.Type.Mace_Star:
         case Weapon.Type.None:
            return Effect.Type.Blunt_Physical;
         default:
            D.warning("No attack effect defined for weapon: " + weaponType);
            return Effect.Type.Blunt_Physical;
      }
   }

   public override float getTotalAnimLength (Battler attacker, Battler target) {
      // The animation length depends on the distance between attacker and target
      float jumpDuration = getJumpDuration(attacker, target);

      // Add up the amount of time it takes to animate an entire melee action
      return jumpDuration + Battler.PAUSE_LENGTH + attacker.getPreContactLength() +
          Battler.POST_CONTACT_LENGTH + jumpDuration + Battler.PAUSE_LENGTH;
   }

   protected float getJumpDuration (Battler source, Battler target) {
      // The animation length depends on the distance between attacker and target
      float distance = Battle.getDistance(source, target);
      float jumpDuration = distance * Battler.JUMP_LENGTH;

      return jumpDuration;
   }

   public override IEnumerator display (float timeToWait, BattleAction battleAction, bool isFirst) {
      // Default all abilities to display as attacks
      if (!(battleAction is AttackAction)) {
         D.warning("Ability doesn't know how to handle action: " + battleAction + ", ability: " + this);
         yield break;
      }

      // Cast version of the Attack Action
      AttackAction action = (AttackAction) battleAction;

      // Look up our needed references
      Ability ability = AbilityManager.getAbility(action.abilityType);
      Battle battle = BattleManager.self.getBattle(action.battleId);
      Battler sourceBattler = battle.getBattler(action.sourceId);
      Battler targetBattler = battle.getBattler(action.targetId);
      Vector2 startPos = sourceBattler.battleSpot.transform.position;
      float jumpDuration = getJumpDuration(sourceBattler, targetBattler);

      // Don't start animating until both sprites are available
      yield return new WaitForSeconds(timeToWait);

      // Make sure the source battler is still alive at this point
      if (sourceBattler.isDead()) {
         yield break;
      }

      // Mark the source battler as jumping
      sourceBattler.isJumping = true;

      // Play an appropriate jump sound
      sourceBattler.playJumpSound();

      // Smoothly jump into position
      sourceBattler.playAnim(Anim.Type.Jump_East);
      Vector2 targetPosition = targetBattler.getMeleeStandPosition();
      float startTime = Time.time;
      while (Time.time - startTime < jumpDuration) {
         float timePassed = Time.time - startTime;
         Vector2 newPos = Vector2.Lerp(startPos, targetPosition, (timePassed / jumpDuration));
         sourceBattler.transform.position = new Vector3(newPos.x, newPos.y, sourceBattler.transform.position.z);
         yield return 0;
      }

      // Make sure we're exactly in position now that the jump is over
      sourceBattler.transform.position = new Vector3(targetPosition.x, targetPosition.y, sourceBattler.transform.position.z);

      // Pause for a moment after reaching our destination
      yield return new WaitForSeconds(Battler.PAUSE_LENGTH);
      
      sourceBattler.playAnim(ability.getAnimation());

      // Play any sounds that go along with the ability being cast
      playCastSound(sourceBattler, targetBattler);

      // Apply the damage at the correct time in the swing animation
      yield return new WaitForSeconds(sourceBattler.getPreContactLength());

      // Note that the contact is happening right now
      targetBattler.showDamageText(action);

      // Play an impact sound appropriate for the ability
      playImpactSound(sourceBattler, targetBattler);

      // Move the sprite back and forward to simulate knockback
      targetBattler.StartCoroutine(targetBattler.animateKnockback());

      // If the action was blocked, animate that
      if (action.wasBlocked) {
         targetBattler.StartCoroutine(targetBattler.animateBlock(sourceBattler));

      } else {
         // Play an appropriate attack animation effect
         Vector2 effectPosition = targetBattler.getMagicGroundPosition() + new Vector2(0f, .25f);
         EffectManager.playAttackEffect(sourceBattler, targetBattler, action, effectPosition);

         // Make the target sprite display its "Hit" animation
         targetBattler.StartCoroutine(targetBattler.animateHit(sourceBattler, action));
      }

      // If either sprite is owned by the client, play a camera shake
      if (Util.isPlayer(sourceBattler.userId) || Util.isPlayer(targetBattler.userId)) {
         BattleCamera.self.shakeCamera(.25f);
      }

      targetBattler.displayedHealth -= action.damage;
      targetBattler.displayedHealth = Util.clamp<int>(targetBattler.displayedHealth, 0, targetBattler.getStartingHealth());
      yield return new WaitForSeconds(Battler.POST_CONTACT_LENGTH);

      // Now jump back to where we started from
      sourceBattler.playAnim(Anim.Type.Jump_East);
      startTime = Time.time;
      while (Time.time - startTime < jumpDuration) {
         float timePassed = Time.time - startTime;
         Vector2 newPos = Vector2.Lerp(targetBattler.getMeleeStandPosition(), startPos, (timePassed / jumpDuration));
         sourceBattler.transform.position = new Vector3(newPos.x, newPos.y, sourceBattler.transform.position.z);
         yield return 0;
      }

      // Make sure we're exactly in position now that the jump is over
      sourceBattler.transform.position = new Vector3(startPos.x, startPos.y, sourceBattler.transform.position.z);

      // Wait for a moment after we reach our jump destination
      yield return new WaitForSeconds(Battler.PAUSE_LENGTH);

      // Switch back to our battle stance
      sourceBattler.playAnim(Anim.Type.Battle_East);

      // Mark the source sprite as no longer jumping
      sourceBattler.isJumping = false;

      // Add any AP we earned
      sourceBattler.displayedAP = Util.clamp<int>(sourceBattler.displayedAP + action.sourceApChange, 0, Battler.MAX_AP);
      targetBattler.displayedAP = Util.clamp<int>(targetBattler.displayedAP + action.targetApChange, 0, Battler.MAX_AP);

      // If the target died, animate that death now
      if (targetBattler.displayedHealth <= 0) {
         targetBattler.StartCoroutine(targetBattler.animateDeath());
      }
   }

   #region Private Variables

   #endregion
}