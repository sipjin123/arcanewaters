using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class QuestRewardFriendship : QuestReward
{
   #region Public Variables

   // The amount of rewarded friendship
   public int rewardedFriendship;

   #endregion

   public QuestRewardFriendship () {

   }

   public QuestRewardFriendship(int rewardedFriendship) {
      this.rewardedFriendship = rewardedFriendship;
   }

   // Must be called from the background thread!
   public override Item giveRewardToUser (int npcId, int userId) {
      // Retrieve the current friendship
      int currentFriendship = DB_Main.getFriendshipLevel(npcId, userId);

      // Increase the friendship level
      int newFriendshipLevel = NPCFriendship.addToFriendship(currentFriendship, rewardedFriendship);

      // Save the friendship value
      if (newFriendshipLevel != currentFriendship) {
         DB_Main.updateNPCRelationship(npcId, userId, newFriendshipLevel);
      }
      return null;
   }

   #region Private Variables

   #endregion
}
