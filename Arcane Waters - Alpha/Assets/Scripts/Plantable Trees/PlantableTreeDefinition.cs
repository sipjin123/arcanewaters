using UnityEngine;

public class PlantableTreeDefinition
{
   #region Public Variables

   // How long it takes for 1 stage of growth
   public long growthDuration = 5;

   // How many waterings are required, not currently saved in database
   public int wateringsRequired = 3;

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
   }

#endif

   public void waterTree (PlantableTreeInstanceData treeData, long currentTimestamp) {
      treeData.lastUpdateTime = currentTimestamp;
      treeData.growthStagesCompleted++;
   }

   public bool needsWatering (PlantableTreeInstanceData treeData, long currentTimestamp) {
      if (treeData.lastUpdateTime == currentTimestamp) {
         return false;
      }

      if (treeData.growthStagesCompleted >= wateringsRequired) {
         return false;
      }

      if (currentTimestamp - treeData.lastUpdateTime < growthDuration) {
         return false;
      }

      return true;
   }

   public bool isFullyGrown (PlantableTreeInstanceData treeData, long currentTimestamp) {
      return treeData.growthStagesCompleted >= wateringsRequired && currentTimestamp - treeData.lastUpdateTime >= growthDuration;
   }

   public string getStatusText (PlantableTreeInstanceData treeData, long currentTimestamp) {
      long timeRemaining = (long) Mathf.Max(0, growthDuration - (int) (currentTimestamp - treeData.lastUpdateTime));

      if (isFullyGrown(treeData, currentTimestamp)) {
         return "Ready to\nHarvest";
      }

      if (currentTimestamp - treeData.lastUpdateTime >= growthDuration) {
         return "Use Watering Can";
      }

      if (timeRemaining > 0) {
         return $"{timeRemaining}s\nGrowing...";
      }

      return "";
   }

   public Item getHarvestLoot () {
      return new Item {
         category = Item.Category.CraftingIngredients,
         count = Random.Range(3, 6),
         itemTypeId = (int) CraftingIngredients.Type.Wood,
         data = "",
         durability = 100,
         itemName = CraftingIngredients.Type.Wood.ToString(),
      };
   }

   #region Private Variables

   #endregion
}
