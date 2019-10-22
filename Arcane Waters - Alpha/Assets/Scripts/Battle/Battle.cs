using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
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
   public SyncListInt attackers = new SyncListInt();

   // The list of Battlers that are defending
   public SyncListInt defenders = new SyncListInt();

   // Local events to execute whenever we finish a battle. (hiding battle UI for example)
   [HideInInspector] public UnityEvent onBattleEnded = new UnityEvent();

   #endregion

   public void Start () {
      // Make the client keep track of the Battle when it gets created
      BattleManager.self.storeBattle(this);

      // Keep track of the Battle Board we're using
      this.battleBoard = BattleManager.self.getBattleBoard(this.biomeType);

      // Set battle end UI events
      onBattleEnded.AddListener(BattleUIManager.self.disableBattleUI);

      clientBattleReposition();
   }

   // Accommodates the battle item with their respective battlers, if no parent is set
   private void clientBattleReposition () {
      if (transform.parent == null) {
         transform.SetParent(BattleManager.self.transform, true);
      }

      foreach (BattlerBehaviour battler in BattleManager.self.getBattle(battleId).getParticipants()) {
         if (battler.transform.parent == null) {
            battler.transform.SetParent(transform, false);
         }
      }
   }
   
   public TickResult tick () {
      // Everything below here is only valid for the server
      if (!NetworkServer.active) {
         return TickResult.None;
      }

      // Cycle over all of the participants in the battle
      foreach (BattlerBehaviour battler in getParticipants()) {

         if (battler.transform.parent == null) {
            battler.transform.SetParent(transform, false);
         }

         float bufferedCooldown = battler.cooldownEndTime + MONSTER_ATTACK_BUFFER;

         // Dead battlers don't do anything
         if (battler.isDead()) {
            continue;
         }

         // Basic Monster AI.
         // If any of our monsters are ready to attack, then do so
         if (battler.isMonster() && Util.netTime() > battler.animatingUntil && Util.netTime() > bufferedCooldown) {
            BattlePlan battlePlan = battler.getBattlePlan(this);

            // If we couldn't find a valid target, then move on
            if (battlePlan == null || battlePlan.targets == null || battlePlan.targets.Count == 0) {
               continue;
            }

            // Handles the current and only attack a monster can do
            BattleManager.self.executeBattleAction(this, battler, battlePlan.targets, 0);
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

   public BattlerBehaviour getBattler (int userId) {
      foreach (BattlerBehaviour battler in getParticipants()) {
         if (battler.userId == userId) {
            return battler;
         }
      }

      return null;
   }

   public List<BattlerBehaviour> getParticipants () {
      List<BattlerBehaviour> participants = new List<BattlerBehaviour>();

      participants.AddRange(getAttackers());
      participants.AddRange(getDefenders());

      return participants;
   }

   public List<BattlerBehaviour> getAttackers () {
      List<BattlerBehaviour> list = new List<BattlerBehaviour>();

      foreach (int userId in attackers) {
         list.Add(BattleManager.self.getBattler(userId));
      }

      return list;
   }

   public List<BattlerBehaviour> getDefenders () {
      List<BattlerBehaviour> list = new List<BattlerBehaviour>();

      foreach (int userId in defenders) {
         list.Add(BattleManager.self.getBattler(userId));
      }

      return list;
   }

   public List<BattlerBehaviour> getTeam (TeamType teamType) {
      if (teamType == TeamType.Attackers) {
         return getAttackers();
      } else if (teamType == TeamType.Defenders) {
         return getDefenders();
      }

      return new List<BattlerBehaviour>();
   }

   public void resetAllBattleIDs () {
      // Remove the Battle ID for any participants
      foreach (BattlerBehaviour participant in this.getParticipants()) {
         if (participant == null) {
            continue;
         }

         participant.player.battleId = 0;
      }
   }

   public bool hasRoomLeft (TeamType teamType) {
      List<BattlerBehaviour> battlers = new List<BattlerBehaviour>();

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
      List<BattlerBehaviour> battlers = new List<BattlerBehaviour>();

      // Add the appropriate Battlers to our list
      if (teamType == TeamType.Attackers) {
         battlers.AddRange(getAttackers());
      } else if (teamType == TeamType.Defenders) {
         battlers.AddRange(getDefenders());
      }

      // Check if any of the Battlers are still alive
      foreach (BattlerBehaviour battler in battlers) {
         if (!battler.isDead()) {
            return false;
         }
      }

      return true;
   }

   public float getTimeToWait (BattlerBehaviour sourceBattler, List<BattlerBehaviour> targetBattlers) {
      float timeToWait = 0f;

      // Check if our sprite or the target sprite is busy being animated
      foreach (BattlerBehaviour targetBattler in targetBattlers) {
         if (sourceBattler.animatingUntil > Util.netTime() || targetBattler.animatingUntil > Util.netTime()) {
            // Check if the source or the target has the longer wait time
            float diff = Mathf.Max(sourceBattler.animatingUntil - Util.netTime(), targetBattler.animatingUntil - Util.netTime());

            // Set our timeToWait to the largest value we've found so far
            timeToWait = Mathf.Max(timeToWait, diff);
         }
      }

      return timeToWait;
   }

   public TeamType getTeamThatWon () {
      int attackersAlive = 0;
      int defendersAlive = 0;

      // Check if there are any attackers alive
      foreach (BattlerBehaviour attacker in getAttackers()) {
         if (!attacker.isDead()) {
            attackersAlive++;
            break;
         }
      }

      if (attackersAlive == 0) {
         return TeamType.Defenders;
      }

      // Check if there are any defenders alive
      foreach (BattlerBehaviour defender in getDefenders()) {
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

   public static float getDistance (BattlerBehaviour source, BattlerBehaviour target) {
      Vector2 sourcePosition = source.battleSpot.transform.position;
      Vector2 targetPosition = target.battleSpot.transform.position;

      return Vector2.Distance(sourcePosition, targetPosition);
   }

   [ClientRpc]
   public void Rpc_SendCombatAction (string[] actionStrings, BattleActionType type) {
      List<BattleAction> actionList = new List<BattleAction>();
      BattleAction actionToSend = null;

      if (type == BattleActionType.Stance) {
         // Stance action change
         actionToSend = StanceAction.deserialize(actionStrings[0]);
         AbilityManager.self.execute((StanceAction)actionToSend);

      } else if (type != BattleActionType.UNDEFINED) {
         // Standard attack
         foreach (string actionString in actionStrings) {
            switch (type) {
               case BattleActionType.Attack:
                  actionToSend = AttackAction.deseralize(actionString);
                  actionList.Add(actionToSend);
                  break;
               case BattleActionType.BuffDebuff:
                  actionToSend = BuffAction.deseralize(actionString);
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
   protected static float MONSTER_ATTACK_BUFFER = 1.0f;

   #endregion
}
