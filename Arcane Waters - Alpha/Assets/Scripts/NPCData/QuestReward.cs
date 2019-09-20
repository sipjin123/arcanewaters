using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public abstract class QuestReward
{
   #region Public Variables

   #endregion

   // Must be called from the background thread!
   public abstract Item giveRewardToUser (int npcId, int userId);

   #region Private Variables

   #endregion
}
