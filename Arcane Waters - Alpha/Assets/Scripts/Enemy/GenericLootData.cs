using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;
using System.Linq;
using System.Xml.Serialization;

public class GenericLootData : ScriptableObject {
   #region Public Variables

   // List of loots with their chance rate
   public List<LootInfo> lootList;

   // If all chances have failed, set this item as the return loot
   public CraftingIngredients.Type defaultLoot;

   [Range(0, 5)]
   public int minQuantity;

   [Range(0, 5)]
   public int maxQuantity;

   #endregion

   public List<LootInfo> requestLootList() {
      List<LootInfo> newLootList = new List<LootInfo>();

      // If there are no item that passed their chance ratio, an optional default value can be set
      if (newLootList.Count == 0 || defaultLoot != CraftingIngredients.Type.None) {
         newLootList.Add(new LootInfo { lootType = defaultLoot, quantity = 1 });
         return newLootList;
      }

      int randomizedLootCount = UnityEngine.Random.Range(minQuantity, maxQuantity);
      int lastIndex = lootList.Count - 1;
      float maxRange = 100.00f;

      // Handles the chance drops of each loot item
      foreach (LootInfo lootInfo in lootList) {
         float randomFactor = UnityEngine.Random.Range(0, maxRange);
         if(randomFactor <= lootInfo.chanceRatio) {
            newLootList.Add(lootInfo);
            if (newLootList.Count >= randomizedLootCount) {
               break;
            }
         }
      }

      return newLootList;
   }
}

public class RawGenericLootData
{
   #region Public Variables

   // List of loots with their chance rate
   public LootInfo[] lootList;

   [XmlElement(Namespace = "IngredientType")]
   // If all chances have failed, set this item as the return loot
   public CraftingIngredients.Type defaultLoot;

   [Range(0, 5)]
   public int minQuantity;

   [Range(0, 5)]
   public int maxQuantity;

   #endregion

   public List<LootInfo> requestLootList () {
      List<LootInfo> newLootList = new List<LootInfo>();

      // If there are no item that passed their chance ratio, an optional default value can be set
      if (newLootList.Count == 0 || defaultLoot != CraftingIngredients.Type.None) {
         newLootList.Add(new LootInfo { lootType = defaultLoot, quantity = 1 });
         return newLootList;
      }

      int randomizedLootCount = UnityEngine.Random.Range(minQuantity, maxQuantity);
      int lastIndex = lootList.Length - 1;
      float maxRange = 100.00f;

      // Handles the chance drops of each loot item
      foreach(LootInfo lootInfo in lootList) { 
         float randomFactor = UnityEngine.Random.Range(0, maxRange);
         if (randomFactor <= lootInfo.chanceRatio) {
            newLootList.Add(lootInfo);
            if (newLootList.Count >= randomizedLootCount) {
               break;
            }
         }
      }

      return newLootList;
   }
}

[Serializable]
public class LootInfo
{
   [XmlElement(Namespace = "IngredientType")]
   // Type of loot
   public CraftingIngredients.Type lootType;

   // Number of loots
   public int quantity = 1;

   // Percentage Chance to drop
   [Range(0.00f,100.00f)]
   public float chanceRatio;
}