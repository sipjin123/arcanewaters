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

               if (rawData.itemCategory == 0 && rawData.itemType == 0) {
                  AchievementData castData = castedData.Find(_ => _.actionType == rawData.actionType && _.achievementUniqueID == rawData.achievementUniqueID);

                  if (castData == null) {
                     D.debug("The cast data with no item is null!");
                     return;
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

               DB_Main.updateAchievementData(rawData, userId, isComplete, count);
               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  if (steamId != -1) {
                     StartCoroutine(CO_PublishSteamAchievements(actionType, resultCount, steamId.ToString()));
                  }
               });
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

               DB_Main.updateAchievementData(newData, userId, isComplete);

               UnityThreadHelper.UnityDispatcher.Dispatch(() => {
                  if (steamId != -1) {
                     StartCoroutine(CO_PublishSteamAchievements(actionType, 1, steamId.ToString()));
                  } 
               });
            }
         }
      });
      #endif
   }

   private IEnumerator CO_PublishSteamAchievements (ActionType actionType, int progressCount, string steamUserId) {
      string achievementName = "";
      switch (actionType) {
         case ActionType.OpenTreasureChest:
            achievementName = STEAM_OPENED_TREASURE_CHEST + progressCount;
            break;
         case ActionType.KillLandMonster:
            achievementName = STEAM_SLAY_LAND_MONSTERS + progressCount;
            break;
         case ActionType.CombatDie:
            achievementName = STEAM_DIE + progressCount;
            break;
         case ActionType.HarvestCrop:
            achievementName = STEAM_HARVEST_CROP + progressCount;
            break;
         case ActionType.LevelUp:
            achievementName = STEAM_REACH_LEVEL + progressCount;
            break;
         default:
            yield return null;
            break;
      }

      if (string.IsNullOrEmpty(steamUserId)) {
         D.debug("Steam Achievement Error! Invalid Steam Id: " + steamUserId);
         yield return null;
      }

      WWWForm form = new WWWForm();
      form.AddField("key", SteamLoginSystem.SteamLoginManagerServer.STEAM_WEB_PUBLISHER_API_KEY);
      form.AddField("steamid", steamUserId);
      form.AddField("appid", SteamLoginSystem.SteamLoginManagerServer.GAME_APPID);
      form.AddField("count", "1");
      form.AddField("name[0]", achievementName);
      form.AddField("value[0]", progressCount);

      if (achievementName.Length > 1) {
         using (UnityWebRequest www = UnityWebRequest.Post(STEAMWEBAPI_SET_USER_STATS, form)) {
            yield return www.SendWebRequest();
            if (www.isNetworkError || www.isHttpError) {
               Debug.Log(www.error);
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

   // The web API post command for altering achievements
   private const string STEAMWEBAPI_SET_USER_STATS = "https://partner.steam-api.com/ISteamUserStats/SetUserStatsForGame/v1/";
   
   #endregion
}
