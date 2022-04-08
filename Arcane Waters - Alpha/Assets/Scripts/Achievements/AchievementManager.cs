using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System.IO;
using Steamworks;
using UnityEngine.Networking;

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
            AchievementGroupData newData = self._achievementDataCollection.Find(_ => _.uniqueKey == actionKey);
            if (newData != null) {
               newDataList.Add(newData.achievementData);
            } else {
               D.debug("No achievement found for: {" + actionKey + "}");
            }
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
      self.processAchievement(player.userId, action, customCount, dependencyItem);
   }

   public static void registerUserAchievement(int userId, ActionType action, int customCount = 1, Item dependencyItem = null) {
      self.processAchievement(userId, action, customCount, dependencyItem);
   }

   public void processAchievement (int userId, ActionType actionType, int count, Item dependencyItem = null) {      
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
         string accountName = DB_Main.getAccountName(userId);
         int steamId = DB_Main.getSteamAccountId(accountName);

         if (!accountName.Contains("@steam")) {
            D.debug("Not a steam user!: {" + accountName + "} {" + userId + "}");
            return;
         }

         List<AchievementData> achievementDB = DB_Main.getAchievementData(userId, actionType);
         bool existsInDatabase = true;

         if (achievementDB.Count < 1) {
            existsInDatabase = false;
         }

         if (existsInDatabase) {
            // Update existing data
            foreach (AchievementData rawData in achievementDB) {
               int resultCount = rawData.count + count;
               int requirementCount = 99;
               List<AchievementData> newAchievementData = new List<AchievementData>();

               if (rawData.itemCategory == 0 && rawData.itemType == 0) {
                  newAchievementData = castedData.FindAll(_ => _.actionType == rawData.actionType && _.achievementUniqueID == rawData.achievementUniqueID);

                  if (newAchievementData == null || newAchievementData.Count < 1) {
                     D.debug("The cast data with no item is null!");
                     return;
                  }
               } else {
                  newAchievementData = castedData.FindAll(_ => _.actionType == rawData.actionType &&
                       _.achievementUniqueID == rawData.achievementUniqueID &&
                       _.itemCategory == rawData.itemCategory &&
                       _.itemType == rawData.itemType);


                  if (newAchievementData == null || newAchievementData.Count < 1) {
                     D.debug("The cast data with ITEM is null!");
                     return;
                  }
               }

               foreach (AchievementData achievementDataEntry in newAchievementData) {
                  requirementCount = achievementDataEntry.count;
                  bool isComplete = false;

                  // Marks the achievements as complete
                  if (resultCount >= requirementCount) {
                     isComplete = true;
                  }

                  DB_Main.updateAchievementData(achievementDataEntry, userId, isComplete, count);
                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     if (steamId != -1) {
                        StartCoroutine(CO_PublishSteamAchievements(achievementDataEntry.achievementUniqueID, resultCount, accountName.Replace("@steam","").ToString(), isComplete));
                     }
                  });
               }
            }
         } else {
            // Creating new data
            foreach (AchievementData rawData in castedData) {
               bool isComplete = false;
               int requirementCount = 99;
               List<AchievementData> newAchievementData = new List<AchievementData>();

               if (rawData.itemCategory == 0 && rawData.itemType == 0) {
                  newAchievementData = castedData.FindAll(_ => _.actionType == rawData.actionType && _.achievementUniqueID == rawData.achievementUniqueID);

                  if (newAchievementData == null || newAchievementData.Count < 1) {
                     D.debug("The cast data with no item is null!");
                     return;
                  }
               } else {
                  newAchievementData = castedData.FindAll(_ => _.actionType == rawData.actionType &&
                  _.achievementUniqueID == rawData.achievementUniqueID &&
                  _.itemCategory == rawData.itemCategory &&
                  _.itemType == rawData.itemType);

                  if (newAchievementData == null || newAchievementData.Count < 1) {
                     D.debug("The cast data with ITEM is null!");
                  }
               }

               foreach (AchievementData achievementDataEntry in newAchievementData) {
                  requirementCount = achievementDataEntry.count;

                  if (requirementCount == 1) {
                     isComplete = true;
                  }

                  AchievementData newData = AchievementData.CreateAchievementData(achievementDataEntry);
                  D.adminLog("Created new entry achievement for user {" + userId + "} {" + newData.achievementName + "} {" + newData.achievementUniqueID + "} {" + newData.count + "}", D.ADMIN_LOG_TYPE.Achievement);
                  newData.count = 1;

                  DB_Main.updateAchievementData(newData, userId, isComplete);

                  UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                     if (steamId != -1) {
                        StartCoroutine(CO_PublishSteamAchievements(achievementDataEntry.achievementUniqueID, 1, accountName.Replace("@steam", "").ToString(), isComplete));
                     }
                  });
               }
            }
         }
      });
      #endif
   }

   private IEnumerator CO_PublishSteamAchievements (string uniqueAchievementId, int progressCount, string steamUserId, bool isComplete) {
      string achievementName = uniqueAchievementId;
      if (string.IsNullOrEmpty(steamUserId)) {
         D.debug("Steam Achievement Error! Invalid Steam Id: " + steamUserId);
         yield return null;
      }

      WWWForm form = new WWWForm();
      form.AddField("key", Steam.SteamStatics.STEAM_WEB_PUBLISHER_API_KEY);
      form.AddField("steamid", steamUserId);
      form.AddField("appid", Steam.SteamStatics.GAME_APPID);
      form.AddField("count", "1");
      form.AddField("name[0]", achievementName);
      form.AddField("value[0]", progressCount);

      D.adminLog("Should post achievement for user {" + steamUserId + "} {" + achievementName + "} {" + progressCount + "} {" + isComplete + "}", D.ADMIN_LOG_TYPE.Achievement);
      if (achievementName.Length > 1) {
         using (UnityWebRequest www = UnityWebRequest.Post(Steam.SteamStatics.STEAMWEBAPI_SET_USER_STATS, form)) {
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError) {
               D.error(www.error);
            }
         }
      }
   }

   #region Private Variables

   // Holds the collection of the xml translated data
   private List<AchievementGroupData> _achievementDataCollection = new List<AchievementGroupData>();

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
