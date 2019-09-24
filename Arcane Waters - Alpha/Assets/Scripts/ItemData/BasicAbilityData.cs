using System.Collections;
using UnityEngine;

// Chris Palacios
/// <summary>
/// Not to be used directly, this class is just a base for all abilities.
/// Please use AttackAbilityData or BuffAbilityData instead.
/// </summary>
[System.Serializable]
public class BasicAbilityData : BattleItemData
{
   // Builder scriptable object instance builder.
   public static BasicAbilityData CreateInstance (BasicAbilityData datacopy) {
      // If a new value needs to be added to the abilitydata class, it needs to be included in here!
      BasicAbilityData data = CreateInstance<BasicAbilityData>();

      // Basic battle item data.
      /*data.setName(datacopy.getName());
      data.setDescription(datacopy.getDescription());
      data.setItemIcon(datacopy.getItemIcon());
      data.setItemID(datacopy.getItemID());
      data.setLevelRequirement(datacopy.getLevelRequirement());

      data.setItemElement(datacopy.getElementType());

      data.setHitAudioClip(datacopy.getHitAudioClip());
      data.setHitParticle(datacopy.getHitParticle());

      data.setBattleItemType(datacopy.getBattleItemType());*/
      data.setBaseBattleItemData(datacopy);

      // Ability Data.
      data.setAbilityCost(datacopy.getAbilityCost());

      data.setCastParticle(datacopy.getCastParticle());
      data.setCastAudioClip(datacopy.getCastAudioClip());

      data.setAllowedStances(datacopy.getAllowedStances());
      data.setClassRequirement(datacopy.getClassRequirement());

      data.setAbilityType(datacopy.getAbilityType());
      data.setAbilityCooldown(datacopy.getCooldown());
      
      data.setApChange(datacopy.getApChange());

      return data;
   }

   // Builder for the ability data
   public static BasicAbilityData CreateInstance (BattleItemData basicData, int abCost, ParticleSystem castPrt,
       AudioClip castClip, Battler.Stance[] _allowedStances, Weapon.Class _classRequirement, AbilityType abilityType, float cooldown,
       int apChange) {
      BasicAbilityData data = CreateInstance<BasicAbilityData>();

      // Basic battle item data
      data.setBaseBattleItemData(basicData);

      // Ability Data
      data.setAbilityCost(abCost);

      data.setCastParticle(castPrt);
      data.setCastAudioClip(castClip);

      data.setAllowedStances(_allowedStances);
      data.setClassRequirement(_classRequirement);

      data.setAbilityType(abilityType);
      data.setAbilityCooldown(cooldown);
      
      data.setApChange(apChange);

      return data;
   }

   // Setters
   protected void setAbilityCost (int value) { _abilityCost = value; }
   
   protected void setCastParticle (ParticleSystem value) { _castParticle = value; }
   protected void setCastAudioClip (AudioClip value) { _castAudioClip = value; }
   protected void setAllowedStances (Battler.Stance[] value) { _allowedStances = value; }
   protected void setAbilityType (AbilityType value) { _abilityType = value; }
   protected void setAbilityCooldown (float value) { _abilityCooldown = value; }
   protected void setApChange (int value) { _apChange = value; }

   /// <summary>
   /// Gets all base battle item data and sets it to this object.
   /// </summary>
   /// <param name="battleItemData"></param>
   protected void setBaseBattleItemData (BattleItemData battleItemData) {
      // Basic battle item data
      setName(battleItemData.getName());
      setDescription(battleItemData.getDescription());
      setItemID(battleItemData.getItemID());
      setItemIcon(battleItemData.getItemIcon());
      setLevelRequirement(battleItemData.getLevelRequirement());

      setItemElement(battleItemData.getElementType());

      setHitAudioClip(battleItemData.getHitAudioClip());
      setHitParticle(battleItemData.getHitParticle());

      setBattleItemType(battleItemData.getBattleItemType());
   }

   protected void setBaseAbilityData (BasicAbilityData basicAbilityData) {
      // Ability Data
      setAbilityCost(basicAbilityData.getAbilityCost());

      setCastParticle(basicAbilityData.getCastParticle());
      setCastAudioClip(basicAbilityData.getCastAudioClip());

      setAllowedStances(basicAbilityData.getAllowedStances());
      setClassRequirement(basicAbilityData.getClassRequirement());

      setAbilityType(basicAbilityData.getAbilityType());
      setAbilityCooldown(basicAbilityData.getCooldown());

      setApChange(basicAbilityData.getApChange());
   }

   // Getters
   public int getAbilityCost () { return _abilityCost; }
   public float getCooldown () { return _abilityCooldown; }
   public ParticleSystem getCastParticle () { return _castParticle; }
   public AudioClip getCastAudioClip () { return _castAudioClip; }
   public Battler.Stance[] getAllowedStances () { return _allowedStances; }
   public AbilityType getAbilityType () { return _abilityType; }
   public int getApChange () { return _apChange; }

   #region Private Variables

   // Cast parameters, effects that will play whenever we start casting the ability (if required)

   [SerializeField] private ParticleSystem _castParticle;
   [SerializeField] private AudioClip _castAudioClip;

   // Combat stances required to be able to use this ability
   [SerializeField] private Battler.Stance[] _allowedStances;

   // Type of the ability. (Standard or buff/debuff)
   [SerializeField] private AbilityType _abilityType;
   [SerializeField] private float _abilityCooldown;
   [SerializeField] private int _apChange;

   // Cost required to execute this ability
   [SerializeField] private int _abilityCost;

   #endregion
}

// Interface method
public interface IAttackBehaviour
{
   IEnumerator attackDisplay (float timeToWait, BattleAction battleAction, bool isFirstAction);
}

public enum AbilityType
{
   Standard,
   BuffDebuff
}
