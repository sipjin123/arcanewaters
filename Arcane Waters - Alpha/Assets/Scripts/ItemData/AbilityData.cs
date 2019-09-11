using System.Collections;
using UnityEngine;

// Chris Palacios

[System.Serializable]
public class AbilityData : BattleItemData
{
   // Builder scriptable object instance builder.
   public static AbilityData CreateInstance (AbilityData datacopy) {
      // If a new value needs to be added to the abilitydata class, it needs to be included in here!
      AbilityData data = CreateInstance<AbilityData>();

      // Basic battle item data.

      data.setName(datacopy.getName());
      data.setDescription(datacopy.getDescription());
      data.setItemIcon(datacopy.getItemIcon());
      data.setItemID(datacopy.getItemID());
      data.setLevelRequirement(datacopy.getLevelRequirement());

      data.setItemDamage(datacopy.getBaseDamage());
      data.setItemElement(datacopy.getElementType());

      data.setHitAudioClip(datacopy.getHitAudioClip());
      data.setHitParticle(datacopy.getHitParticle());

      data.setBattleItemType(datacopy.getBattleItemType());

      // Ability Data.
      data.setAbilityCost(datacopy.getAbilityCost());
      data.setBlockStatus(datacopy.getBlockStatus());

      data.setCastParticle(datacopy.getCastParticle());
      data.setCastAudioClip(datacopy.getCastAudioClip());

      data.setAllowedStances(datacopy.getAllowedStances());
      data.setClassRequirement(datacopy.getClassRequirement());

      data.setAbilityType(datacopy.getAbilityType());
      data.setAbilityCooldown(datacopy.getCooldown());

      data.setKnockup(datacopy.hasKnockup());
      data.setShake(datacopy.hasShake());
      data.setApChange(datacopy.getApChange());

      return data;
   }

   // Builder for the ability data
   public static AbilityData CreateInstance (BattleItemData basicData, int abCost, bool blockStatus, ParticleSystem castPrt,
       AudioClip castClip, Battler.Stance[] _allowedStances, Weapon.Class _classRequirement, AbilityType abilityType, float cooldown,
       bool hasKnockup, bool hasShake, int apChange) {
      AbilityData data = CreateInstance<AbilityData>();

      // Basic battle item data.

      data.setName(basicData.getName());
      data.setDescription(basicData.getDescription());
      data.setItemID(basicData.getItemID());
      data.setItemIcon(basicData.getItemIcon());
      data.setLevelRequirement(basicData.getLevelRequirement());

      data.setItemDamage(basicData.getBaseDamage());
      data.setItemElement(basicData.getElementType());

      data.setHitAudioClip(basicData.getHitAudioClip());
      data.setHitParticle(basicData.getHitParticle());

      data.setBattleItemType(basicData.getBattleItemType());

      // Ability Data.
      data.setAbilityCost(abCost);
      data.setBlockStatus(blockStatus);

      data.setCastParticle(castPrt);
      data.setCastAudioClip(castClip);

      data.setAllowedStances(_allowedStances);
      data.setClassRequirement(_classRequirement);

      data.setAbilityType(abilityType);
      data.setAbilityCooldown(cooldown);

      data.setKnockup(hasKnockup);
      data.setShake(hasShake);
      data.setApChange(apChange);

      return data;
   }

   // Setters
   protected void setAbilityCost (int value) { _abilityCost = value; }
   protected void setBlockStatus (bool value) { _canBeBlocked = value; }
   protected void setCastParticle (ParticleSystem value) { _castParticle = value; }
   protected void setCastAudioClip (AudioClip value) { _castAudioClip = value; }
   protected void setAllowedStances (Battler.Stance[] value) { _allowedStances = value; }
   protected void setAbilityType (AbilityType value) { _abilityType = value; }
   protected void setAbilityCooldown (float value) { _abilityCooldown = value; }
   protected void setKnockup (bool value) { _hasKnockup = value; }
   protected void setShake (bool value) { _hasShake = value; }
   protected void setApChange (int value) { _apChange = value; }

   // Getters
   public int getAbilityCost () { return _abilityCost; }

