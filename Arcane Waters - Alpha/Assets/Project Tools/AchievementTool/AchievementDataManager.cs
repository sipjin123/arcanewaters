using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using static AchievementData;

public class AchievementDataManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static AchievementDataManager self;

   // The files containing the achievement data
   public TextAsset[] rawDataAssets;

   // Determines if the list is generated already
   public bool hasInitialized;

   // Holds the list of the xml translated data
   public List<AchievementData> achievementDataList;

   #endregion
   public void Awake () {
      self = this;
      initializeDataCache();
   }

   public AchievementData getAchievementData (string uniqueID) {
      AchievementData returnData = achievementDataList.Find(_ => _.achievementUniqueID == uniqueID);
      return returnData;
   }

   public static List<AchievementData> castData (ActionType actionType, int count, Item item) {
      List<AchievementData> newDataList = new List<AchievementData>();

      if (item == null) {
         newDataList = self.achievementDataList.FindAll(_ => _.achievementType == actionType);
      } else {
         newDataList = self.achievementDataList.FindAll(_ => _.achievementType == actionType && _.itemCategory == (int)item.category && _.itemType == item.itemTypeId);
      }
      if (newDataList == null) {
         Debug.LogWarning("Achievement data group does not exist");
         return null;
      }

      return newDataList;
   }

   private void initializeDataCache () {
      if (!hasInitialized) {
         achievementDataList = new List<AchievementData>();
         hasInitialized = true;
         // Iterate over the files
         foreach (TextAsset textAsset in rawDataAssets) {
            // Read and deserialize the file
            AchievementData rawData = Util.xmlLoad<AchievementData>(textAsset);
            string uniqueID = rawData.achievementUniqueID;

            // Save the achievement data in the memory cache
            if (!_achievementData.ContainsKey(uniqueID)) {
               _achievementData.Add(uniqueID, rawData);
               achievementDataList.Add(rawData);
            }
         }
      }
   }

   #region Private Variables

   // The cached achievement data 
   private Dictionary<string, AchievementData> _achievementData = new Dictionary<string, AchievementData>();

   #endregion
}
