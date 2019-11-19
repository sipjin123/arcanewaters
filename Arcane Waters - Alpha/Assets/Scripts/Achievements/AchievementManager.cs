using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class AchievementManager : NetworkBehaviour {
   #region Public Variables

   // Self
   public static AchievementManager self;

   #endregion

   void Awake () {
      self = this;
      _player = GetComponent<NetEntity>();
   }

   [Server]
   private void processAchievement (int userID, ActionType actionType, int count, Item dependencyItem = null) {
      D.debug("Register Achievement: " + actionType);
      List<AchievementData> castedData = AchievementDataManager.castData(actionType, count, dependencyItem);
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
               rawData.count = 1;
               DB_Main.updateAchievementData(rawData, userID, isComplete);
            }
         }

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // INSERT UNITY LOGIC HERE
         });
      });
   }

   public static void registerUserAchievement (ActionType action, int customCount = 1, Item dependencyItem = null) {
      self.processAchievement(self._player.userId, action, customCount, dependencyItem);
   }

   public static void registerTargetUserAchivement (int userID, ActionType action, int customCount = 1, Item dependencyItem = null) {
      self.processAchievement(userID, action, customCount, dependencyItem);
   }

   #region Private Variables

   // Our associated Player object
   protected NetEntity _player;

   #endregion
}