   // TODO: Add this to the item creation window.
   public float getCooldown () { return _abilityCooldown; }

   /// <summary> Can this ability be blocked? </summary>
   public bool getBlockStatus () { return _canBeBlocked; }
   public ParticleSystem getCastParticle () { return _castParticle; }
   public AudioClip getCastAudioClip () { return _castAudioClip; }
   public Battler.Stance[] getAllowedStances () { return _allowedStances; }
   public AbilityType getAbilityType () { return _abilityType; }

   public bool hasShake () { return _hasShake; }
   public bool hasKnockup () { return _hasKnockup; }
   public int getApChange () { return _apChange; }

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

   // TODO ZERONEV: create correct anim length method, check buff abilities and other.
   public float getTotalAnimLength (Battler attacker, Battler target) {

      float shakeLength = 0;
      float knockupLength = 0;

      switch (getAbilityType()) {
         case AbilityType.Melee:
            // The animation length depends on the distance between attacker and target
            float jumpDuration = getJumpDuration(attacker, target);

            // Add up the amount of time it takes to animate an entire melee action
            return jumpDuration + Battler.PAUSE_LENGTH + attacker.getPreContactLength() +
                Battler.POST_CONTACT_LENGTH + jumpDuration + Battler.PAUSE_LENGTH;

         case AbilityType.Ranged:
            shakeLength = hasShake() ? Battler.SHAKE_LENGTH : 0f;
            knockupLength = hasKnockup() ? Battler.KNOCKUP_LENGTH : 0f;

            // Add up the amount of time it takes to animate an entire action
            return attacker.getPreMagicLength(this) + shakeLength + knockupLength + getPreDamageLength + getPostDamageLength;

         case AbilityType.Projectile:
            shakeLength = hasShake() ? Battler.SHAKE_LENGTH : 0f;
            knockupLength = hasKnockup() ? Battler.KNOCKUP_LENGTH : 0f;

            // Add up the amount of time it takes to animate an entire action
            return attacker.getPreMagicLength(this) + shakeLength + knockupLength + getPreDamageLength + getPostDamageLength;

         default:
            Debug.LogWarning("Ability type is not defined for getting anim length");
            return 0;
      }
   }

   // TODO ZERONEV: Used for buffs only.
   public float getDuration () { return 12.0f; }

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
      return getAbilityType().Equals(AbilityType.Melee);
   }

   public bool isHeal () {
      return getElementType().Equals(Element.Heal);
   }

   public bool isProjectile () {
      return getAbilityType().Equals(AbilityType.Projectile);
   }

   public bool isCancel () {
      return getAbilityType().Equals(AbilityType.Cancel);
   }

   private float getPreDamageLength { get { return 0.4f; } }
   private float getPostDamageLength { get { return 0.4f; } }

   #endregion

   #region Private Variables

   // Cost required to execute this ability
   [SerializeField] private int _abilityCost;

   // Can this ability be blocked?
   [SerializeField] private bool _canBeBlocked;

   // Cast parameters, effects that will play whenever we start casting the ability (if required.)

   [SerializeField] private ParticleSystem _castParticle;
   [SerializeField] private AudioClip _castAudioClip;

   // Combat stances required to be able to use this ability.
   [SerializeField] private Battler.Stance[] _allowedStances;

   // Type of the ability.
   [SerializeField] private AbilityType _abilityType;
   [SerializeField] private float _abilityCooldown;

   [SerializeField] private bool _hasShake;
   [SerializeField] private bool _hasKnockup;

   [SerializeField] private int _apChange;

   #endregion

}

// Interface method.
public interface IAttackBehaviour
{
   IEnumerator attackDisplay (float timeToWait, BattleAction battleAction, bool isFirstAction);
}

public enum AbilityType
{
   UNDEFINED = 0,
   Melee = 1,        // Close quarters ability.
   Ranged = 2,       // Ranged ability
   Projectile = 3,    // Created just in case, replace if ranged can do anything projectile does.
   Cancel = 4
}

public enum ActionType
{
   UNDEFINED = 0,
   Attack = 1,
   BuffDebuff = 2
}
