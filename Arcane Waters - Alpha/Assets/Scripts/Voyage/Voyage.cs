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

   // The voyage difficulty
   public enum Difficulty { None = 0, Easy = 1, Medium = 2, Hard = 3 }

   // The voyage mode chosen by the player
   public enum Mode { Quickmatch = 1, Private = 2 }

   // The unique id of the voyage
   public int voyageId;

   // The key of the area the voyage is set in
   public string areaKey;

   // The voyage difficulty
   public Difficulty difficulty = Difficulty.None;

   // Gets set to true when the voyage is PvP - Otherwise, the voyage is PvE
   public bool isPvP = false;

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

   // The time passed since the creation of the voyage instance
   public long timeSinceStart = 0;

   #endregion

   public Voyage () {

   }

   public Voyage (int voyageId, string areaKey, Difficulty difficulty, bool isPvP,
      long creationDate, int treasureSiteCount, int capturedTreasureSiteCount) {
      this.voyageId = voyageId;
      this.areaKey = areaKey;
      this.difficulty = difficulty;
      this.isPvP = isPvP;
      this.creationDate = creationDate;
      this.treasureSiteCount = treasureSiteCount;
      this.capturedTreasureSiteCount = capturedTreasureSiteCount;
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

   #region Private Variables

   #endregion
}
