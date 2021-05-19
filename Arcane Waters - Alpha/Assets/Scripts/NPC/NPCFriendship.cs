using UnityEngine;
using System.Collections;

public class NPCFriendship
{
   #region Public Variables

   // The friendship ranks
   public enum Rank { None = 0, Stranger = 1, Acquaintance = 2, CasualFriend = 3, CloseFriend = 4, BestFriend = 5
   }

   #endregion

   public static Rank getRank (int friendshipLevel) {
      if (friendshipLevel < 5) {
         return Rank.Stranger;
      } else if (friendshipLevel < 35) {
         return Rank.Acquaintance;
      } else if (friendshipLevel < 65) {
         return Rank.CasualFriend;
      } else if (friendshipLevel <= 99) {
         return Rank.CloseFriend;
      } else {
         return Rank.BestFriend;
      }
   }

   public static string getRankName (Rank rank) {
      switch (rank) {
         case Rank.Stranger:
            return "Stranger";
         case Rank.Acquaintance:
            return "Acquaintance";
         case Rank.CasualFriend:
            return "Casual Friend";
         case Rank.CloseFriend:
            return "Close Friend";
         case Rank.BestFriend:
            return "Best Friend";
         default:
            return rank.ToString();
      }
   }

   public static string getRankName (int friendshipLevel) {
      return getRankName(getRank(friendshipLevel));
   }

   public static int addToFriendship (int currentFriendship, int additionalFriendship) {
      if (currentFriendship < 99) {
         return Mathf.Clamp(currentFriendship + additionalFriendship, 0, 99);
      } else {
         return currentFriendship;
      }
   }

   public static bool isRankAboveOrEqual (int currentFriendshipLevel, Rank requiredRank) {
      Rank currentRank = getRank(currentFriendshipLevel);
      return isRankAboveOrEqual(currentRank, requiredRank);
   }

   public static bool isRankAboveOrEqual(Rank current, Rank required) {
      if ((int) current >= (int) required) {
         return true;
      } else {
         return false;
      }
   }

   public static void checkAchievementTrigger (NetEntity player, int oldFriendship, int newFriendship) {
      Rank oldRank = getRank(oldFriendship);
      Rank newRank = getRank(newFriendship);

      if (oldRank != newRank) {
         switch (newRank) {
            case Rank.Acquaintance:
               AchievementManager.registerUserAchievement(player, ActionType.NPCAcquaintance);
               break;
            case Rank.CasualFriend:
               AchievementManager.registerUserAchievement(player, ActionType.NPCCasualFriend);
               break;
            case Rank.CloseFriend:
               AchievementManager.registerUserAchievement(player, ActionType.NPCCloseFriend);
               break;
            case Rank.BestFriend:
               AchievementManager.registerUserAchievement(player, ActionType.NPCBestFriend);
               break;
         }
      }
   }

   #region Private Variables

   #endregion
}
