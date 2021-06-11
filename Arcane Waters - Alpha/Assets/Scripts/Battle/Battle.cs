using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine.Events;

public class Battle : NetworkBehaviour {
   #region Public Variables

   // The sides of the battle
   public enum TeamType { None = 0, Attackers = 1, Defenders = 2 };

   // The result of the last Battle tick
   public enum TickResult { None, BattleOver }

   // The unique ID assigned to this battle
   [SyncVar]
   public int battleId;

   // The difficulty level set for this battle
   [SyncVar]
   public int difficultyLevel;

   // If this is a pvp battle
   [SyncVar]
   public bool isPvp;

   // The Biome Type this Battle is in
   [SyncVar]
   public Biome.Type biomeType;

   // The Area that this Battle started in
   public Area area;

   // The Battle Board for this Battle
   public BattleBoard battleBoard;

   // The team that has won (server only)
   public TeamType teamThatWon = TeamType.None;

   // The list of Battlers that are attacking
   public SyncList<int> attackers = new SyncList<int>();

   // The list of Battlers that are defending
   public SyncList<int> defenders = new SyncList<int>();

   // Local events to execute whenever we finish a battle. (hiding battle UI for example)
   [HideInInspector] public UnityEvent onBattleEnded = new UnityEvent();

   // The max number of enemies
   public const int MAX_ENEMY_COUNT = 6;

   // The queued rpc action executed by the battle manager
   public List<QueuedRpcAction> queuedRpcActionList = new List<QueuedRpcAction>();

   #endregion

   public void Start () {
      // Make the client keep track of the Battle when it gets created
      BattleManager.self.storeBattle(this);

      // Keep track of the Battle Board we're using
      this.battleBoard = BattleManager.self.battleBoard;

      // Set battle end UI events
      onBattleEnded.AddListener(()=> {
         BattleUIManager.self.disableBattleUI();
      });

      clientBattleReposition();
   }

   // Accommodates the battle item with their respective battlers, if no parent is set
   private void clientBattleReposition () {
      if (transform.parent == null) {
         transform.SetParent(BattleManager.self.transform, true);
      }

      try {
         foreach (Battler battler in BattleManager.self.getBattle(battleId).getParticipants()) {
            if (battler.transform.parent == null) {
               battler.transform.SetParent(transform, true);
               battler.snapToBattlePosition();
            }
         }
      } catch {
         D.editorLog("Battle has not loaded yet: " + battleId + " : {" + BattleManager.self.getBattle(battleId) + "}", Color.red);
         StartCoroutine(CO_RetryRepositioning());
      }
   }

   private IEnumerator CO_RetryRepositioning () {
      yield return new WaitForSeconds(1);
      clientBattleReposition();
   }
   
