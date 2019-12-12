using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class AbilityManager : MonoBehaviour
{
   #region Public Variables

   // The maximum number of abilities that can be equipped in combat
   public static int MAX_EQUIPPED_ABILITIES = 5;

   // A convenient self reference
   public static AbilityManager self;

   // Holds the xml data for all abilities
   public TextAsset[] abilityDataAssets;

   // References to all the abilities
   public List<BasicAbilityData> allGameAbilities { get { return _allGameAbilities; } }

   #endregion

   void Awake () {
      self = this;
   }

   public void addNewAbility(BasicAbilityData ability) {
      if (!_allGameAbilities.Exists(_ => _.itemName == ability.itemName)) {
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

   public void addNewAbilities (BasicAbilityData[] abilities) {
      foreach (BasicAbilityData ability in abilities) {
         if (_allGameAbilities.Exists(_ => _.itemName == ability.itemName)) {
            Debug.LogWarning("Duplicated ability name: " + ability.itemName + " will not add again");
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
         Battler sourceBattler = battle.getBattler(action.sourceId);
         Battler targetBattler = battle.getBattler(action.targetId);
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

               StartCoroutine(sourceBattler.attackDisplay(timeToWait, action, isFirst));
               break;

            case BattleActionType.Stance: 
               StanceAction stanceAction = action as StanceAction;
               actionToExecute = stanceAction;

               // Make note of the time that this action is going to occur
               sourceBattler.lastStanceChange = actionToExecute.actionEndTime;

               // Check how long we need to wait before displaying this action
               timeToWait = actionToExecute.actionEndTime - Util.netTime();


               StartCoroutine(sourceBattler.attackDisplay(timeToWait, action, isFirst));
               break;

            case BattleActionType.BuffDebuff: 
               BuffAction buffAction = action as BuffAction;
               actionToExecute = buffAction;

               // Make note of the time that this action is going to occur
               sourceBattler.lastStanceChange = actionToExecute.actionEndTime;

               // Check how long we need to wait before displaying this action
               timeToWait = actionToExecute.actionEndTime - Util.netTime();

               StartCoroutine(sourceBattler.buffDisplay(timeToWait, action, isFirst));
               
               break;

            case BattleActionType.Cancel: 
               CancelAction cancelAction = action as CancelAction;
               actionToExecute = cancelAction;

               // Update the battler's action timestamps
               sourceBattler.cooldownEndTime -= cancelAction.timeToSubtract;
               
               break;
         }

         // Don't affect the source for any subsequent actions
         isFirst = false;
      }
   }

   public void execute (StanceAction action) {
      Battle battle = BattleManager.self.getBattle(action.battleId);
      Battler sourceBattler = battle.getBattler(action.sourceId);

      // Make note of the time that this action is going to occur
      sourceBattler.lastStanceChange = action.actionEndTime;
      sourceBattler.stanceCooldownEndTime = action.actionEndTime;
      sourceBattler.stance = action.newStance;
   }

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

   public static AttackAbilityData getAttackAbility (int abilityGlobalID) {
      AttackAbilityData returnAbility = new AttackAbilityData();
      returnAbility = self._attackAbilities.Find(_ => _.itemID == abilityGlobalID);
      return returnAbility;
   }

   public static BuffAbilityData getBuffAbility (int abilityGlobalID) {
      BuffAbilityData returnAbility = new BuffAbilityData();
      returnAbility = self._buffAbilities.Find(_ => _.itemID == abilityGlobalID);
      return returnAbility;
   }

   #region Private Variables

   // Stores the list of all abilities
   [SerializeField] private List<BasicAbilityData> _allGameAbilities = new List<BasicAbilityData>();

   // Stores the list of all attack abilities
   [SerializeField] private List<AttackAbilityData> _attackAbilities = new List<AttackAbilityData>();

   // Stores the list of all buff abilities
   [SerializeField] private List<BuffAbilityData> _buffAbilities = new List<BuffAbilityData>();

   #endregion
}
