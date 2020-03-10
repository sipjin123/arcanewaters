using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using MapCreationTool.Serialization;

public class DB_MainStub : MonoBehaviour
{
   #region Public Variables

   #endregion

   public static void updateCropsXML (string rawData, int xmlId, int cropsType, bool isEnabled, string cropsName) {
   }

   public static List<XMLPair> getCropsXML () {
      return new List<XMLPair>();
   }

   public static void deleteCropsXML (int xmlId) {
   }

   public static int updateBackgroundXML (int xmlId, string rawData, string bgName) {
      return 0;
   }

   public static List<XMLPair> getBackgroundXML () {
      return new List<XMLPair>();
   }

   public static void deleteBackgroundXML (int xmlId) {
   }

   public static List<SQLEntryNameClass> getSQLDataByName (EditorSQLManager.EditorToolType editorType) {
      return new List<SQLEntryNameClass>();
   }

   public static List<SQLEntryIDClass> getSQLDataByID (EditorSQLManager.EditorToolType editorType, EquipmentToolManager.EquipmentType equipmentType = EquipmentToolManager.EquipmentType.None) {
      return new List<SQLEntryIDClass>();
   }

   public static void updateShipAbilities (int shipId, string abilityXML) {

   }

   public static void updateShipAbilityXML (string rawData, string shipAbilityName) {
   }

   public static List<string> getShipAbilityXML () {
      return new List<string>();
   }

   public static void deleteShipAbilityXML (string shipAbilityName) {
   }

   public static void updateTutorialXML (string rawData, string name, int order) {

   }

   public static void deleteTutorialXML (string name) {
   }

   public static List<string> getTutorialXML () {
      return new List<string>();
   }

   public static void updateEquipmentXML (string rawData, int typeID, EquipmentToolManager.EquipmentType equipType, string equipmentName, bool isEnabled, int equipmentTypeID) {

   }

   public static void deleteEquipmentXML (int type, EquipmentToolManager.EquipmentType equipType) {
   }

   public static List<XMLPair> getEquipmentXML (EquipmentToolManager.EquipmentType equipType) {
      return new List<XMLPair>();
   }

   public static void updatePlayerClassXML (string rawData, int key, ClassManager.PlayerStatType playerStatType) {

   }

   public static List<string> getPlayerClassXML (ClassManager.PlayerStatType playerStatType) {
      return new List<string>();
   }

   public static void deletePlayerClassXML (ClassManager.PlayerStatType playerStatType, int typeID) {

   }

   public static void updateCraftingXML (int xmlID, string rawData, string name, int typeId, int category) {

   }

   public static void deleteCraftingXML (int xmlID) {

   }

   public static List<XMLPair> getCraftingXML () {
      return new List<XMLPair>();
   }

   public static void updateAchievementXML (string rawData, string name, int xmlId) {

   }

   public static void deleteAchievementXML (string name) {
   }

   public static List<XMLPair> getAchievementXML () {
      return new List<XMLPair>();
   }

   public static void updateNPCXML (string rawData, int typeIndex) {

   }

   public static List<string> getNPCXML () {
      return new List<string>();
   }

   public static void deleteNPCXML (int typeID) {

   }

   public static List<Map> getMaps () {
      return new List<Map>();
   }

   public static MapInfo getMapInfo (string areaKey) {
      return null;
   }

   public static Dictionary<string, MapInfo> getLiveMaps () {
      return new Dictionary<string, MapInfo>();
   }

   public static List<MapVersion> getMapVersions (Map map) {
      return new List<MapVersion>();
   }

   public static string getMapLiveVersionGameData (string mapName) {
      return null;
   }

   public static string getMapVersionEditorData (MapVersion version) {
      return null;
   }

   public static List<MapSpawn> getMapSpawns () {
      return new List<MapSpawn>();
   }

   public static void createMap (MapVersion mapVersion) {

   }

