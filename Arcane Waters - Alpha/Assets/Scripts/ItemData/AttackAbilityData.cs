using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AttackAbilityData : BasicAbilityData
{
   #region Public Variables

   #endregion

   // Builder scriptable object instance builder
   public static AttackAbilityData CreateInstance (AttackAbilityData datacopy) {
      // If a new value needs to be added to the abilitydata class, it needs to be included in here!
      AttackAbilityData data = CreateInstance<AttackAbilityData>();

      // Basic battle item data
      /*data.setName(datacopy.getName());
      data.setDescription(datacopy.getDescription());
      data.setItemIcon(datacopy.getItemIcon());
      data.setItemID(datacopy.getItemID());
      data.setLevelRequirement(datacopy.getLevelRequirement());

      data.setItemElement(datacopy.getElementType());

      data.setHitAudioClip(datacopy.getHitAudioClip());
      data.setHitParticle(datacopy.getHitParticle());

      data.setBattleItemType(datacopy.getBattleItemType());*/

      // Sets base ability data
      data.setBaseAbilityData(datacopy);

      // Sets base battle item data
      data.setBaseBattleItemData(datacopy);

      // Ability Data
      /*data.setAbilityCost(datacopy.getAbilityCost());
      data.setBlockStatus(datacopy.getBlockStatus());

      data.setCastParticle(datacopy.getCastParticle());
      data.setCastAudioClip(datacopy.getCastAudioClip());

      data.setAllowedStances(datacopy.getAllowedStances());
      data.setClassRequirement(datacopy.getClassRequirement());

      data.setAbilityType(datacopy.getAbilityType());
      data.setAbilityCooldown(datacopy.getCooldown());

      data.setKnockup(datacopy.hasKnockup());
      data.setShake(datacopy.hasShake());
      data.setApChange(datacopy.getApChange());*/

      // Sets attack ability item properties
      data.setKnockup(datacopy.hasKnockup());
      data.setShake(datacopy.hasShake());
      data.setDamage(datacopy.getBaseDamage());
      data.setBlockStatus(datacopy.getBlockStatus());
      data.setAbilityActionType(datacopy.getAbilityActionType());

      return data;
   }

   // Builder for the Item creation window
   public static AttackAbilityData CreateInstance (BasicAbilityData basicAbilityData, bool _hasKnockup, int _damage, bool _hasShake,
      AbilityActionType abilityActionType, bool _canBeBlocked) {

      AttackAbilityData data = CreateInstance<AttackAbilityData>();

      // Sets base ability data
      data.setBaseAbilityData(basicAbilityData);
      data.setBaseBattleItemData(basicAbilityData);

      // Sets attack ability item properties
      data.setKnockup(_hasKnockup);
      data.setShake(_hasShake);
      data.setDamage(_damage);
      data.setBlockStatus(_canBeBlocked);
      data.setAbilityActionType(abilityActionType);

      return data;
   }

   // Setters
   protected void setKnockup (bool value) { _hasKnockup = value; }
   protected void setShake (bool value) { _hasShake = value; }
   protected void setDamage (int value) { _baseDamage = value; }
   protected void setBlockStatus (bool value) { _canBeBlocked = value; }
   protected void setAbilityActionType (AbilityActionType value) { _abilityActionType = value; }

   // Getters
   public bool hasKnockup () { return _hasKnockup; }
   public bool hasShake () { return _hasShake; }
   public int getBaseDamage () { return _baseDamage; }
   public bool getBlockStatus () { return _canBeBlocked; }
   public AbilityActionType getAbilityActionType () { return _abilityActionType; }

   #region Custom Helper Methods

   public void playCastClipAtTarget (Vector3 targetPosition) {
      SoundManager.playClipOneShotAtPoint(getCastAudioClip(), targetPosition);
   }

   public void playHitClipAtTarget (Vector3 targetPosition) {
      SoundManager.playClipOneShotAtPoint(getHitAudioClip(), targetPosition);
   }

   public bool isReadyForUseBy (Battler sourceBattler) {
      // Return true if we have enough AP and the cooldown is completed.
      return (sourceBattler.AP >= getAbilityCost()) && (Util.netTime() >= sourceBattler.cooldownEndTime);
   }

   // No damage increase or decrease by default
   public float getModifier { get { return 1.0f; } }

   public float getTotalAnimLength (Battler attacker, Battler target) {

      float shakeLength = 0;
      float knockupLength = 0;

      switch (getAbilityActionType()) {
         case AbilityActionType.Melee:
            // The animation length depends on the distance between attacker and target
            float jumpDuration = getJumpDuration(attacker, target);

            // Add up the amount of time it takes to animate an entire melee action
            return jumpDuration + Battler.PAUSE_LENGTH + attacker.getPreContactLength() +
                Battler.POST_CONTACT_LENGTH + jumpDuration + Battler.PAUSE_LENGTH;

         case AbilityActionType.Ranged:
            shakeLength = hasShake() ? Battler.SHAKE_LENGTH : 0f;
            knockupLength = hasKnockup() ? Battler.KNOCKUP_LENGTH : 0f;

            // Add up the amount of time it takes to animate an entire action
            return attacker.getPreMagicLength(this) + shakeLength + knockupLength + getPreDamageLength + getPostDamageLength;

         case AbilityActionType.Projectile:
            shakeLength = hasShake() ? Battler.SHAKE_LENGTH : 0f;
            knockupLength = hasKnockup() ? Battler.KNOCKUP_LENGTH : 0f;

            // Add up the amount of time it takes to animate an entire action
            return attacker.getPreMagicLength(this) + shakeLength + knockupLength + getPreDamageLength + getPostDamageLength;

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
      return getAbilityType().Equals(AbilityActionType.Melee);
   }

   public bool isHeal () {
      return getElementType().Equals(Element.Heal);
   }

   public bool isProjectile () {
      return getAbilityType().Equals(AbilityActionType.Projectile);
   }

   public bool isCancel () {
      return getAbilityType().Equals(AbilityActionType.Cancel);
   }

   private float getPreDamageLength { get { return 0.4f; } }
   private float getPostDamageLength { get { return 0.4f; } }

   #endregion

   #region Private Variables

   [SerializeField] private int _baseDamage;

   [SerializeField] private bool _hasShake;
   [SerializeField] private bool _hasKnockup;
   [SerializeField] private AbilityActionType _abilityActionType;

   // Can this ability be blocked?
   [SerializeField] private bool _canBeBlocked;

   #endregion
}

public enum AbilityActionType
{
   UNDEFINED = 0,
   Melee = 1,            // Close quarters ability.
   Ranged = 2,           // Ranged ability
   Projectile = 3,       // Created just in case, replace if ranged can do anything projectile does.
   Cancel = 4,
   StanceChange = 5
}
