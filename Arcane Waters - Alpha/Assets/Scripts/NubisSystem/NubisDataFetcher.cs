﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Networking;
using UnityEngine.Events;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Threading.Tasks;

public enum XmlSlotIndex
{
   None = 0,
   Default = 3,
}

namespace NubisDataHandling {

   public class InventoryBundle
   {
      #region Public Variables
      // Reference to the user info.
      public UserInfo user;

      // The guild info.
      public GuildInfo guildInfo;

      // The equipped weapon.
      public Item equippedWeapon;

      // The equipped armor.
      public Item equippedArmor;

      // The equipped hat.
      public Item equippedHat;

      // The item count.
      public int totalItemCount;

      // String containing the encoded inventory data.
      public string inventoryData;

      #endregion
   }

   public class XmlVersionEvent : UnityEvent<int> { 
   }

   public class NubisDataFetcher : GenericGameManager {

      #region Public Variables

      // Self
      public static NubisDataFetcher self;

      // The web directory
      public string webDirectory = "";

      // The xml version event 
      public XmlVersionEvent xmlVersionEvent = new XmlVersionEvent();

      // Space syntax for data sending
      public static string SPACER = "_space_";

      // The valid length of an xml data
      public static int VALID_XML_LENGTH = 5;

      #endregion

      protected override void Awake () {
         base.Awake();
         self = this;
         webDirectory = "http://" + Global.getAddress(MyNetworkManager.ServerType.AmazonVPC) + ":7900/";
      }

      public static int getSlotIndex () {
         return (int) XmlSlotIndex.Default;
      }

      #region Xml Version Features

      public void fetchXmlVersion () {
         processXmlVersion();
      }

      private async void processXmlVersion () {
         string returnCode = await NubisClient.call(nameof(DB_Main.fetchXmlVersion));

         try {
            int xmlVersion = int.Parse(returnCode);
            if (xmlVersion < 1) {
               D.error("Something went wrong with xml version fetching! Report: " + nameof(DB_Main.fetchXmlVersion));
            }
            xmlVersionEvent.Invoke(xmlVersion);
         } catch {
            xmlVersionEvent.Invoke(0);
            D.error("Something went wrong with xml version fetching! Report: " + nameof(DB_Main.fetchXmlVersion));
            D.error("Return code is: " + returnCode);
         }
      }

      #endregion

      #region Auction Features

      public async void getAuctionList (int pageNumber, int rowsPerPage, Item.Category[] categoryFilters,
         AuctionPanel.ListFilter auctionFilter) {
         AuctionPanel panel = (AuctionPanel) PanelManager.self.get(Panel.Type.Auction);
         panel.setLoadBlocker(true);

         // Prepare the category filter
         List<int> categoryFilterInt = Array.ConvertAll(categoryFilters.ToArray(), x => (int) x).ToList();
         string categoryFilterJSON = JsonConvert.SerializeObject(categoryFilterInt);

         // Call the nubis functions in parallel
         Task<string> listTask = NubisClient.call(nameof(DB_Main.getAuctionList), pageNumber.ToString(),
            rowsPerPage.ToString(), categoryFilterJSON, Global.player.userId.ToString(), auctionFilter.ToString());
         Task<string> totalCountTask = NubisClient.call(nameof(DB_Main.getAuctionListCount),
            Global.player.userId.ToString(), categoryFilterJSON, auctionFilter.ToString());
         Task<string> userInfoTask = NubisClient.call(nameof(DB_Main.getUserInfoJSON), Global.player.userId.ToString());

         await Task.WhenAll(listTask, totalCountTask, userInfoTask);

         // Parse the received data
         List<AuctionItemData> auctionList = new List<AuctionItemData>();
         int totalAuctionCount = 0;
         UserInfo userInfo = new UserInfo();
         try {
            auctionList = Util.xmlLoad<List<AuctionItemData>>(listTask.Result);
            totalAuctionCount = int.Parse(totalCountTask.Result);
            userInfo = JsonConvert.DeserializeObject<UserInfo>(userInfoTask.Result);
         } catch (Exception e) {
            D.error("Something went wrong with nubis fetching: " + nameof(DB_Main.getAuctionList) + ".\n" + e.ToString());
         }

         // Update the panel with the results
         PanelManager.self.linkIfNotShowing(Panel.Type.Auction);
         panel.receiveAuctionsFromServer(auctionList, categoryFilters, auctionFilter, pageNumber, totalAuctionCount, userInfo.gold);
      }

      #endregion

      #region Crafting Features

      public void checkCraftingInfo (int bluePrintId) {
         processCraftingInfo(bluePrintId);
      }