   public static MapVersion createNewMapVersion (MapVersion mapVersion) {
      return null;
   }

   public static void updateMapVersion (MapVersion mapVersion) {

   }

   public static void deleteMap (string name) {

   }

   public static void deleteMapVersion (string name, int version) {

   }

   public static void setLiveMapVersion (MapVersion version) {

   }

   public static void updateShopXML (string rawData, string shopName) {

   }

   public static List<string> getShopXML () {
      return new List<string>();
   }

   public static void deleteShopXML (string shopName) {

   }

   public static void updateShipXML (string rawData, int typeIndex, Ship.Type shipType, string shipName, bool isActive) {

   }

   public static List<XMLPair> getShipXML () {
      return new List<XMLPair>();
   }

   public static void deleteShipXML (int typeID) {

   }

   public static void updateSeaMonsterXML (string rawData, int typeIndex, SeaMonsterEntity.Type enemyType, string battlerName, bool isActive) {

   }

   public static void deleteSeamonsterXML (int typeID) {
   }

   public static List<XMLPair> getSeaMonsterXML () {
      return new List<XMLPair>();
   }

   public static List<XMLPair> getLandMonsterXML () {
      return new List<XMLPair>();
   }

   public static void deleteLandmonsterXML (int typeID) {
   }

   public static void updateLandMonsterXML (string rawData, int typeIndex, Enemy.Type enemyType, string battlerName, bool isActive) {

   }

   public static void updateAbilitiesData (int userID, AbilitySQLData abilityData) {

   }

   public static List<AbilitySQLData> getAllAbilities (int userID) {
      return new List<AbilitySQLData>();
   }

   public static List<AbilityXMLContent> getDefaultAbilities () {
      return new List<AbilityXMLContent>();
   }

   public static List<SoundEffect> getSoundEffects () {
      return new List<SoundEffect>();
   }

   public static void updateSoundEffect (SoundEffect effect) {

   }

   public static void deleteSoundEffect (SoundEffect effect) {

   }

   public static void updateAchievementData (AchievementData achievementData, int userID, bool hasReached, int addedCount) {

   }

   public static AchievementData getAchievementData (int userID, ActionType actionType) {
      return new AchievementData();
   }

   public static List<AchievementData> getAchievementDataList (int userID) {
      return new List<AchievementData>();
   }

   public static void updateBattleAbilities (int skillId, string abilityName, string abilityXML, int abilityType) {

   }

   public static void deleteBattleAbilityXML (int skillId) {

   }

   public static List<AbilityXMLContent> getBattleAbilityXML () {
      return null;
   }

   public static void readTest () {

   }

   public static void createNPCRelationship (int npcId, int userId, int friendshipLevel) {

   }

   public static int getFriendshipLevel (int npcId, int userId) {
      return 0;
   }

   public static void updateNPCRelationship (int npcId, int userId, int friendshipLevel) {

   }

   public static void createQuestStatus (int npcId, int userId, int questId, int questNodeId) {

   }

   public static void updateQuestStatus (int npcId, int userId, int questId, int questNodeId) {

   }

   public static QuestStatusInfo getQuestStatus (int npcId, int userId, int questId) {
      return null;
   }

   public static List<QuestStatusInfo> getQuestStatuses (int npcId, int userId) {
      return null;
   }

   public static List<Item> getRequiredIngredients (int usrId, List<CraftingIngredients.Type> itemList) {
      return new List<Item>();
   }

   public static List<CropInfo> getCropInfo (int userId) {
      return null;
   }

   public static int insertCrop (CropInfo cropInfo) {
      return 0;
   }

   public static void waterCrop (CropInfo cropInfo) {

   }

   public static int getAccountId (string accountName, string accountPassword) {
      return -1;
   }

   public static int getAccountPermissionLevel (int accountId) {
      return 0;
   }

   public static List<UserInfo> getUsersForAccount (int accId, int userId = 0) {
      return new List<UserInfo>();
   }

