using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
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

   // Delay, in seconds, for playing the cast sound effect.
   public float castSoundEffectDelay = 0f;

   // Combat stances required to be able to use this ability
   public Battler.Stance[] allowedStances;

   // Type of the ability. (Standard or buff/debuff)
   public AbilityType abilityType;

   // The cooldown of the skill
   public float abilityCooldown = 2;

   // The action point change value
   public int apChange;

   // Cost required to execute this ability
   public int abilityCost;

   // This can be null, like a sword attack for example
   public string[] castSpritesPath;

   // Visual effect time per frame
   public float FXTimePerFrame = 0.10f;

   // The hit vfx time per frame
   public float hitFXTimePerFrame = 0.10f;

   // The predefined position where the ability cast vfx will spawn
   public AbilityCastPosition abilityCastPosition;

   // The projectile id associated with this ability
   public int projectileId;

   // The maximum targets
   public int maxTargets = 1;

   // Determines if a special animation needs to be used upon casting the ability
   public bool useSpecialAnimation;

   public enum AbilityCastPosition
   {
      Self = 0,
      Target = 1,
      AboveSelf = 2,
      AboveTarget = 3
   }

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
      int apChange, float fxTimePerFrame, AbilityCastPosition castPosition, float hitFXTimePerFrame) {
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
      data.abilityCastPosition = castPosition;
      data.hitFXTimePerFrame = hitFXTimePerFrame;

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
      hitFXTimePerFrame = basicAbilityData.hitFXTimePerFrame;
      abilityCastPosition = basicAbilityData.abilityCastPosition;
   }

   #region Helper Methods

   public void playCastSfxAtTarget (Transform targetTransform) {
      SoundEffect soundEffect = SoundEffectManager.self.getSoundEffect(castSoundEffectId);
      if (soundEffect != null) {
         // Play the fmod sfx if the fmod id is valid
         if (soundEffect.fmodId.Length > 1) {
            SoundEffectManager.self.playFmodWithPath(soundEffect.fmodId, targetTransform);
         } else {
            if (SoundEffectManager.self.isValidSoundEffect(castSoundEffectId)) {
               // Play intended sfx using legacy approach, referencing sfx in resource folder
               SoundEffectManager.self.playSoundEffect(castSoundEffectId, targetTransform);
            } else {
               // Play a default clip
               AudioClip castClip = AudioClipManager.self.getAudioClipData(AudioClipManager.self.defaultCastAudio).audioClip;
               SoundManager.playClipAtPoint(castClip, targetTransform.position);
            }
         }
      } else {
         if (castSoundEffectId > 0) {
            D.debug("Failed to get sfx content for Cast ID: {" + castSoundEffectId + "}");
         }

         // The buff abilities should have a default Cast sfx
         if (abilityType == AbilityType.BuffDebuff) {
            // Play a default clip
            AudioClip castClip = AudioClipManager.self.getAudioClipData(AudioClipManager.self.defaultCastAudio).audioClip;
            SoundManager.playClipAtPoint(castClip, targetTransform.position);
         }
      }
   }

   public void playHitSfxAtTarget (Transform targetTransform) {
      SoundEffect soundEffect = SoundEffectManager.self.getSoundEffect(hitSoundEffectId);
      if (soundEffect != null) {
         // Play the fmod sfx if the fmod id is valid
         if (soundEffect.fmodId.Length > 1) {
            SoundEffectManager.self.playFmodWithPath(soundEffect.fmodId, targetTransform);
         } else {
            if (SoundEffectManager.self.isValidSoundEffect(hitSoundEffectId)) {
               // Play intended sfx using legacy approach, referencing sfx in resource folder
               SoundEffectManager.self.playSoundEffect(hitSoundEffectId, targetTransform);
            } else {
               // Play a default clip
               SoundEffectManager.self.playFmodWithPath(SoundEffectManager.GENERIC_HIT_LAND, targetTransform);
               //AudioClip hitClip = AudioClipManager.self.getAudioClipData(AudioClipManager.self.defaultHitAudio).audioClip;
               //SoundManager.playClipAtPoint(hitClip, targetTransform.position);
            }
         }
      } else {
         if (hitSoundEffectId > 0) {
            D.debug("Failed to get sfx content for Hit ID: {" + hitSoundEffectId + "}");
         }

         // The NON buff abilities should have a default Hit sfx
         if (abilityType != AbilityType.BuffDebuff) {
            // Play a default clip
            //AudioClip hitClip = AudioClipManager.self.getAudioClipData(AudioClipManager.self.defaultHitAudio).audioClip;
            //SoundManager.playClipAtPoint(hitClip, targetTransform.position);
            SoundEffectManager.self.playFmodWithPath(SoundEffectManager.GENERIC_HIT_LAND, targetTransform);
         }
      }
   }

   public bool isMelee () {
      return classRequirement == Weapon.Class.Melee;
   }

   public bool isProjectile () {
      return classRequirement == Weapon.Class.Ranged;
   }

   public bool isRum () {
      return classRequirement == Weapon.Class.Rum;
   }

   public bool isReadyForUseBy (Battler sourceBattler, bool logData = false) {
      if (abilityType == AbilityType.Standard) {

         if (sourceBattler.AP < abilityCost) {
            sourceBattler.player.Target_ReceiveNormalChat("Not enough AP! (" + sourceBattler.AP + " / " + abilityCost + ")", ChatInfo.Type.System);
            D.editorLog("User does not have enough AP!", Color.red);
            // TODO: Insert Logic Here (Could be spawn effect prefab indicating low AP)
         }

         if (NetworkTime.time < sourceBattler.cooldownEndTime) {
            sourceBattler.player.Target_ReceiveNormalChat("Ability is not ready to be used! Wait for (" + (sourceBattler.cooldownEndTime - NetworkTime.time) + ") seconds", ChatInfo.Type.System);
            D.editorLog("User is still cooling down ability cast!", Color.red);
            // TODO: Insert Logic Here (Could be spawn effect prefab indicating ability is on cooldown)
         }

         return (sourceBattler.AP >= abilityCost) && (NetworkTime.time >= sourceBattler.cooldownEndTime);

      } else if (abilityType == AbilityType.BuffDebuff) {

         if (sourceBattler.AP < abilityCost) {
            // TODO: Insert Logic Here (Could be spawn effect prefab indicating low AP)
         }

         if (NetworkTime.time < sourceBattler.cooldownEndTime) {
            // TODO: Insert Logic Here (Could be spawn effect prefab indicating ability is on cooldown)
         }

         return (sourceBattler.AP >= abilityCost) && (NetworkTime.time >= sourceBattler.cooldownEndTime);

      } else if (abilityType == AbilityType.Stance) {
         return NetworkTime.time >= sourceBattler.stanceCooldownEndTime;

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
   IEnumerator attackDisplay (double timeToWait, BattleAction battleAction, bool isFirstAction);
}

public enum AbilityType
{
   Undefined = 0,
   Standard = 1,
   BuffDebuff = 2,
   Stance = 3,
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
      foreach (int basicDataId in record.basicAbilityDataList) {
         basicList.Add(basicDataId);

         BasicAbilityData abilityData = AbilityManager.self.allGameAbilities.Find(_ => _.itemID == basicDataId);
         if (abilityData != null) {
            if (abilityData.abilityType == AbilityType.Standard) {
               attackList.Add(basicDataId);
            } else if (abilityData.abilityType == AbilityType.BuffDebuff) {
               buffList.Add(basicDataId);
            }
         }
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