      private async void processCraftingInfo (int bluePrintId) {
         // Get the panel
         CraftingPanel craftingPanel = (CraftingPanel) PanelManager.self.get(Panel.Type.Craft);

         // Show the crafting panel, except if the reward panel is showing
         if (!craftingPanel.isShowing()) {
            PanelManager.self.linkPanel(craftingPanel.type);
         }

         int userId = Global.player == null ? 0 : Global.player.userId;
         EquippedItemData equippedItemData = new EquippedItemData();
         List<Item> craftingIngredients = new List<Item>();
         List<CraftableItemData> craftableItems = new List<CraftableItemData>();
         
         string rawBlueprintData = await NubisClient.call(nameof(DB_Main.fetchSingleBlueprint), bluePrintId, userId);
         string craftingIngredientData = await NubisClient.call(nameof(DB_Main.fetchCraftingIngredients), userId);
         string equippedItemContent = await NubisClient.call(nameof(DB_Main.fetchEquippedItems), userId);

         craftingIngredients = CraftingIngredients.processCraftingIngredients(craftingIngredientData);
         craftableItems = CraftableItem.processCraftableGroups(rawBlueprintData, craftingIngredients, Item.Category.None);
         equippedItemData = EquippedItems.processEquippedItemData(equippedItemContent);

         List<Item> equippedItems = new List<Item>();
         equippedItems.Add(equippedItemData.weaponItem);
         equippedItems.Add(equippedItemData.armorItem);
         equippedItems.Add(equippedItemData.hatItem);
         craftingPanel.updatePanelWithSingleBlueprintWebRequest(craftableItems[0].craftableItem, equippedItems, craftingIngredients, craftableItems[0].craftableRequirements.combinationRequirements);
      }

      public void fetchCraftableData (int pageIndex, int itemsPerPage) {
         processCraftableData(pageIndex, itemsPerPage);
      }

      private async void processCraftableData (int pageIndex, int itemsPerPage) {
         // Get the panel
         CraftingPanel craftingPanel = (CraftingPanel) PanelManager.self.get(Panel.Type.Craft);
         craftingPanel.clearContent();

         // Show the crafting panel, except if the reward panel is showing
         if (!craftingPanel.isShowing()) {
            PanelManager.self.linkPanel(craftingPanel.type);
         }

         int userId = Global.player == null ? 0 : Global.player.userId;
         List<Item> craftingIngredients = new List<Item>();
         List<Item> craftableItems = new List<Item>();
         List<Blueprint.Status> blueprintStatus = new List<Blueprint.Status>();

         string craftingIngredientXml = await NubisClient.call(nameof(DB_Main.fetchCraftingIngredients), userId);
         string armorFetch = await NubisClient.call(nameof(DB_Main.fetchCraftableArmors), userId);
         string weaponFetch = await NubisClient.call(nameof(DB_Main.fetchCraftableWeapons), userId);
         string hatFetch = await NubisClient.call(nameof(DB_Main.fetchCraftableHats), userId);

         craftingIngredients = CraftingIngredients.processCraftingIngredients(craftingIngredientXml);
         List<CraftableItemData> weaponCraftables = CraftableItem.processCraftableGroups(weaponFetch, craftingIngredients, Item.Category.Weapon);
         List<CraftableItemData> armorCraftables = CraftableItem.processCraftableGroups(armorFetch, craftingIngredients, Item.Category.Armor);
         List<CraftableItemData> hatCraftables = CraftableItem.processCraftableGroups(hatFetch, craftingIngredients, Item.Category.Hats);

         foreach (CraftableItemData weaponData in weaponCraftables) {
            craftableItems.Add(weaponData.craftableItem);
            blueprintStatus.Add(weaponData.craftingStatus);
         }
         foreach (CraftableItemData armorData in armorCraftables) {
            craftableItems.Add(armorData.craftableItem);
            blueprintStatus.Add(armorData.craftingStatus);
         }
         foreach (CraftableItemData hatData in hatCraftables) {
            craftableItems.Add(hatData.craftableItem);
            blueprintStatus.Add(hatData.craftingStatus);
         }
         craftingPanel.updatePanelWithBlueprintList(craftableItems.ToArray(), blueprintStatus.ToArray(), pageIndex, itemsPerPage);
      }

      #endregion

      #region Equipment Features