   public static List<Armor> getArmorForAccount (int accId, int userId = 0) {
      return null;
   }

   public static List<Weapon> getWeaponsForAccount (int accId, int userId = 0) {
      return null;
   }

   public static List<Weapon> getWeaponsForUser (int userId) {
      return null;
   }

   public static List<Armor> getArmorForUser (int userId) {
      return null;
   }

   public static void deleteCrop (int cropId, int userId) {

   }

   public static void setWeaponId (int userId, int newWeaponId) {

   }

   public static void setArmorId (int userId, int newArmorId) {

   }

   public static bool hasItem (int userId, int itemId, int itemCategory) {
      return false;
   }

   public static void setNewLocalPosition (int userId, Vector2 localPosition, Direction facingDirection, string areaKey) {

   }

   public static void storeShipHealth (int shipId, int shipHealth) {

   }

   public static List<SiloInfo> getSiloInfo (int userId) {
      return null;
   }

   public static void addToSilo (int userId, Crop.Type cropType, int amount = 1) {

   }

   public static List<TutorialInfo> getTutorialInfo (int userId) {
      return null;
   }

   public static TutorialData completeTutorialStep (int userId, int stepIndex) {
      return new TutorialData();
   }

   public static int getUserId (string username) {
      return 0;
   }

   public static void addGold (int userId, int amount) {

   }

   public static void addGoldAndXP (int userId, int gold, int XP) {

   }

   public static void saveBugReport (NetEntity player, string subject, string bugReport) {

   }

   public static int storeChatLog (int userId, string message, DateTime dateTime, ChatInfo.Type chatType) {
      return 0;
   }

   public static List<ChatInfo> getChat (ChatInfo.Type chatType, int minutes) {
      return new List<ChatInfo>();
   }

   public static int getAccountId (int userId) {
      return 0;
   }

   public static int getAccountStatus (int accountId) {
      return 0;
   }

   public static UserInfo getUserInfo (int userId) {
      return null;
   }

   public static UserInfo getUserInfo (string userName) {
      return null;
   }

   public static ShipInfo getShipInfo (int userId) {
      return null;
   }

   public static Armor getArmor (int userId) {
      return null;
   }

   public static Weapon getWeapon (int userId) {
      return null;
   }

   public static UserObjects getUserObjects (int userId) {
      return null;
   }

   public static int createUser (int accountId, int usrAdminFlag, UserInfo userInfo, Area area) {
      return 0;
   }

   public static Item createNewItem (int userId, Item baseItem) {
      return null;
   }

   public static int insertNewArmor (int userId, int armorType, ColorType color1, ColorType color2) {
      return 0;
   }

   public static int insertNewWeapon (int userId, int weaponType, ColorType color1, ColorType color2) {
      return 0;
   }

   public static void setItemOwner (int userId, int itemId) {

   }

   public static void deleteAllFromTable (int accountId, int userId, string table) {

   }

   public static void deleteUser (int accountId, int userId) {

   }

   public static ShipInfo createStartingShip (int userId) {
      return null;
   }

   public static ShipInfo createShipFromShipyard (int userId, ShipInfo shipyardInfo) {
      return null;
   }

   public static void setCurrentShip (int userId, int shipId) {

   }

   public static int getGold (int userId) {
      return 0;
   }

   public static int getGems (int accountId) {
      return 0;
   }

   public static int getItemID (int userId, int itmCategory, int itmType) {
      return 0;
   }

   public static void createOrUpdateItemListCount (int userId, List<Item> itemList) {

   }

   public static Item createItemOrUpdateItemCount (int userId, Item baseItem) {
      return null;
   }

   public static void transferItem (Item item, int fromUserId, int toUserId, int amount) {

   }

   public static void updateItemQuantity (int userId, int itmId, int itmCount) {

   }

   public static void decreaseQuantityOrDeleteItem (int userId, int itmId, int deductCount) {

   }

