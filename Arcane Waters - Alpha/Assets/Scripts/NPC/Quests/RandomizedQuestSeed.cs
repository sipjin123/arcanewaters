using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

public class QuestSeed
{
   // The type of item needed for the quest
   public Item requiredItem;

   // The count of ingredients needed for the quest
   public int quantity;

   // The randomized reward item
   public Item rewardItem;

   public static QuestSeed randomizedQuestSeed (int seedValue) {
      Random.InitState(seedValue);

      int itemType = Random.Range(0, QuestManager.self.deliverableItemList.Count);
      int itemCount = Random.Range(1, 5);
      int rewardCount = Random.Range(0, QuestManager.self.rewardItemList.Count);

      QuestSeed randomizedSeed = new QuestSeed {
         requiredItem = QuestManager.self.deliverableItemList[itemType].getCastItem(),
         quantity = itemCount,
         rewardItem = QuestManager.self.rewardItemList[rewardCount].getCastItem()
      };
      randomizedSeed.requiredItem.count = itemCount;

      return randomizedSeed;
   }
}