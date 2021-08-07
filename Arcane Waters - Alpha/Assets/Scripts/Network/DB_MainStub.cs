using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;
using MapCreationTool.Serialization;
using MapCustomization;
using System.Threading.Tasks;
using Steam;
using Store;

public class DB_MainStub : MonoBehaviour
{
   #region Public Variables

   #endregion

   #region Nubis Requests

   public static string fetchSingleBlueprint (int bpId, int usrId) {
      return "";
   }

   public static string fetchZipRawData () {
      return "";
   }

   public static string userInventory (int usrId, Item.Category[] categoryFilter, int[] itemIdsToExclude,
      bool mustExcludeEquippedItems, int currentPage, int itemsPerPage, Item.DurabilityFilter itemDurabilityFilter) {
      return "";
   }

   public static int userInventoryCount (int usrId, Item.Category[] categoryFilter, int[] itemIdsToExclude,
      bool mustExcludeEquippedItems, Item.DurabilityFilter itemDurabilityFilter) {
      return 0;
   }

   public static string fetchXmlVersion () {
      return "";
   }

   public static string fetchCraftableHats (int usrId) {
      return "";
   }

   public static string fetchCraftableArmors (int usrId) {
      return "";
   }

   public static string fetchCraftableWeapons (int usrId) {
      return "";
   }

   public static string fetchCraftingIngredients (int usrId) {
      return "";
   }

   public static string fetchEquippedItems (int usrId) {
      return "";
   }

   public static string fetchMapData (string mapName, int version) {
      return "";
   }

   public static List<AbilitySQLData> userAbilities (int usrId, AbilityEquipStatus abilityEquipStatus) {
      return new List<AbilitySQLData>();
   }

   #endregion

   public static string getMapContents () {
      return "";
   }

   public static TreasureStateData getTreasureStateForChest (int userId, int chestId, string areaId) {
      return new TreasureStateData();
   }

   public static int updateTreasureStatus (int userId, int treasureId, string areaKey) {
      return 0;
   }

   public static List<AuctionItemData> getAuctionList (int pageNumber, int rowsPerPage, Item.Category[] categoryFilter, int userId, AuctionPanel.ListFilter auctionFilter) {
      return new List<AuctionItemData>();
   }

   public static int getAuctionListCount (int userId, Item.Category[] filterData, AuctionPanel.ListFilter auctionFilter) {
      return 0;
   }

   public static List<AuctionItemData> getAuctionsToDeliver () {
      return null;
   }

   public static void deliverAuction (int auctionId, int mailId, int recipientUserId) {

   }

   public static void deleteAuction (int auctionId) {

   }

   public static int createAuction (int sellerUserId, string sellerName, int mailId, DateTime expiryDate,
      int highestBidPrice, int buyoutPrice, Item.Category itemCategory, string itemName, int itemCount) {
      return 0;
   }

   public static void updateAuction (int auctionId, int highestBidUser, int highestBidPrice, DateTime expiryDate) {

   }

   public static AuctionItemData getAuction (int auctionId, bool readItemData) {
      return null;
   }

   public static void addBidderOnAuction (int auctionId, int userId) {

   }

   public static void updateNPCQuestXML (string rawData, int typeIndex, string xmlName, int isActive) {
   }

   public static List<XMLPair> getNPCQuestXML () {
      return new List<XMLPair>();
   }

   public static void deleteNPCQuestXML (int typeID) {
   }

   public static List<XMLPair> getProjectileXML () {
      return new List<XMLPair>();
   }

   public static void updateBiomeTreasureDrops (int xmlId, string rawXmlContent, Biome.Type biomeType) {
   }

   public static List<XMLPair> getBiomeTreasureDrops () {
      return new List<XMLPair>();
   }

   public static void writeZipData (byte[] bytes, int slot) {
   }

   public static string getXmlContent (string tableName, EditorSQLManager.EditorToolType toolType = EditorSQLManager.EditorToolType.None) {
      return "";
   }

   public static List<RawPaletteToolData> getPaletteXmlContent (string tableName) {
      return new List<RawPaletteToolData>();
   }

   public static int getLatestXmlVersion () {
      return 0;
   }

   public static string getLastUpdate (EditorSQLManager.EditorToolType editorType) {
      return "";
   }

