using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text;

public class DB_MainStub : MonoBehaviour {
   #region Public Variables

   #endregion

   public static void readTest () {
      
   }

   public static void createNPCRelation (NPCRelationInfo npcInfo) {

   }

   public static List<NPCRelationInfo> getNPCRelationInfo (int user_id, int npc_id) {
      return new List<NPCRelationInfo>();
   }

   public static void updateNPCRelation (int userId, int npcID, int relationLevel) {

   }

   public static void updateNPCProgress (int userId, int npcID, int questProgress, int questIndex, string questType) {

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

   public static void setNewPosition (int userId, Vector2 localPosition, Direction facingDirection, int areaId) {

   }

   public static void storeShipHealth (int shipId, int shipHealth) {

   }

   public static List<SiloInfo> getSiloInfo(int userId) {
      return null;
   }

   public static void addToSilo(int userId, Crop.Type cropType, int amount=1)
   {
      
   }

   public static List<TutorialInfo> getTutorialInfo (int userId) {
      return null;
   }

   public static Step completeTutorialStep (int userId, Step step) {
      return default(Step);
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

   public static int createUser (int accountId, UserInfo userInfo, Area area) {
      return 0;
   }

   public static Item createNewItem (int userId, Item baseItem) {
      return null;
   }

   public static int insertNewArmor (int userId, Armor.Type armorType, ColorType color1, ColorType color2) {
      return 0;
   }

   public static int insertNewWeapon (int userId, Weapon.Type weaponType, ColorType color1, ColorType color2) {
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

   public static int getItemCount (int userId) {
      return 0;
   }

   public static List<Item> getItems (int userId, int page, int itemsPerPage) {
      return null;
   }

   public static Item getItem (int userId, int itemId) {
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

   public static void addJobXP (int userId, Jobs.Type jobType, int XP) {

   }

   public static void insertIntoJobs (int userId) {

   }

   public static List<ShipInfo> getShips (int userId, int page, int shipsPerPage) {
      return null;
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
