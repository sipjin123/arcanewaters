using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

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

   #endregion

   public void Start () {
      // Make the client keep track of the Battle when it gets created
      BattleManager.self.storeBattle(this);

      // Keep track of the Battle Board we're using
      this.battleBoard = BattleManager.self.getBattleBoard(this.biomeType);
   }

   public TickResult tick () {
      // Everything below here is only valid for the server
      if (!NetworkServer.active) {
         return TickResult.None;
      }

      // Cycle over all of the participants in the battle
      foreach (Battler battler in getParticipants()) {
         float bufferedCooldown = battler.cooldownEndTime + MONSTER_ATTACK_BUFFER;

         // Dead battlers don't do anything
         if (battler.isDead()) {
            continue;
         }

         // If any of our monsters are ready to attack, then do so
         if (battler.isMonster() && Util.netTime() > battler.animatingUntil && Util.netTime() > bufferedCooldown) {
            BattlePlan battlePlan = battler.getBattlePlan(this);

            // If we couldn't find a valid target, then move on
            if (battlePlan == null || battlePlan.targets == null || battlePlan.targets.Count == 0) {
               continue;
            }

            // If there was a random target available, attack it
            if (battlePlan.ability is BuffAbility) {
               BattleManager.self.executeBuff(this, battler, battlePlan.targets, battlePlan.ability.type);
            } else {
               BattleManager.self.executeAttack(this, battler, battlePlan.targets, battlePlan.ability.type);
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

         participant.player.battleId = 0;
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

   public float getTimeToWait (Battler sourceBattler, List<Battler> targetBattlers) {
      float timeToWait = 0f;

      // Check if our sprite or the target sprite is busy being animated
      foreach (Battler targetBattler in targetBattlers) {
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
   public void Rpc_SendAttackAction (string[] actionStrings) {
      List<AttackAction> attackActionList = new List<AttackAction>();

      // Convert each of the object arrays into deserialized AttackActions
      foreach (string actionString in actionStrings) {
         AttackAction attackAction = AttackAction.deseralize(actionString);
         attackActionList.Add(attackAction);
      }

      // We just received an action from the server, so execute it at the assigned time
      AbilityManager.self.execute(attackActionList.ToArray());
   }

   [ClientRpc]
   public void Rpc_SendBuffAction (string[] actionStrings) {
      List<BuffAction> actionList = new List<BuffAction>();

      // Convert each of the object arrays into deserialized Actions
      foreach (string actionString in actionStrings) {
         BuffAction buffAction = BuffAction.deseralize(actionString);
         actionList.Add(buffAction);
      }

      // We just received an action from the server, so execute it at the assigned time
      AbilityManager.self.execute(actionList.ToArray());
   }

   #region Private Variables

   // A small buffer time we use to make monsters wait slightly longer than their attack cooldowns
   protected static float MONSTER_ATTACK_BUFFER = 1.0f;

   #endregion
}