   public static int getItemCount (int userId) {
      return 0;
   }

   public static int getItemCount (int userId, int itemCategory, int itemType) {
      return 0;
   }

   public static int getItemCount (int userId, Item.Category[] categories) {
      return 0;
   }

   public static int getItemCount (int userId, Item.Category[] categories, List<int> itemIdsToFilter,
      List<Item.Category> categoriesToFilter) {
      return 0;
   }

   public static List<Item> getItems (int userId, Item.Category[] categories, int page, int itemsPerPage) {
      return null;
   }

   public static List<Item> getItems (int userId, Item.Category[] categories, int page, int itemsPerPage,
      List<int> itemIdsToFilter, List<Item.Category> categoriesToFilter) {
      return null;
   }

   public static List<Item> getItems (int userId, Item.Category[] categories, int page, int itemsPerPage,
      List<int> itemIdsToFilter) {
      return null;
   }

   public static List<Item> getCraftingIngredients (int usrId, List<CraftingIngredients.Type> ingredientTypes) {
      return null;
   }

   public static Item getItem (int userId, int itemId) {
      return null;
   }

   public static Item getFirstItem (int userId, Item.Category itemCategory, int itemTypeId) {
      return null;
   }

   public static void addGems (int accountId, int amount) {

   }

   public static int insertNewUsableItem (int userId, UsableItem.Type itemType, ColorType color1, ColorType color2) {
      return 0;
   }

   public static int insertNewUsableItem (int userId, UsableItem.Type itemType, Ship.SkinType skinType) {
      return 0;
   }

   public static int insertNewUsableItem (int userId, UsableItem.Type itemType, HairLayer.Type hairType) {
      return 0;
   }

   public static void setHairColor (int userId, ColorType newColor) {

   }

   public static void setHairType (int userId, HairLayer.Type newType) {

   }

   public static void setShipSkin (int shipId, Ship.SkinType newSkin) {

   }

   public static int deleteItem (int userId, int itemId) {
      return 0;
   }

   public static Stats getStats (int userId) {
      return null;
   }

   public static Jobs getJobXP (int userId) {
      return null;
   }

   public static GuildInfo getGuildInfo (int guildId) {
      return null;
   }

   public static List<UserInfo> getUsersForGuild (int guildId) {
      return new List<UserInfo>();
   }

   public static int createGuild (string guildName, Faction.Type guildFaction) {
      return 0;
   }

   public static void assignGuild (int userId, int guildId) {

   }

   public static void addJobXP (int userId, Jobs.Type jobType, Faction.Type faction, int XP) {

   }

   public static void insertIntoJobs (int userId) {

   }

   public static List<ShipInfo> getShips (int userId, int page, int shipsPerPage) {
      return null;
   }

   public static void addToTradeHistory (int userId, TradeHistoryInfo tradeInfo) {

   }

   public static int getTradeHistoryCount (int userId) {
      return 0;
   }

   public static List<TradeHistoryInfo> getTradeHistory (int userId, int page, int tradesPerPage) {
      return null;
   }

   public static void pruneJobHistory (DateTime untilDate) {
   }

   public static List<LeaderBoardInfo> calculateLeaderBoard (Jobs.Type jobType, Faction.Type faction,
      LeaderBoardsManager.Period period, DateTime startDate, DateTime endDate) {
      return null;
   }

   public static void deleteLeaderBoards (LeaderBoardsManager.Period period) {
   }

   public static void updateLeaderBoards (List<LeaderBoardInfo> entries) {
   }

   public static void updateLeaderBoardDates (LeaderBoardsManager.Period period,
      DateTime startDate, DateTime endDate) {
   }

   public static DateTime getLeaderBoardEndDate (LeaderBoardsManager.Period period) {
      return DateTime.UtcNow;
   }

