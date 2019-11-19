using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Not to be used directly, this class is just a base for all abilities.
/// Please use AttackAbilityData or BuffAbilityData instead.
/// </summary>
[System.Serializable]
public class BasicAbilityData : BattleItemData {

   #region Public Variables

   // Cast parameters, effects that will play whenever we start casting the ability (if required)
   public string castAudioClipPath;

   // Combat stances required to be able to use this ability
   public Battler.Stance[] allowedStances;

   // Type of the ability. (Standard or buff/debuff)
   public AbilityType abilityType;

   // The cooldown of the skill
   public float abilityCooldown;

   // The action point change value
   public int apChange;

   // Cost required to execute this ability
   public int abilityCost;

   // This can be null, like a sword attack for example
   public string[] castSpritesPath;

   // Visual effect time per frame
   public float FXTimePerFrame = 0.10f;

   #endregion

   public BasicAbilityData () { }

   // Builder scriptable object instance builder
   public static BasicAbilityData CreateInstance (BasicAbilityData datacopy) {
      // If a new value needs to be added to the abilitydata class, it needs to be included in here!
      BasicAbilityData data = new BasicAbilityData();

      data.setBaseBattleItemData(datacopy);
      data.setBaseAbilityData(datacopy);

      return data;
   }

   // Builder for the ability data
   public static BasicAbilityData CreateInstance (BattleItemData basicData, int abCost, string[] castSprites,
       string castClip, Battler.Stance[] _allowedStances, AbilityType abilityType, float cooldown,
       int apChange, float fxTimePerFrame) {
      BasicAbilityData data = new BasicAbilityData();

      // Basic battle item data
      data.setBaseBattleItemData(basicData);

      // Ability Data
      data.abilityCost = abCost;

      data.castSpritesPath = castSprites;
      data.castAudioClipPath = castClip;

      data.allowedStances = _allowedStances;

      data.abilityType = abilityType;
      data.abilityCooldown = cooldown;

      data.apChange = apChange;
      data.FXTimePerFrame = fxTimePerFrame;

      return data;
   }

   protected void setBaseAbilityData (BasicAbilityData basicAbilityData) {
      abilityCost = basicAbilityData.abilityCost;
      abilityCooldown = basicAbilityData.abilityCooldown;

      castAudioClipPath = basicAbilityData.castAudioClipPath;

      allowedStances = basicAbilityData.allowedStances;

      abilityType = basicAbilityData.abilityType;
      apChange = basicAbilityData.apChange;
      castSpritesPath = basicAbilityData.castSpritesPath;

      FXTimePerFrame = basicAbilityData.FXTimePerFrame;
   }

   #region Helper Methods

   public bool isReadyForUseBy (Battler sourceBattler) {
      if (abilityType == AbilityType.Standard) {

         if (sourceBattler.AP < abilityCost) {
            Debug.Log("not enough ap");
         }

         if (Util.netTime() < sourceBattler.cooldownEndTime) {
            Debug.Log("Time on cooldown");
         }

         return (sourceBattler.AP >= abilityCost) && (Util.netTime() >= sourceBattler.cooldownEndTime);

      } else if (abilityType == AbilityType.Stance) {
         return Util.netTime() >= sourceBattler.stanceCooldownEndTime;

      } else {
         Debug.Log("The ability type is undefined");
         return false;
      }
   }

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

[Serializable]
public class AbilityDataRecord
{
   // Generic Skills
   public BasicAbilityData[] basicAbilityDataList;
   public string[] basicAbilityRawData;

   // Offensive Skills
   public AttackAbilityData[] attackAbilityDataList;
   public string[] attackAbilityRawData;

   // Support Skills
   public BuffAbilityData[] buffAbilityDataList;
   public string[] buffAbilityRawData;
}