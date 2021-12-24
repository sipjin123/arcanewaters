using UnityEngine;

public class PlantableTreeDefinition
{
   #region Public Variables

   // How long it takes for 1 stage of growth
   public long growthDuration = 5;

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
      treeData.state = PlantableTreeInstanceData.StateType.Watered;
   }

   public bool needsWatering (PlantableTreeInstanceData treeData, long currentTimestamp) {
      if (treeData.lastUpdateTime == currentTimestamp) {
         return false;
      }

      return treeData.state == PlantableTreeInstanceData.StateType.Planted;
   }

   public bool isFullyGrown (PlantableTreeInstanceData treeData, long currentTimestamp) {
      return
         treeData.state == PlantableTreeInstanceData.StateType.Untethered &&
         amountGrownFromLastAction(treeData, currentTimestamp) >= 2;
   }

   public int amountGrownFromLastAction (PlantableTreeInstanceData treeData, long currentTimestamp) {
      if (treeData.state == PlantableTreeInstanceData.StateType.Tethered) {
         if (currentTimestamp - treeData.lastUpdateTime >= growthDuration) {
            return 1;
         } else {
            return 0;
         }
      }

      if (treeData.state == PlantableTreeInstanceData.StateType.Untethered) {
         if (currentTimestamp - treeData.lastUpdateTime >= growthDuration * 2) {
            return 2;
         } else if (currentTimestamp - treeData.lastUpdateTime >= growthDuration) {
            return 1;
         } else {
            return 0;
         }
      }

      return 0;
   }

   public bool canTetherUntether (PlantableTreeInstanceData treeData, long currentTimestamp) {
      if (treeData.lastUpdateTime == currentTimestamp) {
         return false;
      }

      if (treeData.state == PlantableTreeInstanceData.StateType.Watered) {
         return true;
      }

      if (treeData.state == PlantableTreeInstanceData.StateType.Tethered) {
         if (currentTimestamp - treeData.lastUpdateTime >= 5) {
            return true;
         }
      }

      return false;
   }

   public void tetherUntetherTree (PlantableTreeInstanceData tree, long currentTimestamp) {
      if (tree.state == PlantableTreeInstanceData.StateType.Watered) {
         tree.lastUpdateTime = currentTimestamp;
         tree.state = PlantableTreeInstanceData.StateType.Tethered;
      } else if (tree.state == PlantableTreeInstanceData.StateType.Tethered) {
         tree.lastUpdateTime = currentTimestamp;
         tree.state = PlantableTreeInstanceData.StateType.Untethered;
      }
   }

   public string getStatusText (PlantableTreeInstanceData treeData, long currentTimestamp) {
      long timeRemaining = (long) Mathf.Max(0, 5 - (int) (currentTimestamp - treeData.lastUpdateTime));

      if (treeData.state == PlantableTreeInstanceData.StateType.Planted) {
         return "Use Watering Can";
      }

      if (treeData.state == PlantableTreeInstanceData.StateType.Watered) {
         return "Click To Tether";
      }

      if (treeData.state == PlantableTreeInstanceData.StateType.Tethered) {
         if (timeRemaining == 0) {
            return "Click To Untethered";
         } else {
            return $"{timeRemaining}s\nGrowing...";
         }
      }

      if (treeData.state == PlantableTreeInstanceData.StateType.Untethered) {
         timeRemaining = (long) Mathf.Max(0, 10 - (int) (currentTimestamp - treeData.lastUpdateTime));
         if (timeRemaining > 0) {
            return $"{timeRemaining}s\nGrowing...";
         }
      }

      return "";
   }

   #region Private Variables

   #endregion
}
