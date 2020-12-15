﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using Steamworks;

public class AchievementManager : MonoBehaviour {
   #region Public Variables

   // Self
   public static AchievementManager self;

   // Determines how many tiers will be setup for all the achievements (Example: Bronze Minerx100, Silver Minerx500, Gold Minerx500, Platinum Minerx1000)
   public const int MAX_TIER_COUNT = 4;

   // List of achievement data for editor review
   public List<AchievementData> achievmentDataList;
   
   // Logs the achievements upon trigger
   public bool logAchievements;

   public class AchievementGroupData {
      // The xml id of the achievement
      public int achievementXmlId;

      // The whole data content of each data
      public AchievementData achievementData;

      // The unique Id of each action type
      public string uniqueKey;
   }

   #endregion

   public void Awake () {
      self = this;
   }

   public AchievementData getAchievementData (string uniqueKey) {
      AchievementData returnData = _achievementDataCollection.Find(_=>_.uniqueKey == uniqueKey).achievementData;
      return returnData;
   }

   public AchievementData getAchievementData (int xmlId) {
      AchievementData returnData = _achievementDataCollection.Find(_ => _.achievementXmlId == xmlId).achievementData;
      return returnData;
   }

   public AchievementData getAchievementData (ActionType actionType, int tier) {
      List<AchievementGroupData> actionTierGroup = _achievementDataCollection.FindAll(_ => _.achievementData.actionType == actionType);
      foreach (AchievementGroupData achievement in actionTierGroup) {
         if (tier == achievement.achievementData.tier) {
            return achievement.achievementData;
         }
      }
      return null;
   }

   public static List<AchievementData> castData (ActionType actionType, Item item) {
      List<AchievementData> newDataList = new List<AchievementData>();

      // Gathers all the achievement that uses the same Action Type such as ([ActionType.Mining]Mine1x, Mine10x, Mine100x)
      for (int i = 1; i < MAX_TIER_COUNT; i++) {
         string actionKey = actionType.ToString() + i.ToString();
         if (self._achievementDataCollection.Exists(_=>_.uniqueKey == actionKey)) {
            newDataList.Add(self._achievementDataCollection.Find(_=>_.uniqueKey == actionKey).achievementData);
         }
      }

      if (newDataList.Count < 1) {
         Debug.LogWarning("Achievement data group does not exist");
         return null;
      }

      return new List<AchievementData>(newDataList);
   }

   public void initializeDataCache () {
      _achievementDataCollection = new List<AchievementGroupData>();
      
      UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
         List<XMLPair> rawXMLData = DB_Main.getAchievementXML();

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            foreach (XMLPair rawXmlData in rawXMLData) {
               TextAsset newTextAsset = new TextAsset(rawXmlData.rawXmlData);
               AchievementData achievementData = Util.xmlLoad<AchievementData>(newTextAsset);
               string actionKey = achievementData.actionType.ToString() + achievementData.tier.ToString();

               // Save the achievement data in the memory cache
               if (!_achievementDataCollection.Exists(_=>_.uniqueKey == actionKey)) {
                  AchievementGroupData newGroupData = new AchievementGroupData {
                     uniqueKey = actionKey,
                     achievementData = achievementData,
                     achievementXmlId = rawXmlData.xmlId
                  };
                  _achievementDataCollection.Add(newGroupData);
                  achievmentDataList.Add(achievementData);
               }
            }
         });
      });
   }

   public static void registerUserAchievement (NetEntity player, ActionType action, int customCount = 1, Item dependencyItem = null) {
      self.processAchievement(player, action, customCount, dependencyItem);
   }

   public void processAchievement (NetEntity player, ActionType actionType, int count, Item dependencyItem = null) {      
      int userID = player.userId;
      #if IS_SERVER_BUILD
      if (logAchievements) { 
         D.debug("Register Achievement: " + actionType);
      }

      List<AchievementData> castedData = AchievementManager.castData(actionType, dependencyItem);
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

               player.rpc.Target_GrantSteamAchievement(player.connectionToClient, actionType, resultCount);
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

               player.rpc.Target_GrantSteamAchievement(player.connectionToClient, actionType, 1);
               DB_Main.updateAchievementData(newData, userID, isComplete);
            }
         }

         UnityThreadHelper.UnityDispatcher.Dispatch(() => {
            // INSERT UNITY LOGIC HERE
         });
      });
      #endif
   }

   public void processSteamAchievement (ActionType actionType, int currentCount) {
      switch (actionType) {
         case ActionType.OpenTreasureChest:
            SteamUserStats.SetAchievement(STEAM_OPENED_TREASURE_CHEST + currentCount);
            SteamUserStats.SetStat(STAT_UNLOCKED_TREASURE_CHEST, currentCount);
            break;
         case ActionType.KillLandMonster:
            SteamUserStats.SetAchievement(STEAM_SLAY_LAND_MONSTERS + currentCount);
            SteamUserStats.SetStat(STAT_ENEMY_SLAIN, currentCount);
            break;
         case ActionType.CombatDie:
            SteamUserStats.SetAchievement(STEAM_DIE + currentCount);
            SteamUserStats.SetStat(STAT_DEATH_COUNT, currentCount);
            break;
         case ActionType.HarvestCrop:
            SteamUserStats.SetAchievement(STEAM_HARVEST_CROP + currentCount);
            SteamUserStats.SetStat(STAT_HARVEST_CROP, currentCount);
            break;
         case ActionType.LevelUp:
            SteamUserStats.SetAchievement(STEAM_REACH_LEVEL + currentCount);
            SteamUserStats.SetStat(STAT_BASE_LEVEL, currentCount);
            break;
      }
      SteamUserStats.StoreStats();
   }

   #region Private Variables

   // Holds the collection of the xml translated data
   private List<AchievementGroupData> _achievementDataCollection;

   // The achievements registered in steam dashboard
   private const string STEAM_REACH_LEVEL = "REACH_LEVEL_";
   private const string STEAM_HARVEST_CROP = "HARVEST_CROP_";
   private const string STEAM_DIE = "DIE_";
   private const string STEAM_SLAY_LAND_MONSTERS = "SLAY_LAND_MONSTERS_";
   private const string STEAM_OPENED_TREASURE_CHEST = "OPENED_TREASURE_CHEST_";

   // The stats registered in steam dashboard
   private const string STAT_BASE_LEVEL = "base_level";
   private const string STAT_HARVEST_CROP = "harvest_crop";
   private const string STAT_ENEMY_SLAIN = "enemies_slain";
   private const string STAT_UNLOCKED_TREASURE_CHEST = "unlocked_treasure_chest";
   private const string STAT_DEATH_COUNT = "death_count";

   #endregion
}
