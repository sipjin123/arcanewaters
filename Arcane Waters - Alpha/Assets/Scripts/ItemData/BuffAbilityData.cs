using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class BuffAbilityData : BasicAbilityData
{
   #region Public Variables

   #endregion

   // Builder scriptable object instance builder
   public static BuffAbilityData CreateInstance (BuffAbilityData datacopy) {
      // If a new value needs to be added to the abilitydata class, it needs to be included in here!
      BuffAbilityData data = CreateInstance<BuffAbilityData>();

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
      data.setBuffDuration(datacopy.getBuffDuration());
      data.setBuffType(datacopy.getBuffType());
      data.setBuffActionType(datacopy.getBuffActionType());
      data.setBuffIcon(datacopy.getBuffIcon());

      return data;
   }

   /// <summary>
   /// Used for creating a BuffAbilityData, only on item creation window
   /// </summary>
   public static BuffAbilityData CreateInstance (BasicAbilityData basicAbilityData, float buffDuration, BuffType buffType,
      BuffActionType buffActionType, Sprite buffIcon, int buffValue) {

      BuffAbilityData data = CreateInstance<BuffAbilityData>();

      // Sets base ability data
      data.setBaseAbilityData(basicAbilityData);
      data.setBaseBattleItemData(basicAbilityData);

      // Sets attack ability item properties
      data.setBuffDuration(buffDuration);
      data.setBuffType(buffType);
      data.setBuffActionType(buffActionType);
      data.setBuffIcon(buffIcon);
      data.setBuffValue(buffValue);

      return data;
   }

   #region Custom Helper Methods

   public float getTotalAnimLength (Battler attacker, Battler target) {

      // TODO: ZERONEV - Not sure how animation lengths will be handled for buffs, for now I will just set a hardcoded value
      // below lines were commented out because they're not being used atm.
      // float shakeLength = 0;
      // float knockupLength = 0;

      return 1;

      /*switch (getAbilityActionType()) {
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
      }*/
   }

   #endregion

   // Setters
   public void setBuffDuration (float value) { _duration = value; }
   public void setBuffType (BuffType value) { _buffType = value; }
   public void setBuffActionType (BuffActionType value) { _buffActionType = value; }
   public void setBuffIcon (Sprite value) { _icon = value; }
   public void setBuffValue (int value) { _value = value; }

   // Getters
   public float getBuffDuration () { return _duration; }
   public BuffType getBuffType () { return _buffType; }
   public BuffActionType getBuffActionType () { return _buffActionType; }
   public Sprite getBuffIcon () { return _icon; }
   public int getBuffValue () { return _value; }

   #region Private Variables

   [SerializeField] private BuffActionType _buffActionType;
   [SerializeField] private BuffType _buffType;
   [SerializeField] private Sprite _icon;
   [SerializeField] private float _duration;

   // This will be the value in which we will increase or reduce the value, depending on the buff action
   // (For example, if the action is defense, and the value is 10, and it is a buff, then we will increase defense by 10, percentage or raw value) 
   [SerializeField] private int _value;

   #endregion
}

public enum BuffType
{
   UNDEFINED = 0,
   Buff = 1,
   Debuff = 2
}

public enum BuffActionType
{
   UNDEFINED = 0,
   Defense = 1,      // If debuff, this will reduce defense instead of increasing it.
   Haste = 2,        // If debuff, this will increase the cooldown instead of reducing it
   Regeneration = 3  // If debuff, this will decrease health overtime, instead of increasing it
}
