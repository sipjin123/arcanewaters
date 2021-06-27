using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class SilverManager : NetworkBehaviour
{
   #region Public Variables

   // In Pvp Battles this is the amount of silver that players have at the beginning of the match
   public static int SILVER_PLAYER_INITIAL_AMOUNT = 0;

   // How much silver is gained for each fill in a pvp battle.
   public static int SILVER_KILL_REWARD = 100;

   // Self
   public static SilverManager self;
   #endregion

   public void Awake () {
      initialize();
      self = this;
   }

   public void initialize () {
      silverRegistry = new Dictionary<int, SilverInfo>();
   }

   public SilverInfo createDefaultSilverInfo () {
      return new SilverInfo {
         amount = SILVER_PLAYER_INITIAL_AMOUNT,
         rank = 1
      };
   }

   [Server]
   public void removePlayer (int playerId) {
      if (silverRegistry != null && silverRegistry.ContainsKey(playerId)) {
         silverRegistry.Remove(playerId);
      }
   }

   [Server]
   public void addPlayer (int playerId) {
      if (silverRegistry != null && !silverRegistry.ContainsKey(playerId)) {
         silverRegistry.Add(playerId, createDefaultSilverInfo());
      }
   }

   [Server]
   public void setSilverAmount (int playerId, int newAmount) {
      if (silverRegistry != null && silverRegistry.ContainsKey(playerId)) {
         SilverInfo info = silverRegistry[playerId];
         info.amount = newAmount;
      }
   }

   [Server]
   public void setSilverRank (int playerId, int newRank) {
      if (silverRegistry != null && silverRegistry.ContainsKey(playerId)) {
         SilverInfo info = silverRegistry[playerId];
         info.rank = newRank;
      }
   }

   [Server]
   public SilverInfo getSilverInfo () {
      return getSilverInfo(Global.player.userId);
   }

   [Server]
   public SilverInfo getSilverInfo (int playerId) {
      SilverInfo info = null;
      if (silverRegistry != null && silverRegistry.ContainsKey(playerId)) {
         info = silverRegistry[playerId];
      }
      return info;
   }


   #region Private Variables

   // Dictionary that stores the silver information for each player, indexed by the player id.
   private Dictionary<int, SilverInfo> silverRegistry;

   #endregion

   public class SilverInfo
   {
      #region Public Variables

      // The amount of silver owned by a player
      public int amount;

      // The current silver level. Used in pvp battles.
      public int rank;

      #endregion
   }
}
