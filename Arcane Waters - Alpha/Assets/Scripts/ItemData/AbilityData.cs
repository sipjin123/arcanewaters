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

      data.setName(datacopy.getName);
      data.setDescription(datacopy.getDescription);
      data.setItemIcon(datacopy.getItemIcon);
      data.setItemID(datacopy.getItemID);

      data.setItemDamage(datacopy.getBaseDamage);
      data.setItemElement(datacopy.getElementType);

      data.setHitAudioClip(datacopy.getHitAudioClip);
      data.setHitParticle(datacopy.getHitParticle);

      data.setBattleItemType(datacopy.getBattleItemType);

      // Ability Data.
      data.SetAbilityCost(datacopy.GetAbilityCost);
      data.SetBlockStatus(datacopy.GetBlockStatus);

      data.SetCastParticle(datacopy.GetCastParticle);
      data.SetCastAudioClip(datacopy.GetCastAudioClip);

      data.SetAllowedStances(datacopy.GetAllowedStances);
      data.setClassRequirement(datacopy.getClassRequirement);

      return data;
   }

   // Builder for the ability data
   public static AbilityData CreateInstance (BattleItemData basicData, int abCost, bool blockStatus, ParticleSystem castPrt,
       AudioClip castClip, Battler.Stance[] _allowedStances, Weapon.Class _classRequirement) {
      AbilityData data = CreateInstance<AbilityData>();

      // Basic battle item data.

      data.setName(basicData.getName);
      data.setDescription(basicData.getDescription);
      data.setItemID(basicData.getItemID);
      data.setItemIcon(basicData.getItemIcon);

      data.setItemDamage(basicData.getBaseDamage);
      data.setItemElement(basicData.getElementType);

      data.setHitAudioClip(basicData.getHitAudioClip);
      data.setHitParticle(basicData.getHitParticle);

      data.setBattleItemType(basicData.getBattleItemType);

      // Ability Data.
      data.SetAbilityCost(abCost);
      data.SetBlockStatus(blockStatus);

      data.SetCastParticle(castPrt);
      data.SetCastAudioClip(castClip);

      data.SetAllowedStances(_allowedStances);
      data.setClassRequirement(_classRequirement);

      return data;
   }

   // Setters
   protected void SetAbilityCost (int value) { _abilityCost = value; }
   protected void SetBlockStatus (bool value) { _canBeBlocked = value; }
   protected void SetCastParticle (ParticleSystem value) { _castParticle = value; }
   protected void SetCastAudioClip (AudioClip value) { _castAudioClip = value; }
   protected void SetAllowedStances (Battler.Stance[] value) { _allowedStances = value; }

   // Getters
   public int GetAbilityCost { get { return _abilityCost; } }
   public bool GetBlockStatus { get { return _canBeBlocked; } }
   public ParticleSystem GetCastParticle { get { return _castParticle; } }
   public AudioClip GetCastAudioClip { get { return _castAudioClip; } }
   public Battler.Stance[] GetAllowedStances { get { return _allowedStances; } }

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

   #endregion

}
