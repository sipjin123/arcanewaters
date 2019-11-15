using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

public class AchievementManager : NetworkBehaviour {
   #region Public Variables
   
   #endregion

   void Awake () {
      _player = GetComponent<NetEntity>();
   }

   [Server]
   public void registerAchievement (int userID, AchievementData.ActionType actionType, int count, Item dependencyItem = null) {
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
            // UPDATE EXISTING DATA
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

               // MARKS THE ACHIEVEMENT AS COMPLETE
               if (resultCount >= requirementCount) {
                  isComplete = true;
               }
               DB_Main.updateAchievementData(rawData, userID, isComplete, count);
            }
         } else {
            // CREATE NEW DATA
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

               DB_Main.updateAchievementData(rawData, userID, isComplete);
            }
         }

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // INSERT UNITY LOGIC HERE
         });
      });
   }

   #region Private Variables

   // Our associated Player object
   protected NetEntity _player;

   // Gets set to true once we've loaded our crops on the server
   protected bool _cropsDoneLoading = false;

   // The crops for this player
   protected List<CropInfo> _crops = new List<CropInfo>();

   // The time at which the specified user ID last sold something
   protected static Dictionary<int, float> _lastSellTime = new Dictionary<int, float>();

   #endregion
}
