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

   // The voyage difficulty
   public enum Difficulty { None = 0, Easy = 1, Medium = 2, Hard = 3 }

   // The unique id of the voyage
   public int voyageId;

   // The key of the area the voyage is set in
   public string areaKey;

   // The name of the area the voyage is set in
   public string areaName;

   // The voyage difficulty
   public Difficulty difficulty = Difficulty.None;

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

   // The number of groups in the voyage instance
   public int groupCount = 0;

   // The biome of the area the voyage is set in
   public Biome.Type biome = Biome.Type.None;

   #endregion

   public Voyage () {

   }

   public Voyage (int voyageId, string areaKey, string areaName, Difficulty difficulty, Biome.Type biome, bool isPvP,
      bool isLeague, int leagueIndex, long creationDate, int treasureSiteCount, int capturedTreasureSiteCount, int groupCount) {
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
      this.groupCount = groupCount;
   }

   public static int getMaxGroupSize (Difficulty voyageDifficulty) {
      switch (voyageDifficulty) {
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

   public static int getMaxGroupsPerInstance (Difficulty voyageDifficulty) {
      // Get the number of players per group
      int groupSize = getMaxGroupSize(voyageDifficulty);

      // Calculate the maximum number of groups
      int maxGroups = Mathf.FloorToInt(((float) MAX_PLAYERS_PER_INSTANCE) / groupSize);

      return maxGroups;
   }

   public static bool isLastLeagueMap (int leagueIndex) {
      return leagueIndex >= MAPS_PER_LEAGUE - 1;
   }

   #region Private Variables

   #endregion
}
