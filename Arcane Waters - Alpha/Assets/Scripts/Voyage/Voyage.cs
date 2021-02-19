using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

[Serializable]
public class Voyage
{
   #region Public Variables

   // The number of voyage instances that must always be available to join
   public static int OPEN_VOYAGE_INSTANCES = 3;

   // The time interval since the voyage creation during which new groups can join
   public static float INSTANCE_OPEN_DURATION = 20 * 60;

   // The maximum number of players per instance
   public static int MAX_PLAYERS_PER_INSTANCE = 50;

   // The maximum number of players in a group for each map difficulty
   public static int MAX_PLAYERS_PER_GROUP_EASY= 2;
   public static int MAX_PLAYERS_PER_GROUP_MEDIUM = 4;
   public static int MAX_PLAYERS_PER_GROUP_HARD = 6;

   // The total number of maps in a league (series of voyages maps)
   public static int MAPS_PER_LEAGUE = 5;

   // The maximum distance apart the group members can be when starting a league
   public static int LEAGUE_START_MEMBERS_MAX_DISTANCE = 3;

   // The voyage difficulty as enum, mostly used for the UI
   public enum Difficulty { None = 0, Easy = 1, Medium = 2, Hard = 3 }

   // The unique id of the voyage
   public int voyageId;

   // The key of the area the voyage is set in
   public string areaKey;

   // The name of the area the voyage is set in
   public string areaName;

   // The voyage difficulty
   public int difficulty = 0;

   // Gets set to true when the voyage is PvP - Otherwise, the voyage is PvE
   public bool isPvP = false;

   // Gets set to true when the map is part of a league (series of connected voyages)
   public bool isLeague = false;

   // The index of this voyage in the league series
   public int leagueIndex = 0;

   // The creation time of the voyage instance
   public long creationDate;

   // The total number of treasure sites in the map
   public int treasureSiteCount = 0;

   // The number of captured treasure sites
   public int capturedTreasureSiteCount = 0;

   // The number of alive enemies in the instance
   public int aliveNPCEnemyCount = 0;

   // The total number of enemies in the instance
   public int totalNPCEnemyCount = 0;

   // The number of groups in the voyage instance
   public int groupCount = 0;

   // The biome of the area the voyage is set in
   public Biome.Type biome = Biome.Type.None;

   #endregion

   public Voyage () {

   }

   public Voyage (int voyageId, string areaKey, string areaName, int difficulty, Biome.Type biome, bool isPvP,
      bool isLeague, int leagueIndex, long creationDate, int treasureSiteCount, int capturedTreasureSiteCount, int aliveNPCEnemyCount, 
      int totalNPCEnemyCount, int groupCount) {
      this.voyageId = voyageId;
      this.areaKey = areaKey;
      this.areaName = areaName;
      this.difficulty = difficulty;
      this.biome = biome;
      this.isPvP = isPvP;
      this.isLeague = isLeague;
      this.leagueIndex = leagueIndex;
      this.creationDate = creationDate;
      this.treasureSiteCount = treasureSiteCount;
      this.capturedTreasureSiteCount = capturedTreasureSiteCount;
      this.aliveNPCEnemyCount = aliveNPCEnemyCount;
      this.totalNPCEnemyCount = totalNPCEnemyCount;
      this.groupCount = groupCount;
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

   public static int getMaxGroupSize (int voyageDifficulty) {
      switch (getDifficultyEnum(voyageDifficulty)) {
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

   public static int getMaxGroupsPerInstance (int voyageDifficulty) {
      // Get the number of players per group
      int groupSize = getMaxGroupSize(voyageDifficulty);

      // Calculate the maximum number of groups
      int maxGroups = Mathf.FloorToInt(((float) MAX_PLAYERS_PER_INSTANCE) / groupSize);

      return maxGroups;
   }

   public static bool isLastLeagueMap (int leagueIndex) {
      return leagueIndex >= MAPS_PER_LEAGUE;
   }

   public static string getLeagueAreaName (int leagueIndex) {
      if (leagueIndex == 0) {
         return "Lobby";
      } else {
         return leagueIndex + " of " + MAPS_PER_LEAGUE;
      }
   }

   #region Private Variables

   #endregion
}