   public TickResult tick () {
      // Everything below here is only valid for the server
      if (!NetworkServer.active) {
         return TickResult.None;
      }

      // Check if a queued action end time has already lapsed, if so then remove from list
      List<QueuedRpcAction> queuedRpcActionToRemove = new List<QueuedRpcAction>();
      foreach (QueuedRpcAction queuedRpc in queuedRpcActionList) {
         if (NetworkTime.time > queuedRpc.actionEndTime) {
            queuedRpcActionToRemove.Add(queuedRpc);
         }
      }
      foreach (QueuedRpcAction actionToDelete in queuedRpcActionToRemove) {
         queuedRpcActionList.Remove(actionToDelete);
      }

      // Cycle over all of the participants in the battle
      foreach (Battler battler in getParticipants()) {
         if (battler.transform.parent == null) {
            battler.transform.SetParent(transform, false);
         }

         float bufferedCooldown = (float) battler.cooldownEndTime + MONSTER_ATTACK_BUFFER;

         // Dead battlers don't do anything
         if (battler.isDead()) {
            continue;
         }

         if (battler.debuffList.Count > 0) {
            Dictionary<Status.Type, float> newDebuffData = new Dictionary<Status.Type, float>();
            List<Status.Type> endedDebuffs = new List<Status.Type>();
            foreach (KeyValuePair<Status.Type, float> debuffData in battler.debuffList) {
               if (debuffData.Value < 1 && debuffData.Value >= 0) {
                  // Mark the debuffs that recently ended for future game logic such as in game notification
                  if (!endedDebuffs.Contains(debuffData.Key)) {
                     endedDebuffs.Add(debuffData.Key);
                  }
               } else { 
                  // Deduct the timer value based on the tick interval
                  Status.Type currentStatusType = debuffData.Key;
                  float newValue = debuffData.Value - BattleManager.TICK_INTERVAL;

                  // Add debuff to the new list that will override the synclist
                  newDebuffData.Add(currentStatusType, newValue);
               } 
            }

            if (newDebuffData.Count > 0) {
               // Override the debuff synclist with the new one
               battler.debuffList.Clear();
               foreach (KeyValuePair<Status.Type, float> debuffData in newDebuffData) {
                  // For logging purposes
                  // D.debug("Updated value of {" + debuffData.Key + "} to {" + debuffData.Value + "}");
                  battler.debuffList.Add(debuffData.Key, debuffData.Value);
               }
            } else {
               // Clear the debuff list if the new debuff data is blank
               if (battler.debuffList.Count > 0) {
                  battler.debuffList.Clear();
               }
            }
         }

         // Basic Monster AI.
         // If any of our monsters are ready to attack, then do so
         if (battler.isMonster() && NetworkTime.time > bufferedCooldown) {
            BattlePlan battlePlan = battler.getBattlePlan(this);
            BattlerData battlerData = MonsterManager.self.getBattlerData(battler.enemyType);
            List<Battler> battlerAllies = new List<Battler>();
            int lowHpAllies = battlePlan.targetAllies.FindAll(_ => _.health < _.getStartingHealth(_.enemyType)).Count;

            if (lowHpAllies > 0 && battlePlan.targetAllies.Count > 0) {
               Battler lowestHpBattler = battlePlan.targetAllies[0];
               float lowestHealth = -1;
               foreach (Battler allyBattler in battlePlan.targetAllies) {
                  if (lowestHealth < 0) {
                     // Assigns the default battler to heal
                     lowestHpBattler = allyBattler;
                     lowestHealth = allyBattler.health;
                  } else {
                     // Assigns the battler with the lower hp in percentage as the default battler to heal
                     float selectedAllyHealthRatio = allyBattler.health / allyBattler.getStartingHealth(allyBattler.enemyType);
                     float newAllyHealthRatio = lowestHpBattler.health / lowestHpBattler.getStartingHealth(lowestHpBattler.enemyType);
                     if (selectedAllyHealthRatio < newAllyHealthRatio) {
                        lowestHpBattler = allyBattler;
                        lowestHealth = allyBattler.health;
                     }
                  }
               }
               battlerAllies.Add(lowestHpBattler);
            }

            // If we couldn't find a valid target, then move on
            if (battlePlan == null || battlePlan.targets == null || battlePlan.targets.Count == 0) {
               continue;
            }

            // Handles the current and only attack a monster can do
            if (battlerData.isSupportType && lowHpAllies > 0) {
               BattleManager.self.executeBattleAction(this, battler, battlerAllies, 0, AbilityType.BuffDebuff);
            } else {
               if (battler.isBossType) {
                  int actionRandomizer = Random.Range(1, 10);

                  // 50% chance to use AOE ability
                  if (actionRandomizer > 5 && battler.basicAbilityIDList.Count > 1) {
                     int targetCounter = 0;
                     List<Battler> targetBattlers = new List<Battler>();
                     AttackAbilityData abilityData = AbilityManager.self.getAttackAbility(battler.basicAbilityIDList[1]);
                     battlePlan.targets = new List<Battler>();
                     D.adminLog("1. Boss fetching ability :: " +
                        "Name:{" + abilityData.itemName + "} " +
                        "ID1:{" + battler.basicAbilityIDList[1] + "} " +
                        "ID2:{" + abilityData.itemID + "} " +
                        "MaxTarget: {" + abilityData.maxTargets + "}", D.ADMIN_LOG_TYPE.Boss);

                     // Setup the maximum targets affected by this ability
                     foreach (Battler attacker in getAttackers()) {
                        targetBattlers.Add(attacker);
                        battlePlan.targets.Add(attacker);
                        D.adminLog("2. Added attacker target {" + attacker.userId + " : " + attacker.enemyType + "}", D.ADMIN_LOG_TYPE.Boss);
                        targetCounter++;
                        if (targetCounter >= abilityData.maxTargets) {
                           break;
                        }
                     }

                     D.adminLog("3. Boss is attacking using AOE attack having {" + battlePlan.targets.Count + "} targets, " +
                        "AttackerCount:{" + getAttackers().Count + "}", D.ADMIN_LOG_TYPE.Boss);
                     BattleManager.self.executeBattleAction(this, battler, targetBattlers, 1, AbilityType.Standard);
                  } else {
                     D.adminLog("Boss is attacking using regular attack", D.ADMIN_LOG_TYPE.Boss);
                     BattleManager.self.executeBattleAction(this, battler, battlePlan.targets, 0, AbilityType.Standard);
                  }
               } else {
                  BattleManager.self.executeBattleAction(this, battler, battlePlan.targets, 0, AbilityType.Standard);
               }
            }
         } else {
            // Enable log here if there are abnormalities with the enemy AI not attacking
            //D.debug("Battler cant process this ! " + battler.isMonster() + " " + (NetworkTime.time + " > " + battler.animatingUntil) + " " +( NetworkTime.time + " > " + bufferedCooldown));
         }
      }

      // Check if the battle is over
      teamThatWon = getTeamThatWon();

      // If someone just won this tick, then let the clients know
      if (teamThatWon != TeamType.None) {
         return TickResult.BattleOver;
      }

      return TickResult.None;
   }

