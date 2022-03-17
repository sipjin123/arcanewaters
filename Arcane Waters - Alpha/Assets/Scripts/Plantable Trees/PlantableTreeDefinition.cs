using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class PlantableTreeDefinition
{
   #region Public Variables

   // If the growth stage is this or higher, we'll consider it a stump
   public const int STUMP_GROWTH_STAGE = 1000;

   // How long it takes for 1 stage of growth
   public long growthDuration;

   // How many growth stages are there (excluding stump)
   public int growthStages;

   // Does the tree leave a stump behind after it is chopped
   public bool leavesStump;

   // Path to growth stage sprites 
   public string growthStageSprites;

   // Unique identifier
   public int id;

   // Id of seedbag that is used to plant this tree
   public int seedBagId;

   // Name of the tree
   public string title;

   // Id of the prefab
   public int prefabId;

   #endregion

   public PlantableTreeDefinition () {

   }

#if IS_SERVER_BUILD

   public PlantableTreeDefinition (MySql.Data.MySqlClient.MySqlDataReader dataReader) {
      id = DataUtil.getInt(dataReader, "id");
      seedBagId = DataUtil.getInt(dataReader, "seedBagId");
      prefabId = DataUtil.getInt(dataReader, "prefabId");
      title = DataUtil.getString(dataReader, "title");
      growthDuration = DataUtil.getInt(dataReader, "growthDuration");
      growthStages = DataUtil.getInt(dataReader, "growthStages");
      leavesStump = DataUtil.getBoolean(dataReader, "leavesstump");
      growthStageSprites = DataUtil.getString(dataReader, "growthStageSprites");
   }

#endif

   public void waterTree (PlantableTreeInstanceData treeData, long currentTimestamp) {
      treeData.lastUpdateTime = currentTimestamp;
      treeData.growthStagesCompleted++;
   }

   public void turnTreeToStump (PlantableTreeInstanceData treeData, long currentTimestamp) {
      treeData.lastUpdateTime = currentTimestamp;
      treeData.growthStagesCompleted = STUMP_GROWTH_STAGE;
   }

   public bool needsWatering (PlantableTreeInstanceData treeData, long currentTimestamp) {
      if (treeData.lastUpdateTime == currentTimestamp) {
         return false;
      }

      if (treeData.growthStagesCompleted >= growthStages) {
         return false;
      }

      if (currentTimestamp - treeData.lastUpdateTime < growthDuration) {
         return false;
      }

      return true;
   }

   public bool isFullyGrown (PlantableTreeInstanceData treeData, long currentTimestamp) {
      return treeData.growthStagesCompleted >= growthStages;
   }

   public bool isStump (PlantableTreeInstanceData treeData) {
      return treeData.growthStagesCompleted >= STUMP_GROWTH_STAGE;
   }

   public string getStatusText (PlantableTreeInstanceData treeData, long currentTimestamp, bool forOwner) {
      long timeRemaining = (long) Mathf.Max(0, growthDuration - (int) (currentTimestamp - treeData.lastUpdateTime));

      if (isFullyGrown(treeData, currentTimestamp)) {
         if (forOwner) {
            return "Ready to\nHarvest";
         } else {
            return "";
         }
      }

      if (currentTimestamp - treeData.lastUpdateTime >= growthDuration) {
         return "Use Watering Can";
      }

      if (timeRemaining > 0) {
         return $"{timeRemaining}s\nGrowing...";
      }

      return "";
   }

   public List<Item> getMainHarvestLoot () {
      return Enumerable.Repeat(
         new Item {
            category = Item.Category.CraftingIngredients,
            count = 1,
            itemTypeId = (int) CraftingIngredients.Type.Wood,
            data = "",
            durability = 100,
            itemName = CraftingIngredients.Type.Wood.ToString(),
         },
         Random.Range(3, 6)
      ).ToList();
   }

   public List<Item> getStumpHarvestLoot () {
      return new List<Item> {
         new Item {
            category = Item.Category.CraftingIngredients,
            count = 1,
            itemTypeId = (int) CraftingIngredients.Type.Wood,
            data = "",
            durability = 100,
            itemName = CraftingIngredients.Type.Wood.ToString(),
         }
      };
   }

   #region Private Variables

   #endregion
}
