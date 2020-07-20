using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Not to be used directly, this class is just a base for all abilities.
/// Please use AttackAbilityData or BuffAbilityData instead.
/// </summary>
[System.Serializable]
public class BasicAbilityData : BattleItemData
{

   #region Public Variables

   // SoundEffect that will play whenever we start casting the ability (if required) -1 == no effect
   public int castSoundEffectId = -1;

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
      int castSoundEffectId, Battler.Stance[] _allowedStances, AbilityType abilityType, float cooldown,
      int apChange, float fxTimePerFrame) {
      BasicAbilityData data = new BasicAbilityData();

      // Basic battle item data
      data.setBaseBattleItemData(basicData);

      // Ability Data
      data.abilityCost = abCost;

      data.castSpritesPath = castSprites;
      data.castSoundEffectId = castSoundEffectId;

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

      castSoundEffectId = basicAbilityData.castSoundEffectId;

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
            D.editorLog("User does not have enough AP!", Color.red);
            // TODO: Insert Logic Here (Could be spawn effect prefab indicating low AP)
         }

         if (Util.netTime() < sourceBattler.cooldownEndTime) {
            D.editorLog("User is still cooling down ability cast!", Color.red);
            // TODO: Insert Logic Here (Could be spawn effect prefab indicating ability is on cooldown)
         }

         return (sourceBattler.AP >= abilityCost) && (Util.netTime() >= sourceBattler.cooldownEndTime);

      } else if (abilityType == AbilityType.BuffDebuff) {

         if (sourceBattler.AP < abilityCost) {
            // TODO: Insert Logic Here (Could be spawn effect prefab indicating low AP)
         }

         if (Util.netTime() < sourceBattler.cooldownEndTime) {
            // TODO: Insert Logic Here (Could be spawn effect prefab indicating ability is on cooldown)
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
   public static AbilityDataRecord CreateInstance (AbilityDataRecord record) {
      AbilityDataRecord newRecord = new AbilityDataRecord();

      List<int> attackList = new List<int>();
      List<int> buffList = new List<int>();
      List<int> basicList = new List<int>();

      foreach (int attackDataId in record.attackAbilityDataList) {
         attackList.Add(attackDataId);
         basicList.Add(attackDataId);
      }
      foreach (int buffDataId in record.buffAbilityDataList) {
         buffList.Add(buffDataId);
         basicList.Add(buffDataId);
      }
      foreach (int basicDataId in record.buffAbilityDataList) {
         basicList.Add(basicDataId);
      }

      newRecord.attackAbilityDataList = attackList.ToArray();
      newRecord.buffAbilityDataList = buffList.ToArray();
      newRecord.basicAbilityDataList = basicList.ToArray();

      return newRecord;
   }

   // Generic Skills1
   public int[] basicAbilityDataList = new int[0];

   // Offensive Skills
   public int[] attackAbilityDataList = new int[0];

   // Support Skills
   public int[] buffAbilityDataList = new int[0];
}