   public static void getLeaderBoards (LeaderBoardsManager.Period period, Faction.Type boardFaction, out List<LeaderBoardInfo> farmingEntries,
      out List<LeaderBoardInfo> sailingEntries, out List<LeaderBoardInfo> exploringEntries, out List<LeaderBoardInfo> tradingEntries,
      out List<LeaderBoardInfo> craftingEntries, out List<LeaderBoardInfo> miningEntries) {
      farmingEntries = null;
      sailingEntries = null;
      exploringEntries = null;
      tradingEntries = null;
      craftingEntries = null;
      miningEntries = null;
   }

   public static void createFriendship (int userId, int friendUserId, Friendship.Status friendshipStatus, DateTime lastContactDate) {
   }

   public static void updateFriendship (int userId, int friendUserId, Friendship.Status friendshipStatus, DateTime lastContactDate) {
   }

   public static void deleteFriendship (int userId, int friendUserId) {
   }

   public static FriendshipInfo getFriendshipInfo (int userId, int friendUserId) {
      return null;
   }

   public static List<FriendshipInfo> getFriendshipInfoList (int userId, Friendship.Status friendshipStatus, int page, int friendsPerPage) {
      return null;
   }

   public static int getFriendshipInfoCount (int userId, Friendship.Status friendshipStatus) {
      return 0;
   }

   public static int createMail (MailInfo mailInfo) {
      return 0;
   }

   public static void updateMailReadStatus (int mailId, bool isRead) {
   }

   public static void deleteMail (int mailId) {
   }

   public static MailInfo getMailInfo (int mailId) {
      return null;
   }

   public static List<MailInfo> getMailInfoList (int userId, int page, int mailsPerPage) {
      return null;
   }

   public static int getMailInfoCount (int userId) {
      return 0;
   }

   public static int getMinimumClientGameVersionForWindows () {
      return 0;
   }

   public static int getUsrAdminFlag (int accountId) {
      return 0;
   }

   public static int getMinimumClientGameVersionForMac () {
      return 0;
   }

   public static int getMinimumClientGameVersionForLinux () {
      return 0;
   }

   public static int getMinimumToolsVersionForWindows () {
      return 0;
   }

   public static int getMinimumToolsVersionForMac () {
      return 0;
   }

   public static int createVoyageGroup (VoyageGroupInfo groupInfo) {
      return 0;
   }

   public static VoyageGroupInfo getVoyageGroup (int groupId) {
      return null;
   }

   public static void updateVoyageGroupQuickmatchStatus (int groupId, bool isQuickmatchEnabled) {

   }

   public static void deleteVoyageGroup (int groupId) {

   }

   public static VoyageGroupInfo getBestVoyageGroupForQuickmatch (string areaKey) {
      return null;
   }

   public static int getVoyageGroupForMember (int userId) {
      return 0;
   }

   public static List<int> getVoyageGroupMembers (int groupId) {
      return null;
   }

   public static void addMemberToVoyageGroup (int groupId, int userId) {

   }

   public static void deleteMemberFromVoyageGroup (int groupId, int userId) {

   }

   public static void setServerFromConfig () {

   }

   public static void createXmlTemplatesTable () {

   }

   public static void saveXmlTemplate (string xmlName, string xmlContent) {

   }

   public static void createJsonEnumsTable () {

   }

   public static void saveJsonEnum (string jsonName, string jsonContent) {

   }

   public static bool updateDeploySchedule (long scheduleDateAsTicks, int buildVersion) {
      return false;
   }

   public static DeployScheduleInfo getDeploySchedule () {
      return null;
   }


   public static bool cancelDeploySchedule () {
      return false;
   }