   public Battler getBattler (int userId) {
      foreach (Battler battler in getParticipants()) {
         if (battler == null) {
            continue;
         }

         if (battler.userId == userId) {
            return battler;
         }
      }

      return null;
   }

   public List<Battler> getParticipants () {
      List<Battler> participants = new List<Battler>();

      participants.AddRange(getAttackers());
      participants.AddRange(getDefenders());

      return participants;
   }

   public List<Battler> getAttackers () {
      List<Battler> list = new List<Battler>();

      foreach (int userId in attackers) {
         list.Add(BattleManager.self.getBattler(userId));
      }

      return list;
   }

   public List<Battler> getDefenders () {
      List<Battler> list = new List<Battler>();

      foreach (int userId in defenders) {
         list.Add(BattleManager.self.getBattler(userId));
      }

      return list;
   }

   public List<Battler> getTeam (TeamType teamType) {
      if (teamType == TeamType.Attackers) {
         return getAttackers();
      } else if (teamType == TeamType.Defenders) {
         return getDefenders();
      }

      return new List<Battler>();
   }

   public void resetAllBattleIDs (Battle.TeamType winningTeam) {
      // Remove the Battle ID for any participants
      foreach (Battler participant in this.getParticipants()) {
         if (participant == null) {
            continue;
         }

         if (participant.player != null) {
            // If the enemy wins, reset all battle id for all participants
            // If players win, reset battle id for only players. Enemies should not have a valid battle id anymore so that they cant be engaged in combat
            if (winningTeam == TeamType.Defenders || participant.player is PlayerBodyEntity || (winningTeam == TeamType.Attackers && participant.isAttacker())) {
               participant.player.battleId = 0;
               if (participant.enemyType != Enemy.Type.PlayerBattler) {
                  D.adminLog("{" + participant.battleId + "} End battle state for enemy {" + participant.enemyType + "}", D.ADMIN_LOG_TYPE.CombatEnd);
               } else { 
                  participant.player.rpc.Target_ResetMoveDisable(participant.player.connectionToClient);
               }
            }
         }
      }
   }

   public bool hasRoomLeft (TeamType teamType) {
      List<Battler> battlers = new List<Battler>();

      if (teamType == TeamType.Attackers) {
         battlers.AddRange(getAttackers());
      } else if (teamType == TeamType.Defenders) {
         battlers.AddRange(getDefenders());
      }

      return (battlers.Count < 6);
   }

   public bool isOver () {
      return teamThatWon != TeamType.None;
   }

   public bool isTeamDead (TeamType teamType) {
      List<Battler> battlers = new List<Battler>();

      // Add the appropriate Battlers to our list
      if (teamType == TeamType.Attackers) {
         battlers.AddRange(getAttackers());
      } else if (teamType == TeamType.Defenders) {
         battlers.AddRange(getDefenders());
      }

      // Check if any of the Battlers are still alive
      foreach (Battler battler in battlers) {
         if (!battler.isDead()) {
            return false;
         }
      }

      return true;
   }

