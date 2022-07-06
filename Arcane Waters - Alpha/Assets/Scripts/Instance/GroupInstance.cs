using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

/// <summary>
/// Group instances are instances that are created for a specific group and that can only be accessed by that group.
/// </summary>
[Serializable]
public class GroupInstance
{
   #region Public Variables

   // The time interval since the group instance creation during which new groups can join
   public static float INSTANCE_OPEN_DURATION = 20 * 60;

   // The maximum number of players per instance
   public static int MAX_PLAYERS_PER_INSTANCE = 50;

   // The maximum number of players in a group for each map difficulty
   public static int MAX_PLAYERS_PER_GROUP_EASY= 2;
   public static int MAX_PLAYERS_PER_GROUP_MEDIUM = 4;
   public static int MAX_PLAYERS_PER_GROUP_HARD = 6;

   // The maximum number of players in a group for a pvp game
   public static int MAX_PLAYERS_PER_GROUP_PVP = 6;

   // The total number of maps in a voyage (series of league maps)
   public static int MAPS_PER_VOYAGE = 3;

   // The maximum distance apart the group members can be when starting a voyage
   public static int VOYAGE_START_MEMBERS_MAX_DISTANCE = 3;

   // The radius around the voyage entrance where the exit spawn is searched
   public static float VOYAGE_EXIT_SPAWN_SEARCH_RADIUS = 2f;

   // The group instance difficulty as enum
   public enum Difficulty { None = 0, Easy = 1, Medium = 2, Hard = 3 }

   // The id of the group instance, unique in all the server network
   public int groupInstanceId;

   // The id of the instance in the hosting server
   public int instanceId;

   // The key of the area the instance is set in
   public string areaKey;

   // The name of the area the instance is set in
   public string areaName;

   // The group instance difficulty
   public int difficulty = 0;

   // Gets set to true when the instance is PvP - Otherwise, the instance is PvE
   public bool isPvP = false;

   // Gets set to true when the map is part of a voyage (series of connected leagues)
   public bool isLeague = false;

   // The index of this league in the league series (voyage)
   public int leagueIndex = 0;

   // The random seed used to create the league map series
   public int leagueRandomSeed = -1;

   // The area key where users are warped to when exiting a voyage
   public string voyageExitAreaKey = "";

   // The spawn key where users are warped to when exiting a voyage
   public string voyageExitSpawnKey = "";

   // The facing direction when exiting a voyage
   public Direction voyageExitFacingDirection = Direction.South;

   // The creation time of the instance
   public long creationDate;

   // The total number of treasure sites in the instance
   public int treasureSiteCount = 0;

   // The number of captured treasure sites
   public int capturedTreasureSiteCount = 0;

   // The number of alive enemies in the instance
   public int aliveNPCEnemyCount = 0;

   // The total number of enemies in the instance
   public int totalNPCEnemyCount = 0;

   // The number of groups in the instance
   public int groupCount = 0;

   // The number of players in the instance
   public int playerCount = 0;

   // The biome of the area the instance is set in
   public Biome.Type biome = Biome.Type.None;

   // The number of players in team A
   public int playerCountTeamA = 0;

   // The number of players in team B
   public int playerCountTeamB = 0;

   // The maximum number of players in the pvp game
   public int pvpGameMaxPlayerCount = 0;

   // The state of the game for pvp arenas
   public PvpGame.State pvpGameState = PvpGame.State.None;

   #endregion

   public GroupInstance () {

   }

   public GroupInstance (int groupInstanceId, int instanceId, string areaKey, string areaName, int difficulty, Biome.Type biome, bool isPvP, bool isLeague, int leagueIndex, int leagueRandomSeed, string voyageExitAreaKey, string voyageExitSpawnKey, Direction voyageExitFacingDirection, long creationDate, int treasureSiteCount, int capturedTreasureSiteCount, int aliveNPCEnemyCount, int totalNPCEnemyCount, int groupCount, int playerCount, int playerCountTeamA, int playerCountTeamB, int pvpGameMaxPlayerCount, PvpGame.State pvpGameState) {
      this.groupInstanceId = groupInstanceId;
      this.instanceId = instanceId;
      this.areaKey = areaKey;
      this.areaName = areaName;
      this.difficulty = difficulty;
      this.biome = biome;
      this.isPvP = isPvP;
      this.isLeague = isLeague;
      this.leagueIndex = leagueIndex;
      this.leagueRandomSeed = leagueRandomSeed;
      this.voyageExitAreaKey = voyageExitAreaKey;
      this.voyageExitSpawnKey = voyageExitSpawnKey;
      this.voyageExitFacingDirection = voyageExitFacingDirection;
      this.creationDate = creationDate;
      this.treasureSiteCount = treasureSiteCount;
      this.capturedTreasureSiteCount = capturedTreasureSiteCount;
      this.aliveNPCEnemyCount = aliveNPCEnemyCount;
      this.totalNPCEnemyCount = totalNPCEnemyCount;
      this.groupCount = groupCount;
      this.playerCount = playerCount;
      this.playerCountTeamA = playerCountTeamA;
      this.playerCountTeamB = playerCountTeamB;
      this.pvpGameMaxPlayerCount = pvpGameMaxPlayerCount;
      this.pvpGameState = pvpGameState;
   }

   public static Difficulty getDifficultyEnum (int difficulty) {
      if (difficulty > 4) {
         return Difficulty.Hard;
      } else if (difficulty > 2) {
         return Difficulty.Medium;
      } else if (difficulty > 0) {
         return Difficulty.Easy;
      } else {
         return Difficulty.None;
      }
   }

   public static int getMaxDifficulty () {
      return MAX_PLAYERS_PER_GROUP_HARD;
   }

   public static int getMaxGroupSize (int groupInstanceDifficulty) {
      switch (getDifficultyEnum(groupInstanceDifficulty)) {
         case Difficulty.Easy:
            return MAX_PLAYERS_PER_GROUP_EASY;
         case Difficulty.Medium:
            return MAX_PLAYERS_PER_GROUP_MEDIUM;
         case Difficulty.Hard:
            return MAX_PLAYERS_PER_GROUP_HARD;
         default:
            return MAX_PLAYERS_PER_GROUP_HARD;
      }
   }

   public static int getMaxGroupsPerInstance (int groupInstanceDifficulty) {
      // Get the number of players per group
      int groupSize = getMaxGroupSize(groupInstanceDifficulty);

      // Calculate the maximum number of groups
      int maxGroups = Mathf.FloorToInt(((float) MAX_PLAYERS_PER_INSTANCE) / groupSize);

      return maxGroups;
   }

   public static bool isLastVoyageMap (int leagueIndex) {
      return leagueIndex >= MAPS_PER_VOYAGE - 1;
   }

   public static bool isFirstVoyageMap (int leagueIndex) {
      return leagueIndex == 0;
   }

   public static string getLeagueAreaName (int leagueIndex) {
      return (leagueIndex + 1) + " of " + MAPS_PER_VOYAGE;
   }

   #region Private Variables

   #endregion
}
