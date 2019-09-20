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

      // Increase and save the friendship value
      DB_Main.updateNPCRelationship(npcId, userId, currentFriendship + rewardedFriendship);
      return null;
   }

   #region Private Variables

   #endregion
}
