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
   public enum TickResult { None, BattleOver, Missing }

   // The unique ID assigned to this battle
   [SyncVar]
   public int battleId;

   // The difficulty level set for this battle
   [SyncVar]
   public int difficultyLevel;

   // Determines the number of player count which will be used to scale up the difficulty based on party member size
   [SyncVar]
   public int partyMemberCount = 1;

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

   // The damage per tick of the burn status in percentage (1% per tick)
   public const float BURN_DAMAGE_PER_TICK_PERCENTAGE = .01f;

   // The damage per tick of the poison status in percentage (2.5% per tick)
   public const float POISON_DAMAGE_PER_TICK_PERCENTAGE = .025f;

   // Completes the opposing team, by filling the empty battle spots
   public static bool FORCE_COMPLETE_DEFENDING_TEAM = false;

   // The reference of the triggered enemy combat
   public Enemy enemyReference;

   // Is the battle happening on a ship deck?
   public bool isShipBattle = false;

   #endregion

   public void Start () {
      // Make the client keep track of the Battle when it gets created
      BattleManager.self.storeBattle(this);

      // Keep track of the Battle Board we're using
      this.battleBoard = BattleManager.self.battleBoard;

      // Set battle end UI events
      onBattleEnded.AddListener(() => {
         BattleUIManager.self.disableBattleUI();
      });

      clientBattleReposition();
   }

   // Accommodates the battle item with their respective battlers, if no parent is set
   private void clientBattleReposition () {
      if (transform.parent == null) {
         transform.SetParent(BattleManager.self.transform, true);
      }
      Battle battleInfo = BattleManager.self.getBattle(battleId);
      if (battleInfo == null) {
         D.editorLog("Battle has not loaded yet: " + battleId + " : {" + BattleManager.self.getBattle(battleId) + "}", Color.red);
         StartCoroutine(CO_RetryRepositioning());
      } else {
         foreach (Battler battler in battleInfo.getParticipants()) {
            if (battler == null) {
               StartCoroutine(CO_RetryRepositioning());
               break;
            } else {
               if (battler.transform.parent == null) {
                  battler.transform.SetParent(transform, true);
                  battler.snapToBattlePosition();
               }
            }
         }
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

      if (getParticipants().FindAll(_ => _.health > 0 && _.displayedHealth > 0).Count < 1 || getParticipants().Count < 1) {
         D.debug("There are no more battlers alive at this point!");
         return TickResult.Missing;
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
         if (battler == null) {
            D.debug("Warning! A battler in battle:{" + battleId + "} is corrupted!");
            continue;
         }

         if (battler.transform.parent == null) {
            battler.transform.SetParent(transform, false);
         }

         float bufferedCooldown = (float) battler.cooldownEndTime + MONSTER_ATTACK_BUFFER;

         // Dead battlers don't do anything
         if (battler.isDead()) {
            if (!getDeadBattlersPreviousTick().Contains(battler)) {
               BattleManager.self.onBattlerDeath(battler);
               getDeadBattlersPreviousTick().Add(battler);
            }

            continue;
         }

         if (battler.debuffList.Count > 0) {
            Dictionary<Status.Type, StatusData> newDebuffData = new Dictionary<Status.Type, StatusData>();
            List<Status.Type> endedDebuffs = new List<Status.Type>();
            foreach (KeyValuePair<Status.Type, StatusData> debuffData in battler.debuffList) {
               if (debuffData.Value.statusDuration < 1 && debuffData.Value.statusDuration >= 0) {
                  // Mark the debuffs that recently ended for future game logic such as in game notification
                  if (!endedDebuffs.Contains(debuffData.Key)) {
                     endedDebuffs.Add(debuffData.Key);

                     switch (debuffData.Key) {
                        case Status.Type.Burning:

                           break;
                        case Status.Type.Stunned:
                           battler.isDisabledByDebuff = false;
                           break;
                     }
                  }
               } else { 
                  // Deduct the timer value based on the tick interval
                  Status.Type currentStatusType = debuffData.Key;
                  float newValue = debuffData.Value.statusDuration - BattleManager.TICK_INTERVAL;
                  int damagePerTick = 1;
                  switch (currentStatusType) {
                     case Status.Type.Burning:
                        if (battler.health > 1) {
                           damagePerTick = (int) (battler.getStartingHealth() * BURN_DAMAGE_PER_TICK_PERCENTAGE);
                           battler.health -= damagePerTick;
                           battler.displayedHealth -= damagePerTick;
                           battler.damageTicks += damagePerTick;
                           Rpc_DealDamagePerTick(battleId, battler.userId, damagePerTick, Element.Fire);
                        }

                        // TODO: Enable this if we now allow burn tick to kill enemy
                        /*
                        if (battler.health < 1) {
                           // Register burn achievement
                           if (debuffData.Value.casterId > 0) {
                              AchievementManager.registerUserAchievement(debuffData.Value.casterId, ActionType.BurnEnemy);
                           }
                           battler.isAlreadyDead = true;
                        }*/
                        break;
                     case Status.Type.Poisoned:
                        if (battler.health > 1) {
                           // Boss battlers are more resistant against poison damage ticks
                           float bossDamageMultiplier = battler.isBossType ? .5f : 1f;
                           damagePerTick = (int) (battler.health * (POISON_DAMAGE_PER_TICK_PERCENTAGE * bossDamageMultiplier));
                           battler.health -= damagePerTick;
                           battler.displayedHealth -= damagePerTick;
                           battler.damageTicks += damagePerTick;
                           Rpc_DealDamagePerTick(battleId, battler.userId, damagePerTick, Element.Poison);
                        }

                        // TODO: Enable this if we now allow poison tick to kill enemy
                        /*
                        if (battler.health < 1) {
                           // Register poison achievement
                           if (debuffData.Value.casterId > 0) {
                              AchievementManager.registerUserAchievement(debuffData.Value.casterId, ActionType.Poisoned);
                           }
                           battler.isAlreadyDead = true;
                        }*/
                        break;
                     case Status.Type.Stunned:
                        battler.isDisabledByDebuff = true;
                        break;
                  }

                  // Add debuff to the new list that will override the synclist
                  newDebuffData.Add(currentStatusType, new StatusData {
                     abilityIdReference = debuffData.Value.abilityIdReference,
                     statusDuration = newValue,
                     casterId = debuffData.Value.casterId
                  });
               } 
            }

            if (newDebuffData.Count > 0) {
               // Override the debuff synclist with the new one
               battler.debuffList.Clear();
               foreach (KeyValuePair<Status.Type, StatusData> debuffData in newDebuffData) {
                  // For logging purposes
                  // D.debug("Updated value of {" + debuffData.Key + "} to {" + debuffData.Value + "}");
                  battler.debuffList.Add(debuffData.Key, new StatusData {
                     abilityIdReference = debuffData.Value.abilityIdReference,
                     statusDuration = debuffData.Value.statusDuration,
                     casterId = debuffData.Value.casterId
                  });
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
                  if (!battler.isDisabledByStatus()) {
                     if (battler.useSpecialAttack && battler.basicAbilityIDList.Count > 1) {
                        battler.useSpecialAttack = false;
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
                        battler.useSpecialAttack = true;
                        D.adminLog("Boss is attacking using regular attack", D.ADMIN_LOG_TYPE.Boss);
                        BattleManager.self.executeBattleAction(this, battler, battlePlan.targets, 0, AbilityType.Standard);
                     }
                  } else { 
                     // Do logic here if enemy is disabled by status, possibly launch a visual feedback to the clients
                  }
               } else {
                  if (!battler.isDisabledByStatus()) {
                     BattleManager.self.executeBattleAction(this, battler, battlePlan.targets, 0, AbilityType.Standard);
                  } else { 
                     D.adminLog("This battler {" + battler.enemyType + "} is disabled, skipping action", D.ADMIN_LOG_TYPE.CombatStatus);
                  }
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

   public List<Battler> getParticipants (bool logData = false) {
      List<Battler> participants = new List<Battler>();

      participants.AddRange(getAttackers(logData));
      participants.AddRange(getDefenders());

      return participants;
   }

   public List<Battler> getDeadBattlers () {
     return getParticipants().FindAll(_ => _.isDead());
   }

   public List<Battler> getDeadBattlersPreviousTick () {
      if (_previousDeadBattlers == null) {
         _previousDeadBattlers = new List<Battler>();
      }

      return _previousDeadBattlers;
   }

   public List<Battler> getAttackers (bool logData = false) {
      List<Battler> list = new List<Battler>();
      foreach (int userId in attackers) {
         Battler battlerRef = BattleManager.self.getBattler(userId);
         if (battlerRef != null) {
            list.Add(battlerRef);
         } else {
            if (logData) {
               D.debug("Missing battler reference: {" + userId + "}");
            }
         }
      }

      return list;
   }

   public List<Battler> getDefenders () {
      List<Battler> list = new List<Battler>();

      foreach (int userId in defenders) {
         Battler battlerRef = BattleManager.self.getBattler(userId);
         if (battlerRef != null) {
            list.Add(battlerRef);
         }
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
      string playerIds = "Players: {";

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
               if (participant.enemyType == Enemy.Type.PlayerBattler) {
                  bool isVoyageTreasureSite = participant.player.isInGroup() && VoyageManager.isTreasureSiteArea(participant.player.areaKey);

                  if (isVoyageTreasureSite || (participant.teamType == winningTeam && participant.enemyType == Enemy.Type.PlayerBattler)) {
                     // Allow refresh movement when the winning battler is a player or when respawning in a voyage treasure site
                     playerIds += ": " + participant.player.userId;
                  }
               }
            }
         }
      }
      D.adminLog("3. {" + battleId + "} Movement Restored, PlayerCount: {" + this.getParticipants().Count + "} " + playerIds + "}", D.ADMIN_LOG_TYPE.CombatEnd);
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
   public void Rpc_DealDamagePerTick (int battleId, int battlerId, int damage, Element element) {
      Battle battle = BattleManager.self.getBattle(battleId);
      if (battle == null) {
         return;
      }
      Battler target = battle.getBattler(battlerId);
      if (target == null) {
         return;
      }

      // Display 1 damage as minimum
      if (damage < 1) {
         damage = 1;
      }
      BattleUIManager.self.showDamagePerTick(target, damage, element);
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

   // The set of battlers that died so far
   protected List<Battler> _previousDeadBattlers;

   #endregion
}
