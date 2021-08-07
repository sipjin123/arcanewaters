﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SilverManager : NetworkBehaviour
{
   #region Public Variables

   // In Pvp Battles this is the amount of silver that players have at the beginning of the match
   public static int SILVER_PLAYER_INITIAL_AMOUNT = 0;

   // Amount of silver earned after destroying a player's ship
   public static int SILVER_PLAYER_SHIP_KILL_REWARD = 100;

   // Amount of silver earned after killing a sea monster
   public static int SILVER_SEA_MONSTER_KILL_REWARD = 30;

   // Amount of silver earned after destroying a sea structure
   public static int SILVER_SEA_STRUCTURE_KILL_REWARD = 400;

   // Amount of silver earned after destroying an enemy bot ship
   public static int SILVER_BOT_SHIP_KILL_REWARD = 20;

   // The fraction of silver earned by player who assisted a kill
   public static float SILVER_ASSIST_REWARD_MULTIPLIER = 0.25f;

   // The fraction of silver lost after death
   public static float SILVER_DEATH_PENALTY_MULTIPLIER = 0.40f;

   // The fraction of silver lost during the ship healing process
   public static int SILVER_SHIP_HEAL_PENALTY = 20;

   #endregion

   private static int computeClosestMultipleOfTen (float number) {
      return Mathf.CeilToInt(number / 10.0f) * 10;
   }

   public static int computeAssistReward (NetEntity mainAttacker, NetEntity assistAttacker, NetEntity target) {
      int computedReward = computeSilverReward(mainAttacker, target);
      float computedAssistRewardRaw = computedReward * SILVER_ASSIST_REWARD_MULTIPLIER;

      // Rounds to the next multiple of 10
      int computedAssistRewardRounded = computeClosestMultipleOfTen(computedAssistRewardRaw);
      return computedAssistRewardRounded * GameStatsManager.self.getSilverRank(assistAttacker.userId);
   }

   public static int computeSilverReward (NetEntity attacker, NetEntity target) {
      int attackerRank = GameStatsManager.self.getSilverRank(attacker.userId);

      if (target is PlayerShipEntity) {
         int targetRank = GameStatsManager.self.getSilverRank(target.userId);
         return (attackerRank + targetRank * 2) * SILVER_PLAYER_SHIP_KILL_REWARD;
      }

      if (target is BotShipEntity) {
         return attackerRank * SILVER_BOT_SHIP_KILL_REWARD;
      }

      if (target is SeaMonsterEntity) {
         return attackerRank * SILVER_SEA_MONSTER_KILL_REWARD;
      }

      if (target is SeaStructure) {
         return attackerRank * SILVER_SEA_STRUCTURE_KILL_REWARD;
      }

      return 0;
   }

   public static int computeSilverPenalty (NetEntity target) {
      if (target == null) {
         return 0;
      }

      int targetSilver = GameStatsManager.self.getSilverAmount(target.userId);

      if (targetSilver == 0) {
         return 0;
      }

      // The returned penalty is a positive integer
      return computeClosestMultipleOfTen(targetSilver * SILVER_DEATH_PENALTY_MULTIPLIER);
   }

   public static int computeHealSilverPenalty (NetEntity target) {
      if (target == null) {
         return 0;
      }

      return SILVER_SHIP_HEAL_PENALTY;
   }

   // The reason for awarding silver to a player
   public enum SilverRewardReason
   {
      // None
      None = 0,

      // The player is rewarded for a kill
      Kill = 1,

      // The player is rewarded for an assist
      Assist = 2,

      // The player is rewarded for dying during a battle. In this case the reward is negative
      Death = 3,

      // The player is being healed
      Heal = 4
   }
}
