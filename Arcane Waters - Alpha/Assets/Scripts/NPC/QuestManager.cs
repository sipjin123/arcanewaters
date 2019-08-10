using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
   #region Public Variables

   public class RandomizedQuestSeed
   {
      // The type of ingredient needed for the quest
      public CraftingIngredients.Type requiredItem;

      // The count of ingredients needed for the quest
      public int quantity;

      // The randomized reward item
      public Item rewardItem;
   }

   // Self Reference
   public static QuestManager self;
   
   // List of items to provide on delivery quests
   public List<CraftingIngredients.Type> deliverableItemList;

   // List of items to reward after delivery quests
   public List<Item> rewardItemList;

   // The npc data for all deliver quests
   public NPCQuestData deliveryQuestData;

   #endregion

   private void Awake () {
      self = this;
   }

   public RandomizedQuestSeed randomizedQuestSeed (int seedValue) {
      Random.InitState(seedValue);

      int itemType = Random.Range(0, deliverableItemList.Count);
      int itemCount = Random.Range(1, 5);
      int rewardCount = Random.Range(0, rewardItemList.Count);

      RandomizedQuestSeed randomizedSeed = new RandomizedQuestSeed {
         requiredItem = (CraftingIngredients.Type) itemType,
         quantity = itemCount,
         rewardItem = rewardItemList[rewardCount].getCastItem()
      };

      return randomizedSeed;
   }
}