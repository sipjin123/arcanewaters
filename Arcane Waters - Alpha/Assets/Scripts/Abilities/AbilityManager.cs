using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AbilityManager : MonoBehaviour
{
   #region Public Variables

   // A convenient self reference
   public static AbilityManager self;

   // ZERONEV-COMMENT: For now I will just add them manually into the inspector, all the abilities in-game.
   // I will load them later on from the resource folder and not make them public like this, it can be unsafe in the long run.
   public List<BasicAbilityData> allGameAbilities { get { return _allGameAbilities; } }

   #endregion

   void Awake () {
      self = this;

      // TODO ZERONEV: Instead of this method below, it would be better to grab all the files from the abilities folder.
      // This way we do not miss any ability and we have them in memory, in case we want to adjust an ability at runtime,
      // change an ability for an enemy at runtime, etc, and we can do that by grabbing their name or their ID. or even just an element.
      initAllGameAbilities();
   }

   public void addNewAbility(BasicAbilityData ability) {
      if (_allGameAbilities.Exists(_=>_.itemName == ability.itemName)) {
         Debug.LogWarning("Duplicated ability name: "+ability.itemName);
         return;
      }

      if (ability.abilityType == AbilityType.Standard) {
         AttackAbilityData newInstance = AttackAbilityData.CreateInstance((AttackAbilityData) ability);
         _attackAbilities.Add(newInstance);
         _allGameAbilities.Add(newInstance);
      } else if (ability.abilityType == AbilityType.BuffDebuff) {
         BuffAbilityData newInstance = BuffAbilityData.CreateInstance((BuffAbilityData) ability);
         _buffAbilities.Add(newInstance);
         _allGameAbilities.Add(newInstance);
      }
   }

   public void addNewAbilities (BasicAbilityData[] abilities) {
      foreach (BasicAbilityData ability in abilities) {
         if (_allGameAbilities.Exists(_ => _.itemName == ability.itemName)) {
            Debug.LogWarning("Duplicated ability name: " + ability.itemName);
            return;
         }
         if (ability.abilityType == AbilityType.Standard) {
            AttackAbilityData newInstance = AttackAbilityData.CreateInstance((AttackAbilityData) ability);
            _attackAbilities.Add(newInstance);
            _allGameAbilities.Add(newInstance);
         } else if (ability.abilityType == AbilityType.BuffDebuff) {
            BuffAbilityData newInstance = BuffAbilityData.CreateInstance((BuffAbilityData) ability);
            _buffAbilities.Add(newInstance);
            _allGameAbilities.Add(newInstance);
         }
      }
   }

   public void execute (BattleAction[] actions) {
      bool isFirst = true;

      foreach (BattleAction action in actions) {
         BattleAction actionToExecute = null;
         Battle battle = BattleManager.self.getBattle(action.battleId);
         BattlerBehaviour sourceBattler = battle.getBattler(action.sourceId);
         BattlerBehaviour targetBattler = battle.getBattler(action.targetId);
         float timeToWait = 0;

         // Update timestamps
         sourceBattler.lastAbilityEndTime = action.actionEndTime;
         sourceBattler.cooldownEndTime = action.actionEndTime + action.cooldownDuration;

         // Mark both battlers as animating for the duration
         sourceBattler.animatingUntil = action.actionEndTime;
         targetBattler.animatingUntil = action.actionEndTime;

         // Get the ability object for this action
         AttackAbilityData abilityData = sourceBattler.getAttackAbilities()[action.abilityInventoryIndex];

         switch (action.battleActionType) {
            case BattleActionType.Attack:
               AttackAction attackAction = action as AttackAction;
               actionToExecute = attackAction;

               // Check how long we need to wait before displaying this action
               timeToWait = actionToExecute.actionEndTime - Util.netTime() - abilityData.getTotalAnimLength(sourceBattler, targetBattler);
               break;

            case BattleActionType.Stance:
               StanceAction stanceAction = action as StanceAction;
               actionToExecute = stanceAction;

               // Make note of the time that this action is going to occur
               sourceBattler.lastStanceChange = actionToExecute.actionEndTime;

               // Check how long we need to wait before displaying this action
               timeToWait = actionToExecute.actionEndTime - Util.netTime();

               break;

            case BattleActionType.BuffDebuff:
               // TODO Zeronev: Implement.
               break;

            case BattleActionType.Cancel:
               CancelAction cancelAction = action as CancelAction;
               actionToExecute = cancelAction;

               // Update the battler's action timestamps
               sourceBattler.cooldownEndTime -= cancelAction.timeToSubtract;
               break;
         }

         StartCoroutine(sourceBattler.attackDisplay(timeToWait, action, isFirst));

         // Don't affect the source for any subsequent actions
         isFirst = false;
      }
   }

   public void execute (StanceAction action) {
      Battle battle = BattleManager.self.getBattle(action.battleId);
      BattlerBehaviour sourceBattler = battle.getBattler(action.sourceId);

      // Make note of the time that this action is going to occur
      sourceBattler.lastStanceChange = action.actionEndTime;
      sourceBattler.stanceCooldownEndTime = action.actionEndTime;
      sourceBattler.stance = action.newStance;
   }

   // Prepares all game abilities
   private void initAllGameAbilities () {
      foreach (BasicAbilityData ability in _allGameAbilities) {
         if (ability.abilityType == AbilityType.Standard) {
            AttackAbilityData newInstance = AttackAbilityData.CreateInstance((AttackAbilityData) ability);
            _attackAbilities.Add(newInstance);
         } else if (ability.abilityType == AbilityType.BuffDebuff) {
            BuffAbilityData newInstance = BuffAbilityData.CreateInstance((BuffAbilityData) ability);
            _buffAbilities.Add(newInstance);
         }
      }
   }

   /// <summary>
   /// Gets UNINITIALIZED AbilityData, list only to be used for reading, do not modify values directly here.
   /// </summary>
   /// <param name="abilityGlobalID"></param>
   /// <returns></returns>
   public static BasicAbilityData getAbility (int abilityGlobalID, AbilityType abilityType) {
      BasicAbilityData returnAbility = new BasicAbilityData();
      switch (abilityType) {
         case AbilityType.Standard:
            returnAbility = self._attackAbilities.Find(_ => _.itemID == abilityGlobalID);
            return returnAbility;
         case AbilityType.BuffDebuff:
            returnAbility = self._buffAbilities.Find(_ => _.itemID == abilityGlobalID);
            return returnAbility;
         default:
            returnAbility = self._allGameAbilities.Find(_ => _.itemID == abilityGlobalID);
            if (returnAbility == null) {
               Debug.LogWarning("Tried to search an unexisting item");
            }
            return returnAbility;
      }
   }

   #region Private Variables

   // Stores the list of all abilities
   private List<BasicAbilityData> _allGameAbilities = new List<BasicAbilityData>();

   // Stores the list of all attack abilities
   private List<AttackAbilityData> _attackAbilities = new List<AttackAbilityData>();

   // Stores the list of all buff abilities
   private List<BuffAbilityData> _buffAbilities = new List<BuffAbilityData>();

   #endregion
}