      public async void getUserInventory (List<Item.Category> categoryFilter, int pageIndex = 1, int itemsPerPage = 42, int itemDurabilityFilter = 0, Panel.Type panelType = Panel.Type.Inventory) {
         if (Global.player == null) {
            return;
         }

         int userId = Global.player.userId;
         int[] categoryFilterInt = Array.ConvertAll(categoryFilter.ToArray(), x => (int) x);
         string categoryFilterJSON = JsonConvert.SerializeObject(categoryFilterInt);

         InventoryBundle inventoryBundle = null;

         // Request the inventory to Nubis
         string inventoryBundleString = await NubisClient.callDirect("getUserInventoryPage",
            userId.ToString(), categoryFilterJSON, pageIndex.ToString(), itemsPerPage.ToString(), itemDurabilityFilter.ToString());

         try {
            inventoryBundle = JsonConvert.DeserializeObject<InventoryBundle>(inventoryBundleString);
         } catch {
            D.debug("Inventory bundle failed to fetch! {" + inventoryBundleString + "} :: {" + categoryFilterJSON + "} :: {" + pageIndex + "} :: {" + itemsPerPage + "}");
         }

         if (inventoryBundle == null) {
            D.debug("Inventory bundle is not existing!"+ " : " + inventoryBundleString);
            return;
         }

         // Update sql id of each equipment cache loaded
         if (inventoryBundle.equippedWeapon.itemTypeId != 0) {
            WeaponStatData xmlWeaponData = EquipmentXMLManager.self.getWeaponData(inventoryBundle.equippedWeapon.itemTypeId);
            if (xmlWeaponData != null) {
               inventoryBundle.equippedWeapon.data = WeaponStatData.serializeWeaponStatData(xmlWeaponData);
            } else {
               D.debug("NUBIS DATA FETCHER :: Weapon data missing! ID:" + inventoryBundle.equippedWeapon.itemTypeId + " WeaponDataContent: " + EquipmentXMLManager.self.weaponStatList.Count);
               inventoryBundle.equippedWeapon.itemTypeId = 0;
            }
         }
         if (inventoryBundle.equippedArmor.itemTypeId != 0) {
            ArmorStatData xmlArmorData = EquipmentXMLManager.self.getArmorDataBySqlId(inventoryBundle.equippedArmor.itemTypeId);
            if (xmlArmorData != null) {
               inventoryBundle.equippedArmor.data = ArmorStatData.serializeArmorStatData(xmlArmorData);
            } else {
               D.debug("NUBIS DATA FETCHER :: Armor data missing! ID:" + inventoryBundle.equippedArmor.itemTypeId + " ArmorDataContent: " + EquipmentXMLManager.self.armorStatList.Count);
               inventoryBundle.equippedArmor.itemTypeId = 0;
            }
         }
         if (inventoryBundle.equippedHat.itemTypeId != 0) {
            HatStatData xmlHatData = EquipmentXMLManager.self.getHatData(inventoryBundle.equippedHat.itemTypeId);
            if (xmlHatData != null) {
               inventoryBundle.equippedHat.data = HatStatData.serializeHatStatData(xmlHatData);
            } else {
               D.debug("NUBIS DATA FETCHER :: Hat data missing! ID:" + inventoryBundle.equippedHat.itemTypeId + " HatDataContent: " + EquipmentXMLManager.self.hatStatList.Count);
               inventoryBundle.equippedHat.itemTypeId = 0;
            }
         }

         List<Item> itemList = UserInventory.processUserInventory(inventoryBundle.inventoryData);
        
         // Filter inventory items here
         itemList.RemoveAll(_ => _.category == Item.Category.Usable);

         if (panelType == Panel.Type.Inventory) {
            // Get the inventory panel
            InventoryPanel inventoryPanel = (InventoryPanel) PanelManager.self.get(Panel.Type.Inventory);

            // Make sure the inventory panel is showing
            if (!inventoryPanel.isShowing()) {
               PanelManager.self.linkPanel(Panel.Type.Inventory);

               // When inventory panel is opened, we should always start at the first page
               pageIndex = 0;
            }
            inventoryPanel.clearPanel();

            UserObjects userObjects = new UserObjects { userInfo = inventoryBundle.user, weapon = inventoryBundle.equippedWeapon, armor = inventoryBundle.equippedArmor, hat = inventoryBundle.equippedHat };
            inventoryPanel.receiveItemForDisplay(itemList, userObjects, inventoryBundle.guildInfo, categoryFilter, pageIndex, inventoryBundle.totalItemCount, true);
         } else if (panelType == Panel.Type.Craft) {
            // Get the crafting panel
            CraftingPanel craftingPanel = (CraftingPanel) PanelManager.self.get(Panel.Type.Craft);
            craftingPanel.receiveRefineableItems(itemList, pageIndex);
         }
      }