   public static void saveComplaint (int sourceUsrId, int sourceAccId, string sourceUsrName, string sourceEmail, string sourceIPAddress, int targetUsrId, int targetAccId, string targetUsrName, string ticketDescription, string playerPosition, string playerArea, string chatLog, byte[] screenshotBytes, string sourceMachineIdentifier, int deploymentId) {

   }

   public static ChatInfo getLatestChatInfo () {
      return new ChatInfo();
   }

   public static void updateCompanionExp (int xmlId, int userId, int exp) {
   }

   public static void updateCompanions (int xmlId, int userId, string companionName, int companionLevel, int companionType, int equippedSlot, string iconPath, int companionExp) {

   }

   public static void updateCompanionRoster (int xmlId, int userId, int slot) {

   }

   public static List<CompanionInfo> getCompanions (int userId) {
      return new List<CompanionInfo>();
   }

   public static void updateAccountMode (int accoundId, bool isSinglePlayer) {

   }

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

   public static List<SQLEntryIDClass> getSQLDataByID (EditorSQLManager.EditorToolType editorType, EquipmentType equipmentType = EquipmentType.None) {
      return new List<SQLEntryIDClass>();
   }

   public static void updateShipAbilities (int shipId, string abilityXML) {

   }

   public static void updateShipAbilityXML (string rawData, string shipAbilityName, int xmlId) {
   }

   public static List<XMLPair> getShipAbilityXML () {
      return new List<XMLPair>();
   }

   public static void deleteShipAbilityXML (int xmlId) {
   }

   public static void updateEquipmentXML (string rawData, int typeID, EquipmentType equipType, string equipmentName, bool isEnabled) {

   }

   public static void deleteEquipmentXML (int type, EquipmentType equipType) {
   }

   public static List<XMLPair> getEquipmentXML (EquipmentType equipType) {
      return new List<XMLPair>();
   }

   public static List<ItemDefinition> getItemDefinitions () {
      return new List<ItemDefinition>();
   }

   public static ItemInstance getItemInstance (object command, int userId, int itemDefinitionId) {
      return null;
   }

   public static void createOrAppendItemInstance (object command, ItemInstance item) {

   }

   public static void createNewItemInstance (object command, ItemInstance itemInstance) {

   }

   public static void increaseItemInstanceCount (object command, int id, int increaseBy) {

   }

   public static void decreaseOrDeleteItemInstance (object command, int id, int decreaseBy) {

   }

   public static void createNewItemDefinition (ItemDefinition definition) {

   }

   public static void updateItemDefinition (ItemDefinition definition) {

   }

   public static void deleteItemDefinition (int id) {

   }

   public static List<ItemInstance> getItemInstances (object command, int ownerUserId, ItemDefinition.Category category) {
      return new List<ItemInstance>();
   }

   public static void updateCraftingXML (int xmlID, string rawData, string name, int typeId, int category) {

   }

   public static void deleteCraftingXML (int xmlID) {

   }

   public static string getTooltipXmlContent () {
      return "";
   }

