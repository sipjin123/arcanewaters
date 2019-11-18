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

   // Holds the list of the xml translated data
   public HashSet<AchievementData> achievementDataList;

   #endregion

   public void Awake () {
      self = this;
      initializeDataCache();
   }

   public AchievementData getAchievementData (string uniqueID) {
      AchievementData returnData = achievementDataList.ToList<AchievementData>().Find(_=>_.achievementUniqueID == uniqueID);
      return returnData;
   }

   public static List<AchievementData> castData (ActionType actionType, int count, Item item) {
      List<AchievementData> newDataList = new List<AchievementData>();
      
      if (item == null) {
         newDataList = self.achievementDataList.ToList().FindAll(_ => _.actionType == actionType);
      } else {
         newDataList = self.achievementDataList.ToList().FindAll(_ => _.actionType == actionType && _.itemCategory == (int)item.category && _.itemType == item.itemTypeId);
      }
      if (newDataList.Count < 1) {
         Debug.LogWarning("Achievement data group does not exist");
         return null;
      }

      return new List<AchievementData>(newDataList);
   }

   private void initializeDataCache () {
      if (!hasInitialized) {
         achievementDataList = new HashSet<AchievementData>();
         hasInitialized = true;
         // Iterate over the files
         foreach (TextAsset textAsset in rawDataAssets) {
            // Read and deserialize the file
            AchievementData rawData = Util.xmlLoad<AchievementData>(textAsset);
            string uniqueID = rawData.achievementUniqueID;

            // Save the achievement data in the memory cache
            achievementDataList.Add(rawData);
         }
      }
   }

   #region Private Variables

   #endregion
}
