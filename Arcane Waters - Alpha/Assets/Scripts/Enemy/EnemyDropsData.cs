using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Linq;

public class EnemyDropsData : ScriptableObject {
   #region Public Variables

   public List<LootInfo> lootList;

   [Range(0, 5)]
   public int minQuantity;

   [Range(0, 5)]
   public int maxQuantity;

   #endregion

   public List<LootInfo> requestLootList() {
      List<LootInfo> newLootList = new List<LootInfo>();
      int randomizedLootCount = UnityEngine.Random.Range(minQuantity, maxQuantity);
      int lastIndex = lootList.Count - 1;
      int maxRange = 100;

      // Handles the chance drops of each loot item
      for(int i = 0; i < lootList.Count; i++) {
         LootInfo lootInfo = lootList[i];
         float randomFactor = UnityEngine.Random.Range(0, maxRange);
         if(randomFactor <= lootInfo.chanceRatio) {
            newLootList.Add(lootInfo);
            if (newLootList.Count >= randomizedLootCount) {
               break;
            }
         }
      }

      // Ensures that the item count is atleast 1
      if(newLootList.Count == 0) {
         newLootList.Add(lootList[lastIndex]);
      }

      return newLootList;
   }
}

[Serializable]
public class LootInfo
{
   public CraftingIngredients.Type lootType;
   [Range(0,100)]
   public float chanceRatio;
}