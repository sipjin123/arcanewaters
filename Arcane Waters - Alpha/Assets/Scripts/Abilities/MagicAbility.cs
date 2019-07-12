using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public abstract class MagicAbility : AttackAbility {
   #region Public Variables

   #endregion

   public override float getTotalAnimLength (Battler attacker, Battler target) {
      float shakeLength = hasShake() ? Battler.SHAKE_LENGTH : 0f;
      float knockupLength = hasKnockup() ? Battler.KNOCKUP_LENGTH : 0f;

      // Add up the amount of time it takes to animate an entire action
      return attacker.getPreMagicLength(this) + shakeLength + knockupLength + getPreDamageLength() + getPostDamageLength();
   }

   public virtual float getAnimationSpeed () {
      // The default framerate is 10 FPS, so twice that is 20 FPS or .05 seconds per frame
      return 2f;
   }

   public virtual float getPreDamageLength () {
      // The amount of time a magic effect animates for before damage is applied (.4 seconds = 8 frames, at defaults)
      return .4f;
   }

   public virtual float getPostDamageLength () {
      // The amount of time a magic effect continues animating after damage is applied (.4 seconds = 8 frames, at defaults)
      return .4f;
   }

   public virtual float getPositionOffset () {
      // Default vertical offset for placing the magic effect
      return 0f;
   }

   public virtual void playMagicCastEffect (Battler sourceBattler) {
      // Play the magic cast effect at the feet of the source sprite
      // EffectManager.playMagicCastEffect(sourceBattler);
   }

   public virtual void applyAction (Battler sourceBattler, Battler targetBattler, BattleAction battleAction) {
      AttackAction action = (AttackAction) battleAction;

      // Show the damage text and play a damage sound
      targetBattler.showDamageText(action);

      // If the action was blocked, animate that
      if (action.wasBlocked) {
         targetBattler.StartCoroutine(targetBattler.animateBlock(sourceBattler));

      } else if (!isHeal()) {
         // Play an appropriate attack animation effect
         Vector2 effectPosition = targetBattler.getMagicGroundPosition() + new Vector2(0f, .20f);
         EffectManager.playAttackEffect(sourceBattler, targetBattler, action, effectPosition);

         // Make the target sprite display it's "Hit" animation
         targetBattler.StartCoroutine(targetBattler.animateHit(sourceBattler, action));
      }

      // If the target is owned by the player, play a camera shake
      if (Util.isPlayer(targetBattler.userId) && !isHeal()) {
         BattleCamera.self.shakeCamera(.25f);
      }

      targetBattler.displayedHealth -= action.damage;
      targetBattler.displayedHealth = Util.clamp<int>(targetBattler.displayedHealth, 0, targetBattler.getStartingHealth());
   }

   public override IEnumerator display (float timeToWait, BattleAction action, bool isFirstAction) {
      // Look up our needed references
      Ability ability = AbilityManager.getAbility(action.abilityType);
      Battle battle = BattleManager.self.getBattle(action.battleId);
      Battler sourceBattler = battle.getBattler(action.sourceId);
      Battler targetBattler = battle.getBattler(action.targetId);

      // Don't start animating until both sprites are available
      yield return new WaitForSeconds(timeToWait);

      // Make sure the battlers are still alive at this point
      /*if (sourceBattler.isDead() || targetBattler.isDead()) {
         yield break;
      }

      // Start the attack animation that will eventually create the magic effect
      if (isFirstAction) {
         sourceBattler.playAnim(ability.getAnimation());

         // Play the magic cast effect at the feet of the source sprite
         playMagicCastEffect(sourceBattler);

         // Play any sounds that go along with the ability being cast
         playCastSound(sourceBattler, targetBattler);
      }

      // Wait the appropriate amount of time before creating the magic effect
      yield return new WaitForSeconds(sourceBattler.getPreMagicLength(ability));

      // Play the sound associated with the magic efect
      playMagicSound(sourceBattler, targetBattler);

      // Create a Magic Effect instance on top of our targets, if we have an animation
      if (hasEffectAnimation()) {
         GameObject magicPrefab = Resources.Load<GameObject>("Magic/Spell");
         GameObject magicInstance = (GameObject) GameObject.Instantiate(magicPrefab);
         magicInstance.transform.SetParent(battle.battleBoard.transform, false);

         // Use the correct sprite sheet
         AnimatedSprite magicAnim = magicInstance.GetComponent<AnimatedSprite>();
         magicAnim.sheet = Resources.Load<SpriteSheet>("Sheets/Magic/" + type);
         magicAnim.PlayAnimation(magicAnim.startAnimation, 0f, getAnimationSpeed());

         // Flip the local scale if we're the attacker
         if (sourceBattler.isAttacker()) {
            magicInstance.transform.localScale = new Vector3(-1, 1, 1);
         }

         // Figure out where the magic effect should be at
         Vector2 targetPos = targetBattler.getMagicGroundPosition();
         magicInstance.transform.position = new Vector3(targetPos.x, targetPos.y + getPositionOffset(), magicInstance.transform.position.z);
      }

      // If this magic ability has knockup, then start it now
      if (hasKnockup()) {
         targetBattler.StartCoroutine(targetBattler.animateKnockup());
         yield return new WaitForSeconds(Battler.KNOCKUP_LENGTH);
      } else if (hasShake()) {
         targetBattler.StartCoroutine(targetBattler.animateShake());
         yield return new WaitForSeconds(Battler.SHAKE_LENGTH);
      }

      // Wait until the animation gets to the point that it deals damage
      yield return new WaitForSeconds(getPreDamageLength());

      applyAction(sourceBattler, targetBattler, action);

      // Now wait the specified amount of time before switching back to our battle stance
      yield return new WaitForSeconds(Battler.POST_CONTACT_LENGTH);

      if (isFirstAction) {
         // Switch back to our battle stance
         sourceBattler.playAnim(AnimUtil.BATTLE_EAST);

         // Add any AP we earned
         sourceBattler.addAP(action.sourceApChange);
      }

      // Add any AP the target earned
      targetBattler.addAP(action.targetApChange);

      // If the target died, animate that death now
      if (targetBattler.isDead()) {
         targetBattler.StartCoroutine(targetBattler.animateDeath());
      }*/
   }

   #region Private Variables

   #endregion
}
