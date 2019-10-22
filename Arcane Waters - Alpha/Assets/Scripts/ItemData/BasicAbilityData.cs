using System.Collections;
using UnityEngine;

/// <summary>
/// Not to be used directly, this class is just a base for all abilities.
/// Please use AttackAbilityData or BuffAbilityData instead.
/// </summary>
[System.Serializable]
public class BasicAbilityData : BattleItemData {
   // Builder scriptable object instance builder
   public static BasicAbilityData CreateInstance (BasicAbilityData datacopy) {
      // If a new value needs to be added to the abilitydata class, it needs to be included in here!
      BasicAbilityData data = CreateInstance<BasicAbilityData>();

      data.setBaseBattleItemData(datacopy);
      data.setBaseAbilityData(datacopy);

      return data;
   }

   // Builder for the ability data
   public static BasicAbilityData CreateInstance (BattleItemData basicData, int abCost, Sprite[] castSprites,
       AudioClip castClip, BattlerBehaviour.Stance[] _allowedStances, AbilityType abilityType, float cooldown,
       int apChange, float fxTimePerFrame) {
      BasicAbilityData data = CreateInstance<BasicAbilityData>();

      // Basic battle item data
      data.setBaseBattleItemData(basicData);

      // Ability Data
      data.setAbilityCost(abCost);
      
      data.setCastEffect(castSprites);
      data.setCastAudioClip(castClip);

      data.setAllowedStances(_allowedStances);

      data.setAbilityType(abilityType);
      data.setAbilityCooldown(cooldown);
      
      data.setApChange(apChange);
      data.setFXTimePerFrame(fxTimePerFrame);

      return data;
   }

   // Setters
   protected void setAbilityCost (int value) { _abilityCost = value; }
   protected void setCastAudioClip (AudioClip value) { _castAudioClip = value; }
   protected void setAllowedStances (BattlerBehaviour.Stance[] value) { _allowedStances = value; }
   protected void setAbilityType (AbilityType value) { _abilityType = value; }
   protected void setAbilityCooldown (float value) { _abilityCooldown = value; }
   protected void setApChange (int value) { _apChange = value; }
   protected void setCastEffect (Sprite[] value) { _castSprites = value; }
   protected void setFXTimePerFrame (float value) { _FXTimePerFrame = value; }

   // Getters
   public int getAbilityCost () { return _abilityCost; }
   public float getCooldown () { return _abilityCooldown; }
   public AudioClip getCastAudioClip () { return _castAudioClip; }
   public BattlerBehaviour.Stance[] getAllowedStances () { return _allowedStances; }
   public AbilityType getAbilityType () { return _abilityType; }
   public int getApChange () { return _apChange; }
   public Sprite[] getCastEffect () { return _castSprites; }
   public float getFXTimePerFrame () { return _FXTimePerFrame; }

   protected void setBaseAbilityData (BasicAbilityData basicAbilityData) {
      setAbilityCost(basicAbilityData.getAbilityCost());
      setAbilityCooldown(basicAbilityData.getCooldown());

      setCastAudioClip(basicAbilityData.getCastAudioClip());

      setAllowedStances(basicAbilityData.getAllowedStances());

      setAbilityType(basicAbilityData.getAbilityType());

      setApChange(basicAbilityData.getApChange());
      setCastEffect(basicAbilityData.getCastEffect());

      setFXTimePerFrame(basicAbilityData.getFXTimePerFrame());
   }

   #region Helper Methods

   public bool isReadyForUseBy (BattlerBehaviour sourceBattler) {
      if (_abilityType == AbilityType.Standard) {

         if (sourceBattler.AP < getAbilityCost()) {
            Debug.Log("not enough ap");
         }

         if (Util.netTime() < sourceBattler.cooldownEndTime) {
            Debug.Log("Time on cooldown");
         }

         return (sourceBattler.AP >= getAbilityCost()) && (Util.netTime() >= sourceBattler.cooldownEndTime);

      } else if (_abilityType == AbilityType.Stance) {
         return Util.netTime() >= sourceBattler.stanceCooldownEndTime;

      } else {
         Debug.Log("The ability type is undefined");
         return false;
      }
   }

   #endregion

   #region Private Variables

   // Cast parameters, effects that will play whenever we start casting the ability (if required)
   [SerializeField] private AudioClip _castAudioClip;

   // Combat stances required to be able to use this ability
   [SerializeField] private BattlerBehaviour.Stance[] _allowedStances;

   // Type of the ability. (Standard or buff/debuff)
   [SerializeField] private AbilityType _abilityType;
   [SerializeField] private float _abilityCooldown;
   [SerializeField] private int _apChange;

   // Cost required to execute this ability
   [SerializeField] private int _abilityCost;

   // This can be null, like a sword attack for example
   [SerializeField] private Sprite[] _castSprites;

   // Visual effect time per frame
   [SerializeField] private float _FXTimePerFrame = 0.10f;

   #endregion
}

// Interface method
public interface IAttackBehaviour
{
   IEnumerator attackDisplay (float timeToWait, BattleAction battleAction, bool isFirstAction);
}

public enum AbilityType
{
   Undefined = 0,
   Standard = 1,
   BuffDebuff = 2,
   Stance = 3
}
