using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using UnityEngine.Events;
using UnityEngine.UIElements;

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

      // Cycle over all of the participants in the battle
      foreach (Battler battler in getParticipants()) {
         if (battler.transform.parent == null) {
            battler.transform.SetParent(transform, false);
         }

         float bufferedCooldown = (float)battler.cooldownEndTime + MONSTER_ATTACK_BUFFER;

         // Dead battlers don't do anything
         if (battler.isDead()) {
            continue;
         }

         // Basic Monster AI.
         // If any of our monsters are ready to attack, then do so
         if (battler.isMonster() && NetworkTime.time > battler.animatingUntil && NetworkTime.time > bufferedCooldown) {
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
               BattleManager.self.executeBattleAction(this, battler, battlePlan.targets, 0, AbilityType.Standard);
            }
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

   public void resetAllBattleIDs () {
      // Remove the Battle ID for any participants
      foreach (Battler participant in this.getParticipants()) {
         if (participant == null) {
            continue;
         }

         if (participant.player != null) {
            participant.player.battleId = 0;
            participant.player.resetCombatInit();
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
         if (sourceBattler.animatingUntil > NetworkTime.time || targetBattler.animatingUntil > NetworkTime.time) {
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

   public void inflictBossDamageToTeam (int damage) {
      foreach (Battler attackerEntity in getAttackers()) {
         StartCoroutine(CO_TriggerBattlerBossDamageReaction(attackerEntity.playerNetId, damage));
      }
   }

   private IEnumerator CO_TriggerBattlerBossDamageReaction (uint netId, int damage) {
      yield return new WaitForSeconds(.5f);
      Rpc_TriggerBattlerBossDamageReaction(netId, damage);
   }

   [ClientRpc]
   public void Rpc_TriggerBattlerBossDamageReaction (uint netId, int damage) {
      Battler battler = getAttackers().Find(_ => _.playerNetId == netId);
      if (battler != null) {
         battler.inflictBossDamage(damage);
      }
   }

   [ClientRpc]
   public void Rpc_TriggerBossAnimation (uint netId) {
      StartCoroutine(CO_TriggerBossAnimation(netId));
   }

   private IEnumerator CO_TriggerBossAnimation (uint netId) {
      yield return new WaitForSeconds(1.5f);
      Battler battler = getDefenders().Find(_ => _.playerNetId == netId);

      if (battler != null) {
         battler.modifyAnimSpeed(.15f);
         battler.playAnim(Anim.Type.BossAnimation);
         yield return new WaitForSeconds(3f);
         battler.modifyAnimSpeed(-1);
         battler.pauseAnim(false);
         battler.playAnim(Anim.Type.Idle_East);
      } else {
         D.debug("Failed to get battler for anim");
      }
   }

   [ClientRpc]
   public void Rpc_SendCombatAction (string[] actionStrings, BattleActionType battleActionType, bool cancelAbility) {
      List<BattleAction> actionList = new List<BattleAction>();
      BattleAction actionToSend = null;

      // TODO: Investigate instances wherein this value is blank
      if (actionStrings.Length < 1) {
         D.debug("ERROR HERE! There are no action strings for this combat action: " + battleActionType);
         return;
      }
      if (battleActionType == BattleActionType.Stance) {
         // Stance action change
         actionToSend = StanceAction.deserialize(actionStrings[0]);
         AbilityManager.self.execute((StanceAction) actionToSend);

      }  else if (battleActionType != BattleActionType.UNDEFINED) {
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
                  actionList.Add(actionToSend);
                  break;
               case BattleActionType.BuffDebuff:
                  if (cancelAbility) {
                     actionToSend = CancelAction.deseralize(actionString);
                     actionToSend.battleActionType = BattleActionType.Cancel;
                  } else {
                     actionToSend = BuffAction.deseralize(actionString);
                  }
                  actionList.Add(actionToSend);
                  break;
            }
         }
         AbilityManager.self.execute(actionList.ToArray());
      } 
   }

   private void OnDestroy () {
      onBattleEnded.Invoke();
   }

   #region Private Variables

   // A small buffer time we use to make monsters wait slightly longer than their attack cooldowns
   protected static float MONSTER_ATTACK_BUFFER = 1.5f;

   #endregion
}