   public static List<TooltipSqlData> getTooltipData () {
      return new List<TooltipSqlData>();
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

   public static List<Perk> getPerkPointsForUser (int usrId) {
      return new List<Perk>();
   }

   public static void assignPerkPoint (int usrId, int perkId) {
   }

   public static int getUnassignedPerkPoints (int usrId) {
      return 0;
   }

   public static int getAssignedPointsByPerkId (int usrId, int perkId) {
      return 0;
   }

   public static void addPerkPointsForUser (int usrId, int perkId, int perkPoints) {
   }

   public static void addPerkPointsForUser (int usrId, List<Perk> perks) {
   }

   public static void updatePerksXML (string xmlContent, int perkId) {
   }

   public static List<PerkData> getPerksXML () {
      return new List<PerkData>();
   }

   public static void deletePerkXML (int xmlId) {
   }

   public static void upsertBook (string content, string title, int bookId) {
   }

   public static void deleteBookByID (int bookId) {
   }

   public static BookData getBookById (int bookId) {
      return new BookData();
   }

   public static List<BookData> getBooksList () {
      return new List<BookData>();
   }

   public static void duplicateDiscovery (DiscoveryData data) {

   }

   public static void upsertDiscovery (DiscoveryData data) {

   }

   public static DiscoveryData getDiscoveryById (int discoveryId) {
      return new DiscoveryData();
   }

   public static void deleteDiscoveryById (int discoveryId) {

   }

   public static List<DiscoveryData> getDiscoveriesList () {
      return new List<DiscoveryData>();
   }

   public static void completeStepForUser (int userId, int stepId) {
   }

   public static void completeStepForUser (int userId, string actionCode) {
   }

   public static bool userHasCompletedAction (int userId, string actionCode) {
      return false;
   }

   public static void updateNPCXML (string rawData, int typeIndex) {

   }

   public static List<string> getNPCXML () {
      return new List<string>();
   }

   public static void deleteNPCXML (int typeID) {

   }

   public static void setCustomHouseBase (object command, int userId, int baseMapId) {

   }

   public static void setCustomFarmBase (object command, int userId, int baseMapId) {

   }

   public static MapCustomizationData getMapCustomizationData (object command, int mapId, int userId) {
      return null;
   }

   public static PrefabState getMapCustomizationChanges (object command, int mapId, int userId, int prefabId) {
      return default;
   }

   public static void setMapCustomizationChanges (object command, int mapId, int userId, PrefabState changes) {

   }

   public static int getMapId (string areaKey) {
      return -1;
   }

   public static List<Map> getMaps (object command) {
      return new List<Map>();
   }

   public static List<Map> getMaps () {
      return new List<Map>();
   }

   public static string getMapInfo (string areaKey) {
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

   public static MapVersion getLatestMapVersionEditor (Map map, bool infiniteCommandTimeout = false) {
      return null;
   }

   public static List<MapSpawn> getMapSpawns () {
      return new List<MapSpawn>();
   }

   public static void createMap (MapVersion mapVersion) {

   }

   public static void duplicateMapGroup (int mapId, int newCreatorId) {

   }

   public static void updateMapDetails (Map map) {

   }

   public static MapVersion createNewMapVersion (MapVersion mapVersion) {
      return null;
   }

   public static void updateMapVersion (MapVersion mapVersion, bool infiniteCommandTimeout = false) {

   }

   public static void deleteMap (int id) {

   }

   public static void deleteMapGroup (int mapId) {

   }

   public static void deleteMapVersion (MapVersion version) {

   }

   public static void publishLatestVersionForAllGroup (int mapId) {

   }

   public static void setLiveMapVersion (MapVersion version) {

   }

   public static void updateShopXML (string rawData, string shopName, int xmlId) {

   }

   public static List<XMLPair> getShopXML () {
      return new List<XMLPair>();
   }

   public static void deleteShopXML (int xmlId) {

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

   public static bool hasAbility (int userId, int abilityId) {
      return false;
   }

   public static void updateAbilitySlot (int userID, int abilityId, int slotNumber) {

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

   public static List<AchievementData> getAchievementData (int userID, ActionType actionType) {
      return new List<AchievementData>();
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

   public static bool hasFriendshipLevel (int npcId, int userId) {
      return false;
   }

   public static int getFriendshipLevel (int npcId, int userId) {
      return 0;
   }

   public static void updateNPCRelationship (int npcId, int userId, int friendshipLevel) {

   }

   public static void createQuestStatus (int npcId, int userId, int questId, int questNodeId) {

   }

   public static List<Item> getRequiredItems (List<Item> itemList, int userId) {
      return new List<Item>();
   }

   public static void updateQuestStatus (int npcId, int userId, int questId, int questNodeId, int dialogueId) {

   }

   public static QuestStatusInfo getQuestStatus (int npcId, int userId, int questId, int questNodeId) {
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

   public static int insertCrop (CropInfo cropInfo, string areaKey) {
      return 0;
   }

   public static void waterCrop (CropInfo cropInfo) {

   }

   public static List<PendingActionInfo> getPendingActions (PendingActionServerType serverType) {
      return new List<PendingActionInfo>();
   }

   public static void completePendingAction (int pendingActionId) {

   }

   public static PenaltyInfo getPenaltyInfoForAccount (int accId, PenaltyType penaltyType) {
      return new PenaltyInfo();
   }

   public static void liftPenaltyForAccount (int accId, PenaltyType penaltyType) {

   }

   public static PenaltyStatus penalizeAccount (PenaltyInfo penaltyInfo) {
      return PenaltyStatus.None;
   }

   public static int getAccountId (string accountName, string accountPassword, string accountPasswordCapsLock) {
      return -1;
   }

   public static int getOverriddenAccountId (string accountName) {
      return -1;
   }

   public static int getSteamAccountId (string accountName) {
      return -1;
   }

   public static string getAccountName (int userId) {
      return "";
   }

   public static Tuple<int, int, string> getUserDataTuple (string username) {
      return Tuple.Create(0, 0, "");
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

   public static List<Hat> getHatsForAccount (int accId, int userId = 0) {
      return new List<Hat>();
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

   public static void setHatId (int userId, int newHatId) {

   }


   public static void setArmorId (int userId, int newArmorId) {

   }

   public static bool hasItem (int userId, int itemId, int itemCategory) {
      return false;
   }

   public static void setNewLocalPosition (int userId, Vector2 localPosition, Direction facingDirection, string areaKey) {

   }

   public static void setNewLocalPositionWhenInArea (List<int> userIds, List<string> areaKeys, Vector2 localPosition, Direction facingDirection, string areaKey) {

   }

   public static void storeShipHealth (int shipId, int shipHealth) {

   }

   public static List<SiloInfo> getSiloInfo (int userId) {
      return null;
   }

   public static void addToSilo (int userId, Crop.Type cropType, int amount = 1) {

   }

   public static int getUserId (string username) {
      return 0;
   }

   public static void addGold (int userId, int amount) {

   }

   public static void addGoldAndXP (int userId, int gold, int XP) {

   }

   public static long saveBugReport (NetEntity player, string subject, string bugReport, int ping, int fps, string playerPosition, byte[] screenshotBytes, string screenResolution, string operatingSystem, int deploymentId, string steamState, string ipAddress) {
      return -1;
   }

   public static void saveBugReportScreenshot (NetEntity player, long bugId, byte[] screenshotBytes) {

   }

   public static int storeChatLog (int userId, string userName, string message, DateTime dateTime, ChatInfo.Type chatType, string ipAddress) {
      return 0;
   }

   public static List<ChatInfo> getChat (ChatInfo.Type chatType, int seconds, string ipAddress, bool hasInterval = true, int limit = 0) {
      return new List<ChatInfo>();
   }

   public static int getAccountId (int userId) {
      return 0;
   }

   public static int getAccountStatus (int accountId) {
      return 0;
   }

   public static string getUserInfoJSON (int userId) {
      return "";
   }

   public static UserInfo getUserInfoById (int userId) {
      return null;
   }

   public static UserInfo getUserInfo (string userName) {
      return null;
   }

   public static List<UserInfo> getUserInfoList (int[] userIdArray) {
      return null;
   }

   public static List<int> getAllUserIds () {
      return null;
   }

   public static ShipInfo getShipInfo (int userId) {
      return null;
   }

   public static ShipInfo getShipInfoForUser (int userId) {
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

   public static string getUserName (int userId) {
      return "";
   }

   public static int createUser (int accountId, int usrAdminFlag, UserInfo userInfo, Area area) {
      return 0;
   }

   public static Item createNewItem (int userId, Item baseItem) {
      return null;
   }

   public static int insertNewArmor (int userId, int armorType, string palettes) {
      return 0;
   }

   public static int insertNewWeapon (int userId, int weaponType, string palettes) {
      return 0;
   }

   public static void setItemOwner (int userId, int itemId) {

   }

   public static void deleteAllFromTable (int accountId, int userId, string table) {

   }

   public static void deleteUser (int accountId, int userId) {

   }

   public static RemoteSetting getRemoteSetting (string settingName) {
      return null;
   }

   public static bool setRemoteSetting (string rsName, string rsValue, RemoteSetting.RemoteSettingValueType rsValueType) {
      return false;
   }

   public static RemoteSettingCollection getRemoteSettings (string[] settingNames) {
      return null;
   }

   public static bool setRemoteSettings (RemoteSettingCollection collection) {
      return false;
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

   public static void updateItemDurability (int userId, int itemId, int durability) {

   }

   public static int getItemDurability (int userId, int itemId) {
      return 0;
   }

   public static void decreaseQuantityOrDeleteItem (int userId, int itmId, int deductCount) {

   }

   public static int getItemCountByType (int userId, int itemCategory, int itemType) {
      return 0;
   }

   public static int getItemCountByCategory (int userId, Item.Category[] categories) {
      return 0;
   }

   public static int getItemCount (int userId, Item.Category[] categories, int[] itemIdsToFilter, Item.Category[] categoriesToFilter) {
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

   public static void updateItemShortcut (int userId, int slotNumber, int itemId) {

   }

   public static void deleteItemShortcut (int userId, int slotNumber) {

   }

   public static List<ItemShortcutInfo> getItemShortcutList (int userId) {
      return null;
   }

   public static void addGems (int accountId, int amount) {

   }

   public static int insertNewUsableItem (int userId, UsableItem.Type itemType, string palettes) {
      return 0;
   }

   public static int insertNewUsableItem (int userId, UsableItem.Type itemType, Ship.SkinType skinType) {
      return 0;
   }

   public static int insertNewUsableItem (int userId, UsableItem.Type itemType, HairLayer.Type hairType) {
      return 0;
   }

   public static void setHairColor (int userId, string newPalette) {

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

   public static string getGuildInfoJSON (int guildId) {
      return null;
   }

   public static List<UserInfo> getUsersForGuild (int guildId) {
      return new List<UserInfo>();
   }

   public static int getUserGuildId (int userId) {
      return -1;
   }

   public static int getMemberCountForGuild (int guildId) {
      return 0;
   }

   public static int createGuild (GuildInfo guildInfo) {
      return 0;
   }

   public static void deleteGuild (int guildId) {

   }

   public static void deleteGuildRanks (int guildId) {

   }

   public static void deleteGuildRank (int guildId, int rankId) {

   }

   public static void assignGuild (int userId, int guildId) {

   }

   public static void assignRankGuild (int userId, int guildRankId) {

   }

   public static int getLowestRankIdGuild (int guildId) {
      return -1;
   }

   public static void createRankGuild (GuildRankInfo rankInfo) {

   }

   public static void updateRankGuild (GuildRankInfo rankInfo) {

   }

   public static void updateRankGuildByID (GuildRankInfo rankInfo, int id) {

   }

   public static List<GuildRankInfo> getGuildRankInfo (int guildId) {
      return null;
   }

   public static int getGuildMemberPermissions (int userId) {
      return 0;
   }

   public static int getGuildMemberRankId (int userId) {
      return -1;
   }

   public static int getGuildMemberRankPriority (int userId) {
      return -1;
   }

   public static void addJobXP (int userId, Jobs.Type jobType, int XP) {

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

   public static List<LeaderBoardInfo> calculateLeaderBoard (Jobs.Type jobType,
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

   public static void getLeaderBoards (LeaderBoardsManager.Period period, out List<LeaderBoardInfo> farmingEntries,
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

   public static List<int> getUserIdsHavingPendingFriendshipRequests (DateTime startDate) {
      return null;
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

   public static List<MailInfo> getSentMailInfoList (int senderUsrId, int page, int mailsPerPage) {
      return null;
   }

   public static int getMailInfoCount (int userId) {
      return 0;
   }

   public static int getSentMailInfoCount (int userId) {
      return 0;
   }

   public static List<int> getUserIdsHavingUnreadMail (DateTime startDate) {
      return null;
   }

   public static bool hasUnreadMail (int userId) {
      return false;
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

   public static int getMinimumToolsVersionForLinux () {
      return 0;
   }

   public static void setServerFromConfig () {

   }

   public static T exec<T> (Func<object, T> action) {
      return default;
   }

   public static void exec (Action<object> action) {

   }

   public static async Task<T> execAsync<T> (Func<object, T> action) {
      await Task.CompletedTask;
      return default;
   }

   public static async Task execAsync (Action<object> action) {
      await Task.CompletedTask;
   }

   public static bool updateDeploySchedule (long scheduleDateAsTicks, int buildVersion) {
      return false;
   }

   public static DeployScheduleInfo getDeploySchedule () {
      return null;
   }

   public static bool finishDeploySchedule () {
      return true;
   }

   public static bool cancelDeploySchedule () {
      return false;
   }

   public static void createServerHistoryEvent (DateTime eventDate, ServerHistoryInfo.EventType eventType, int serverVersion, int serverPort) {
   }

   public static List<ServerHistoryInfo> getServerHistoryList (int serverPort, long startDateBinary, int maxRows) {
      return new List<ServerHistoryInfo>();
   }

   public static List<int> getOnlineServerList () {
      return new List<int>();
   }

   public static void updateServerShutdown (bool shutdown) {

   }

   public static bool getServerShutdown () {
      return false;
   }

   public static bool isMasterServerOnline () {
      return false;
   }

   public static void pruneServerHistory (DateTime untilDate) {
   }

   public static bool setGameMetric (string machineId, string processId, string processName, string name, string value, Metric.MetricValueType valueType, string description = "") {
      return false;
   }

   public static MetricCollection getGameMetrics (string name) {
      return null;
   }

   public static void clearOldGameMetrics (int maxLifetimeSeconds = 300) {
   }

   public static int getTotalPlayersCount () {
      return 0;
   }

   protected static bool setMetric (string machineId, string serverAddress, string serverPort, string keySuffix, string value) {
      return false;
   }

   public static bool setMetricPlayersCount (string machineId, string serverAddress, string serverPort, int playerCount) {
      return false;
   }

   public static bool setMetricAreaInstancesCount (string machineId, string serverAddress, string serverPort, int areaInstancesCount) {
      return false;
   }

   public static bool setMetricPort (string machineId, string processName, string PID, int port) {
      return false;
   }

   public static bool setMetricIP (string machineId, string processName, string PID, string ip) {
      return false;
   }

   public static bool setMetricUptime (string machineId, string processName, string PID, long uptime) {
      return false;
   }

   public static void updatePaletteXML (string rawData, string name, int xmlId, int isEnabled, string tag) {

   }

   public static void deletePaletteXML (string name) {

   }

   public static void deletePaletteXML (int id) {

   }

   public static int getPaletteTagID (string tag) {
      return -1;
   }

   public static List<XMLPair> getPaletteXML (bool onlyEnabledPalettes) {
      return new List<XMLPair>();
   }

   public static List<XMLPair> getPaletteXML (int tagId) {
      return new List<XMLPair>();
   }

   public static List<XMLPair> getPaletteXML (int tagId, string subcategory) {
      return new List<XMLPair>();
   }

   public static int createAccount (string accountName, string accountPassword, string accountEmail, int validated) {
      return 0;
   }

   public static void storeGameAccountLoginEvent (int usrId, int accId, string usrName, string ipAddress, string machineIdent, int deploymentId) {

   }

   public static void storeGameUserCreateEvent (int usrId, int accId, string usrName, string ipAddress) {

   }

   public static void storeGameUserDestroyEvent (int usrId, int accId, string usrName, string ipAddress) {

   }

   public static void setAdmin (int userId, int adminFlag) {

   }

   public static void addUnlockedBiome (int userId, Biome.Type biome) {

   }

   public static bool isBiomeUnlockedForUser (int userId, Biome.Type biome) {
      return false;
   }

   public static List<Biome.Type> getUnlockedBiomes (int userId) {
      return null;
   }

   public static void addAdminGameSettings (AdminGameSettings settings) {
   }

   public static void updateAdminGameSettings (AdminGameSettings settings) {
   }

   public static AdminGameSettings getAdminGameSettings () {
      return null;
   }

   public static ulong createSteamOrder () {
      return 0;
   }

   public static bool deleteSteamOrder (ulong orderId) {
      return false;
   }

   public static bool toggleSteamOrder (ulong orderId, bool closed) {
      return false;
   }

   public static bool updateSteamOrder (ulong orderId, int userId, string status, string content) {
      return false;
   }

   public static SteamOrder getSteamOrder (ulong orderId) {
      return null;
   }

   public static ulong getLastSteamOrderId () {
      return 0;
   }

   public static int createStoreItem () {
      return 0;
   }

   public static bool deleteStoreItem (ulong itemId) {
      return false;
   }

   public static StoreItem getStoreItem (ulong itemId) {
      return null;
   }

   public static List<StoreItem> getAllStoreItems () {
      return new List<StoreItem>();
   }

   /*

   public static void refillSupplies (int userId) {

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