   /*

   public static void refillSupplies (int userId) {

   }

   public static int createAccount (string accountName, string accountPassword, string accountEmail, int validated) {
      return 0;
   }

   public static void deleteAccount (int accountId) {

   }

   public static void setSupplies (int shipId, int suppliesAmount) {

   }

   public static List<PortInfo> getPorts () {
      return null;
   }

   public static PortCargoSummary getPortCargoSummary (Barter.Type barterType, int specificPortId = 0) {
      return null;
   }

   public static ShipCargoSummary getShipCargoSummaryForUser (int userId) {
      return null;
   }

   public static int getShipCount (int userId) {
      return 0;
   }

   public static int getPortCargoAmount (int portId, Cargo.Type cargoType, Barter.Type barterType) {
      return 0;
   }

   public static void removeCargoFromPort (int portId, Cargo.Type cargoType, Barter.Type barterType, int amount) {

   }

   public static void addGoldAndXP (int userId, int gold, int XP, int tradePermitChange = 0) {

   }

   public static void addCargoToShip (int shipId, Barter.Type barterType, Cargo.Type cargoType, int amount) {

   }

   public static void deleteEmptyCargoRow (int shipId, Cargo.Type cargoType) {

   }

   public static TradeRecord insertTradeRecord (int userId, int shipId, int portId, Barter.Type barterType, Cargo.Type cargoType, int amount, int unitPrice, int unitXP) {
      return null;
   }

   public static TradeHistory getTradeHistory (int userId) {
      return null;
   }

   public static void incrementTradePermits () {

   }

   public static List<int> getTestUserIds () {
      return null;
   }

   public static List<BuildingLoc> getBuildingLocs () {
      return null;
   }

   public static List<Area> getAreas () {
      return null;
   }

   public static DateTime getLastUnlock (int userId, int areaId, float localX, float localY) {
      return DateTime.MinValue;
   }

   public static void storeUnlock (int accountId, int userId, int areaId, float localX, float localY, int gold, int itemId) {

   }

   public static void setFlagship (int userId, int shipId) {

   }

   public static List<NPC_Info> getNPCs (int areaId) {
      return null;
   }

   public static void insertNPC (NPC_Info info) {

   }

   public static void deleteNPCs () {

   }

   public static List<Shop_ShipInfo> getShopShips () {
      return null;
   }

   public static int insertShip (PortInfo port, Shop_ShipInfo shipInfo) {
      return 0;
   }

   public static void deleteShopShips (PortInfo port) {

   }

   public static List<Shop_ItemInfo> getShopItems () {
      return null;
   }

   public static void deleteShopItems (PortInfo port) {

   }

   public static int insertItem (PortInfo port, Shop_ItemInfo itemInfo) {
      return 0;
   }

   public static ItemInfo createItemFromStock (int userId, Shop_ItemInfo itemStock) {
      return null;
   }

   public static int unlockDiscovery (int userId, Discovery.Type discoveryType, int primaryKeyID, int areaId) {
      return 0;
   }

   public static List<Discovery> getDiscoveries (int userId, int areaId) {
      return null;
   }

   public static List<SeaMonsterSpawnInfo> getSeaMonsterSpawns () {
      return null;
   }

   public static void deleteAllSeaMonsterSpawns () {

   }

   public static int insertSeaMonsterSpawn (int areaId, SeaMonster.Type monsterType, float localX, float localY) {
      return 0;
   }

   public static int insertArea (string areaName, Area.Type areaType, TileType tileType, int worldX, int worldY, int versionNumber, int serverId) {
      return 0;
   }

   public static void deleteOldTreasureAreas () {

   }

   public static void insertServer (string address, int port) {

   }

   public static int getServerId (string address, int port) {
      return 0;
   }

   public static int insertSite (int innerAreaId, int outerAreaId, string siteName, int siteLevel, Site.Type siteType, float localX, float localY) {
      return 0;
   }

   public static SiteLoc getSiteLoc (int siteId) {
      return new SiteLoc();
   }

   public static List<SiteLoc> getSiteLocs () {
      return null;
   }

   */

   #region Private Variables

   #endregion
}

#if IS_SERVER_BUILD == false
public class DB_Main : DB_MainStub { }
#endif
