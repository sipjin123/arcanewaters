using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class GameStatsManager : MonoBehaviour
{
   #region Public Variables

   // Reference to the singleton instance
   public static GameStatsManager self;

   // Storage for the Player's Game Stats
   public static GameStatsData gameStatsData = new GameStatsData();

   #endregion

   public void Awake () {
      self = this;

      gameStatsData = new GameStatsData {
         stats = new List<GameStats>(),
         isInitialized = true
      };
   }

   #region Stats

   public bool isUserRegistered (int userId) {
      if (gameStatsData == null || gameStatsData.stats == null) {
         return false;
      }
      return gameStatsData.stats.Find(_ => _.userId == userId) != null;
   }

   public void registerUser (int userId) {
      NetEntity entity = EntityManager.self.getEntity(userId);
      registerUser(userId, entity.entityName, PvpTeamType.None);
   }

   public void registerUser (int userId, PvpTeamType teamType) {
      NetEntity entity = EntityManager.self.getEntity(userId);
      registerUser(userId, entity.entityName, teamType);
   }

   public void registerUser (int userId, string userName, PvpTeamType teamType) {
      if (!isUserRegistered(userId)) {
         gameStatsData.stats.Add(new GameStats(userId, userName, (int) teamType));
      }
   }

   public void unregisterUser (int userId) {
      if (isUserRegistered(userId)) {
         gameStatsData.stats.RemoveAll(_ => _.userId == userId);
      }
   }

   public void clear () {
      if (gameStatsData.stats == null) {
         return;
      }
      gameStatsData.stats.Clear();
   }

   public void addPlayerKillCount (int userId) {
      GameStats playerStat = gameStatsData.stats.Find(_ => _.userId == userId);
      if (playerStat == null) {
         return;
      }

      // TODO: Implement visual indication here that will notify player of this specific stat gain

      // Stat Increase
      playerStat.PvpPlayerKills++;
      D.adminLog("Added player kills for: " + userId + " Total of: " + playerStat.PvpPlayerKills, D.ADMIN_LOG_TYPE.Pvp);
   }

   public void addShipKillCount (int userId) {
      GameStats playerStat = gameStatsData.stats.Find(_ => _.userId == userId);
      if (playerStat == null) {
         return;
      }

      // TODO: Implement visual indication here that will notify player of this specific stat gain

      // Stat Increase
      playerStat.playerShipKills++;
      D.adminLog("Added ship kills for: " + userId + " Total of: " + playerStat.playerShipKills, D.ADMIN_LOG_TYPE.Pvp);
   }

   public void addFlagCaptureCount (int userId) {
      GameStats playerStat = gameStatsData.stats.Find(_ => _.userId == userId);
      if (playerStat == null) {
         return;
      }

      // TODO: Implement visual indication here that will notify player of this specific stat gain

      // Stat Increase
      playerStat.flagCount++;
      D.adminLog("Added flag capture count for: " + userId + " Total of: " + playerStat.playerShipKills, D.ADMIN_LOG_TYPE.Pvp);
   }

   public void addMonsterKillCount (int userId) {
      GameStats playerStat = gameStatsData.stats.Find(_ => _.userId == userId);
      if (playerStat == null) {
         return;
      }

      // TODO: Implement visual indication here that will notify player of this specific stat gain

      // Stat Increase
      playerStat.playerMonsterKills++;
      D.adminLog("Added monster kills for: " + userId + " Total of: " + playerStat.playerMonsterKills, D.ADMIN_LOG_TYPE.Pvp);
   }

   public void addSilverAmount (int userId, int gain) {
      GameStats playerStat = gameStatsData.stats.Find(_ => _.userId == userId);
      if (playerStat == null) {
         return;
      }

      // TODO: Implement visual indication here that will notify player of this specific stat gain

      // Stat Increase
      playerStat.silver += gain;
      playerStat.silver = Mathf.Max(playerStat.silver, 0);
      D.adminLog("Added silver kills for: " + userId + " Total of: " + playerStat.silver, D.ADMIN_LOG_TYPE.Pvp);
   }

   public int getSilverAmount (int userId) {
      GameStats playerStat = gameStatsData.stats.Find(_ => _.userId == userId);
      if (playerStat == null) {
         return 0;
      }

      return playerStat.silver;
   }

   public void addSilverRank (int userId, int gain) {
      GameStats playerStat = gameStatsData.stats.Find(_ => _.userId == userId);
      if (playerStat == null) {
         return;
      }

      // Stat Increase
      playerStat.rank += gain;
      D.adminLog("Added silver rank for: " + userId + " Total of: " + playerStat.rank, D.ADMIN_LOG_TYPE.Pvp);
   }

   public void resetSilverRank (int userId) {
      GameStats playerStat = gameStatsData.stats.Find(_ => _.userId == userId);
      if (playerStat == null) {
         return;
      }

      playerStat.rank = 1;
      D.adminLog("Reset silver rank for: " + userId, D.ADMIN_LOG_TYPE.Pvp);
   }

   public int getSilverRank (int userId) {
      GameStats playerStat = gameStatsData.stats.Find(_ => _.userId == userId);
      if (playerStat == null) {
         return 1;
      }

      // Stat Increase
      return playerStat.rank;
   }

   public void addBuildingDestroyedCount (int userId) {
      GameStats playerStat = gameStatsData.stats.Find(_ => _.userId == userId);
      if (playerStat == null) {
         return;
      }

      // TODO: Implement visual indication here that will notify player of this specific stat gain

      // Stat Increase
      playerStat.playerStructuresDestroyed++;
      D.adminLog("Added structure kills for: " + userId + " Total of: " + playerStat.playerStructuresDestroyed, D.ADMIN_LOG_TYPE.Pvp);
   }

   public void addAssistCount (int userId) {
      GameStats playerStat = gameStatsData.stats.Find(_ => _.userId == userId);
      if (playerStat == null) {
         return;
      }

      // TODO: Implement visual indication here that will notify player of this specific stat gain

      // Stat Increase
      playerStat.playerAssists++;
      D.adminLog("Added assist count for: " + userId + " Total of: " + playerStat.playerAssists, D.ADMIN_LOG_TYPE.Pvp);
   }

   public void addDeathCount (int userId) {
      GameStats playerStat = gameStatsData.stats.Find(_ => _.userId == userId);
      if (playerStat == null) {
         return;
      }

      // TODO: Implement visual indication here that will notify player of this specific stat gain

      // Stat Increase
      playerStat.PvpPlayerDeaths++;
      D.adminLog("Added death count for: " + userId + " Total of: " + playerStat.PvpPlayerDeaths, D.ADMIN_LOG_TYPE.Pvp);
   }

   public List<GameStats> getStatsForInstance (int instanceId) {
      List<GameStats> instanceStats = new List<GameStats>();

      foreach (GameStats s in gameStatsData.stats) {
         if (s == null) {
            continue;
         }

         NetEntity userEntity = EntityManager.self.getEntity(s.userId);
         
         if (userEntity == null) {
            continue;
         }

         if (userEntity.instanceId == instanceId) {
            instanceStats.Add(s);
         }
      }

      return instanceStats;
   }

   public GameStats getStatsForUser (int userId) {
      return gameStatsData.stats.Find(_ => _.userId == userId);
   }

   #endregion

   #region Private Variables

   #endregion
}