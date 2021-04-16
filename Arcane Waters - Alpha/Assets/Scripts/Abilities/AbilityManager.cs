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

   // TODO: Propose a compiled starting data and set in sql database to be fetched when creating new users
   // Default Attack = 9
   // Fire ball = 20
   // Air Slash = 37
   // Earth Slash = 6
   // Water Bullet = 18
   public static int[] STARTING_ABILITIES = { 9, 86, 37, 6, 18};

   // The id of the punch ability
   public static int PUNCH_ID = 66;

   // The id of the slash ability
   public static int SLASH_ID = 9;

   // The id of the shoot ability
   public static int SHOOT_ID = 91;

   // The id of the rum ability
   public static int RUM_ID = 88;

   // The id of the healing rum ability
   public static int HEALING_RUM_ID = 89;

   // A convenient self reference
   public static AbilityManager self;

   // References to all the abilities
   public List<BasicAbilityData> allGameAbilities { get { return _allGameAbilities; } }

   // References to all the abilities
   public List<AttackAbilityData> allAttackbilities { get { return _attackAbilities; } }

   // References to all the abilities
   public List<BuffAbilityData> allBuffAbilities { get { return _buffAbilities; } }

   #endregion

   void Awake () {
      self = this;
   }

   public AttackAbilityData getPunchAbility () {
      return allAttackbilities.Find(_ => _.itemID == PUNCH_ID);
   }

   public AttackAbilityData getSlashAbility () {
      return allAttackbilities.Find(_ => _.itemID == SLASH_ID);
   }

   public AttackAbilityData getShootAbility () {
      return allAttackbilities.Find(_ => _.itemID == SHOOT_ID);
   }

   public AttackAbilityData getThrowRumAbility () {
      return allAttackbilities.Find(_ => _.itemID == RUM_ID);
   }

   public BuffAbilityData getHealingRumAbility () {
      return allBuffAbilities.Find(_ => _.itemID == HEALING_RUM_ID);
   }

   public void initializeAbilities () {
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<AbilityXMLContent> abilityContentList = DB_Main.getBattleAbilityXML();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (AbilityXMLContent abilityXML in abilityContentList) {
               AbilityType abilityType = (AbilityType) abilityXML.abilityType;
               TextAsset newTextAsset = new TextAsset(abilityXML.abilityXML);

               switch (abilityType) {
                  case AbilityType.Standard:
                     try {
                        AttackAbilityData attackAbilityData = Util.xmlLoad<AttackAbilityData>(newTextAsset);
                        addNewAbility(attackAbilityData);
                     } catch {
                        D.editorLog("Error converting attack ability: " + abilityXML.abilityId, Color.red);
                     }
                     break;
                  case AbilityType.Stance:
                     try {
                        AttackAbilityData stancebilityData = Util.xmlLoad<AttackAbilityData>(newTextAsset);
                        addNewAbility(stancebilityData);
                     } catch {
                        D.editorLog("Error converting stance ability: " + abilityXML.abilityId, Color.red);
                     }
                     break;
                  case AbilityType.BuffDebuff:
                     try {
                        BuffAbilityData buffAbilityData = Util.xmlLoad<BuffAbilityData>(newTextAsset);
                        addNewAbility(buffAbilityData);
                     } catch {
                        D.editorLog("Error converting buff ability: " + abilityXML.abilityId, Color.red);
                     }
                     break;
                  default:
                     try {
                        BasicAbilityData abilityData = Util.xmlLoad<BasicAbilityData>(newTextAsset);
                        addNewAbility(abilityData);
                     } catch {
                        D.editorLog("Error converting basic ability: " + abilityXML.abilityId, Color.red);
                     }
                     break;
               }
            }
         });
      });
   }
   
   public void addNewAbility (BasicAbilityData ability) {
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

   public void receiveAbilitiesFromZipData (AttackAbilityData[] attackAbilities, BuffAbilityData[] buffAbilities) {
      _attackAbilities = new List<AttackAbilityData>(attackAbilities);
      _buffAbilities = new List<BuffAbilityData>(buffAbilities);

      foreach (AttackAbilityData attackData in _attackAbilities) {
         _allGameAbilities.Add(attackData);
      }
      foreach (BuffAbilityData buffData in _buffAbilities) {
         _allGameAbilities.Add(buffData);
      }
   }

   public void execute (BattleAction[] actions) {
      bool isFirst = true;

      foreach (BattleAction action in actions) {
         BattleAction actionToExecute = null;
         Battle battle = BattleManager.self.getBattle(action.battleId);
         if (battle == null) {
            D.debug("Battle fetched was null!");
            return;
         }
         Battler sourceBattler = battle.getBattler(action.sourceId);
         Battler targetBattler = battle.getBattler(action.targetId);
         if (sourceBattler == null) {
            D.debug("Cannot execute, Source battler not existing: " + action.sourceId);
            continue;
         }
         if (targetBattler == null) {
            D.debug("Cannot execute, Target battler not existing: " + action.targetId);
            continue;
         }

         // Show targeting effects on remote clients
         if (sourceBattler.battlerType == BattlerType.PlayerControlled && !sourceBattler.isLocalBattler()) {
            sourceBattler.showTargetingEffects(targetBattler);
         }

         double timeToWait = 0;

         // Update timestamps
         sourceBattler.lastAbilityEndTime = action.actionEndTime;
         sourceBattler.cooldownEndTime = action.actionEndTime + action.cooldownDuration;

         // Mark both battlers as animating for the duration
         sourceBattler.animatingUntil = action.actionEndTime;
         if (targetBattler != null) {
            targetBattler.animatingUntil = action.actionEndTime;
         }

         float animationLength = .6f;
         switch (action.battleActionType) {
            case BattleActionType.Attack:
               // Get the ability object for this action
               if (sourceBattler.getAttackAbilities().Count < action.abilityInventoryIndex || sourceBattler.getAttackAbilities().Count == 0) {
                  D.debug("Failed to process attack execution! AbilityIndex: " + action.abilityInventoryIndex + " TotalAttacks: " + sourceBattler.getAttackAbilities().Count + " Total Abilities: " + sourceBattler.getBasicAbilities().Count);
                  return;
               }
               AttackAbilityData abilityData = sourceBattler.getAttackAbilities()[action.abilityInventoryIndex];
               if (abilityData != null) {
                  if (targetBattler != null) {
                     animationLength = abilityData.getTotalAnimLength(sourceBattler, targetBattler);
                  }
               } else {
                  D.editorLog("Issue getting ability data: " + action.abilityInventoryIndex + " - " + action.abilityGlobalID);
               }

               AttackAction attackAction = action as AttackAction;
               actionToExecute = attackAction;

               // Check how long we need to wait before displaying this action
               timeToWait = BattleManager.TICK_INTERVAL + actionToExecute.actionEndTime - NetworkTime.time - animationLength;

               if (sourceBattler.enemyType == Enemy.Type.PlayerBattler) {
                  D.adminLog("Source battler is attacking" + " : " + sourceBattler.enemyType
                     + " Target is: " + targetBattler.enemyType
                     + " TimeToWait: " + timeToWait
                     + " CurrAnim: " + targetBattler.getAnim()[0].currentAnimation
                     + " TargetHealth: " + targetBattler.health + " : " + targetBattler.displayedHealth, D.ADMIN_LOG_TYPE.AnimationFreeze);
               }

               sourceBattler.registerNewActionCoroutine(sourceBattler.attackDisplay(timeToWait, action, isFirst), action.battleActionType);
               break;

            case BattleActionType.Stance: 
               StanceAction stanceAction = action as StanceAction;
               actionToExecute = stanceAction;

               // Make note of the time that this action is going to occur
               sourceBattler.lastStanceChange = actionToExecute.actionEndTime;

               // Check how long we need to wait before displaying this action
               timeToWait = BattleManager.TICK_INTERVAL + actionToExecute.actionEndTime - NetworkTime.time;

               sourceBattler.registerNewActionCoroutine(sourceBattler.attackDisplay(timeToWait, action, isFirst), action.battleActionType);
               break;

            case BattleActionType.BuffDebuff: 
               BuffAction buffAction = action as BuffAction;
               actionToExecute = buffAction;

               // Check how long we need to wait before displaying this action
               timeToWait = BattleManager.TICK_INTERVAL + actionToExecute.actionEndTime - NetworkTime.time;

               sourceBattler.registerNewActionCoroutine(sourceBattler.buffDisplay(timeToWait, action, isFirst), action.battleActionType);
               break;

            case BattleActionType.Cancel:
               CancelAction cancelAction = action as CancelAction;
               actionToExecute = cancelAction;
               if (sourceBattler.canCancelAction) {
                  // Update the battler's action timestamps
                  sourceBattler.cooldownEndTime -= cancelAction.timeToSubtract;
                  sourceBattler.stopActionCoroutine();
                  sourceBattler.setBattlerCanCastAbility(true);
               } else {
                  D.debug("Ability can not be canceled!");
               }
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

      if (action.newStance == Battler.Stance.Attack && Global.player != null && Global.player.userId == sourceBattler.userId) {
         TutorialManager3.self.tryCompletingStep(TutorialTrigger.SwitchToOffensiveStance);
      }
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
         case AbilityType.Undefined:
            returnAbility = self._allGameAbilities.Find(_ => _.itemID == abilityGlobalID);
            return returnAbility;
         default:
            returnAbility = self._allGameAbilities.Find(_ => _.itemID == abilityGlobalID);
            if (returnAbility == null) {
               Debug.LogWarning("Tried to search an unexisting item: " + abilityGlobalID + " : " + abilityType);
            }
            return returnAbility;
      }
   }

   public static BasicAbilityData getAbility (string abilityName) {
      BasicAbilityData returnAbility = self._allGameAbilities.Find(_ => _.itemName == abilityName);

      return returnAbility;
   }

   public AttackAbilityData getAttackAbility (int abilityGlobalID) {
      AttackAbilityData returnAbility = new AttackAbilityData();
      returnAbility = self._attackAbilities.Find(_ => _.itemID == abilityGlobalID);

      if (returnAbility == null) {
         BasicAbilityData attackAbility = self.allGameAbilities.Find(_ => _.itemID == abilityGlobalID);
         if (attackAbility != null && attackAbility.abilityType == AbilityType.Standard) {
            return (AttackAbilityData) attackAbility;
         }
      }

      return returnAbility;
   }

   public BuffAbilityData getBuffAbility (int abilityGlobalID) {
      BuffAbilityData returnAbility = new BuffAbilityData();
      returnAbility = allBuffAbilities.Find(_ => _.itemID == abilityGlobalID);

      if (returnAbility == null) {
         BasicAbilityData buffAbility = allGameAbilities.Find(_ => _.itemID == abilityGlobalID);
         if (buffAbility != null && buffAbility.abilityType == AbilityType.BuffDebuff) {
            return (BuffAbilityData) buffAbility;
         } 
      }

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
