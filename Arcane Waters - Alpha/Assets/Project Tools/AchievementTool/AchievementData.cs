public class AchievementData
{
   // Key values that is relevant to the achievements
   public enum ActionType
   {
      None = 0,
      LootGainTotal = 1, Craft = 2, LevelUp = 3, KillLandMonster = 4, KillSeaMonster = 5,
      OpenedLootBag = 6, CombatDie = 7, SellItem = 8,  BuyItem = 9, BuyShip = 10,
      TalkToNPC = 11, NPCAcquaintance = 12, NPCCasualFriend = 13, NPCCloseFriend = 14, NPCBestFriend = 15,
      QuestComple = 16, QuestDelivery = 17, NPCGift = 18, CannonHits = 19, SinkedShips = 20,
      ShipDie = 21, Electrocuted = 22, Poisoned = 23, Frozen = 24, HitPlayerWithCannon = 25,
      UsePotion = 26, UseStatsBuff = 27, TrashItem = 28, WeaponBuy = 29, ArmorBuy = 30,
      HeadgearBuy = 31, BuffSkillUse = 32, OffensiveSkillUse = 33, EnterCombat = 34, JumpOnBouncePad = 35,
      MineOre = 36, OpenTreasureChest = 37, HarvestCrop = 38, SellCrop = 39, WaterCrop = 40,
      PlantCrop = 41, EarnGold = 42, GatherItem = 43
   }

   // The type of action key
   public ActionType achievementType;

   // The name of the achievement
   public string achievementName;

   // The info of the achievement
   public string achievementDescription;

   // The quantity needed for the achievement
   public int value;

   // The tier value of the achievement such as (Hoarder 100 / Hoarder 500 / Hoarder 1000)
   public string achievementKey;

   // The value for items type that need distinction
   public int valueType;

   // The value for items category that need distinction 
   public int valueCategory;

   // The path of the icon
   public string iconPath;
}