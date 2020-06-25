using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

[Serializable]
public class AttackAbilityData : BasicAbilityData
{
   #region Public Variables

   // Holds the base damage
   public int baseDamage;

   // Does the cam shake upon casting?
   public bool hasShake;

   // Has knockup?
   public bool hasKnockup;

   // Has knockBack?
   public bool hasKnockBack;

   // Determines the action type
   public AbilityActionType abilityActionType;

   // Can this ability be blocked?
   public bool canBeBlocked;

   // The projectile speed
   public float projectileSpeed = 3;

   // The path location of the projectile sprite
   public string projectileSpritePath = null;

   // The projectile scale
   public float projectileScale = 1;


   #endregion

   public AttackAbilityData () { }

   // Builder scriptable object instance builder
   public static AttackAbilityData CreateInstance (AttackAbilityData datacopy) {
      // If a new value needs to be added to the abilitydata class, it needs to be included in here!
      AttackAbilityData data = new AttackAbilityData();

      // Sets base battle item data
      data.setBaseBattleItemData(datacopy);

      // Sets base ability data
      data.setBaseAbilityData(datacopy);

      // Sets attack ability item properties
      data.hasKnockup = datacopy.hasKnockup;
      data.hasShake = datacopy.hasShake;
      data.baseDamage = datacopy.baseDamage;
      data.canBeBlocked = datacopy.canBeBlocked;
      data.hasKnockBack = datacopy.hasKnockBack;
      data.abilityActionType = datacopy.abilityActionType;
      data.projectileSpeed = datacopy.projectileSpeed;
      data.projectileSpritePath = datacopy.projectileSpritePath;
      data.projectileScale = datacopy.projectileScale;

      return data;
   }

   // Builder for the Item creation window
   public static AttackAbilityData CreateInstance (BasicAbilityData basicAbilityData, bool _hasKnockup, int _damage, bool _hasShake,
      AbilityActionType _abilityActionType, bool _canBeBlocked, bool _hasKnockBack, float _projectileSpeed, string _projectileSpritePath, float _projectileScale) {

      AttackAbilityData data = new AttackAbilityData();

      // Sets base ability data
      data.setBaseAbilityData(basicAbilityData);
      data.setBaseBattleItemData(basicAbilityData);

      // Sets attack ability item properties
      data.hasKnockup = _hasKnockup;
      data.hasShake = _hasShake;
      data.baseDamage = _damage;
      data.canBeBlocked = _canBeBlocked;
      data.hasKnockBack = _hasKnockBack;
      data.abilityActionType = _abilityActionType;
      data.projectileSpeed = _projectileSpeed;
      data.projectileSpritePath = _projectileSpritePath;
      data.projectileScale = _projectileScale;

      return data;
   }

   #region Custom Helper Methods

   public void playCastClipAtTarget (Vector3 targetPosition) {
      if (SoundEffectManager.self.isValidSoundEffect(castSoundEffectId)) {
         SoundEffectManager.self.playSoundEffect(castSoundEffectId);
      } else {
         AudioClip castClip = AudioClipManager.self.getAudioClipData(AudioClipManager.self.defaultCastAudio).audioClip;
         SoundManager.playClipAtPoint(castClip, targetPosition);
      }
   }

   public void playHitClipAtTarget (Vector3 targetPosition) {
      if (SoundEffectManager.self.isValidSoundEffect(hitSoundEffectId)) {
         SoundEffectManager.self.playSoundEffect(hitSoundEffectId);
      } else {
         AudioClip hitclip = AudioClipManager.self.getAudioClipData(AudioClipManager.self.defaultHitAudio).audioClip;
         SoundManager.playClipAtPoint(hitclip, targetPosition);
      }
   }

   // No damage increase or decrease by default
   public float getModifier { get { return 1.0f; } }

   public float getTotalAnimLength (Battler attacker, Battler target) {
      float shakeLength = 0;
      float knockupLength = 0;

      switch (abilityActionType) {
         case AbilityActionType.Melee:
            // The animation length depends on the distance between attacker and target
            float jumpDuration = getJumpDuration(attacker, target);

            // Add up the amount of time it takes to animate an entire melee action
            return jumpDuration + Battler.PAUSE_LENGTH + attacker.getPreContactLength() +
                Battler.POST_CONTACT_LENGTH + jumpDuration + Battler.PAUSE_LENGTH;

         case AbilityActionType.Ranged:
            shakeLength = hasShake ? Battler.SHAKE_LENGTH : 0f;
            knockupLength = hasKnockup ? Battler.KNOCKUP_LENGTH : 0f;

            // Add up the amount of time it takes to animate an entire action
            return attacker.getPreMagicLength() + shakeLength + knockupLength + getPreDamageLength + getPostDamageLength + Battler.AIM_DURATION + Battler.PRE_AIM_DELAY + Battler.POST_SHOOT_DELAY + Battler.PRE_SHOOT_DELAY;

         case AbilityActionType.Projectile:
            shakeLength = hasShake ? Battler.SHAKE_LENGTH : 0f;
            knockupLength = hasKnockup ? Battler.KNOCKUP_LENGTH : 0f;

            // Add up the amount of time it takes to animate an entire action
            return attacker.getPreMagicLength() + shakeLength + knockupLength + getPreDamageLength + getPostDamageLength;

         case AbilityActionType.CastToTarget:
            shakeLength = hasShake ? Battler.SHAKE_LENGTH : 0f;
            knockupLength = hasKnockup ? Battler.KNOCKUP_LENGTH : 0f;

            // Add up the amount of time it takes to animate an entire action
            return attacker.getPreMagicLength() + shakeLength + knockupLength + getPreDamageLength + getPostDamageLength + Battler.POST_CAST_DELAY + Battler.PRE_CAST_DELAY;

         default:
            Debug.LogWarning("Ability type is not defined for getting anim length");
            return 0;
      }
   }

   public float getJumpDuration (Battler source, Battler target) {
      // The animation length depends on the distance between attacker and target
      float distance = Battle.getDistance(source, target);
      float jumpDuration = distance * Battler.JUMP_LENGTH;

      return jumpDuration;
   }

   // By default, we'll use the Attack animation when the ability is activated
   public Anim.Type getAnimation () {
      return Anim.Type.Attack_East;
   }

   public bool isMelee () {
      return abilityType.Equals(AbilityActionType.Melee);
   }

   public bool isHeal () {
      return abilityType.Equals(Element.Heal);
   }

   public bool isProjectile () {
      return abilityType.Equals(AbilityActionType.Projectile);
   }

   public bool isCancel () {
      return abilityType.Equals(AbilityActionType.Cancel);
   }

   public float getPreDamageLength { get { return 0.4f; } }
   public float getPostDamageLength { get { return 0.4f; } }

   #endregion
}

public enum AbilityActionType
{
   UNDEFINED = 0,
   Melee = 1,            // Close quarters ability
   Ranged = 2,           // Ranged ability
   Projectile = 3,       // Created just in case, replace if ranged can do anything projectile does
   Cancel = 4,
   StanceChange = 5,
   CastToTarget = 6
}
