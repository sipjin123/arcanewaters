using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.Linq;

public class VoyageRatingManager : MonoBehaviour {
   #region Public Variables

   // The points for each rating level
   public static readonly int[] levelPointsCollection = {
      // Level 0 
      100,

      // Level 1
      80,

      // Level 2
      40,

      // Level 3 
      20,

      // Level 4
      10
   };

   // The display string for each level
   public static readonly string[] levelDisplayStringCollection = {
      // Level 0 
      "Low",

      // Level 1
      "Average",

      // Level 2
      "High",

      // Level 3 
      "Super",

      // Level 4
      "Ultra"
   };

   // Rating Points awarded when receiving damage
   private const int RATING_POINTS_DAMAGE_RECEIVED_REWARD = -10;

   // Rating points awarded on death (sea)
   private const int RATING_POINTS_DEATH_AT_SEA_REWARD = -15;

   // Rating points awarded on death (land)
   private const int RATING_POINTS_DEATH_ON_LAND_REWARD = -40;

   // Reasons for updating the rating
   public enum RewardReason
   {
      // None
      None = 0,

      // Damage Received
      DamageReceived = 1,

      // Death (Sea)
      DeathAtSea = 2,

      // Death (Land)
      DeathOnLand = 3
   }

   #endregion

   public static int computeMaxPointsForRatingLevel(int ratingLevel) {
      // ratingLevel is expected to be a zero-based value
      if (levelPointsCollection == null || levelPointsCollection.Length == 0) {
         return 1;
      }

      if (ratingLevel < 0) {
         return levelPointsCollection[0];
      }

      if (ratingLevel >= levelPointsCollection.Length) {
         return levelPointsCollection.Last();
      }

      return levelPointsCollection[ratingLevel];

   }

   public static int computeRatingLevelFromPoints(int points) {
      int rating = 0;
      int accumulator = 0;

      foreach (int levelPoints in levelPointsCollection) {
         accumulator += levelPoints;
         if (points <= accumulator) {
            return rating;
         }
         rating++;
      }

      return rating;
   }

   public static int getHighestRatingLevel () {
      if (levelPointsCollection == null) {
         return 0;
      }

      return levelPointsCollection.Length - 1;
   }

   public static int getLowestRatingLevel () {
      return 0;
   }

   public static int computeVoyageRatingPointsReward (RewardReason reason) {
      switch (reason) {
         case RewardReason.None:
            return 0;
         case RewardReason.DamageReceived:
            return RATING_POINTS_DAMAGE_RECEIVED_REWARD;
         case RewardReason.DeathAtSea:
            return RATING_POINTS_DEATH_AT_SEA_REWARD;
         case RewardReason.DeathOnLand:
            return RATING_POINTS_DEATH_ON_LAND_REWARD;
         default:
            return 0;
      }
   }

   public static int computeRatingPointsToNextLevel(int points) {
      int pointsAccumulator = 0;
      int difference = 0;
      int computedLevel = 0;
      points = Mathf.Max(getPointsMin(), points);

      foreach (var levelPoints in levelPointsCollection) {
         pointsAccumulator += levelPoints;

         if (points < pointsAccumulator) {
            difference = pointsAccumulator - points;
            break;
         }
         computedLevel++;
      }

      return difference;
   }

   public static int getPointsMax () {
      return levelPointsCollection.Sum();
   }

   public static int getPointsMin () {
      return 0;
   }

   public static string computeDisplayStringForRating (int ratingPoints) {
      int computedLevel = computeRatingLevelFromPoints(ratingPoints);
      
      if (computedLevel < 0 || computedLevel >= levelDisplayStringCollection.Length) {
         return string.Empty;
      }

      return levelDisplayStringCollection[computedLevel];
   }

   #region Private Variables

   #endregion
}