      public async void getInventoryForItemSelection (List<Item.Category> categoryFilter, List<int> itemIdsToExclude,
         int pageIndex, int itemsPerPage) {
         if (Global.player == null) {
            return;
         }

         PanelManager.self.itemSelectionScreen.setLoadBlocker(true);

         int userId = Global.player.userId;
         int[] categoryFilterInt = Array.ConvertAll(categoryFilter.ToArray(), x => (int) x);
         string categoryFilterJSON = JsonConvert.SerializeObject(categoryFilterInt);
         string itemIdsToExcludeJSON = JsonConvert.SerializeObject(itemIdsToExclude);

         // Call the list and list count in parallel
         Task<string> listTask = NubisClient.call(nameof(DB_Main.userInventory), userId.ToString(), categoryFilterJSON,
            itemIdsToExcludeJSON, "1", pageIndex.ToString(), itemsPerPage.ToString(), "0");
         Task<string> totalCountTask = NubisClient.call(nameof(DB_Main.userInventoryCount), userId.ToString(), categoryFilterJSON,
            itemIdsToExcludeJSON, "1", "0");

         await Task.WhenAll(listTask, totalCountTask);

         // Parse the received data
         if (!int.TryParse(totalCountTask.Result, out int totalCount)) {
            totalCount = 0;
         }

         List<Item> itemList = UserInventory.processUserInventory(listTask.Result);

         // Filter inventory items here
         itemList.RemoveAll(_ => _.category == Item.Category.Usable);

         PanelManager.self.itemSelectionScreen.receiveItemsFromServer(itemList, pageIndex, totalCount);
      }

      #endregion

      #region Ability Features

      public void fetchUserAbilities () {
         processUserAbilities();
      }

      private async void processUserAbilities () {
         int userId = Global.player == null ? 0 : Global.player.userId;
         AbilityPanel panel = (AbilityPanel) PanelManager.self.get(Panel.Type.Ability_Panel);
         panel.clearContent();

         // Make sure the panel is showing
         if (!panel.isShowing()) {
            PanelManager.self.linkPanel(Panel.Type.Ability_Panel);
         }

         // Fetch content from Nubis
         List<AbilitySQLData> abilityList = await NubisClient.callJSONList<List<AbilitySQLData>>(nameof(DB_Main.userAbilities), userId, (int) AbilityEquipStatus.ALL);

         if (abilityList == null || abilityList.Count < 1) {
            D.debug("Failed to fetch Nubis abilities");
            return;
         }

         // Update the Skill Panel with the abilities we received from the server
         panel.receiveDataFromServer(abilityList.ToArray());
      }

      #endregion

      #region Server History

      public async void getServerHistory (int maxRows, DateTime startDate) {
         // Serialize input parameters
         string startDateString = startDate.ToBinary().ToString();

         // Call the functions
         Task<string> isServerOnlineTask = NubisClient.call(nameof(DB_Main.isMasterServerOnline));
         Task<string> serverHistoryListTask = NubisClient.call(nameof(DB_Main.getServerHistoryList), Global.MASTER_SERVER_PORT.ToString(), startDateString, maxRows.ToString());

         await Task.WhenAll(isServerOnlineTask, serverHistoryListTask);

         //Parse the received data
         if (!bool.TryParse(isServerOnlineTask.Result, out bool isServerOnline)) {
            isServerOnline = false;
         }
         List<ServerHistoryInfo> serverHistoryList = null;
         try {
            serverHistoryList = JsonConvert.DeserializeObject<List<ServerHistoryInfo>>(serverHistoryListTask.Result);
         } catch (Exception e) {
            D.debug("Failed to fetch the server history.\n" + e.ToString());
         }

         if (serverHistoryList == null) {
            serverHistoryList = new List<ServerHistoryInfo>();
         }

         ServerStatusPanel.self.updatePanelWithServerHistory(isServerOnline, serverHistoryList);
      }

      public async void getOnlineServersListForClientLogin (bool isSteam) {
         string serverPortListString = await NubisClient.call(nameof(DB_Main.getOnlineServerList));
         
         List<int> serverPortList = null;
         try {
            serverPortList = JsonConvert.DeserializeObject<List<int>>(serverPortListString);
         } catch (Exception e) {
            D.debug("Failed to fetch the list of online servers.\n" + e.ToString());
         }

         if (serverPortList == null) {
            serverPortList = new List<int>();
         }

         if (serverPortList.Count > 0) {
            TitleScreen.self.startUpNetworkClient(isSteam, serverPortList);
         } else {
            // If the request failed, try logging in to the master server anyway
            TitleScreen.self.startUpNetworkClient(isSteam, new List<int>() { Global.MASTER_SERVER_PORT });
         }
      }

      #endregion
   }
}