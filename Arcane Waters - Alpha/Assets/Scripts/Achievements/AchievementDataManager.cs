using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using static AchievementData;
using System.Linq;

public class AchievementDataManager : NetworkBehaviour
{
   #region Public Variables

   // Self
   public static AchievementDataManager self;

   // The files containing the achievement data
   public TextAsset[] rawDataAssets;

   // Determines if the list is generated already
   public bool hasInitialized;
   
   // Determines how many tiers will be setup for each achievement (Example: Bronze Minerx100, Silver Minerx500, Gold Minerx500, Platinum Minerx1000)
   public const int TIER_COUNT = 4;

   #endregion

   public void Awake () {
      self = this;
      initializeDataCache();
   }

   public AchievementData getAchievementData (string uniqueID) {
      AchievementData returnData = _achievementDataCollection[uniqueID];
      return returnData;
   }

   public static List<AchievementData> castData (ActionType actionType, Item item) {
      List<AchievementData> newDataList = new List<AchievementData>();

      // Gathers all the achievement that uses the same Action Type such as ([ActionType.Mining]Mine1x, Mine10x, Mine100x)
      for (int i = 1; i < TIER_COUNT; i++) {
         string actionKey = actionType.ToString() + i.ToString();
         if (self._achievementDataCollection.ContainsKey(actionKey)) {
            newDataList.Add(self._achievementDataCollection[actionKey]);
         }
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
            string actionKey = rawData.actionType.ToString() + rawData.tier.ToString();

            // Save the achievement data in the memory cache
            if (_achievementDataCollection.ContainsKey(actionKey)) {
               Debug.LogWarning("Duplicated ID: " + actionKey +" : "+rawData.achievementName);
            } else {
               _achievementDataCollection.Add(actionKey, rawData);
            }
         }
      }
   }

   public static void registerUserAchievement (int userID, ActionType action, int customCount = 1, Item dependencyItem = null) {
      self.processAchievement(userID, action, customCount, dependencyItem);
   }

   [Server]
   public void processAchievement (int userID, ActionType actionType, int count, Item dependencyItem = null) {
      D.debug("Register Achievement: " + actionType);
      List<AchievementData> castedData = AchievementDataManager.castData(actionType, dependencyItem);
      if (castedData == null) {
         Debug.LogWarning("Warning!: The XML does not exist yet, please create the data using the Achievement Tool Editor : (" + actionType + ")");
         return;
      }

      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<AchievementData> achievementDB = DB_Main.getAchievementData(userID, actionType);
         bool existsInDatabase = true;

         if (achievementDB.Count < 1) {
            existsInDatabase = false;
         }

         if (existsInDatabase) {
            // Update existing data
            foreach (AchievementData rawData in achievementDB) {
               int resultCount = rawData.count + count;
               int requirementCount = 99;

               if (rawData.itemCategory == 0 && rawData.itemType == 0) {
                  AchievementData castData = castedData.Find(_ => _.actionType == rawData.actionType && _.achievementUniqueID == rawData.achievementUniqueID);

                  if (castData == null) {
                     D.debug("The cast data with no item is null!");
                  }
                  requirementCount = castData.count;
               } else {
                  AchievementData castData = castedData.Find(_ => _.actionType == rawData.actionType &&
                  _.achievementUniqueID == rawData.achievementUniqueID &&
                  _.itemCategory == rawData.itemCategory &&
                  _.itemType == rawData.itemType);

                  if (castData == null) {
                     D.debug("The cast data with ITEM is null!");
                  }
                  requirementCount = castData.count;
               }

               bool isComplete = false;

               // Marks the achievements as complete
               if (resultCount >= requirementCount) {
                  isComplete = true;
               }
               DB_Main.updateAchievementData(rawData, userID, isComplete, count);
            }
         } else {
            // Creating new data
            foreach (AchievementData rawData in castedData) {
               bool isComplete = false;

               int requirementCount = 99;
               if (rawData.itemCategory == 0 && rawData.itemType == 0) {
                  AchievementData castData = castedData.Find(_ => _.actionType == rawData.actionType && _.achievementUniqueID == rawData.achievementUniqueID);

                  if (castData == null) {
                     D.debug("The cast data with no item is null!");
                  }
                  requirementCount = castData.count;
               } else {
                  AchievementData castData = castedData.Find(_ => _.actionType == rawData.actionType &&
                  _.achievementUniqueID == rawData.achievementUniqueID &&
                  _.itemCategory == rawData.itemCategory &&
                  _.itemType == rawData.itemType);

                  if (castData == null) {
                     D.debug("The cast data with ITEM is null!");
                  }
                  requirementCount = castData.count;
               }

               if (requirementCount == 1) {
                  isComplete = true;
               }

               AchievementData newData = AchievementData.CreateAchievementData(rawData);
               newData.count = 1;

               DB_Main.updateAchievementData(newData, userID, isComplete);
            }
         }

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // INSERT UNITY LOGIC HERE
         });
      });
   }

   #region Private Variables

   // Holds the collection of the xml translated data
   public Dictionary<string, AchievementData> _achievementDataCollection = new Dictionary<string, AchievementData>();

   #endregion
}