   public double getTimeToWait (Battler sourceBattler, List<Battler> targetBattlers) {
      double timeToWait = 0f;

      // Check if our sprite or the target sprite is busy being animated
      foreach (Battler targetBattler in targetBattlers) {
         bool isSourceBattlerAnimating = sourceBattler.animatingUntil > NetworkTime.time;
         bool isTargetBattlerAnimating = targetBattler.animatingUntil > NetworkTime.time;
         if (isSourceBattlerAnimating || isTargetBattlerAnimating) {
            // Check if the source or the target has the longer wait time
            double diff = Util.maxDouble(sourceBattler.animatingUntil - NetworkTime.time, targetBattler.animatingUntil - NetworkTime.time);

            // Set our timeToWait to the largest value we've found so far
            timeToWait = Util.maxDouble(timeToWait, diff);
         } 
      }

      return timeToWait;
   }

   public TeamType getTeamThatWon () {
      int attackersAlive = 0;
      int defendersAlive = 0;

      // Check if there are any attackers alive
      foreach (Battler attacker in getAttackers()) {
         if (!attacker.isDead()) {
            attackersAlive++;
            break;
         }
      }

      if (attackersAlive == 0) {
         return TeamType.Defenders;
      }

      // Check if there are any defenders alive
      foreach (Battler defender in getDefenders()) {
         if (!defender.isDead()) {
            defendersAlive++;
            break;
         }
      }

      if (defendersAlive == 0) {
         return TeamType.Attackers;
      }

      return TeamType.None;
   }

   public static float getDistance (Battler source, Battler target) {
      Vector2 sourcePosition = source.battleSpot.transform.position;
      Vector2 targetPosition = target.battleSpot.transform.position;

      return Vector2.Distance(sourcePosition, targetPosition);
   }

   [ClientRpc]
   public void Rpc_SendCombatAction (string[] actionStrings, BattleActionType battleActionType, bool cancelAbility) {
      processCombatAction(actionStrings, battleActionType, cancelAbility);
   }

   public void processCombatAction (string[] actionStrings, BattleActionType battleActionType, bool cancelAbility, bool isQueuedAction = false) {
      List<BattleAction> actionList = new List<BattleAction>();
      BattleAction actionToSend = null;
      
      // TODO: Investigate instances wherein this value is blank
      if (actionStrings.Length < 1) {
         D.debug("ERROR HERE! There are no action strings for this combat action: " + battleActionType);
         return;
      }
      if (battleActionType == BattleActionType.Stance) {
         // Stance action change
         try {
            actionToSend = StanceAction.deserialize(actionStrings[0]);
            AbilityManager.self.execute((StanceAction) actionToSend);
         } catch {
            D.debug("Error here! Something went wrong with the stance action deserialization");
         }
      } else if (battleActionType != BattleActionType.UNDEFINED) {
         // Standard attack
         foreach (string actionString in actionStrings) {
            switch (battleActionType) {
               case BattleActionType.Attack:
                  if (cancelAbility) {
                     actionToSend = CancelAction.deseralize(actionString);
                     actionToSend.battleActionType = BattleActionType.Cancel;
                  } else {
                     actionToSend = AttackAction.deseralize(actionString);
                  }
                  actionToSend.isQueuedAction = isQueuedAction;
                  actionList.Add(actionToSend);
                  break;
               case BattleActionType.BuffDebuff:
                  if (cancelAbility) {
                     actionToSend = CancelAction.deseralize(actionString);
                     actionToSend.battleActionType = BattleActionType.Cancel;
                  } else {
                     actionToSend = BuffAction.deseralize(actionString);
                  }
                  actionToSend.isQueuedAction = isQueuedAction;
                  actionList.Add(actionToSend);
                  break;
            }
         }
         AbilityManager.self.execute(actionList.ToArray());
      } 
   }

   [ClientRpc]
   public void Rpc_ReceiveCancelAction (int battleId, int sourceId, int targetId, double time, float timeToSubtract) {
      CancelAction cancelAction = new CancelAction(battleId, sourceId, targetId, time, timeToSubtract);
      AbilityManager.self.execute(new[] { cancelAction });
   }

   private void OnDestroy () {
      onBattleEnded.Invoke();
   }

   #region Private Variables

   // A small buffer time we use to make monsters wait slightly longer than their attack cooldowns
   protected static float MONSTER_ATTACK_BUFFER = 1.5f;

   #endregion
}
