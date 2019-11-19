using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using static AchievementData;
using System.Linq;

public class AchievementDataManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static AchievementDataManager self;

   // The files containing the achievement data
   public TextAsset[] rawDataAssets;

   // Determines if the list is generated already
   public bool hasInitialized;
   
   #endregion

   public void Awake () {
      self = this;
      initializeDataCache();
   }

   public AchievementData getAchievementData (string uniqueID) {
      AchievementData returnData = _achievementDataCollection[uniqueID];
      return returnData;
   }

   public static List<AchievementData> castData (ActionType actionType, int count, Item item) {
      List<AchievementData> newDataList = new List<AchievementData>();

      // Gathers all the achievement that uses the same Action Type such as ([ActionType.Mining]Mine1x, Mine10x, Mine100x)
      if (item == null) {
         newDataList = self._achievementDataCollection.Values.ToList().FindAll(_ => _.actionType == actionType);
      } else {
         newDataList = self._achievementDataCollection.Values.ToList().FindAll(_ => _.actionType == actionType && _.itemCategory == (int)item.category && _.itemType == item.itemTypeId);
      }
      if (newDataList.Count < 1) {
         Debug.LogWarning("Achievement data group does not exist");
         return null;
      }

      return new List<AchievementData>(newDataList);
   }

   private void initializeDataCache () {
      if (!hasInitialized) {
         hasInitialized = true;
         // Iterate over the files
         foreach (TextAsset textAsset in rawDataAssets) {
            // Read and deserialize the file
            AchievementData rawData = Util.xmlLoad<AchievementData>(textAsset);
            string uniqueID = rawData.achievementUniqueID;

            // Save the achievement data in the memory cache
            if (_achievementDataCollection.ContainsKey(uniqueID)) {
               Debug.LogWarning("Duplicated ID: " + uniqueID +" : "+rawData.achievementName);
            } else {
               _achievementDataCollection.Add(uniqueID, rawData);
            }
         }
      }
   }

   #region Private Variables

   // Holds the collection of the xml translated data
   public Dictionary<string, AchievementData> _achievementDataCollection = new Dictionary<string, AchievementData>();

   #endregion
}
