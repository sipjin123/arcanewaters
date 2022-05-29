using System;
#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif

[Serializable]
public class AchievementData
{
   #region Public Variables

   // The name of the achievement
   public string achievementName;

   // The type of action key
   public ActionType actionType;

   // The info of the achievement
   public string achievementDescription;

   // The count needed for the achievement
   public int count;

   // The tier value of the achievement such as (Hoarder 100 / Hoarder 500 / Hoarder 1000)
   public string achievementUniqueID;

   // The value for items type that need distinction
   public int itemType;

   // The value for items category that need distinction 
   public int itemCategory;

   // The path of the icon
   public string iconPath;

   // The path of the locked icon
   public string lockedIconPath;

   // Dictates the level of achievement such as (Beginner Level x1 / Mid Level Achievement x5/ High level achievement 100x)
   public int tier = 1;

   #endregion

   public AchievementData () { }

   #if IS_SERVER_BUILD

   public AchievementData (MySqlDataReader dataReader) {
      this.actionType = (ActionType) DataUtil.getInt(dataReader, "actionTypeId");
      this.achievementName = DataUtil.getString(dataReader, "achievementName");
      this.achievementDescription = DataUtil.getString(dataReader, "achievementDescription");
      this.count = DataUtil.getInt(dataReader, "achievementCount");
      this.achievementUniqueID = DataUtil.getString(dataReader, "achievementUniqueID");
      this.itemType = DataUtil.getInt(dataReader, "achievementItemTypeID");
      this.itemCategory = DataUtil.getInt(dataReader, "achievementItemCategoryID");
      this.tier = DataUtil.getInt(dataReader, "tier");
   }

   #endif

   public AchievementData (ActionType actionType, string name, string description, string uniqueKey, int count, int typeId, int categoryId) {
      this.actionType = actionType;
      this.achievementName = name;
      this.achievementDescription = description;
      this.count = count;
      this.achievementUniqueID = uniqueKey;
      this.itemType = typeId;
      this.itemCategory = categoryId;
   }

   public static AchievementData CreateAchievementData (AchievementData copy) {
      AchievementData newData = new AchievementData();
      newData.actionType = copy.actionType;
      newData.achievementName = copy.achievementName;
      newData.achievementDescription = copy.achievementDescription;
      newData.count = copy.count;
      newData.achievementUniqueID = copy.achievementUniqueID;
      newData.itemType = copy.itemType;
      newData.itemCategory = copy.itemCategory;
      newData.iconPath = copy.iconPath;
      newData.lockedIconPath = copy.lockedIconPath;
      newData.tier = copy.tier;
      return newData;
   }
}

// Key values that is relevant to the achievements
public enum ActionType
{
   None = 0,
   LootGainTotal = 1, Craft = 2, LevelUp = 3, KillLandMonster = 4, KillSeaMonster = 5,
   OpenedLootBag = 6, CombatDie = 7, SellItem = 8, BuyItem = 9, BuyShip = 10,
   TalkToNPC = 11, NPCAcquaintance = 12, NPCCasualFriend = 13, NPCCloseFriend = 14, NPCBestFriend = 15,
   QuestComplete = 16, QuestDelivery = 17, NPCGift = 18, CannonHits = 19, SinkedShips = 20,
   ShipDie = 21, Electrocuted = 22, Poisoned = 23, Frozen = 24, HitPlayerWithCannon = 25,
   UsePotion = 26, UseStatsBuff = 27, TrashItem = 28, WeaponBuy = 29, ArmorBuy = 30,
   HeadgearBuy = 31, BuffSkillUse = 32, OffensiveSkillUse = 33, EnterCombat = 34, JumpOnBouncePad = 35,
   MineOre = 36, OpenTreasureChest = 37, HarvestCrop = 38, SellCrop = 39, WaterCrop = 40,
   PlantCrop = 41, EarnGold = 42, GatherItem = 43, OreGain = 45,
   HitSeaMonster = 46, HitEnemyShips = 47, CraftArmor = 53, CraftWeapon = 54, PetAnimal = 55, BurnEnemy = 56, FreezeEnemy = 57,
   PoisonEnemy = 58, EarlyExplorer = 59,
}